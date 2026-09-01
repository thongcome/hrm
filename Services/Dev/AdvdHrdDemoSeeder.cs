namespace HRM.Services.Dev;

using HRM.Models;
using HRM.Services.Engagement;
using Microsoft.EntityFrameworkCore;

// HRD demo slice for the AdvanceDigital (ADVD) demo company — CEO order
// 1 ก.ย. 2569, follow-up to DemoCompanySeeder (7,000 employees + org tree)
// and AdvdCompetencySeeder (competency catalog). Without this, every HRD
// screen (Performance, 9-box Talent grid, Succession bench, IDP gap analysis,
// Career path, LMS, Engagement eNPS/Culture) renders empty for ADVD because
// those modules only ever had company "001" sample rows.
//
// Design (all verified against the live schema + company "001" rows before
// writing — same discipline as DemoCompanySeeder):
//
// - ONE-SHOT / IDEMPOTENT: returns immediately if a Perf_EvaluationPeriod
//   already exists for CompanyId="ADVD" (that period is the marker this
//   seeder ran). To re-seed, delete the ADVD HRD rows and restart.
//
// - DETERMINISTIC: no DateTime.Now / no unseeded Random anywhere. Every date
//   is derived from the fixed Anchor; every score/rating/readiness value is a
//   hand-authored constant, so a wipe + rerun reproduces identical data.
//
// - COHERENT SLICE, not all 7,000. One clean manager→staff chain inside
//   ฝ่ายการเงินและบัญชี (division AD-FIN): department แผนกบัญชีทั่วไป
//   (AD-FIN-01) and its first section (AD-FIN-01-1). Employees used:
//     * AD0004  ผู้อำนวยการฝ่ายการเงิน (A06) — division head
//     * AD0018  ผู้จัดการแผนกบัญชีทั่วไป (A04) — department manager
//     * AD0078  หัวหน้างาน (A03) — section head; approver_empid of
//               AD-FIN-01-1, so DirectReportResolverHelper resolves the
//               section's rank-and-file as this person's real team.
//     * AD2375..AD2392 (18 rank-and-file A01/A02 in section AD-FIN-01-1)
//   21 employees total — enough for a scattered 9-box, a real team view, and
//   an internally consistent succession bench, without seeding thousands.
//
// - IDENTITY VERDICTS (sys.columns.is_identity checked 1 ก.ย. 2569): every
//   table this seeder inserts into has a real IDENTITY PK (Id) — Perf_*,
//   Talent_*, Succ_KeyPosition, Succ_SuccessorNominations, Idp_*, Career_*,
//   Lms_*, Eng_*, Job_CompetencyRequirements, Pos_PositionSlot, Job_Family.
//   So EF's Identity mapping is correct and EntitySearchHelper.NextIdAsync is
//   NOT needed here. Pattern throughout: insert parent → SaveChanges → use the
//   generated Id for children (never a hard-coded key).
//
// - Pos_PositionSlot PREREQUISITE: DemoCompanySeeder deliberately created NO
//   headcount slots for ADVD (documented deviation there). But IDP gap
//   analysis (IdpAssessmentService), 9-box position names (TalentGridService)
//   and Career "my path" (CareerPathService) all resolve an employee's
//   Pos_ExecType (Job) via their ACTIVE Pos_PositionSlot. So this seeder
//   creates one active slot per slice employee (linking Hremployee → the ADVD
//   Pos_ExecType matching their POS_CODE). Without it, gap analysis returns
//   empty and the grid shows no position titles.
//
// - Approval statuses are set DIRECTLY (Approved), not run through the live
//   WorkflowEngine — the lazy SyncStatusFromJobAsync in every consumer service
//   is a no-op unless Status==PendingApproval && JobMasterId!=null, so a
//   directly-Approved row with JobMasterId=null is stable on every read
//   (mirrors how an approved 001 row looks to the UI, without needing a real
//   job_master / approver chain to exist for ADVD).
public static class AdvdHrdDemoSeeder
{
    private const string CompanyId = "ADVD";
    private const string SeederName = "AdvdHrdDemoSeeder";
    private const int FixedSeed = 25690901; // referenced for provenance; all values are hand-authored constants (deterministic)

    private static readonly DateTime Anchor = new(2026, 9, 1);
    private static readonly DateOnly AnchorDate = new(2026, 9, 1);

    // --- The coherent slice (EmpNo → position + demo performance/potential) ---
    // Score == null  → still InProgress (no FinalScore/grade → not on the grid).
    // Potential == null on an Approved row → lands in "ยังไม่ได้ประเมินศักยภาพ".
    // Potential set on an InProgress row → lands in "ไม่มีข้อมูลผลงานรอบนี้".
    private sealed record SlicePerson(string EmpNo, string PosCode, int? Score, PotentialLevel? Potential);

    private static readonly SlicePerson[] Slice =
    {
        new("AD0004", "A06", 92, PotentialLevel.High),   // division director — top-right star
        new("AD0018", "A04", 88, PotentialLevel.High),   // department manager
        new("AD0078", "A03", 84, PotentialLevel.High),   // section head (has a real team below)
        new("AD2375", "A02", 79, PotentialLevel.Medium),
        new("AD2376", "A01", 72, PotentialLevel.Medium),
        new("AD2377", "A02", 90, PotentialLevel.High),
        new("AD2378", "A01", 66, PotentialLevel.Low),
        new("AD2379", "A01", 58, PotentialLevel.Low),
        new("AD2380", "A02", 81, PotentialLevel.Medium),
        new("AD2381", "A01", 74, PotentialLevel.Medium),
        new("AD2382", "A01", 69, PotentialLevel.Low),
        new("AD2383", "A02", 86, PotentialLevel.High),
        new("AD2384", "A02", 77, PotentialLevel.Medium),
        new("AD2385", "A01", 63, PotentialLevel.Low),
        new("AD2386", "A02", 83, PotentialLevel.Medium),
        new("AD2387", "A01", 71, PotentialLevel.Medium),
        new("AD2388", "A01", 55, null),                  // Approved but not yet rated for potential
        new("AD2389", "A02", 89, PotentialLevel.High),
        new("AD2390", "A01", null, PotentialLevel.Medium), // InProgress + rating → "no performance data this period"
        new("AD2392", "A01", null, null),                  // InProgress, no rating → shown nowhere on grid
    };

