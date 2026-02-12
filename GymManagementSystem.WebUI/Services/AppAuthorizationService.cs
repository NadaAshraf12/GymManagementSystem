using GymManagementSystem.Application.Exceptions;
using GymManagementSystem.Application.Interfaces;

namespace GymManagementSystem.WebUI.Services;

public class AppAuthorizationService : IAppAuthorizationService
{
    private const string AdminRole = "Admin";
    private const string TrainerRole = "Trainer";
    private const string MemberRole = "Member";

    private readonly ICurrentUserService _currentUser;

    public AppAuthorizationService(ICurrentUserService currentUser)
    {
        _currentUser = currentUser;
    }

    public Task EnsureAdminFullAccessAsync()
    {
        if (_currentUser.IsInRole(AdminRole))
        {
            return Task.CompletedTask;
        }

        throw new UnauthorizedException("Admin access required.");
    }

    public Task EnsureTrainerOwnsResourceAsync(string trainerId)
    {
        if (_currentUser.IsInRole(AdminRole))
        {
            return Task.CompletedTask;
        }

        if (_currentUser.IsInRole(TrainerRole) && !string.IsNullOrWhiteSpace(_currentUser.UserId) && _currentUser.UserId == trainerId)
        {
            return Task.CompletedTask;
        }

        throw new UnauthorizedException("You do not have permission to access this resource.");
    }

    public Task EnsureMemberReadOnlyAsync()
    {
        if (_currentUser.IsInRole(MemberRole))
        {
            return Task.CompletedTask;
        }

        throw new UnauthorizedException("Member access required.");
    }

    public Task EnsureMemberOwnsResourceAsync(string memberId)
    {
        if (_currentUser.IsInRole(AdminRole))
        {
            return Task.CompletedTask;
        }

        if (_currentUser.IsInRole(MemberRole) && !string.IsNullOrWhiteSpace(_currentUser.UserId) && _currentUser.UserId == memberId)
        {
            return Task.CompletedTask;
        }

        throw new UnauthorizedException("You do not have permission to access this member resource.");
    }
}
