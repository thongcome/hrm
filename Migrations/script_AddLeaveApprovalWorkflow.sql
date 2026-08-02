BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260802084420_AddLeaveApprovalWorkflow'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'workflowid', N'wname', N'wstatus', N'workflowcode', N'isshow', N'isactive', N'description', N'url') AND [object_id] = OBJECT_ID(N'[wf_workflow]'))
        SET IDENTITY_INSERT [wf_workflow] ON;
    EXEC(N'INSERT INTO [wf_workflow] ([workflowid], [wname], [wstatus], [workflowcode], [isshow], [isactive], [description], [url])
    VALUES (CAST(9 AS bigint), N''ขออนุมัติลางาน'', N''ACTIVE'', N''LEAVE_APPROVAL'', CAST(1 AS bit), CAST(1 AS bit), N''อนุมัติคำขอลางาน (Lve_LeaveRequest) — ระดับ 1 หัวหน้างานตามผังองค์กร, ระดับ 2 ฝ่ายบุคคล'', N''/leave-requests/detail/{refid}'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'workflowid', N'wname', N'wstatus', N'workflowcode', N'isshow', N'isactive', N'description', N'url') AND [object_id] = OBJECT_ID(N'[wf_workflow]'))
        SET IDENTITY_INSERT [wf_workflow] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260802084420_AddLeaveApprovalWorkflow'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'subworkflowid', N'workflowid', N'wlevel', N'subject', N'isAdhocUser', N'iscustomApprover', N'isupperrole', N'isupperuser', N'iscustomRole', N'iscustomUser', N'iscondition', N'isorcondition', N'isandcondition', N'forwardstatus', N'standstatus', N'backwardstatus', N'istop', N'isReturnSender', N'isshow', N'isLOA', N'isAutoApproveAllow', N'isNeedBudgetApproval', N'isPool', N'isApproverSameOrg', N'isApproverSameCostCenter', N'isManualButton') AND [object_id] = OBJECT_ID(N'[wf_sub_workflow_master]'))
        SET IDENTITY_INSERT [wf_sub_workflow_master] ON;
    EXEC(N'INSERT INTO [wf_sub_workflow_master] ([subworkflowid], [workflowid], [wlevel], [subject], [isAdhocUser], [iscustomApprover], [isupperrole], [isupperuser], [iscustomRole], [iscustomUser], [iscondition], [isorcondition], [isandcondition], [forwardstatus], [standstatus], [backwardstatus], [istop], [isReturnSender], [isshow], [isLOA], [isAutoApproveAllow], [isNeedBudgetApproval], [isPool], [isApproverSameOrg], [isApproverSameCostCenter], [isManualButton])
    VALUES (CAST(15 AS bigint), CAST(9 AS bigint), 1, N''หัวหน้างานอนุมัติ (ตามผังองค์กร)'', CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), N''APPROVED'', N''PENDING'', N''RETURNED'', CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit)),
    (CAST(16 AS bigint), CAST(9 AS bigint), 2, N''ฝ่ายบุคคล (HR) อนุมัติ'', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), N''COMPLETED'', N''PENDING'', N''RETURNED'', CAST(1 AS bit), CAST(0 AS bit), CAST(1 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit))');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'subworkflowid', N'workflowid', N'wlevel', N'subject', N'isAdhocUser', N'iscustomApprover', N'isupperrole', N'isupperuser', N'iscustomRole', N'iscustomUser', N'iscondition', N'isorcondition', N'isandcondition', N'forwardstatus', N'standstatus', N'backwardstatus', N'istop', N'isReturnSender', N'isshow', N'isLOA', N'isAutoApproveAllow', N'isNeedBudgetApproval', N'isPool', N'isApproverSameOrg', N'isApproverSameCostCenter', N'isManualButton') AND [object_id] = OBJECT_ID(N'[wf_sub_workflow_master]'))
        SET IDENTITY_INSERT [wf_sub_workflow_master] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260802084420_AddLeaveApprovalWorkflow'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'id', N'subworkflowid', N'workflowid', N'wlevel', N'userid', N'empid', N'isactive') AND [object_id] = OBJECT_ID(N'[wf_custom_user]'))
        SET IDENTITY_INSERT [wf_custom_user] ON;
    EXEC(N'INSERT INTO [wf_custom_user] ([id], [subworkflowid], [workflowid], [wlevel], [userid], [empid], [isactive])
    VALUES (CAST(13 AS bigint), CAST(16 AS bigint), CAST(9 AS bigint), 2, CAST(16 AS bigint), N''002'', CAST(1 AS bit))');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'id', N'subworkflowid', N'workflowid', N'wlevel', N'userid', N'empid', N'isactive') AND [object_id] = OBJECT_ID(N'[wf_custom_user]'))
        SET IDENTITY_INSERT [wf_custom_user] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260802084420_AddLeaveApprovalWorkflow'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260802084420_AddLeaveApprovalWorkflow', N'10.0.10');
END;

COMMIT;
GO

