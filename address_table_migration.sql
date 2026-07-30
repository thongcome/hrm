BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730165707_AddAddressTable'
)
BEGIN
    CREATE TABLE [address] (
        [id] bigint NOT NULL IDENTITY,
        [hremployeeid] bigint NOT NULL,
        [address_type_id] bigint NULL,
        [no] nvarchar(100) NULL,
        [road] nvarchar(250) NULL,
        [soi] nvarchar(250) NULL,
        [moo] nvarchar(250) NULL,
        [buildingname] nvarchar(250) NULL,
        [village] nvarchar(100) NULL,
        [subdistrict] nvarchar(100) NULL,
        [districtid] nvarchar(100) NULL,
        [provinceid] bigint NULL,
        [province] nvarchar(20) NULL,
        [postcode] nvarchar(10) NULL,
        [tel] nvarchar(50) NULL,
        [mobileno] nvarchar(50) NULL,
        [officeno] nvarchar(18) NULL,
        [fax] nvarchar(18) NULL,
        [email] nvarchar(250) NULL,
        [remark] nvarchar(1000) NULL,
        [createdate] datetime NULL,
        [createby] bigint NULL,
        [moddate] datetime NULL,
        [modby] bigint NULL,
        [isactive] bit NOT NULL,
        CONSTRAINT [PK_address] PRIMARY KEY ([id]),
        CONSTRAINT [FK_address_HREMPLOYEE] FOREIGN KEY ([hremployeeid]) REFERENCES [HREMPLOYEE] ([ID]) ON DELETE NO ACTION,
        CONSTRAINT [FK_address_mas_address_type] FOREIGN KEY ([address_type_id]) REFERENCES [mas_address_type] ([id]),
        CONSTRAINT [FK_address_mas_province] FOREIGN KEY ([provinceid]) REFERENCES [mas_province] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730165707_AddAddressTable'
)
BEGIN
    CREATE INDEX [IX_address_address_type_id] ON [address] ([address_type_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730165707_AddAddressTable'
)
BEGIN
    CREATE INDEX [IX_address_hremployeeid] ON [address] ([hremployeeid]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730165707_AddAddressTable'
)
BEGIN
    CREATE INDEX [IX_address_provinceid] ON [address] ([provinceid]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730165707_AddAddressTable'
)
BEGIN
    CREATE INDEX [IX_address_hremployeeid_address_type_id] ON [address] ([hremployeeid], [address_type_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730165707_AddAddressTable'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'id', N'name', N'name_en', N'code', N'isActive') AND [object_id] = OBJECT_ID(N'[mas_address_type]'))
        SET IDENTITY_INSERT [mas_address_type] ON;
    EXEC(N'INSERT INTO [mas_address_type] ([id], [name], [name_en], [code], [isActive])
    VALUES (CAST(1 AS bigint), N''ที่อยู่ตามทะเบียนบ้าน'', N''Registered Address'', N''REG'', CAST(1 AS bit)),
    (CAST(2 AS bigint), N''ที่อยู่ปัจจุบัน/ติดต่อได้'', N''Current/Contact Address'', N''CUR'', CAST(1 AS bit))');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'id', N'name', N'name_en', N'code', N'isActive') AND [object_id] = OBJECT_ID(N'[mas_address_type]'))
        SET IDENTITY_INSERT [mas_address_type] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730165707_AddAddressTable'
)
BEGIN

                    INSERT INTO [address] (hremployeeid, address_type_id, [no], subdistrict, districtid, province, postcode, createdate, moddate, isactive)
                    SELECT [ID], 1, ADR_NO, ADR_TAMBOL, ADR_AMPHUR, ADR_PROVINCE, ADR_POSTCODE, GETDATE(), GETDATE(), 1
                    FROM HREMPLOYEE
                    WHERE ADR_NO IS NOT NULL;

                    INSERT INTO [address] (hremployeeid, address_type_id, [no], subdistrict, districtid, province, postcode, tel, email, createdate, moddate, isactive)
                    SELECT [ID], 2, ADN_NO, ADN_TAMBOL, ADN_AMPHUR, ADN_PROVINCE, ADN_POSTCODE, ADN_TEL, ADN_EMAIL, GETDATE(), GETDATE(), 1
                    FROM HREMPLOYEE
                    WHERE ADN_NO IS NOT NULL;
                
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730165707_AddAddressTable'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260730165707_AddAddressTable', N'10.0.10');
END;

COMMIT;
GO

