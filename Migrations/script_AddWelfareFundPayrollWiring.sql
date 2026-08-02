BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260802151954_AddWelfareFundPayrollWiring'
)
BEGIN
    ALTER TABLE [Pay_PayrollEmployee] ADD [WelfareFundCompanyAmount] decimal(15,2) NOT NULL DEFAULT 0.0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260802151954_AddWelfareFundPayrollWiring'
)
BEGIN
    ALTER TABLE [Pay_PayrollEmployee] ADD [WelfareFundEmployeeAmount] decimal(15,2) NOT NULL DEFAULT 0.0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260802151954_AddWelfareFundPayrollWiring'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Category', N'Code', N'DefaultSignFlag', N'GLAccountCode', N'IsActive', N'IsSystemReserved', N'NameEn', N'NameTh', N'SortOrder') AND [object_id] = OBJECT_ID(N'[Pay_PayItemType]'))
        SET IDENTITY_INSERT [Pay_PayItemType] ON;
    EXEC(N'INSERT INTO [Pay_PayItemType] ([Id], [Category], [Code], [DefaultSignFlag], [GLAccountCode], [IsActive], [IsSystemReserved], [NameEn], [NameTh], [SortOrder])
    VALUES (13, 1, N''WELFAREFUND'', -1, N''2260-WF-PAYABLE'', CAST(1 AS bit), CAST(1 AS bit), N''Employee Welfare Fund Deduction'', N''หักกองทุนสงเคราะห์ลูกจ้าง'', 13)');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Category', N'Code', N'DefaultSignFlag', N'GLAccountCode', N'IsActive', N'IsSystemReserved', N'NameEn', N'NameTh', N'SortOrder') AND [object_id] = OBJECT_ID(N'[Pay_PayItemType]'))
        SET IDENTITY_INSERT [Pay_PayItemType] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260802151954_AddWelfareFundPayrollWiring'
)
BEGIN
    EXEC(N'UPDATE [Pay_WelfareFundPolicy] SET [Note] = N''ยืนยันจากตัวบทกฎหมาย (พ.ร.บ.คุ้มครองแรงงาน ม.130-131): บังคับเฉพาะนายจ้าง 10 คนขึ้นไป เว้นแต่มี PF/สวัสดิการอื่นครอบคลุมอยู่แล้ว, อัตราตามกฎหมายต้องไม่เกิน 5%, ไม่มีเพดานค่าจ้างในตัวบทกฎหมาย — อัตรา 0.25%/0.25% และวันที่เริ่ม 1 ต.ค. 2569 ยืนยันจากเว็บกรมสวัสดิการฯ (ewf.labour.go.th) + หลายแหล่งข่าว — วันสิ้นสุดช่วงนี้ (30 ก.ย. 2574) ยังมีแหล่งข้อมูลบางส่วนระบุ 2573 ต่างกัน ควรตรวจสอบก่อนเปิดใช้งานจริง''
    WHERE [Id] = CAST(1 AS bigint);
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260802151954_AddWelfareFundPayrollWiring'
)
BEGIN
    EXEC(N'UPDATE [Pay_WelfareFundPolicy] SET [Note] = N''อัตราขั้นที่ 2 (0.5%/0.5%) ตามกฎกระทรวงที่เผยแพร่ — เงื่อนไขทางกฎหมายเดียวกับแถวแรก (ม.130-131) วันเริ่มของช่วงนี้ผูกกับวันสิ้นสุดช่วงแรกที่ยังมีแหล่งข้อมูลขัดแย้งกันเล็กน้อย (2573 หรือ 2574) ควรตรวจสอบก่อนเปิดใช้งานจริง''
    WHERE [Id] = CAST(2 AS bigint);
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260802151954_AddWelfareFundPayrollWiring'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260802151954_AddWelfareFundPayrollWiring', N'10.0.10');
END;

COMMIT;
GO

