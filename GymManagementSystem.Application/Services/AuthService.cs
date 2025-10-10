using GymManagementSystem.Application.Configrations;
using GymManagementSystem.Application.DTOs;
using GymManagementSystem.Application.Interfaces;
using GymManagementSystem.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace GymManagementSystem.Application.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IApplicationDbContext _context;
    private readonly JwtSettings _jwtSettings;

    public AuthService(UserManager<ApplicationUser> userManager, IApplicationDbContext context, IOptions<JwtSettings> jwtSettings)
    {
        _userManager = userManager;
        _context = context;
        _jwtSettings = jwtSettings.Value;
    }

    public async Task<AuthResponseDto> LoginAsync(LoginDto loginDto, string? ipAddress = null, string? userAgent = null)
    {
        var user = await _userManager.FindByEmailAsync(loginDto.Email);
        var loginAudit = new LoginAudit
        {
            UserId = user?.Id ?? string.Empty,
            Email = loginDto.Email,
            IpAddress = ipAddress ?? "Unknown",
            UserAgent = userAgent ?? "Unknown",
            LoginTime = DateTime.UtcNow,
            IsSuccessful = false
        };

        try
        {
            if (user == null || !await _userManager.CheckPasswordAsync(user, loginDto.Password))
            {
                loginAudit.FailureReason = "Invalid email or password";
                _context.LoginAudits.Add(loginAudit);
                await _context.SaveChangesAsync();
                throw new UnauthorizedAccessException("Invalid email or password");
            }

            if (!user.IsActive)
            {
                loginAudit.FailureReason = "Account is deactivated";
                _context.LoginAudits.Add(loginAudit);
                await _context.SaveChangesAsync();
                throw new UnauthorizedAccessException("Account is deactivated");
            }

            // Generate tokens
            var accessToken = await GenerateAccessToken(user);
            var refreshToken = GenerateRefreshToken();

            // Save refresh token to database
            var refreshTokenEntity = new RefreshToken
            {
                UserId = user.Id,
                Token = refreshToken,
                ExpiryTime = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpireDays),
                IsRevoked = false,
                CreatedAt = DateTime.UtcNow,
                IpAddress = ipAddress,
                UserAgent = userAgent
            };

            _context.RefreshTokens.Add(refreshTokenEntity);

            // Update login audit for successful login
            loginAudit.IsSuccessful = true;
            loginAudit.UserId = user.Id;
            loginAudit.JwtTokenId = accessToken;
            loginAudit.RefreshTokenId = refreshToken;

            _context.LoginAudits.Add(loginAudit);
            await _context.SaveChangesAsync();

            var roles = await _userManager.GetRolesAsync(user);

            return new AuthResponseDto
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                AccessTokenExpiration = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpireMinutes),
                RefreshTokenExpiration = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpireDays),
                UserId = user.Id,
                Email = user.Email!,
                FullName = $"{user.FirstName} {user.LastName}",
                Role = roles.FirstOrDefault() ?? "Member"
            };
        }
        catch (Exception)
        {
            if (!_context.LoginAudits.Any(la => la.Id == loginAudit.Id))
            {
                loginAudit.FailureReason = "Unexpected error during login";
                _context.LoginAudits.Add(loginAudit);
                await _context.SaveChangesAsync();
            }
            throw;
        }
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterDto registerDto)
    {
        var existingUser = await _userManager.FindByEmailAsync(registerDto.Email);
        if (existingUser != null)
        {
            throw new ArgumentException("User with this email already exists");
        }

        ApplicationUser user;

        // Create user based on role
        if (registerDto.Role == "Trainer")
        {
            user = new Trainer
            {
                FirstName = registerDto.FirstName,
                LastName = registerDto.LastName,
                Email = registerDto.Email,
                UserName = registerDto.Email,
                PhoneNumber = registerDto.PhoneNumber,
                HireDate = DateTime.UtcNow
            };
        }
        else // Member or other roles
        {
            user = new Member
            {
                FirstName = registerDto.FirstName,
                LastName = registerDto.LastName,
                Email = registerDto.Email,
                UserName = registerDto.Email,
                PhoneNumber = registerDto.PhoneNumber,
                JoinDate = DateTime.UtcNow,
                MemberCode = GenerateMemberCode()
            };
        }

        var result = await _userManager.CreateAsync(user, registerDto.Password);

        if (!result.Succeeded)
        {
            throw new ArgumentException($"User creation failed: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }

        var roleResult = await _userManager.AddToRoleAsync(user, registerDto.Role);
        if (!roleResult.Succeeded)
        {
            
            Console.WriteLine($"Role assignment failed: {string.Join(", ", roleResult.Errors.Select(e => e.Description))}");
        }

        string accessToken = string.Empty;
        string refreshToken = string.Empty;
        try
        {
            accessToken = await GenerateAccessToken(user);
            refreshToken = GenerateRefreshToken();
            
            // Save refresh token to database
            var refreshTokenEntity = new RefreshToken
            {
                UserId = user.Id,
                Token = refreshToken,
                ExpiryTime = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpireDays),
                IsRevoked = false,
                CreatedAt = DateTime.UtcNow
            };

            _context.RefreshTokens.Add(refreshTokenEntity);
            await _context.SaveChangesAsync();
        }
        catch (Exception tokenEx)
        {
            
            Console.WriteLine($"Token generation failed: {tokenEx.Message}");
            accessToken = string.Empty;
            refreshToken = string.Empty;
        }

        var roles = await _userManager.GetRolesAsync(user);

        return new AuthResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            AccessTokenExpiration = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpireMinutes),
            RefreshTokenExpiration = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpireDays),
            UserId = user.Id,
            Email = user.Email!,
            FullName = $"{user.FirstName} {user.LastName}",
            Role = roles.FirstOrDefault() ?? "Member"
        };
    }
    public async Task<bool> ChangePasswordAsync(string userId, string currentPassword, string newPassword)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            throw new ArgumentException("User not found");
        }

        var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
        return result.Succeeded;
    }

    public async Task<TokenRefreshResponseDto> RefreshTokenAsync(string refreshToken, string? ipAddress = null, string? userAgent = null)
    {
        var tokenEntity = await _context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == refreshToken);

        if (tokenEntity == null || tokenEntity.IsRevoked || tokenEntity.ExpiryTime <= DateTime.UtcNow)
        {
            throw new UnauthorizedAccessException("Invalid refresh token");
        }

        var user = await _userManager.FindByIdAsync(tokenEntity.UserId);
        if (user == null || !user.IsActive)
        {
            throw new UnauthorizedAccessException("User not found or inactive");
        }

        // Revoke the old refresh token
        tokenEntity.IsRevoked = true;

        // Generate new tokens
        var newAccessToken = await GenerateAccessToken(user);
        var newRefreshToken = GenerateRefreshToken();

        // Save new refresh token
        var newRefreshTokenEntity = new RefreshToken
        {
            UserId = user.Id,
            Token = newRefreshToken,
            ExpiryTime = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpireDays),
            IsRevoked = false,
            CreatedAt = DateTime.UtcNow,
            IpAddress = ipAddress,
            UserAgent = userAgent
        };

        _context.RefreshTokens.Add(newRefreshTokenEntity);
        await _context.SaveChangesAsync();

        return new TokenRefreshResponseDto
        {
            AccessToken = newAccessToken,
            RefreshToken = newRefreshToken,
            AccessTokenExpiration = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpireMinutes),
            RefreshTokenExpiration = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpireDays)
        };
    }

    public async Task<bool> RevokeRefreshTokenAsync(string refreshToken)
    {
        var tokenEntity = await _context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == refreshToken);

        if (tokenEntity != null)
        {
            tokenEntity.IsRevoked = true;
            await _context.SaveChangesAsync();
            return true;
        }

        return false;
    }

    public async Task LogoutAsync(string userId)
    {
        // Update login audit logout time
        var loginAudit = await _context.LoginAudits
            .Where(la => la.UserId == userId && la.IsSuccessful && la.LogoutTime == null)
            .OrderByDescending(la => la.LoginTime)
            .FirstOrDefaultAsync();

        if (loginAudit != null)
        {
            loginAudit.LogoutTime = DateTime.UtcNow;
        }

        // Revoke all active refresh tokens for this user
        var activeTokens = await _context.RefreshTokens
            .Where(rt => rt.UserId == userId && !rt.IsRevoked && rt.ExpiryTime > DateTime.UtcNow)
            .ToListAsync();

        foreach (var token in activeTokens)
        {
            token.IsRevoked = true;
        }

        await _context.SaveChangesAsync();
    }

    private async Task<string> GenerateAccessToken(ApplicationUser user)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Email, user.Email!),
            new Claim(ClaimTypes.Name, $"{user.FirstName} {user.LastName}"),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        // Add roles to claims
        var roles = await _userManager.GetRolesAsync(user);
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Key));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpireMinutes),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private string GenerateRefreshToken()
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }

    private string GenerateMemberCode()
    {
        return "MEM" + DateTime.Now.ToString("yyyyMMddHHmmss");
    }
}