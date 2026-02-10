namespace GymManagementSystem.Application.DTOs;

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }
    public List<string> Errors { get; set; } = new();
    public int StatusCode { get; set; }
    public string? CorrelationId { get; set; }

    public static ApiResponse<T> Ok(T data, string message = "Success", int statusCode = 200)
    {
        return new ApiResponse<T>
        {
            Success = true,
            Message = message,
            Data = data,
            StatusCode = statusCode
        };
    }

    public static ApiResponse<T> Fail(string message, int statusCode, IEnumerable<string>? errors = null)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Message = message,
            Errors = errors?.ToList() ?? new List<string>(),
            StatusCode = statusCode
        };
    }
}
