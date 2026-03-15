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

    [HttpGet("financial-overview")]
    public async Task<ActionResult<ApiResponse<FinancialOverviewDto>>> GetFinancialOverview([FromQuery] int? branchId, CancellationToken cancellationToken)
    {
        var data = await _revenueMetricsService.GetFinancialOverviewAsync(branchId, cancellationToken);
        return ApiOk(data, "Financial overview retrieved successfully.");
    }

    [HttpGet("top-plans")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<TopSellingMembershipPlanDto>>>> GetTopPlans([FromQuery] int top = 5, [FromQuery] int? branchId = null, CancellationToken cancellationToken = default)
    {
        var data = await _revenueMetricsService.GetTopPlansAsync(top, branchId, cancellationToken);
        return ApiOk<IReadOnlyList<TopSellingMembershipPlanDto>>(data, "Top plans retrieved successfully.");
    }
}
