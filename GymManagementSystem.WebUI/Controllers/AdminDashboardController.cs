using GymManagementSystem.Application.DTOs;
using GymManagementSystem.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymManagementSystem.WebUI.Controllers;

[ApiController]
[Route("api/admin/dashboard")]
[Authorize(Policy = "AdminFullAccess")]
public class AdminDashboardController : BaseApiController
{
    private readonly IRevenueMetricsService _revenueMetricsService;

    public AdminDashboardController(IRevenueMetricsService revenueMetricsService)
    {
        _revenueMetricsService = revenueMetricsService;
    }

    [HttpGet("metrics")]
    public async Task<ActionResult<ApiResponse<RevenueMetricsDto>>> GetMetrics([FromQuery] int? branchId, CancellationToken cancellationToken)
    {
        var metrics = await _revenueMetricsService.GetDashboardMetricsAsync(branchId, cancellationToken);
        return ApiOk(metrics, "Dashboard metrics retrieved successfully.");
    }

    [HttpGet("metrics/by-branch-type")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<RevenueByBranchTypeDto>>>> GetRevenueByBranchType(CancellationToken cancellationToken)
    {
        var data = await _revenueMetricsService.GetRevenuePerBranchByTypeAsync(cancellationToken);
        return ApiOk<IReadOnlyList<RevenueByBranchTypeDto>>(data, "Revenue by branch/type retrieved successfully.");
    }
}
