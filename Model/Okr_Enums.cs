namespace HRM.Models;

public enum OkrOwnerType { Company = 1, Organization = 2, Employee = 3 }

public enum OkrObjectiveStatus { NotStarted = 1, OnTrack = 2, AtRisk = 3, Completed = 4, Cancelled = 5 }

public enum OkrKeyResultMetricType { Numeric = 1, Percentage = 2, Currency = 3, Milestone = 4 }

public enum OkrConfidenceLevel { OnTrack = 1, AtRisk = 2, OffTrack = 3 }
