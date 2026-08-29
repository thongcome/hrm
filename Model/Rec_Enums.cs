namespace HRM.Models;

public enum RequisitionType { Replacement = 1, NewHeadcount = 2 }
public enum RequisitionStatus { Draft = 1, PendingApproval = 2, Approved = 3, Rejected = 4, Filled = 5, Cancelled = 6 }
public enum PostingStatus { Draft = 1, Open = 2, Closed = 3, Filled = 4, Cancelled = 5 }
public enum ApplicationStage { Applied = 1, Screening = 2, Interview = 3, Offer = 4, Hired = 5, Rejected = 6, Withdrawn = 7 }
public enum InterviewStatus { Scheduled = 1, Completed = 2, Cancelled = 3, NoShow = 4 }
public enum InterviewRecommendation { StrongHire = 1, Hire = 2, NoHire = 3, StrongNoHire = 4 }
// PendingHireApproval: a second, separate approval gate after the candidate
// has accepted the offer — gates RecOfferService.ConfirmHireAsync (the step
// that actually creates the Hremployee row), distinct from the earlier
// OFFER_APPROVAL workflow that only approved the offer's terms.
public enum OfferStatus { Draft = 1, PendingApproval = 2, Sent = 3, Accepted = 4, Declined = 5, Expired = 6, Withdrawn = 7, PendingHireApproval = 8 }

// Recorded on Hrd_EmploymentHistory rows where OrderType = "กลับเข้าทำงาน" —
// which choice HR made for how the returning employee's Hremployee.WorkDate
// gets treated. See Services/Pay/EmployeeRehireService.cs.
public enum TenureTreatment { Continuous = 1, Reset = 2 }

// Services/Shared/EmployeePositionSync.cs writes one Pos_PositionSlot_his
// row whenever a slot's occupant actually changes.
public enum PositionSlotChangeType { Appointment = 1, Transfer = 2, Vacate = 3 }
