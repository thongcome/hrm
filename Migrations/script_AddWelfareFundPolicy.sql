BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260802132947_AddWelfareFundPolicy'
)
BEGIN
    CREATE TABLE [Pay_WelfareFundPolicy] (
        [Id] bigint NOT NULL IDENTITY,
        [CompanyId] nvarchar(6) NOT NULL,
        [EffectiveFrom] date NOT NULL,
        [EffectiveTo] date NULL,
        [EmployeeContributionRate] decimal(5,2) NOT NULL,
        [CompanyContributionRate] decimal(5,2) NOT NULL,
        [WageCapPerMonth] decimal(15,2) NULL,
        [IsEnabled] bit NOT NULL,
        [Note] nvarchar(1000) NULL,
        CONSTRAINT [PK_Pay_WelfareFundPolicy] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260802132947_AddWelfareFundPolicy'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'CompanyId', N'EffectiveFrom', N'EffectiveTo', N'EmployeeContributionRate', N'CompanyContributionRate', N'WageCapPerMonth', N'IsEnabled', N'Note') AND [object_id] = OBJECT_ID(N'[Pay_WelfareFundPolicy]'))
        SET IDENTITY_INSERT [Pay_WelfareFundPolicy] ON;
    EXEC(N'INSERT INTO [Pay_WelfareFundPolicy] ([Id], [CompanyId], [EffectiveFrom], [EffectiveTo], [EmployeeContributionRate], [CompanyContributionRate], [WageCapPerMonth], [IsEnabled], [Note])
    VALUES (CAST(1 AS bigint), N''001'', ''2026-10-01'', ''2031-09-30'', 0.25, 0.25, NULL, CAST(0 AS bit), N''ค่าเริ่มต้นจากการสืบค้นข้อมูลสาธารณะ 2569-08 (ไม่ได้อ้างอิงจากกฎกระทรวง/ประกาศราชกิจจานุเบกษาโดยตรง) — ยังไม่ยืนยันเพดานค่าจ้างและช่องทางนำส่งกับกรมสวัสดิการฯ ตรวจสอบให้แน่ใจก่อนเปิดใช้งาน''),
    (CAST(2 AS bigint), N''001'', ''2031-10-01'', NULL, 0.5, 0.5, NULL, CAST(0 AS bit), N''อัตราขั้นที่ 2 ตามตารางที่เผยแพร่ (1 ต.ค. 2574 เป็นต้นไป) — เงื่อนไขเดียวกับแถวแรก ยังไม่ยืนยันกับแหล่งกฎหมายหลัก'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'CompanyId', N'EffectiveFrom', N'EffectiveTo', N'EmployeeContributionRate', N'CompanyContributionRate', N'WageCapPerMonth', N'IsEnabled', N'Note') AND [object_id] = OBJECT_ID(N'[Pay_WelfareFundPolicy]'))
        SET IDENTITY_INSERT [Pay_WelfareFundPolicy] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260802132947_AddWelfareFundPolicy'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260802132947_AddWelfareFundPolicy', N'10.0.10');
END;

COMMIT;
GO