    // POS_CODE → Thai position title (for slot names + evaluation snapshots).
    private static readonly Dictionary<string, string> PosTitleTh = new()
    {
        ["A01"] = "พนักงาน",
        ["A02"] = "พนักงานอาวุโส",
        ["A03"] = "หัวหน้างาน",
        ["A04"] = "ผู้จัดการแผนก",
        ["A05"] = "ผู้จัดการฝ่าย",
        ["A06"] = "ผู้อำนวยการฝ่าย",
        ["A07"] = "ประธานเจ้าหน้าที่บริหาร",
    };

    private static string GradeFor(decimal pct) =>
        pct >= 90 ? "A" : pct >= 80 ? "B" : pct >= 70 ? "C" : pct >= 60 ? "D" : pct >= 50 ? "E" : "F";

    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<HRMContext>>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(SeederName);
        await using var ctx = await dbFactory.CreateDbContextAsync();

        // ---- One-shot guard -------------------------------------------------
        if (await ctx.Perf_EvaluationPeriods.AnyAsync(p => p.CompanyId == CompanyId))
        {
            logger.LogInformation("ADVD HRD demo data already present (Perf_EvaluationPeriod CompanyId={CompanyId}) — seeder skipped.", CompanyId);
            return;
        }

        // ---- Resolve shared lookups ----------------------------------------
        // An actor user id to stamp on *ByUserId columns (not a DB FK; a stamp
        // only). Prefer the admin login, fall back to any user, then 0.
        var actorUserId =
            (await ctx.sc_users.Where(u => u.loginname == "admin").Select(u => (long?)u.userid).FirstOrDefaultAsync())
            ?? (await ctx.sc_users.Select(u => (long?)u.userid).FirstOrDefaultAsync())
            ?? 0L;

        // ADVD Pos_ExecType: Code (A01..A07) → Id, and its IsBoss flag.
        var execTypes = await ctx.Pos_ExecTypes.Where(t => t.CompanyId == CompanyId).ToListAsync();
        var execTypeIdByCode = execTypes.Where(t => t.Code != null).ToDictionary(t => t.Code!, t => t.Id);
        var execIsBossByCode = execTypes.Where(t => t.Code != null).ToDictionary(t => t.Code!, t => t.IsBoss);

        // ADVD competency catalog: Code → Id (categories are company-scoped).
        var advdCategoryIds = await ctx.Comp_Categories.Where(c => c.CompanyId == CompanyId).Select(c => c.Id).ToListAsync();
        var compIdByCode = await ctx.Comp_Competencies
            .Where(c => advdCategoryIds.Contains(c.CategoryId))
            .ToDictionaryAsync(c => c.Code, c => c.Id);

        // Slice employees (resolve ids by EmpNo — never hard-code Hremployee.id).
        var sliceEmpNos = Slice.Select(s => s.EmpNo).ToList();
        var employees = await ctx.Hremployee
            .Where(e => e.companyid == CompanyId && sliceEmpNos.Contains(e.EmpNo))
            .ToListAsync();
        var empByNo = employees.ToDictionary(e => e.EmpNo);

        // Org names for evaluation snapshots.
        var orgNameByCode = await ctx.com_organizations
            .Where(o => o.comp_code == CompanyId && o.code != null)
            .ToDictionaryAsync(o => o.code!, o => o.name);

        if (employees.Count < sliceEmpNos.Count)
            logger.LogWarning("ADVD HRD demo: only {Found}/{Expected} slice employees found — did DemoCompanySeeder run first?", employees.Count, sliceEmpNos.Count);

        // =====================================================================
        // STEP 0 (prerequisite): active Pos_PositionSlot per slice employee.
        // Bridges Hremployee → Pos_ExecType so IDP gap analysis / grid titles /
        // career path resolve. All Id columns are IDENTITY.
        // =====================================================================
        foreach (var s in Slice)
        {
            if (!empByNo.TryGetValue(s.EmpNo, out var emp)) continue;
            if (!execTypeIdByCode.TryGetValue(s.PosCode, out var execId)) continue;

            var alreadyHasSlot = await ctx.Pos_PositionSlots.AnyAsync(x => x.HremployeeId == emp.id && x.IsActive);
            if (alreadyHasSlot) continue;

            ctx.Pos_PositionSlots.Add(new Pos_PositionSlot
            {
                CompanyId = CompanyId,
                PosCode = emp.EmpNo,                       // human-facing running number (legacy convention)
                PosExecTypeId = execId,
                OrganizationId = emp.OrganizationId,
                Name = PosTitleTh.GetValueOrDefault(s.PosCode, s.PosCode),
                HremployeeId = emp.id,
                EmpNo = emp.EmpNo,
                IsActive = true,
                IsManpower = true,
                IsBoss = execIsBossByCode.GetValueOrDefault(s.PosCode),
                StartDate = AnchorDate,
                CreateDate = Anchor,
                CreateBy = SeederName,
            });
        }
        await ctx.SaveChangesAsync();
        var slotCount = employees.Count;

        // =====================================================================
        // STEP 1: Job_CompetencyRequirements — required levels per Job
        // (Pos_ExecType) so IDP gap analysis has a target to measure against.
        // Mapped for A01/A02/A03/A04 (the slice's positions).
        // =====================================================================
        // (PosCode, CompetencyCode, RequiredLevel, IsCritical)
        var reqDefs = new (string Pos, string Comp, int Level, bool Critical)[]
        {
            // A01 พนักงาน — foundational levels
            ("A01", "INTEGRITY", 2, true), ("A01", "COMMUNICATE", 2, false), ("A01", "TEAMWORK", 2, false),
            ("A01", "QUALITY", 2, false), ("A01", "SAFETY", 2, true), ("A01", "DIGITAL", 2, false),
            // A02 พนักงานอาวุโส
            ("A02", "INTEGRITY", 3, true), ("A02", "COMMUNICATE", 3, false), ("A02", "TEAMWORK", 3, false),
            ("A02", "QUALITY", 3, false), ("A02", "PROBLEM", 3, true), ("A02", "DIGITAL", 3, false),
            // A03 หัวหน้างาน — leadership begins
            ("A03", "COMMUNICATE", 4, false), ("A03", "TEAMWORK", 4, false), ("A03", "LEADERSHIP", 3, true),
            ("A03", "PEOPLE_DEV", 3, false), ("A03", "PLANNING", 3, false), ("A03", "DECISION", 3, false),
            // A04 ผู้จัดการแผนก
            ("A04", "LEADERSHIP", 4, true), ("A04", "DECISION", 4, false), ("A04", "PEOPLE_DEV", 4, false),
            ("A04", "PLANNING", 4, false), ("A04", "DATA", 3, false),
        };
        var reqSortByPos = new Dictionary<string, int>();
        var reqCount = 0;
        foreach (var (pos, comp, level, critical) in reqDefs)
        {
            if (!execTypeIdByCode.TryGetValue(pos, out var execId)) continue;
            if (!compIdByCode.TryGetValue(comp, out var compId)) continue;
            var sort = reqSortByPos.GetValueOrDefault(pos);
            reqSortByPos[pos] = sort + 1;
            ctx.Job_CompetencyRequirements.Add(new Job_CompetencyRequirement
            {
                PosExecTypeId = execId,
                CompetencyId = compId,
                RequiredLevel = level,
                IsCritical = critical,
                SortOrder = sort,
                IsActive = true,
            });
            reqCount++;
        }
        await ctx.SaveChangesAsync();

