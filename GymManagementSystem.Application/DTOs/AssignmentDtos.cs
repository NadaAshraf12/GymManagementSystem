namespace GymManagementSystem.Application.DTOs;

public class AssignTrainerDto
{
    public string TrainerId { get; set; } = string.Empty;
    public string MemberId { get; set; } = string.Empty;
    public string? Notes { get; set; }
}

public class TrainerAssignmentDto
{
    public int Id { get; set; }
    public string TrainerId { get; set; } = string.Empty;
    public string MemberId { get; set; } = string.Empty;
    public DateTime AssignedAt { get; set; }
    public string? Notes { get; set; }
}


