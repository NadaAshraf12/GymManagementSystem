namespace GymManagementSystem.Application.Interfaces;

public interface IAppAuthorizationService
{
    Task EnsureAdminFullAccessAsync();
    Task EnsureTrainerOwnsResourceAsync(string trainerId);
    Task EnsureMemberReadOnlyAsync();
}