        // =====================================================================
        // STEP 2: Performance — period + type + topic/subtopic/indicator tree
        // (weights sum to 100 at each level) + FULL A–F grade bands + 1–5
        // rating-scale anchors + rater-direction config + evaluation instances.
        // =====================================================================
        var period = new Perf_EvaluationPeriod
        {
            Code = "PEP-ADVD-01",
            CompanyId = CompanyId,
            Name = "รอบประเมินผลการปฏิบัติงานประจำปี 2569",
            PeriodType = PerfPeriodType.Annual,
            StartDate = new DateOnly(2026, 1, 1),
            EndDate = new DateOnly(2026, 12, 31),
            ScoreDueDate = new DateOnly(2027, 1, 31),
            IsActive = true,
            IsLocked = false,
        };
        ctx.Perf_EvaluationPeriods.Add(period);
        await ctx.SaveChangesAsync();
        var periodId = period.Id;

        var evalType = new Perf_EvaluationType
        {
            CompanyId = CompanyId,
            Code = "ANNUAL",
            Name = "แบบประเมินผลปฏิบัติงานประจำปี",
            NameEn = "Annual Performance Appraisal",
            IsActive = true,
        };
        ctx.Perf_EvaluationTypes.Add(evalType);
        await ctx.SaveChangesAsync();
        var typeId = evalType.Id;

        // Topics (sum = 100)
        var topKpi = new Perf_Topic { EvaluationTypeId = typeId, Code = "KPI", Name = "ผลงานตามเป้าหมาย (KPI)", Weight = 50m, SortOrder = 1, IsActive = true };
        var topComp = new Perf_Topic { EvaluationTypeId = typeId, Code = "COMP", Name = "สมรรถนะ (Competency)", Weight = 30m, SortOrder = 2, IsActive = true };
        var topEthic = new Perf_Topic { EvaluationTypeId = typeId, Code = "ETHIC", Name = "จริยธรรมและวัฒนธรรมองค์กร", Weight = 20m, SortOrder = 3, IsActive = true };
        ctx.Perf_Topics.AddRange(topKpi, topComp, topEthic);
        await ctx.SaveChangesAsync();

        // SubTopics (sum = 100 within each topic)
        var stQty = new Perf_SubTopic { TopicId = topKpi.Id, Code = "QTY", Name = "ปริมาณงานที่สำเร็จ", Weight = 50m, SortOrder = 1, IsActive = true };
        var stQlty = new Perf_SubTopic { TopicId = topKpi.Id, Code = "QLTY", Name = "คุณภาพของงาน", Weight = 50m, SortOrder = 2, IsActive = true };
        var stCore = new Perf_SubTopic { TopicId = topComp.Id, Code = "CORE", Name = "สมรรถนะหลัก (Core)", Weight = 60m, SortOrder = 1, IsActive = true };
        var stFunc = new Perf_SubTopic { TopicId = topComp.Id, Code = "FUNC", Name = "สมรรถนะเฉพาะงาน (Functional)", Weight = 40m, SortOrder = 2, IsActive = true };
        var stDisc = new Perf_SubTopic { TopicId = topEthic.Id, Code = "DISC", Name = "วินัยและการปฏิบัติตามค่านิยม", Weight = 100m, SortOrder = 1, IsActive = true };
        ctx.Perf_SubTopics.AddRange(stQty, stQlty, stCore, stFunc, stDisc);
        await ctx.SaveChangesAsync();

        // Indicators (sum = 100 within each subtopic); some linked to competencies.
        ctx.Perf_Indicators.AddRange(
            new Perf_Indicator { SubTopicId = stQty.Id, Code = "IND-QTY1", Name = "ทำงานได้ตามปริมาณเป้าหมายที่กำหนด", Weight = 100m, IsActive = true },
            new Perf_Indicator { SubTopicId = stQlty.Id, Code = "IND-QLTY1", Name = "ความถูกต้องและความเรียบร้อยของงาน", Weight = 100m, IsActive = true },
            new Perf_Indicator { SubTopicId = stCore.Id, Code = "IND-CORE1", Name = "การสื่อสาร", Weight = 50m, IsActive = true, CompetencyId = compIdByCode.GetValueOrDefault("COMMUNICATE") is long c1 && c1 != 0 ? c1 : null },
            new Perf_Indicator { SubTopicId = stCore.Id, Code = "IND-CORE2", Name = "การทำงานเป็นทีม", Weight = 50m, IsActive = true, CompetencyId = compIdByCode.GetValueOrDefault("TEAMWORK") is long c2 && c2 != 0 ? c2 : null },
            new Perf_Indicator { SubTopicId = stFunc.Id, Code = "IND-FUNC1", Name = "การแก้ปัญหาหน้างาน", Weight = 100m, IsActive = true, CompetencyId = compIdByCode.GetValueOrDefault("PROBLEM") is long c3 && c3 != 0 ? c3 : null },
            new Perf_Indicator { SubTopicId = stDisc.Id, Code = "IND-DISC1", Name = "การปฏิบัติตามระเบียบวินัยและค่านิยมองค์กร", Weight = 100m, IsActive = true });
        await ctx.SaveChangesAsync();

