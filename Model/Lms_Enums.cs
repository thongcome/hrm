namespace HRM.Models;

public enum CourseDeliveryType { Classroom = 1, Online = 2, Hybrid = 3 }

public enum CourseSessionStatus { Scheduled = 1, Ongoing = 2, Completed = 3, Cancelled = 4 }

public enum EnrollmentStatus { PendingApproval = 1, Approved = 2, Rejected = 3, Attended = 4, Completed = 5, NoShow = 6, Cancelled = 7 }

public enum TrainingNeedStatus { Requested = 1, Planned = 2, Fulfilled = 3, Cancelled = 4 }
