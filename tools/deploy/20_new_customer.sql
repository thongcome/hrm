/* ============================================================================
   20_new_customer.sql — turn a restored copy of the clean template into ONE customer's database.

   Variables — set them via a small :setvar file, NOT sqlcmd -v on the command line: a Thai
   NAME passed as a -v argument gets mangled by Windows console/argv encoding before sqlcmd ever
   sees it (verified while building this script — it either garbles the text or sqlcmd rejects
   the argument outright over the embedded space). A :setvar file is plain UTF-8 text read by
   sqlcmd the same way as this script, so it round-trips Thai correctly:

     :setvar CODE "NEWCO"
     :setvar NAME "บริษัท นิวโค จำกัด"
     :setvar NAME_EN "NewCo Co., Ltd."
     :setvar TAXID "0105560000000"
     :r 20_new_customer.sql

   then:  sqlcmd -S <server> -d <customer db> -i install_vars.sql -f 65001
   (-f 65001 = UTF-8 codepage — needed for 30_check_ready.sql's Thai output too; without it
   sqlcmd decodes the UTF-8 file bytes under the OS codepage and Thai text becomes mojibake, or
   in some cases stray high-bit bytes break parsing entirely.)

     CODE        customer company code   e.g. NEWCO   (letters/digits/_/-, becomes every CompanyId)
     NAME        company name (Thai)     e.g. บริษัท นิวโค จำกัด
     NAME_EN     company name (English)  e.g. NewCo Co., Ltd.
     TAXID       tax id (13 digits)      e.g. 0105560000000

   HRM has two separate company-scoping schemes (CLAUDE.md "Company scoping is a string, not a
   numeric FK — deliberately"): ~81 business tables carry a STRING CompanyId/companyid matching
   com_company.code, while sc_user/sc_role/com_organization.companyid carry the numeric
   com_company.id. Only the STRING side encodes "ADVD" and needs renaming — the numeric id was
   never ADVD-shaped and doesn't change here; it already points at the one surviving company row
   from 10_make_clean_template.sql regardless of what its code is renamed to.

   Refuses to run on our own databases and on anything that is not an untouched template (exactly
   one company, code ADVD, no employees — the same shape 10_make_clean_template.sql produces).
   After this: run `dotnet HRM.dll --init-admin` to get advadmin's one-time password.
   ========================================================================== */
SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET XACT_ABORT ON;

DECLARE @code nvarchar(50) = UPPER(LTRIM(RTRIM(N'$(CODE)')));
DECLARE @name nvarchar(500) = LTRIM(RTRIM(N'$(NAME)'));
DECLARE @nameEn nvarchar(500) = NULLIF(LTRIM(RTRIM(N'$(NAME_EN)')), N'');
DECLARE @taxId nvarchar(50) = NULLIF(LTRIM(RTRIM(N'$(TAXID)')), N'');
DECLARE @from nvarchar(50) = N'ADVD';

IF DB_NAME() IN (N'hrm', N'hrm_e2e', N'hrm_template')
    THROW 50010, 'Refusing: this is one of our own databases — restore the template under the customer''s database name first.', 1;
IF (SELECT COUNT(*) FROM com_company) <> 1 OR NOT EXISTS (SELECT 1 FROM com_company WHERE code = @from)
    THROW 50011, 'Refusing: not an untouched template (expected exactly one company with code ADVD).', 1;
IF EXISTS (SELECT 1 FROM HREMPLOYEE)
    THROW 50012, 'Refusing: the database already has employees.', 1;
IF @code = N'' OR @code LIKE N'%[^A-Z0-9_-]%'
    THROW 50013, 'CODE must be letters/digits/_/- (A-Z 0-9 _ -).', 1;
IF @name = N'' THROW 50014, 'NAME is required.', 1;
IF @taxId IS NOT NULL AND (LEN(@taxId) <> 13 OR @taxId LIKE N'%[^0-9]%')
    THROW 50015, 'TAXID must be 13 digits.', 1;

-- every STRING company column in the database; the code must fit the narrowest one
CREATE TABLE #cols (tbl sysname, col sysname, maxlen int);
INSERT #cols
SELECT t.name, c.name, CASE WHEN ty.name LIKE 'n%' THEN c.max_length / 2 ELSE c.max_length END
FROM sys.tables t JOIN sys.columns c ON c.object_id = t.object_id JOIN sys.types ty ON ty.user_type_id = c.user_type_id
WHERE c.name IN ('companyid', 'CompanyId', 'comp_code', 'Companycode', 'CompanyCode', 'comp_code_all')
  AND ty.name IN ('nvarchar', 'varchar', 'nchar', 'char') AND c.max_length > 0
  AND t.name NOT IN ('sc_user', 'com_organization');   -- these carry the numeric companyid FK, not the string code — handled below
DECLARE @narrowest int = (SELECT MIN(maxlen) FROM #cols);
IF LEN(@code) > @narrowest
BEGIN
    DECLARE @m nvarchar(400) = CONCAT(N'CODE is too long: the narrowest company column holds ', @narrowest, N' characters.');
    THROW 50016, @m, 1;
END

BEGIN TRAN;

DECLARE @t sysname, @c sysname, @sql nvarchar(max), @n int, @total int = 0;
DECLARE k CURSOR LOCAL FAST_FORWARD FOR SELECT tbl, col FROM #cols;
OPEN k; FETCH NEXT FROM k INTO @t, @c;
WHILE @@FETCH_STATUS = 0
BEGIN
    SET @sql = N'UPDATE ' + QUOTENAME(@t) + N' SET ' + QUOTENAME(@c) + N' = @code WHERE ' + QUOTENAME(@c) + N' = @from; SET @n = @@ROWCOUNT;';
    EXEC sp_executesql @sql, N'@code nvarchar(50), @from nvarchar(50), @n int OUTPUT', @code, @from, @n OUTPUT;
    SET @total += @n;
    FETCH NEXT FROM k INTO @t, @c;
END
CLOSE k; DEALLOCATE k;

UPDATE com_company SET code = @code, name = @name, name_en = @nameEn, tax_id = @taxId, abbr = @code,
       isActive = 1, moddate = GETDATE(), modby = N'install';

-- com_organization: numeric companyid FK is untouched (still points at the same com_company row);
-- only the string identity columns move — comp_code is what the ~81 string CompanyId columns above
-- were just renamed to match, code/abbr follow it so the root node's own code isn't left as the old
-- "ADVD"/whatever legacy value while everything downstream now says the new code.
UPDATE com_organization SET code = @code, comp_code = @code, name = @name, name_en = @nameEn, abbr = @code,
       moddate = GETDATE(), modby = N'install'
WHERE isCompany = 1 AND comp_code = @from;

UPDATE Pay_PayslipSettings SET CompanyName = @name, CompanyNameEn = @nameEn, CompanyTaxId = @taxId;

COMMIT;

SELECT DB_NAME() AS [database], @code AS company_code, @total AS rows_renamed,
       (SELECT COUNT(*) FROM #cols) AS company_columns_checked;
PRINT N'NEXT: dotnet HRM.dll --init-admin';
