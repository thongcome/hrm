BEGIN TRANSACTION;
UPDATE com_organization SET approver_userid = 16 WHERE code = 'HR'

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'id', N'empid', N'firstname_th', N'lastname_th', N'cardid', N'sexid', N'orgcode', N'orgname_th', N'isactive') AND [object_id] = OBJECT_ID(N'[wf_employee]'))
    SET IDENTITY_INSERT [wf_employee] ON;
INSERT INTO [wf_employee] ([id], [empid], [firstname_th], [lastname_th], [cardid], [sexid], [orgcode], [orgname_th], [isactive])
VALUES (CAST(1 AS bigint), N'002', N'ทดสอบ', N'ระบบเวิร์กโฟลว์', N'9999999999999', N'M', N'HR', N'HR', CAST(1 AS bit));
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'id', N'empid', N'firstname_th', N'lastname_th', N'cardid', N'sexid', N'orgcode', N'orgname_th', N'isactive') AND [object_id] = OBJECT_ID(N'[wf_employee]'))
    SET IDENTITY_INSERT [wf_employee] OFF;

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'workflowid', N'wname', N'wstatus', N'workflowcode', N'isshow', N'isactive', N'description') AND [object_id] = OBJECT_ID(N'[wf_workflow]'))
    SET IDENTITY_INSERT [wf_workflow] ON;
INSERT INTO [wf_workflow] ([workflowid], [wname], [wstatus], [workflowcode], [isshow], [isactive], [description])
VALUES (CAST(2 AS bigint), N'ทดสอบอนุมัติสายบังคับบัญชา (Vertical Demo)', N'ACTIVE', N'DEMO_VERTICAL', CAST(1 AS bit), CAST(1 AS bit), N'Workflow สาธิต Vertical resolution ผ่าน com_organization.approver_userid — Block 3');
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'workflowid', N'wname', N'wstatus', N'workflowcode', N'isshow', N'isactive', N'description') AND [object_id] = OBJECT_ID(N'[wf_workflow]'))
    SET IDENTITY_INSERT [wf_workflow] OFF;

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'subworkflowid', N'workflowid', N'wlevel', N'subject', N'isAdhocUser', N'iscustomApprover', N'isupperrole', N'isupperuser', N'iscustomRole', N'iscustomUser', N'iscondition', N'isorcondition', N'isandcondition', N'forwardstatus', N'standstatus', N'backwardstatus', N'istop', N'isReturnSender', N'isshow', N'isLOA', N'isAutoApproveAllow', N'isNeedBudgetApproval', N'isPool', N'isApproverSameOrg', N'isApproverSameCostCenter', N'isManualButton') AND [object_id] = OBJECT_ID(N'[wf_sub_workflow_master]'))
    SET IDENTITY_INSERT [wf_sub_workflow_master] ON;
INSERT INTO [wf_sub_workflow_master] ([subworkflowid], [workflowid], [wlevel], [subject], [isAdhocUser], [iscustomApprover], [isupperrole], [isupperuser], [iscustomRole], [iscustomUser], [iscondition], [isorcondition], [isandcondition], [forwardstatus], [standstatus], [backwardstatus], [istop], [isReturnSender], [isshow], [isLOA], [isAutoApproveAllow], [isNeedBudgetApproval], [isPool], [isApproverSameOrg], [isApproverSameCostCenter], [isManualButton])
VALUES (CAST(3 AS bigint), CAST(2 AS bigint), 1, N'อนุมัติโดยหัวหน้าหน่วยงาน (Vertical)', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), N'APPROVED', N'PENDING', N'RETURNED', CAST(1 AS bit), CAST(0 AS bit), CAST(1 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit));
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'subworkflowid', N'workflowid', N'wlevel', N'subject', N'isAdhocUser', N'iscustomApprover', N'isupperrole', N'isupperuser', N'iscustomRole', N'iscustomUser', N'iscondition', N'isorcondition', N'isandcondition', N'forwardstatus', N'standstatus', N'backwardstatus', N'istop', N'isReturnSender', N'isshow', N'isLOA', N'isAutoApproveAllow', N'isNeedBudgetApproval', N'isPool', N'isApproverSameOrg', N'isApproverSameCostCenter', N'isManualButton') AND [object_id] = OBJECT_ID(N'[wf_sub_workflow_master]'))
    SET IDENTITY_INSERT [wf_sub_workflow_master] OFF;

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'workflowid', N'wname', N'wstatus', N'workflowcode', N'isshow', N'isactive', N'description') AND [object_id] = OBJECT_ID(N'[wf_workflow]'))
    SET IDENTITY_INSERT [wf_workflow] ON;