        // FULL A–F grade bands (fixes company 001's A/B-only gap).
        var gradeBands = new[]
        {
            new Perf_GradeBand { EvaluationPeriodId = periodId, Grade = "A", MinPercent = 90m, MaxPercent = 100m, SalaryIncreasePercent = 8m, BonusPercent = 15m, RequiresJustification = true,  SortOrder = 0, IsActive = true },
            new Perf_GradeBand { EvaluationPeriodId = periodId, Grade = "B", MinPercent = 80m, MaxPercent = 89.99m, SalaryIncreasePercent = 6m, BonusPercent = 10m, RequiresJustification = false, SortOrder = 1, IsActive = true },
            new Perf_GradeBand { EvaluationPeriodId = periodId, Grade = "C", MinPercent = 70m, MaxPercent = 79.99m, SalaryIncreasePercent = 4m, BonusPercent = 6m,  RequiresJustification = false, SortOrder = 2, IsActive = true },
            new Perf_GradeBand { EvaluationPeriodId = periodId, Grade = "D", MinPercent = 60m, MaxPercent = 69.99m, SalaryIncreasePercent = 2m, BonusPercent = 3m,  RequiresJustification = false, SortOrder = 3, IsActive = true },
            new Perf_GradeBand { EvaluationPeriodId = periodId, Grade = "E", MinPercent = 50m, MaxPercent = 59.99m, SalaryIncreasePercent = 1m, BonusPercent = 0m,  RequiresJustification = false, SortOrder = 4, IsActive = true },
            new Perf_GradeBand { EvaluationPeriodId = periodId, Grade = "F", MinPercent = 0m,  MaxPercent = 49.99m, SalaryIncreasePercent = 0m, BonusPercent = 0m,  RequiresJustification = true,  SortOrder = 5, IsActive = true },
        };
        ctx.Perf_GradeBands.AddRange(gradeBands);

        // 1–5 rating-scale anchors.
        var scaleText = new[] { "ต้องปรับปรุงอย่างมาก", "ต่ำกว่าความคาดหวัง", "เป็นไปตามความคาดหวัง", "สูงกว่าความคาดหวัง", "ดีเยี่ยม" };
        for (var p = 1; p <= 5; p++)
            ctx.Perf_RatingScaleDescriptions.Add(new Perf_RatingScaleDescription { EvaluationPeriodId = periodId, ScorePoint = p, Description = scaleText[p - 1], IsActive = true });

        // Rater-direction config (mirrors company 001's RDC-0001: self 70% + one superior level 30%).
        ctx.Perf_RaterDirectionConfigs.Add(new Perf_RaterDirectionConfig
        {
            Code = "RDC-ADVD-01",
            CompanyId = CompanyId,
            Name = "ประเมินตนเอง + หัวหน้า 1 ระดับ",
            AllowSelf = true,
            SuperiorLevels = 1,
            SubordinateLevels = 0,
            IncludePeers = false,
            WeightSelf = 70m,
            WeightSuperior = 30m,
            WeightSubordinate = 0m,
            WeightPeer = 0m,
            IsActive = true,
        });
        await ctx.SaveChangesAsync();

        // Evaluation instances for the slice.
        var instanceSeq = 0;
        var approvedInstances = 0;
        var inProgressInstances = 0;
        foreach (var s in Slice)
        {
            if (!empByNo.TryGetValue(s.EmpNo, out var emp)) continue;
            instanceSeq++;
            var isApproved = s.Score is int;
            var scorePct = s.Score is int sc ? (decimal?)sc : null;
            ctx.Perf_EvaluationInstances.Add(new Perf_EvaluationInstance
            {
                InstanceCode = $"PEI-ADVD-{instanceSeq:000}",
                EvaluationPeriodId = periodId,
                EvaluationTypeId = typeId,
                HremployeeId = emp.id,
                SnapshotEmpNo = emp.EmpNo,
                SnapshotEmpName = $"{emp.EmpName} {emp.EmpSurname}",
                SnapshotPositionName = PosTitleTh.GetValueOrDefault(s.PosCode, s.PosCode),
                SnapshotOrganizationCode = emp.orgcode,
                SnapshotOrganizationName = emp.orgcode != null ? orgNameByCode.GetValueOrDefault(emp.orgcode) : null,
                FinalScorePercent = scorePct,
                FinalGrade = scorePct is decimal pct ? GradeFor(pct) : null,
                Status = isApproved ? PerfInstanceStatus.Approved : PerfInstanceStatus.InProgress,
                JobMasterId = null,
                IsMeritApplied = false, // no salary actually raised → leaving the merit flag false (see note in report)
            });
            if (isApproved) approvedInstances++; else inProgressInstances++;
        }
        await ctx.SaveChangesAsync();

        // =====================================================================
        // STEP 3: Talent — potential ratings (same period) + 9-box settings.
        // =====================================================================
        ctx.Talent_NineBoxSettingsList.Add(new Talent_NineBoxSettings
        {
            CompanyId = CompanyId,
            PerformanceLowMaxPercent = 60m,
            PerformanceHighMinPercent = 85m,
        });

        var potentialCount = 0;
        foreach (var s in Slice)
        {
            if (s.Potential is not PotentialLevel level) continue;
            if (!empByNo.TryGetValue(s.EmpNo, out var emp)) continue;
            ctx.Talent_PotentialRatings.Add(new Talent_PotentialRating
            {
                HremployeeId = emp.id,
                EvaluationPeriodId = periodId,
                Level = level,
                RatedByUserId = actorUserId,
                RatedDate = Anchor,
                Note = "ประเมินศักยภาพโดยผู้บริหารสายงาน (ข้อมูลสาธิต)",
            });
            potentialCount++;
        }
        await ctx.SaveChangesAsync();

        // =====================================================================
        // STEP 4: Succession — key positions + successor nominations (mixed
        // readiness, incl. ReadyNow) set directly to Approved so bench strength
        // counts them.
        // =====================================================================
        var kp1 = new Succ_KeyPosition { Code = "SKP-ADVD-01", CompanyId = CompanyId, PosExecTypeId = execTypeIdByCode["A04"], BusinessImpact = BusinessImpactLevel.High, ReplacementDifficulty = ReplacementDifficultyLevel.High, Note = "ผู้จัดการแผนกบัญชี — ควบคุมการปิดงบและกระแสเงินสด", IsActive = true, AddedByUserId = actorUserId, AddedDate = Anchor };
        var kp2 = new Succ_KeyPosition { Code = "SKP-ADVD-02", CompanyId = CompanyId, PosExecTypeId = execTypeIdByCode["A05"], BusinessImpact = BusinessImpactLevel.High, ReplacementDifficulty = ReplacementDifficultyLevel.Medium, Note = "ผู้จัดการฝ่าย — ตำแหน่งบริหารระดับสูงของสายงาน", IsActive = true, AddedByUserId = actorUserId, AddedDate = Anchor };
        var kp3 = new Succ_KeyPosition { Code = "SKP-ADVD-03", CompanyId = CompanyId, PosExecTypeId = execTypeIdByCode["A03"], BusinessImpact = BusinessImpactLevel.Medium, ReplacementDifficulty = ReplacementDifficultyLevel.Medium, Note = "หัวหน้างาน — จุดเชื่อมระหว่างผู้จัดการและพนักงาน", IsActive = true, AddedByUserId = actorUserId, AddedDate = Anchor };
        ctx.Succ_KeyPositions.AddRange(kp1, kp2, kp3);
        await ctx.SaveChangesAsync();

