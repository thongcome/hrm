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
