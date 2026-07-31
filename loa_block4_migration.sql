BEGIN TRANSACTION;
ALTER TABLE [job_subworkflow_master] ADD [isLOA] bit NOT NULL DEFAULT CAST(0 AS bit);

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'workflowid', N'wname', N'wstatus', N'workflowcode', N'isshow', N'isactive', N'description') AND [object_id] = OBJECT_ID(N'[wf_workflow]'))
    SET IDENTITY_INSERT [wf_workflow] ON;
INSERT INTO [wf_workflow] ([workflowid], [wname], [wstatus], [workflowcode], [isshow], [isactive], [description])
VALUES (CAST(4 AS bigint), N'ทดสอบ LOA วงเงิน (Demo)', N'ACTIVE', N'DEMO_LOA', CAST(1 AS bit), CAST(1 AS bit), N'Workflow สาธิต LOA amount-based branching — Block 4. <=10000 ไปสาย low (level 2), >10000 ไปสาย high (level 3) ข้าม level 2');
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'workflowid', N'wname', N'wstatus', N'workflowcode', N'isshow', N'isactive', N'description') AND [object_id] = OBJECT_ID(N'[wf_workflow]'))
    SET IDENTITY_INSERT [wf_workflow] OFF;

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'subworkflowid', N'workflowid', N'wlevel', N'subject', N'isAdhocUser', N'iscustomApprover', N'isupperrole', N'isupperuser', N'iscustomRole', N'iscustomUser', N'iscondition', N'isorcondition', N'isandcondition', N'forwardstatus', N'standstatus', N'backwardstatus', N'istop', N'isReturnSender', N'isshow', N'isLOA', N'isAutoApproveAllow', N'isNeedBudgetApproval', N'isPool', N'isApproverSameOrg', N'isApproverSameCostCenter', N'isManualButton') AND [object_id] = OBJECT_ID(N'[wf_sub_workflow_master]'))
    SET IDENTITY_INSERT [wf_sub_workflow_master] ON;
INSERT INTO [wf_sub_workflow_master] ([subworkflowid], [workflowid], [wlevel], [subject], [isAdhocUser], [iscustomApprover], [isupperrole], [isupperuser], [iscustomRole], [iscustomUser], [iscondition], [isorcondition], [isandcondition], [forwardstatus], [standstatus], [backwardstatus], [istop], [isReturnSender], [isshow], [isLOA], [isAutoApproveAllow], [isNeedBudgetApproval], [isPool], [isApproverSameOrg], [isApproverSameCostCenter], [isManualButton])
VALUES (CAST(6 AS bigint), CAST(4 AS bigint), 1, N'อนุมัติระดับ 1 (ตรวจวงเงิน)', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), N'PENDING', N'PENDING', N'RETURNED', CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), CAST(1 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit)),
(CAST(7 AS bigint), CAST(4 AS bigint), 2, N'อนุมัติระดับ 2 (สาย low — วงเงินไม่เกิน 10,000)', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), N'APPROVED', N'PENDING', N'RETURNED', CAST(1 AS bit), CAST(0 AS bit), CAST(1 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit)),
(CAST(8 AS bigint), CAST(4 AS bigint), 3, N'อนุมัติระดับ 3 (สาย high — วงเงินเกิน 10,000)', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), N'APPROVED', N'PENDING', N'RETURNED', CAST(1 AS bit), CAST(0 AS bit), CAST(1 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit));
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'subworkflowid', N'workflowid', N'wlevel', N'subject', N'isAdhocUser', N'iscustomApprover', N'isupperrole', N'isupperuser', N'iscustomRole', N'iscustomUser', N'iscondition', N'isorcondition', N'isandcondition', N'forwardstatus', N'standstatus', N'backwardstatus', N'istop', N'isReturnSender', N'isshow', N'isLOA', N'isAutoApproveAllow', N'isNeedBudgetApproval', N'isPool', N'isApproverSameOrg', N'isApproverSameCostCenter', N'isManualButton') AND [object_id] = OBJECT_ID(N'[wf_sub_workflow_master]'))
    SET IDENTITY_INSERT [wf_sub_workflow_master] OFF;

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'id', N'subworkflowid', N'workflowid', N'wlevel', N'userid', N'empid', N'isactive') AND [object_id] = OBJECT_ID(N'[wf_custom_user]'))
    SET IDENTITY_INSERT [wf_custom_user] ON;
INSERT INTO [wf_custom_user] ([id], [subworkflowid], [workflowid], [wlevel], [userid], [empid], [isactive])
VALUES (CAST(4 AS bigint), CAST(6 AS bigint), CAST(4 AS bigint), 1, CAST(16 AS bigint), N'002', CAST(1 AS bit)),
(CAST(5 AS bigint), CAST(7 AS bigint), CAST(4 AS bigint), 2, CAST(16 AS bigint), N'002', CAST(1 AS bit)),
(CAST(6 AS bigint), CAST(8 AS bigint), CAST(4 AS bigint), 3, CAST(16 AS bigint), N'002', CAST(1 AS bit));
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'id', N'subworkflowid', N'workflowid', N'wlevel', N'userid', N'empid', N'isactive') AND [object_id] = OBJECT_ID(N'[wf_custom_user]'))
    SET IDENTITY_INSERT [wf_custom_user] OFF;

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'id', N'loaid', N'wfid', N'nowWorkflowid', N'nextWorkflowId', N'nowLevel', N'nextLevel', N'min', N'max', N'isactive', N'subject') AND [object_id] = OBJECT_ID(N'[wf_loa]'))
    SET IDENTITY_INSERT [wf_loa] ON;
INSERT INTO [wf_loa] ([id], [loaid], [wfid], [nowWorkflowid], [nextWorkflowId], [nowLevel], [nextLevel], [min], [max], [isactive], [subject])
VALUES (CAST(1 AS bigint), CAST(1 AS bigint), CAST(4 AS bigint), CAST(4 AS bigint), CAST(4 AS bigint), 1, 2, 0.0, 10000.0, CAST(1 AS bit), N'วงเงินต่ำ (ไม่เกิน 10,000)'),
(CAST(2 AS bigint), CAST(2 AS bigint), CAST(4 AS bigint), CAST(4 AS bigint), CAST(4 AS bigint), 1, 3, 10000.01, NULL, CAST(1 AS bit), N'วงเงินสูง (เกิน 10,000)');
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'id', N'loaid', N'wfid', N'nowWorkflowid', N'nextWorkflowId', N'nowLevel', N'nextLevel', N'min', N'max', N'isactive', N'subject') AND [object_id] = OBJECT_ID(N'[wf_loa]'))
    SET IDENTITY_INSERT [wf_loa] OFF;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260731164844_AddWorkflowLoaBlock4', N'10.0.10');

COMMIT;
GO

