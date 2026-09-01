namespace HRM.Models;

// Dual-ladder career track — separating Individual Contributor growth from
// Managerial growth is the single biggest thing that makes a job architecture
// feel professional (you don't have to become a manager to advance).
public enum CareerTrack
{
    IndividualContributor = 1,
    Managerial = 2,
}

// The three globally recognized competency groupings: Core (everyone in the
// company), Leadership (people-manager roles), Functional (job-family specific).
public enum CompetencyCategoryType
{
    Core = 1,
    Leadership = 2,
    Functional = 3,
}

// Type of a Job_ProfileQualification line item (structured JD, CEO order
// 2026-09-01). Other = 9 leaves room for future specific types without
// renumbering.
public enum JobQualificationType
{
    Education = 0,
    ExperienceYears = 1,
    License = 2,
    Skill = 3,
    Other = 9,
}
