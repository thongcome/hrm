BEGIN TRANSACTION;
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'workflowid', N'wname', N'wstatus', N'workflowcode', N'isshow', N'isactive', N'description') AND [object_id] = OBJECT_ID(N'[wf_workflow]'))
    SET IDENTITY_INSERT [wf_workflow] ON;
INSERT INTO [wf_workflow] ([workflowid], [wname], [wstatus], [workflowcode], [isshow], [isactive], [description])
VALUES (CAST(7 AS bigint), N'ทดสอบส่งงานไปหลายคนจริง (Demo)', N'ACTIVE', N'DEMO_MULTIUSER', CAST(1 AS bit), CAST(1 AS bit), N'Workflow สาธิตว่า engine ส่งงานไปหาคนจริงที่ต่างกันได้ ไม่ใช่คนเดิมซ้ำ — level 1 กระจายไปหาทุกคนใน Role Admin พร้อมกัน (6 คน), level 2 ส่งต่อไปยัง sawat โดยเฉพาะ');
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'workflowid', N'wname', N'wstatus', N'workflowcode', N'isshow', N'isactive', N'description') AND [object_id] = OBJECT_ID(N'[wf_workflow]'))
    SET IDENTITY_INSERT [wf_workflow] OFF;

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'subworkflowid', N'workflowid', N'wlevel', N'subject', N'isAdhocUser', N'iscustomApprover', N'isupperrole', N'isupperuser', N'iscustomRole', N'iscustomUser', N'iscondition', N'isorcondition', N'isandcondition', N'forwardstatus', N'standstatus', N'backwardstatus', N'istop', N'isReturnSender', N'isshow', N'isLOA', N'isAutoApproveAllow', N'isNeedBudgetApproval', N'isPool', N'isApproverSameOrg', N'isApproverSameCostCenter', N'isManualButton') AND [object_id] = OBJECT_ID(N'[wf_sub_workflow_master]'))
    SET IDENTITY_INSERT [wf_sub_workflow_master] ON;
INSERT INTO [wf_sub_workflow_master] ([subworkflowid], [workflowid], [wlevel], [subject], [isAdhocUser], [iscustomApprover], [isupperrole], [isupperuser], [iscustomRole], [iscustomUser], [iscondition], [isorcondition], [isandcondition], [forwardstatus], [standstatus], [backwardstatus], [istop], [isReturnSender], [isshow], [isLOA], [isAutoApproveAllow], [isNeedBudgetApproval], [isPool], [isApproverSameOrg], [isApproverSameCostCenter], [isManualButton])
VALUES (CAST(11 AS bigint), CAST(7 AS bigint), 1, N'ระดับ 1 - ทีม Admin (Custom Role, กระจายไปหลายคนพร้อมกัน)', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), N'APPROVED', N'PENDING', N'RETURNED', CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit)),
(CAST(12 AS bigint), CAST(7 AS bigint), 2, N'ระดับ 2 - ผู้บริหาร sawat (Custom User)', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), N'COMPLETED', N'PENDING', N'RETURNED', CAST(1 AS bit), CAST(0 AS bit), CAST(1 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit));
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'subworkflowid', N'workflowid', N'wlevel', N'subject', N'isAdhocUser', N'iscustomApprover', N'isupperrole', N'isupperuser', N'iscustomRole', N'iscustomUser', N'iscondition', N'isorcondition', N'isandcondition', N'forwardstatus', N'standstatus', N'backwardstatus', N'istop', N'isReturnSender', N'isshow', N'isLOA', N'isAutoApproveAllow', N'isNeedBudgetApproval', N'isPool', N'isApproverSameOrg', N'isApproverSameCostCenter', N'isManualButton') AND [object_id] = OBJECT_ID(N'[wf_sub_workflow_master]'))
    SET IDENTITY_INSERT [wf_sub_workflow_master] OFF;

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'id', N'subworkflowid', N'workflowid', N'wlevel', N'roleid', N'isactive') AND [object_id] = OBJECT_ID(N'[wf_custom_role]'))
    SET IDENTITY_INSERT [wf_custom_role] ON;
INSERT INTO [wf_custom_role] ([id], [subworkflowid], [workflowid], [wlevel], [roleid], [isactive])
VALUES (CAST(1 AS bigint), CAST(11 AS bigint), CAST(7 AS bigint), 1, CAST(9 AS bigint), CAST(1 AS bit));
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'id', N'subworkflowid', N'workflowid', N'wlevel', N'roleid', N'isactive') AND [object_id] = OBJECT_ID(N'[wf_custom_role]'))
    SET IDENTITY_INSERT [wf_custom_role] OFF;

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'id', N'subworkflowid', N'workflowid', N'wlevel', N'userid', N'empid', N'isactive') AND [object_id] = OBJECT_ID(N'[wf_custom_user]'))
    SET IDENTITY_INSERT [wf_custom_user] ON;
INSERT INTO [wf_custom_user] ([id], [subworkflowid], [workflowid], [wlevel], [userid], [empid], [isactive])
VALUES (CAST(11 AS bigint), CAST(12 AS bigint), CAST(7 AS bigint), 2, CAST(12 AS bigint), N'001', CAST(1 AS bit));
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'id', N'subworkflowid', N'workflowid', N'wlevel', N'userid', N'empid', N'isactive') AND [object_id] = OBJECT_ID(N'[wf_custom_user]'))
    SET IDENTITY_INSERT [wf_custom_user] OFF;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260731224652_SeedWorkflowMultiUserDemo', N'10.0.10');

COMMIT;
GO

