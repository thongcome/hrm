namespace HRM.Models;

public enum RequisitionType { Replacement = 1, NewHeadcount = 2 }
public enum RequisitionStatus { Draft = 1, PendingApproval = 2, Approved = 3, Rejected = 4, Filled = 5, Cancelled = 6 }
public enum PostingStatus { Draft = 1, Open = 2, Closed = 3, Filled = 4, Cancelled = 5 }
public enum ApplicationStage { Applied = 1, Screening = 2, Interview = 3, Offer = 4, Hired = 5, Rejected = 6, Withdrawn = 7 }
public enum InterviewStatus { Scheduled = 1, Completed = 2, Cancelled = 3, NoShow = 4 }
public enum InterviewRecommendation { StrongHire = 1, Hire = 2, NoHire = 3, StrongNoHire = 4 }
public enum OfferStatus { Draft = 1, PendingApproval = 2, Sent = 3, Accepted = 4, Declined = 5, Expired = 6, Withdrawn = 7 }
