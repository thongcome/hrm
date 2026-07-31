BEGIN TRANSACTION;
ALTER TABLE [wf_sub_workflow_master] ADD [isNeedsupervisorapprove] int NULL;

ALTER TABLE [job_subworkflow_master] ADD [backwardstatus] nvarchar(50) NULL;

ALTER TABLE [job_subworkflow_master] ADD [forwardstatus] nvarchar(50) NULL;

ALTER TABLE [job_subworkflow_master] ADD [isNeedsupervisorapprove] int NULL;
GO

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'jobstatusid', N'jobstatuscode', N'name', N'name_en', N'businessstatus', N'isactive') AND [object_id] = OBJECT_ID(N'[job_status]'))
    SET IDENTITY_INSERT [job_status] ON;
INSERT INTO [job_status] ([jobstatusid], [jobstatuscode], [name], [name_en], [businessstatus], [isactive])
VALUES (CAST(5 AS bigint), N'RETURNED', N'ตีกลับ/ปฏิเสธ', N'Returned', N'REJECTED', CAST(1 AS bit));
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'jobstatusid', N'jobstatuscode', N'name', N'name_en', N'businessstatus', N'isactive') AND [object_id] = OBJECT_ID(N'[job_status]'))
    SET IDENTITY_INSERT [job_status] OFF;

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'workflowid', N'wname', N'wstatus', N'workflowcode', N'isshow', N'isactive', N'description') AND [object_id] = OBJECT_ID(N'[wf_workflow]'))
    SET IDENTITY_INSERT [wf_workflow] ON;
INSERT INTO [wf_workflow] ([workflowid], [wname], [wstatus], [workflowcode], [isshow], [isactive], [description])
VALUES (CAST(5 AS bigint), N'ทดสอบ AND-Condition % (Demo)', N'ACTIVE', N'DEMO_ANDPERCENT', CAST(1 AS bit), CAST(1 AS bit), N'Workflow สาธิต AND-condition % — Block 5. 3 ผู้อนุมัติ (คนเดียวกัน 3 แถว เพื่อทดสอบด้วย login เดียว) ต้องอนุมัติรวมกัน >= 60% จึงจะผ่าน');
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'workflowid', N'wname', N'wstatus', N'workflowcode', N'isshow', N'isactive', N'description') AND [object_id] = OBJECT_ID(N'[wf_workflow]'))
    SET IDENTITY_INSERT [wf_workflow] OFF;

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'workflowid', N'wname', N'wstatus', N'workflowcode', N'isshow', N'isactive', N'description') AND [object_id] = OBJECT_ID(N'[wf_workflow]'))
    SET IDENTITY_INSERT [wf_workflow] ON;
INSERT INTO [wf_workflow] ([workflowid], [wname], [wstatus], [workflowcode], [isshow], [isactive], [description])
VALUES (CAST(6 AS bigint), N'ทดสอบ Mix Approval (Demo)', N'ACTIVE', N'DEMO_MIX', CAST(1 AS bit), CAST(1 AS bit), N'Workflow สาธิต Mix Approval — Block 6. ต้องให้หัวหน้าสายบังคับบัญชา (org ของผู้ขอ) อนุมัติก่อน 1 ชั้น แล้วจึงเข้าสู่ผู้อนุมัติหลักของระดับ');
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'workflowid', N'wname', N'wstatus', N'workflowcode', N'isshow', N'isactive', N'description') AND [object_id] = OBJECT_ID(N'[wf_workflow]'))
    SET IDENTITY_INSERT [wf_workflow] OFF;

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'subworkflowid', N'workflowid', N'wlevel', N'subject', N'isAdhocUser', N'iscustomApprover', N'isupperrole', N'isupperuser', N'iscustomRole', N'iscustomUser', N'iscondition', N'isorcondition', N'isandcondition', N'andpercent', N'forwardstatus', N'standstatus', N'backwardstatus', N'istop', N'isReturnSender', N'isshow', N'isLOA', N'isAutoApproveAllow', N'isNeedBudgetApproval', N'isPool', N'isApproverSameOrg', N'isApproverSameCostCenter', N'isManualButton', N'isNeedsupervisorapprove') AND [object_id] = OBJECT_ID(N'[wf_sub_workflow_master]'))
    SET IDENTITY_INSERT [wf_sub_workflow_master] ON;
