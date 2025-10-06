namespace GymManagementSystem.Application.DTOs;

public class CheckInDto
{
    public string MemberId { get; set; } = string.Empty;
}

public class CheckOutDto
{
    public int AttendanceId { get; set; }
}

public class AttendanceDto
{
    public int Id { get; set; }
    public string MemberId { get; set; } = string.Empty;
    public DateTime CheckInTime { get; set; }
    public DateTime? CheckOutTime { get; set; }
    public DateTime Date { get; set; }
}


