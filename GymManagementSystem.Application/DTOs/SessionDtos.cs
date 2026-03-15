namespace GymManagementSystem.Application.DTOs;

public class CreateWorkoutSessionDto
{
    public string TrainerId { get; set; } = string.Empty;
    public int? BranchId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime SessionDate { get; set; }
    public decimal Price { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public int MaxParticipants { get; set; }
}

public class UpdateWorkoutSessionDto
{
    public int Id { get; set; }
    public int? BranchId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime SessionDate { get; set; }
    public decimal Price { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public int MaxParticipants { get; set; }
}

public class WorkoutSessionDto
{
    public int Id { get; set; }
    public string TrainerId { get; set; } = string.Empty;
    public int? BranchId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime SessionDate { get; set; }
    public decimal Price { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public int MaxParticipants { get; set; }
    public int CurrentParticipants { get; set; }
}

public class SessionDto
{
    public int Id { get; set; }
    public string TrainerId { get; set; } = string.Empty;
    public int? BranchId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime SessionDate { get; set; }
    public decimal Price { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public int MaxParticipants { get; set; }
    public int CurrentParticipants { get; set; }
}

public class BookMemberToSessionDto
{
    public string MemberId { get; set; } = string.Empty;
    public int WorkoutSessionId { get; set; }
}

public class PaidSessionBookingDto
{
    public string MemberId { get; set; } = string.Empty;
    public int WorkoutSessionId { get; set; }
}

public class SessionBookingResultDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public decimal WalletBalance { get; set; }
    public decimal OriginalPrice { get; set; }
    public decimal ChargedPrice { get; set; }
    public bool UsedIncludedSession { get; set; }
    public decimal DiscountPercentageApplied { get; set; }
    public bool PriorityBookingApplied { get; set; }
}

public class SessionPricingPreviewDto
{
    public int WorkoutSessionId { get; set; }
    public decimal OriginalPrice { get; set; }
    public decimal FinalPrice { get; set; }
    public int RemainingIncludedSessionsThisMonth { get; set; }
    public decimal DiscountPercentage { get; set; }
    public bool PriorityBookingEnabled { get; set; }
    public bool IsIncludedSessionApplied { get; set; }
}

public class MembershipBenefitsSnapshotDto
{
    public bool HasActiveMembership { get; set; }
    public string ActivePlanName { get; set; } = string.Empty;
    public int IncludedSessionsPerMonth { get; set; }
    public int UsedIncludedSessionsThisMonth { get; set; }
    public int RemainingIncludedSessionsThisMonth { get; set; }
    public decimal SessionDiscountPercentage { get; set; }
    public bool PriorityBooking { get; set; }
    public bool AddOnAccess { get; set; }
}


