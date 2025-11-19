namespace GymManagementSystem.Application.DTOs
{
    public class AuthResponseDto
    {
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public DateTime AccessTokenExpiration { get; set; }
        public DateTime RefreshTokenExpiration { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public bool MustChangePassword { get; set; }
        
        public string Token 
        { 
            get => AccessToken; 
            set => AccessToken = value; 
        }
        
        public DateTime Expiration 
        { 
            get => AccessTokenExpiration; 
            set => AccessTokenExpiration = value; 
        }
    }
}