INSERT INTO [wf_sub_workflow_master] ([subworkflowid], [workflowid], [wlevel], [subject], [isAdhocUser], [iscustomApprover], [isupperrole], [isupperuser], [iscustomRole], [iscustomUser], [iscondition], [isorcondition], [isandcondition], [andpercent], [forwardstatus], [standstatus], [backwardstatus], [istop], [isReturnSender], [isshow], [isLOA], [isAutoApproveAllow], [isNeedBudgetApproval], [isPool], [isApproverSameOrg], [isApproverSameCostCenter], [isManualButton], [isNeedsupervisorapprove])
VALUES (CAST(9 AS bigint), CAST(5 AS bigint), 1, N'อนุมัติแบบบอร์ด (AND % >= 60)', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), 60.0, N'COMPLETED', N'PENDING', N'RETURNED', CAST(1 AS bit), CAST(0 AS bit), CAST(1 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), 0),
(CAST(10 AS bigint), CAST(6 AS bigint), 1, N'อนุมัติ Mix (หัวหน้าก่อน 1 ชั้น แล้วอนุมัติเอง)', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), NULL, N'COMPLETED', N'PENDING', N'RETURNED', CAST(1 AS bit), CAST(0 AS bit), CAST(1 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), 1);
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'subworkflowid', N'workflowid', N'wlevel', N'subject', N'isAdhocUser', N'iscustomApprover', N'isupperrole', N'isupperuser', N'iscustomRole', N'iscustomUser', N'iscondition', N'isorcondition', N'isandcondition', N'andpercent', N'forwardstatus', N'standstatus', N'backwardstatus', N'istop', N'isReturnSender', N'isshow', N'isLOA', N'isAutoApproveAllow', N'isNeedBudgetApproval', N'isPool', N'isApproverSameOrg', N'isApproverSameCostCenter', N'isManualButton', N'isNeedsupervisorapprove') AND [object_id] = OBJECT_ID(N'[wf_sub_workflow_master]'))
    SET IDENTITY_INSERT [wf_sub_workflow_master] OFF;

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'id', N'subworkflowid', N'workflowid', N'wlevel', N'userid', N'empid', N'isactive') AND [object_id] = OBJECT_ID(N'[wf_custom_user]'))
    SET IDENTITY_INSERT [wf_custom_user] ON;
INSERT INTO [wf_custom_user] ([id], [subworkflowid], [workflowid], [wlevel], [userid], [empid], [isactive])
VALUES (CAST(7 AS bigint), CAST(9 AS bigint), CAST(5 AS bigint), 1, CAST(16 AS bigint), N'002', CAST(1 AS bit)),
(CAST(8 AS bigint), CAST(9 AS bigint), CAST(5 AS bigint), 1, CAST(16 AS bigint), N'002', CAST(1 AS bit)),
(CAST(9 AS bigint), CAST(9 AS bigint), CAST(5 AS bigint), 1, CAST(16 AS bigint), N'002', CAST(1 AS bit)),
(CAST(10 AS bigint), CAST(10 AS bigint), CAST(6 AS bigint), 1, CAST(16 AS bigint), N'002', CAST(1 AS bit));
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'id', N'subworkflowid', N'workflowid', N'wlevel', N'userid', N'empid', N'isactive') AND [object_id] = OBJECT_ID(N'[wf_custom_user]'))
    SET IDENTITY_INSERT [wf_custom_user] OFF;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260731171635_AddWorkflowBlock569', N'10.0.10');

COMMIT;
GO

