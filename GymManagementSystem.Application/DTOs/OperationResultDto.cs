namespace GymManagementSystem.Application.DTOs;

public class OperationResultDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;

    public static OperationResultDto Ok(string message) => new() { Success = true, Message = message };
    public static OperationResultDto Fail(string message) => new() { Success = false, Message = message };
}