        var nomSeq = 0;
        var nomCount = 0;
        void AddNomination(Succ_KeyPosition kp, string empNo, ReadinessLevel readiness)
        {
            if (!empByNo.TryGetValue(empNo, out var emp)) return;
            nomSeq++;
            ctx.Succ_SuccessorNominations.Add(new Succ_SuccessorNomination
            {
                NominationCode = $"SSN-ADVD-{nomSeq:000}",
                KeyPositionId = kp.Id,
                HremployeeId = emp.id,
                ReadinessLevel = readiness,
                NominatedByUserId = actorUserId,
                NominatedDate = Anchor,
                Note = "เสนอชื่อจากผลการประเมินและศักยภาพ (ข้อมูลสาธิต)",
                IsActive = true,
                Status = SuccessionNominationStatus.Approved, // direct-approved → counts in bench; sync is a no-op on non-Pending rows
                JobMasterId = null,
            });
            nomCount++;
        }
        // KP1 (A04): healthy bench — has a ReadyNow.
        AddNomination(kp1, "AD0078", ReadinessLevel.ReadyNow);
        AddNomination(kp1, "AD2375", ReadinessLevel.Ready1To2Years);
        AddNomination(kp1, "AD2383", ReadinessLevel.Ready3To5Years);
        // KP2 (A05): at-risk — a successor exists but nobody is ReadyNow.
        AddNomination(kp2, "AD0018", ReadinessLevel.Ready1To2Years);
        // KP3 (A03): healthy — a ReadyNow plus one needing development.
        AddNomination(kp3, "AD2377", ReadinessLevel.ReadyNow);
        AddNomination(kp3, "AD2380", ReadinessLevel.DevelopmentNeeded);
        await ctx.SaveChangesAsync();

        // =====================================================================
        // STEP 5: IDP — self+manager competency assessments (so gap computes
        // non-empty) + development plans with 70-20-10 actions.
        // =====================================================================
        // (EmpNo, PosCode) whose position requirements we assess against.
        var idpTargets = new (string EmpNo, string Pos)[] { ("AD0078", "A03"), ("AD2375", "A02"), ("AD2376", "A01") };

        // Deterministic self/manager ratings per (position, competency). Kept a
        // notch below RequiredLevel on a few so the gap analysis shows real gaps.
        // key: $"{pos}:{compCode}" → (self, manager)
        var assessDefs = new Dictionary<string, (int Self, int Mgr)>
        {
            // A03 (AD0078) — requirements COMMUNICATE4/TEAMWORK4/LEADERSHIP3/PEOPLE_DEV3/PLANNING3/DECISION3
            ["A03:COMMUNICATE"] = (4, 4), ["A03:TEAMWORK"] = (4, 3), ["A03:LEADERSHIP"] = (2, 2),
            ["A03:PEOPLE_DEV"] = (3, 3), ["A03:PLANNING"] = (2, 3), ["A03:DECISION"] = (3, 3),
            // A02 (AD2375) — INTEGRITY3/COMMUNICATE3/TEAMWORK3/QUALITY3/PROBLEM3/DIGITAL3
            ["A02:INTEGRITY"] = (3, 3), ["A02:COMMUNICATE"] = (3, 2), ["A02:TEAMWORK"] = (3, 3),
            ["A02:QUALITY"] = (2, 3), ["A02:PROBLEM"] = (2, 2), ["A02:DIGITAL"] = (3, 3),
            // A01 (AD2376) — INTEGRITY2/COMMUNICATE2/TEAMWORK2/QUALITY2/SAFETY2/DIGITAL2
            ["A01:INTEGRITY"] = (2, 2), ["A01:COMMUNICATE"] = (2, 2), ["A01:TEAMWORK"] = (2, 1),
            ["A01:QUALITY"] = (2, 2), ["A01:SAFETY"] = (1, 2), ["A01:DIGITAL"] = (2, 2),
        };
        var assessCount = 0;
        foreach (var (empNo, pos) in idpTargets)
        {
            if (!empByNo.TryGetValue(empNo, out var emp)) continue;
            foreach (var (pc, comp, _, _) in reqDefs.Where(r => r.Pos == pos))
            {
                if (!compIdByCode.TryGetValue(comp, out var compId)) continue;
                if (!assessDefs.TryGetValue($"{pos}:{comp}", out var lv)) continue;
                ctx.Idp_CompetencyAssessments.Add(new Idp_CompetencyAssessment
                {
                    HremployeeId = emp.id, CompetencyId = compId, Source = IdpAssessmentSource.Self,
                    RatedLevel = lv.Self, RatedByUserId = actorUserId, RatedDate = Anchor, Note = "ประเมินตนเอง (ข้อมูลสาธิต)",
                });
                ctx.Idp_CompetencyAssessments.Add(new Idp_CompetencyAssessment
                {
                    HremployeeId = emp.id, CompetencyId = compId, Source = IdpAssessmentSource.Manager,
                    RatedLevel = lv.Mgr, RatedByUserId = actorUserId, RatedDate = Anchor, Note = "ประเมินโดยหัวหน้า (ข้อมูลสาธิต)",
                });
                assessCount++;
            }
        }
        await ctx.SaveChangesAsync();

