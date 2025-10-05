using GymManagementSystem.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GymManagementSystem.Application.Interfaces
{
    public interface IAuthService
    {
        Task<AuthResponseDto> LoginAsync(LoginDto loginDto, string? ipAddress = null, string? userAgent = null);
        Task<AuthResponseDto> RegisterAsync(RegisterDto registerDto);
        Task<bool> ChangePasswordAsync(string userId, string currentPassword, string newPassword);
        Task<TokenRefreshResponseDto> RefreshTokenAsync(string refreshToken, string? ipAddress = null, string? userAgent = null);
        Task<bool> RevokeRefreshTokenAsync(string refreshToken);
        Task LogoutAsync(string userId);
    }
}
