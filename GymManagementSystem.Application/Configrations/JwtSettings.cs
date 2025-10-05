namespace GymManagementSystem.Application.Configrations
{
    public class JwtSettings
    {
        public string Key { get; set; } = string.Empty;
        public string Issuer { get; set; } = string.Empty;
        public string Audience { get; set; } = string.Empty;
        public int AccessTokenExpireMinutes { get; set; } = 15;
        public int RefreshTokenExpireDays { get; set; } = 7;
        
        // Backward compatibility
        public int ExpireMinutes 
        { 
            get => AccessTokenExpireMinutes; 
            set => AccessTokenExpireMinutes = value; 
        }
    }
}