        // Plans: two Approved (with 70-20-10 actions), one Draft.
        int actionCount = 0;
        var plan1 = new Idp_Plan { PlanNo = "IDP-ADVD-01", HremployeeId = empByNo["AD0078"].id, Year = 2569, Status = IdpPlanStatus.Approved, Summary = "พัฒนาสู่การเป็นผู้จัดการแผนก — เน้นภาวะผู้นำและการวางแผน", JobMasterId = null, CreatedByUserId = actorUserId, CreatedDate = Anchor, SubmittedDate = Anchor, ApprovedDate = Anchor };
        var plan2 = new Idp_Plan { PlanNo = "IDP-ADVD-02", HremployeeId = empByNo["AD2375"].id, Year = 2569, Status = IdpPlanStatus.Approved, Summary = "ยกระดับสมรรถนะการสื่อสารและการแก้ปัญหาหน้างาน", JobMasterId = null, CreatedByUserId = actorUserId, CreatedDate = Anchor, SubmittedDate = Anchor, ApprovedDate = Anchor };
        var plan3 = new Idp_Plan { PlanNo = "IDP-ADVD-03", HremployeeId = empByNo["AD2376"].id, Year = 2569, Status = IdpPlanStatus.Draft, Summary = "แผนพัฒนาพนักงานใหม่ (ฉบับร่าง)", JobMasterId = null, CreatedByUserId = actorUserId, CreatedDate = Anchor, SubmittedDate = null, ApprovedDate = null };
        ctx.Idp_Plans.AddRange(plan1, plan2, plan3);
        var planCount = 3;
        await ctx.SaveChangesAsync();

        void AddAction(Idp_Plan plan, long? compId, string title, string? desc, IdpDevelopmentMethod method, IdpActionStatus status, int sort, DateOnly? target)
        {
            ctx.Idp_DevelopmentActions.Add(new Idp_DevelopmentAction
            {
                PlanId = plan.Id,
                CompetencyId = compId,
                Title = title,
                Description = desc,
                TargetDate = target,
                Method = method,
                Status = status,
                CompletedDate = status == IdpActionStatus.Completed ? Anchor : null,
                SortOrder = sort,
            });
            actionCount++;
        }
        long? Comp(string code) => compIdByCode.TryGetValue(code, out var id) ? id : null;
        // Plan1 (AD0078, A03) — 70-20-10 against leadership/planning gaps.
        AddAction(plan1, Comp("LEADERSHIP"), "รับมอบหมายให้นำโครงการปิดงบประจำไตรมาส", "เรียนรู้จากงานจริงในบทบาทหัวหน้าโครงการ (70%)", IdpDevelopmentMethod.OnTheJob, IdpActionStatus.InProgress, 0, new DateOnly(2026, 11, 30));
        AddAction(plan1, Comp("PLANNING"), "โค้ชชิ่งรายเดือนกับผู้จัดการฝ่าย", "พบผู้จัดการฝ่ายเพื่อรับคำแนะนำการวางแผนงาน (20%)", IdpDevelopmentMethod.Coaching, IdpActionStatus.NotStarted, 1, new DateOnly(2026, 12, 31));
        AddAction(plan1, Comp("LEADERSHIP"), "อบรมหลักสูตรภาวะผู้นำสำหรับหัวหน้างาน", "อบรมทางการ (10%)", IdpDevelopmentMethod.FormalTraining, IdpActionStatus.Completed, 2, new DateOnly(2026, 9, 30));
        // Plan2 (AD2375, A02).
        AddAction(plan2, Comp("COMMUNICATE"), "หมุนเวียนงานประสานงานข้ามแผนก", "ฝึกการสื่อสารในงานจริง (70%)", IdpDevelopmentMethod.OnTheJob, IdpActionStatus.InProgress, 0, new DateOnly(2026, 12, 31));
        AddAction(plan2, Comp("PROBLEM"), "พี่เลี้ยงสอนการวิเคราะห์ปัญหา", "เรียนรู้ผ่านพี่เลี้ยง (20%)", IdpDevelopmentMethod.Coaching, IdpActionStatus.NotStarted, 1, new DateOnly(2027, 1, 31));
        AddAction(plan2, Comp("DIGITAL"), "อบรมการใช้เครื่องมือวิเคราะห์ข้อมูล", "อบรมทางการ (10%)", IdpDevelopmentMethod.FormalTraining, IdpActionStatus.NotStarted, 2, new DateOnly(2027, 2, 28));
        // Plan3 (AD2376, A01) — draft.
        AddAction(plan3, Comp("SAFETY"), "เรียนรู้ขั้นตอนความปลอดภัยหน้างาน", "ทำงานจริงภายใต้การดูแล (70%)", IdpDevelopmentMethod.OnTheJob, IdpActionStatus.NotStarted, 0, new DateOnly(2027, 3, 31));
        AddAction(plan3, null, "ปฐมนิเทศพนักงานใหม่", "หลักสูตรพื้นฐานองค์กร (10%)", IdpDevelopmentMethod.FormalTraining, IdpActionStatus.NotStarted, 1, new DateOnly(2026, 10, 31));
        await ctx.SaveChangesAsync();

        // =====================================================================
        // STEP 6: Career — a Job_Family ladder + lattice transitions.
        // =====================================================================
        var famFinance = new Job_Family { CompanyId = CompanyId, Code = "FIN", Name = "สายงานการเงินและบัญชี", NameEn = "Finance & Accounting", Description = "เส้นทางความก้าวหน้าสายการเงินและบัญชี", IsActive = true };
        ctx.Job_Families.Add(famFinance);
        await ctx.SaveChangesAsync();

        // Ladder A01 → A02 → A03 → A04 under the Finance family.
        var ladder = new[] { "A01", "A02", "A03", "A04" };
        var stepCount = 0;
        for (var i = 0; i < ladder.Length; i++)
        {
            if (!execTypeIdByCode.TryGetValue(ladder[i], out var execId)) continue;
            ctx.Career_PathSteps.Add(new Career_PathStep { CompanyId = CompanyId, JobFamilyId = famFinance.Id, PosExecTypeId = execId, SortOrder = i + 1 });
            stepCount++;
        }

