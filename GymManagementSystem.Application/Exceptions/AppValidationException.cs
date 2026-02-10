namespace GymManagementSystem.Application.Exceptions;

public class AppValidationException : AppException
{
    public AppValidationException(string message) : base(message)
    {
    }
}