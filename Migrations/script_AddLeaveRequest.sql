BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260802025254_AddLeaveRequest'
)
BEGIN
    CREATE TABLE [Lve_LeaveRequest] (
        [Id] bigint NOT NULL IDENTITY,
        [HremployeeId] bigint NOT NULL,
        [EmpNo] nvarchar(6) NOT NULL,
        [CompanyId] nvarchar(6) NULL,
        [LeaveType] int NOT NULL,
        [StartDate] date NOT NULL,
        [EndDate] date NOT NULL,
        [TotalDays] decimal(5,1) NOT NULL,
        [Reason] nvarchar(500) NULL,
        [RequestedDate] datetime2 NOT NULL,
        [JobMasterId] bigint NULL,
        CONSTRAINT [PK_Lve_LeaveRequest] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Lve_LeaveRequest_HREMPLOYEE_HremployeeId] FOREIGN KEY ([HremployeeId]) REFERENCES [HREMPLOYEE] ([ID]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260802025254_AddLeaveRequest'
)
BEGIN
    CREATE INDEX [IX_Lve_LeaveRequest_HremployeeId] ON [Lve_LeaveRequest] ([HremployeeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260802025254_AddLeaveRequest'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260802025254_AddLeaveRequest', N'10.0.10');
END;

COMMIT;
GO