        // Lattice edges (multiple outgoing from A02 demonstrates a real lattice).
        var transitionCount = 0;
        void AddTransition(string from, string to, string? note, int sort)
        {
            if (!execTypeIdByCode.TryGetValue(from, out var f) || !execTypeIdByCode.TryGetValue(to, out var t)) return;
            ctx.Career_PathTransitions.Add(new Career_PathTransition
            {
                CompanyId = CompanyId, FromPosExecTypeId = f, ToPosExecTypeId = t, Note = note, SortOrder = sort,
                IsActive = true, CreatedByUserId = actorUserId, CreatedDate = Anchor,
            });
            transitionCount++;
        }
        AddTransition("A01", "A02", "ปฏิบัติงานอย่างน้อย 2 ปีและผ่านการประเมิน", 1);
        AddTransition("A02", "A03", "ผ่านการประเมินสมรรถนะระดับหัวหน้างาน", 1);
        AddTransition("A02", "A04", "เส้นทางลัดสำหรับผู้มีศักยภาพสูง (fast-track)", 2); // lattice alternative
        AddTransition("A03", "A04", "มีประสบการณ์บริหารทีมอย่างน้อย 3 ปี", 1);
        await ctx.SaveChangesAsync();

        // =====================================================================
        // STEP 7: LMS — ADVD courses linked to ADVD competencies (course id 1
        // ORIENT-001 is company 001-scoped, so we create ADVD-owned courses) +
        // sessions + enrollments (one Completed with a passing quiz attempt).
        // =====================================================================
        var course1 = new Lms_Course
        {
            CompanyId = CompanyId, Code = "ADVD-ORIENT", Title = "ปฐมนิเทศพนักงานใหม่ AdvanceDigital",
            Description = "หลักสูตรปฐมนิเทศ ครอบคลุมค่านิยม ระบบงาน และเครื่องมือดิจิทัลขององค์กร",
            DeliveryType = CourseDeliveryType.Classroom, DurationHours = 8m, InstructorName = "ฝ่ายทรัพยากรบุคคล",
            RequiresApproval = false, PassingScorePercent = 70, CompetencyId = Comp("DIGITAL"), IsActive = true,
        };
        var course2 = new Lms_Course
        {
            CompanyId = CompanyId, Code = "ADVD-LEAD101", Title = "ภาวะผู้นำสำหรับหัวหน้างาน",
            Description = "พัฒนาทักษะภาวะผู้นำ การมอบหมายงาน และการให้ข้อมูลป้อนกลับ",
            DeliveryType = CourseDeliveryType.Online, DurationHours = 12m, InstructorName = "วิทยากรภายนอก",
            RequiresApproval = true, PassingScorePercent = 70, CompetencyId = Comp("LEADERSHIP"), IsActive = true,
        };
        ctx.Lms_Courses.AddRange(course1, course2);
        await ctx.SaveChangesAsync();

        var session1 = new Lms_CourseSession { CourseId = course1.Id, SessionCode = "ADVD-ORIENT-2569-01", StartDate = new DateOnly(2026, 7, 1), EndDate = new DateOnly(2026, 7, 1), Location = "ห้องอบรมสำนักงานใหญ่", MaxSeats = 40, ActualCost = 15000m, Status = CourseSessionStatus.Completed };
        var session2 = new Lms_CourseSession { CourseId = course2.Id, SessionCode = "ADVD-LEAD101-2569-01", StartDate = new DateOnly(2026, 10, 1), EndDate = new DateOnly(2026, 10, 2), OnlineLink = "https://learn.advancedigital.local/lead101", MaxSeats = 25, Status = CourseSessionStatus.Scheduled };
        ctx.Lms_CourseSessions.AddRange(session1, session2);
        await ctx.SaveChangesAsync();

        int enrollCount = 0, quizAttemptCount = 0;
        // Two Completed orientation enrollments with passing quiz attempts.
        var enr1 = new Lms_Enrollment { CourseSessionId = session1.Id, HremployeeId = empByNo["AD2376"].id, Status = EnrollmentStatus.Completed, EnrolledDate = new DateOnly(2026, 6, 20).ToDateTime(TimeOnly.MinValue), RequestedByUserId = actorUserId, JobMasterId = null, ApprovedDate = null, AttendedDate = new DateOnly(2026, 7, 1).ToDateTime(TimeOnly.MinValue), CompletedDate = new DateOnly(2026, 7, 1).ToDateTime(TimeOnly.MinValue), QuizScorePercent = 85m };
        var enr2 = new Lms_Enrollment { CourseSessionId = session1.Id, HremployeeId = empByNo["AD2375"].id, Status = EnrollmentStatus.Completed, EnrolledDate = new DateOnly(2026, 6, 20).ToDateTime(TimeOnly.MinValue), RequestedByUserId = actorUserId, JobMasterId = null, ApprovedDate = null, AttendedDate = new DateOnly(2026, 7, 1).ToDateTime(TimeOnly.MinValue), CompletedDate = new DateOnly(2026, 7, 1).ToDateTime(TimeOnly.MinValue), QuizScorePercent = 78m };
        // One approved (leadership course requires approval) but not yet completed.
        var enr3 = new Lms_Enrollment { CourseSessionId = session2.Id, HremployeeId = empByNo["AD0078"].id, Status = EnrollmentStatus.Approved, EnrolledDate = new DateOnly(2026, 9, 10).ToDateTime(TimeOnly.MinValue), RequestedByUserId = actorUserId, JobMasterId = null, ApprovedDate = Anchor, SourceDevelopmentActionId = null };
        ctx.Lms_Enrollments.AddRange(enr1, enr2, enr3);
        await ctx.SaveChangesAsync();
        enrollCount = 3;

        ctx.Lms_QuizAttempts.Add(new Lms_QuizAttempt { EnrollmentId = enr1.Id, AttemptDate = new DateOnly(2026, 7, 1).ToDateTime(TimeOnly.MinValue), ScorePercent = 85m, IsPassed = true });
        ctx.Lms_QuizAttempts.Add(new Lms_QuizAttempt { EnrollmentId = enr2.Id, AttemptDate = new DateOnly(2026, 7, 1).ToDateTime(TimeOnly.MinValue), ScorePercent = 78m, IsPassed = true });
        await ctx.SaveChangesAsync();
        quizAttemptCount = 2;

        // =====================================================================
        // STEP 8: Engagement — one CLOSED eNPS campaign + one CLOSED Culture
        // campaign, each with anonymous responses (no employee identity).
        // =====================================================================
        // Culture question templates (add-missing by code) so the QuestionBank
        // page and the Culture campaign both have the 4 standard dimensions.
        var cultureDims = SurveyService.CultureDimensions;
        var existingTemplateCodes = await ctx.Eng_QuestionTemplates
            .Where(t => t.CompanyId == CompanyId && t.Code != null)
            .Select(t => t.Code!)
            .ToListAsync();
        var templateCount = 0;
        foreach (var dim in cultureDims)
        {
            if (existingTemplateCodes.Contains(dim.Code)) continue;
            ctx.Eng_QuestionTemplates.Add(new Eng_QuestionTemplate
            {
                Code = dim.Code, CompanyId = CompanyId, Text = dim.Text, QuestionType = Eng_QuestionType.Rating, IsActive = true,
            });
            templateCount++;
        }
        await ctx.SaveChangesAsync();
        var templateIdByCode = await ctx.Eng_QuestionTemplates
            .Where(t => t.CompanyId == CompanyId && t.Code != null)
            .ToDictionaryAsync(t => t.Code!, t => t.Id);

