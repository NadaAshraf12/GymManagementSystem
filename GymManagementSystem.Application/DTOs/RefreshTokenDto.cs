namespace GymManagementSystem.Application.DTOs;

public class RefreshTokenDto
{
    public string RefreshToken { get; set; } = string.Empty;
}

public class TokenRefreshResponseDto
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime AccessTokenExpiration { get; set; }
    public DateTime RefreshTokenExpiration { get; set; }
}