INSERT INTO [wf_workflow] ([workflowid], [wname], [wstatus], [workflowcode], [isshow], [isactive], [description])
VALUES (CAST(3 AS bigint), N'ทดสอบข้ามระดับอัตโนมัติเมื่อตำแหน่งว่าง (Auto-Skip Demo)', N'ACTIVE', N'DEMO_AUTOSKIP', CAST(1 AS bit), CAST(1 AS bit), N'Workflow สาธิต isAutoApproveAllow เมื่อ Vertical resolve ไม่เจอใคร — Block 3');
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'workflowid', N'wname', N'wstatus', N'workflowcode', N'isshow', N'isactive', N'description') AND [object_id] = OBJECT_ID(N'[wf_workflow]'))
    SET IDENTITY_INSERT [wf_workflow] OFF;

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'subworkflowid', N'workflowid', N'wlevel', N'subject', N'isAdhocUser', N'iscustomApprover', N'isupperrole', N'isupperuser', N'iscustomRole', N'iscustomUser', N'iscondition', N'isorcondition', N'isandcondition', N'forwardstatus', N'standstatus', N'backwardstatus', N'istop', N'isReturnSender', N'isshow', N'isLOA', N'isAutoApproveAllow', N'isNeedBudgetApproval', N'isPool', N'isApproverSameOrg', N'isApproverSameCostCenter', N'isManualButton') AND [object_id] = OBJECT_ID(N'[wf_sub_workflow_master]'))
    SET IDENTITY_INSERT [wf_sub_workflow_master] ON;
INSERT INTO [wf_sub_workflow_master] ([subworkflowid], [workflowid], [wlevel], [subject], [isAdhocUser], [iscustomApprover], [isupperrole], [isupperuser], [iscustomRole], [iscustomUser], [iscondition], [isorcondition], [isandcondition], [forwardstatus], [standstatus], [backwardstatus], [istop], [isReturnSender], [isshow], [isLOA], [isAutoApproveAllow], [isNeedBudgetApproval], [isPool], [isApproverSameOrg], [isApproverSameCostCenter], [isManualButton])
VALUES (CAST(4 AS bigint), CAST(3 AS bigint), 1, N'อนุมัติโดยหัวหน้าหน่วยงาน (ข้ามถ้าว่าง)', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), N'APPROVED', N'PENDING', N'RETURNED', CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), CAST(0 AS bit), CAST(1 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit)),
(CAST(5 AS bigint), CAST(3 AS bigint), 2, N'อนุมัติระดับสุดท้าย (Custom User)', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), N'APPROVED', N'PENDING', N'RETURNED', CAST(1 AS bit), CAST(0 AS bit), CAST(1 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit));
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'subworkflowid', N'workflowid', N'wlevel', N'subject', N'isAdhocUser', N'iscustomApprover', N'isupperrole', N'isupperuser', N'iscustomRole', N'iscustomUser', N'iscondition', N'isorcondition', N'isandcondition', N'forwardstatus', N'standstatus', N'backwardstatus', N'istop', N'isReturnSender', N'isshow', N'isLOA', N'isAutoApproveAllow', N'isNeedBudgetApproval', N'isPool', N'isApproverSameOrg', N'isApproverSameCostCenter', N'isManualButton') AND [object_id] = OBJECT_ID(N'[wf_sub_workflow_master]'))
    SET IDENTITY_INSERT [wf_sub_workflow_master] OFF;

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'id', N'subworkflowid', N'workflowid', N'wlevel', N'userid', N'empid', N'isactive') AND [object_id] = OBJECT_ID(N'[wf_custom_user]'))
    SET IDENTITY_INSERT [wf_custom_user] ON;
INSERT INTO [wf_custom_user] ([id], [subworkflowid], [workflowid], [wlevel], [userid], [empid], [isactive])
VALUES (CAST(3 AS bigint), CAST(5 AS bigint), CAST(3 AS bigint), 2, CAST(16 AS bigint), N'002', CAST(1 AS bit));
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'id', N'subworkflowid', N'workflowid', N'wlevel', N'userid', N'empid', N'isactive') AND [object_id] = OBJECT_ID(N'[wf_custom_user]'))
    SET IDENTITY_INSERT [wf_custom_user] OFF;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260731155257_SeedWorkflowEngineBlock3', N'10.0.10');

COMMIT;
GO

