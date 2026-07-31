BEGIN TRANSACTION;
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'menuid', N'menuname', N'menuname_en', N'menulevel', N'isfinal', N'menuorder', N'menucode', N'isshow', N'url', N'menugroupid', N'isactive') AND [object_id] = OBJECT_ID(N'[sc_menu]'))
    SET IDENTITY_INSERT [sc_menu] ON;
INSERT INTO [sc_menu] ([menuid], [menuname], [menuname_en], [menulevel], [isfinal], [menuorder], [menucode], [isshow], [url], [menugroupid], [isactive])
VALUES (CAST(19 AS bigint), N'จัดการ Workflow (Engine)', N'Workflow Engine Admin', 1, CAST(1 AS bit), 32, N'WF_WORKFLOW_ADMIN', CAST(1 AS bit), '/wf/workflows', CAST(1 AS bigint), CAST(1 AS bit));
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'menuid', N'menuname', N'menuname_en', N'menulevel', N'isfinal', N'menuorder', N'menucode', N'isshow', N'url', N'menugroupid', N'isactive') AND [object_id] = OBJECT_ID(N'[sc_menu]'))
    SET IDENTITY_INSERT [sc_menu] OFF;

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'rolemenuid', N'menuid', N'roleid', N'isactive', N'canedit') AND [object_id] = OBJECT_ID(N'[sc_role_menu]'))
    SET IDENTITY_INSERT [sc_role_menu] ON;
INSERT INTO [sc_role_menu] ([rolemenuid], [menuid], [roleid], [isactive], [canedit])
VALUES (CAST(17 AS bigint), CAST(19 AS bigint), CAST(9 AS bigint), CAST(1 AS bit), CAST(1 AS bit));
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'rolemenuid', N'menuid', N'roleid', N'isactive', N'canedit') AND [object_id] = OBJECT_ID(N'[sc_role_menu]'))
    SET IDENTITY_INSERT [sc_role_menu] OFF;

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'jobstatusid', N'jobstatuscode', N'name', N'name_en', N'businessstatus', N'isactive') AND [object_id] = OBJECT_ID(N'[job_status]'))
    SET IDENTITY_INSERT [job_status] ON;
INSERT INTO [job_status] ([jobstatusid], [jobstatuscode], [name], [name_en], [businessstatus], [isactive])
VALUES (CAST(1 AS bigint), N'PENDING', N'รออนุมัติ', N'Pending', N'PENDING', CAST(1 AS bit)),
(CAST(2 AS bigint), N'APPROVED', N'อนุมัติแล้ว (ระดับนี้)', N'Approved (this level)', N'APPROVED', CAST(1 AS bit)),
(CAST(3 AS bigint), N'REJECTED', N'ปฏิเสธ', N'Rejected', N'REJECTED', CAST(1 AS bit)),
(CAST(4 AS bigint), N'COMPLETED', N'อนุมัติครบทุกระดับ', N'Completed', N'COMPLETED', CAST(1 AS bit));
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'jobstatusid', N'jobstatuscode', N'name', N'name_en', N'businessstatus', N'isactive') AND [object_id] = OBJECT_ID(N'[job_status]'))
    SET IDENTITY_INSERT [job_status] OFF;

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'workflowid', N'wname', N'wstatus', N'workflowcode', N'isshow', N'isactive', N'description') AND [object_id] = OBJECT_ID(N'[wf_workflow]'))
    SET IDENTITY_INSERT [wf_workflow] ON;
