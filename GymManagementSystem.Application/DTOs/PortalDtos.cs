namespace GymManagementSystem.Application.DTOs;

public class MemberPlanAssignmentDto
{
    public string TrainerId { get; set; } = string.Empty;
    public string TrainerName { get; set; } = string.Empty;
    public string Specialty { get; set; } = string.Empty;
}

public class MemberUpcomingBookingDto
{
    public int WorkoutSessionId { get; set; }
    public DateTime BookingDate { get; set; }
}

public class MemberPlansSnapshotDto
{
    public MemberPlanAssignmentDto? Assignment { get; set; }
    public TrainingPlanDto? TrainingPlan { get; set; }
    public NutritionPlanDto? NutritionPlan { get; set; }
    public List<MemberUpcomingBookingDto> UpcomingBookings { get; set; } = new();
    public List<WorkoutSessionDto> TrainerUpcomingSessions { get; set; } = new();
    public HashSet<int> BookedSessionIds { get; set; } = new();
}

public class TrainerMemberProgressDto
{
    public string MemberId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string MemberCode { get; set; } = string.Empty;
    public int TrainingCompleted { get; set; }
    public int TrainingTotal { get; set; }
    public int NutritionCompleted { get; set; }
    public int NutritionTotal { get; set; }
}

public class SessionAttendanceItemDto
{
    public int MemberSessionId { get; set; }
    public string MemberId { get; set; } = string.Empty;
    public string MemberName { get; set; } = string.Empty;
    public DateTime BookingDate { get; set; }
    public bool Attended { get; set; }
}

public class SessionAttendanceDto
{
    public int SessionId { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime SessionDate { get; set; }
    public List<SessionAttendanceItemDto> Items { get; set; } = new();
}