        // ----- eNPS campaign -----
        var enpsCampaign = new Eng_SurveyCampaign
        {
            Code = "ENPS-ADVD-2569Q3", CompanyId = CompanyId, Title = "สำรวจความผูกพันพนักงาน (eNPS) ไตรมาส 3/2569",
            Description = "วัดคะแนน Employee Net Promoter Score ทั่วทั้งองค์กร (ตอบแบบไม่ระบุตัวตน)",
            CampaignType = Eng_CampaignType.ENPS, Status = Eng_CampaignStatus.Closed,
            OpenDate = new DateOnly(2026, 7, 1), CloseDate = new DateOnly(2026, 7, 15),
            InvitedCount = 25, ResponseCount = 0, CreatedByUserId = actorUserId, CreatedDate = Anchor,
        };
        ctx.Eng_SurveyCampaigns.Add(enpsCampaign);
        await ctx.SaveChangesAsync();

        var enpsQ = new Eng_CampaignQuestion { CampaignId = enpsCampaign.Id, SourceTemplateId = null, Text = "โดยรวมท่านพึงพอใจกับองค์กรมากน้อยเพียงใด", QuestionType = Eng_QuestionType.Rating, SortOrder = 1 };
        ctx.Eng_CampaignQuestions.Add(enpsQ);
        await ctx.SaveChangesAsync();

        // Fixed NPS distribution: promoters(>=9)=9, passives(7-8)=6, detractors(<=6)=3 → eNPS = (9-3)*100/18 = +33.3
        var npsScores = new[] { 10, 10, 9, 9, 9, 10, 9, 8, 8, 7, 7, 8, 10, 9, 6, 5, 4, 7 };
        var enpsResponseCount = 0;
        foreach (var nps in npsScores)
        {
            var resp = new Eng_SurveyResponse { CampaignId = enpsCampaign.Id, SubmittedDate = Anchor, NpsScore = nps };
            ctx.Eng_SurveyResponses.Add(resp);
            await ctx.SaveChangesAsync(); // need resp.Id for the answer row
            var rating = nps >= 9 ? 5 : nps >= 7 ? 4 : nps >= 5 ? 3 : nps >= 3 ? 2 : 1;
            ctx.Eng_SurveyAnswers.Add(new Eng_SurveyAnswer { ResponseId = resp.Id, CampaignQuestionId = enpsQ.Id, RatingValue = rating });
            enpsResponseCount++;
        }
        enpsCampaign.ResponseCount = enpsResponseCount;
        await ctx.SaveChangesAsync();

        // ----- Culture campaign -----
        var cultureCampaign = new Eng_SurveyCampaign
        {
            Code = "CULTURE-ADVD-2569", CompanyId = CompanyId, Title = "สำรวจวัฒนธรรมองค์กร ประจำปี 2569",
            Description = "ประเมิน 4 มิติวัฒนธรรมองค์กร (ตอบแบบไม่ระบุตัวตน)",
            CampaignType = Eng_CampaignType.Culture, Status = Eng_CampaignStatus.Closed,
            OpenDate = new DateOnly(2026, 8, 1), CloseDate = new DateOnly(2026, 8, 20),
            InvitedCount = 25, ResponseCount = 0, CreatedByUserId = actorUserId, CreatedDate = Anchor,
        };
        ctx.Eng_SurveyCampaigns.Add(cultureCampaign);
        await ctx.SaveChangesAsync();

        var cultureQuestions = new List<Eng_CampaignQuestion>();
        var cSort = 0;
        foreach (var dim in cultureDims)
        {
            var q = new Eng_CampaignQuestion
            {
                CampaignId = cultureCampaign.Id,
                SourceTemplateId = templateIdByCode.TryGetValue(dim.Code, out var tid) ? tid : null,
                Text = dim.Text, QuestionType = Eng_QuestionType.Rating, SortOrder = ++cSort,
            };
            cultureQuestions.Add(q);
        }
        ctx.Eng_CampaignQuestions.AddRange(cultureQuestions);
        await ctx.SaveChangesAsync();

        // 16 anonymous responses, each rating all 4 dimensions (values cycle
        // 3/4/5 → averages ≈ 4.0, so the OrgHealth culture tile lights up).
        var cultureResponseCount = 0;
        for (var i = 0; i < 16; i++)
        {
            var resp = new Eng_SurveyResponse { CampaignId = cultureCampaign.Id, SubmittedDate = Anchor, NpsScore = null };
            ctx.Eng_SurveyResponses.Add(resp);
            await ctx.SaveChangesAsync();
            for (var q = 0; q < cultureQuestions.Count; q++)
            {
                var val = 3 + ((i + q) % 3); // 3,4,5
                ctx.Eng_SurveyAnswers.Add(new Eng_SurveyAnswer { ResponseId = resp.Id, CampaignQuestionId = cultureQuestions[q].Id, RatingValue = val });
            }
            cultureResponseCount++;
        }
        cultureCampaign.ResponseCount = cultureResponseCount;
        await ctx.SaveChangesAsync();

        logger.LogInformation(
            "ADVD HRD demo seed finished: {Slots} slots, {Reqs} competency requirements, perf period {PeriodCode} ({Approved} approved + {InProgress} in-progress instances), {Potential} potential ratings, 3 key positions + {Noms} nominations, {Assess} assessments, {Plans} IDP plans / {Actions} actions, career ladder {Steps} steps + {Trans} transitions, {Courses} courses / {Enroll} enrollments, eNPS ({EnpsResp} responses) + Culture ({CultResp} responses) campaigns.",
            slotCount, reqCount, period.Code, approvedInstances, inProgressInstances, potentialCount, nomCount, assessCount, planCount, actionCount, stepCount, transitionCount, 2, enrollCount, enpsResponseCount, cultureResponseCount);
    }
}
