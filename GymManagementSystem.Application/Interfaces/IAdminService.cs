using GymManagementSystem.Application.DTOs;

namespace GymManagementSystem.Application.Interfaces;

public interface IAdminService
{
    Task<PaginatedResult<LoginAuditDto>> GetLoginAuditsAsync(int page, int pageSize);
    Task<IReadOnlyList<ActiveSessionDto>> GetActiveSessionsAsync();
    Task<AssignTrainerLookupDto> GetAssignTrainerLookupsAsync();
}