INSERT INTO [wf_workflow] ([workflowid], [wname], [wstatus], [workflowcode], [isshow], [isactive], [description])
VALUES (CAST(1 AS bigint), N'ทดสอบอนุมัติ 2 ระดับ (Demo)', N'ACTIVE', N'DEMO_2LV', CAST(1 AS bit), CAST(1 AS bit), N'Workflow สาธิตสำหรับทดสอบ Engine Block 2 — 2 ระดับ ผู้อนุมัติเดียวกันทั้งสองระดับ');
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'workflowid', N'wname', N'wstatus', N'workflowcode', N'isshow', N'isactive', N'description') AND [object_id] = OBJECT_ID(N'[wf_workflow]'))
    SET IDENTITY_INSERT [wf_workflow] OFF;

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'subworkflowid', N'workflowid', N'wlevel', N'subject', N'isAdhocUser', N'iscustomApprover', N'isupperrole', N'isupperuser', N'iscustomRole', N'iscustomUser', N'iscondition', N'isorcondition', N'isandcondition', N'forwardstatus', N'standstatus', N'backwardstatus', N'istop', N'isReturnSender', N'isshow', N'isLOA', N'isAutoApproveAllow', N'isNeedBudgetApproval', N'isPool', N'isApproverSameOrg', N'isApproverSameCostCenter', N'isManualButton') AND [object_id] = OBJECT_ID(N'[wf_sub_workflow_master]'))
    SET IDENTITY_INSERT [wf_sub_workflow_master] ON;
INSERT INTO [wf_sub_workflow_master] ([subworkflowid], [workflowid], [wlevel], [subject], [isAdhocUser], [iscustomApprover], [isupperrole], [isupperuser], [iscustomRole], [iscustomUser], [iscondition], [isorcondition], [isandcondition], [forwardstatus], [standstatus], [backwardstatus], [istop], [isReturnSender], [isshow], [isLOA], [isAutoApproveAllow], [isNeedBudgetApproval], [isPool], [isApproverSameOrg], [isApproverSameCostCenter], [isManualButton])
VALUES (CAST(1 AS bigint), CAST(1 AS bigint), 1, N'อนุมัติระดับ 1', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), N'PENDING', N'PENDING', N'RETURNED', CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit)),
(CAST(2 AS bigint), CAST(1 AS bigint), 2, N'อนุมัติระดับ 2 (สุดท้าย)', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), N'APPROVED', N'PENDING', N'RETURNED', CAST(1 AS bit), CAST(0 AS bit), CAST(1 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit));
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'subworkflowid', N'workflowid', N'wlevel', N'subject', N'isAdhocUser', N'iscustomApprover', N'isupperrole', N'isupperuser', N'iscustomRole', N'iscustomUser', N'iscondition', N'isorcondition', N'isandcondition', N'forwardstatus', N'standstatus', N'backwardstatus', N'istop', N'isReturnSender', N'isshow', N'isLOA', N'isAutoApproveAllow', N'isNeedBudgetApproval', N'isPool', N'isApproverSameOrg', N'isApproverSameCostCenter', N'isManualButton') AND [object_id] = OBJECT_ID(N'[wf_sub_workflow_master]'))
    SET IDENTITY_INSERT [wf_sub_workflow_master] OFF;

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'id', N'subworkflowid', N'workflowid', N'wlevel', N'userid', N'empid', N'isactive') AND [object_id] = OBJECT_ID(N'[wf_custom_user]'))
    SET IDENTITY_INSERT [wf_custom_user] ON;
INSERT INTO [wf_custom_user] ([id], [subworkflowid], [workflowid], [wlevel], [userid], [empid], [isactive])
VALUES (CAST(1 AS bigint), CAST(1 AS bigint), CAST(1 AS bigint), 1, CAST(16 AS bigint), N'002', CAST(1 AS bit)),
(CAST(2 AS bigint), CAST(2 AS bigint), CAST(1 AS bigint), 2, CAST(16 AS bigint), N'002', CAST(1 AS bit));
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'id', N'subworkflowid', N'workflowid', N'wlevel', N'userid', N'empid', N'isactive') AND [object_id] = OBJECT_ID(N'[wf_custom_user]'))
    SET IDENTITY_INSERT [wf_custom_user] OFF;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260731152712_SeedWorkflowEngineBlock2', N'10.0.10');

COMMIT;
GO

