namespace HRM.Models;

public enum IdpAssessmentSource { Self = 1, Manager = 2 }

public enum IdpActionStatus { NotStarted = 1, InProgress = 2, Completed = 3, Cancelled = 4 }

public enum IdpPlanStatus { Draft = 1, PendingApproval = 2, Approved = 3, Rejected = 4 }
