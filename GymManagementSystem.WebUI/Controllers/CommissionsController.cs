using GymManagementSystem.Application.DTOs;
using GymManagementSystem.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymManagementSystem.WebUI.Controllers;

[Authorize(Policy = "AdminFullAccess")]
public class CommissionsController : BaseApiController
{
    private readonly ICommissionService _commissionService;

    public CommissionsController(ICommissionService commissionService)
    {
        _commissionService = commissionService;
    }

    [HttpGet("unpaid")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<CommissionReadDto>>>> GetUnpaid()
    {
        var list = await _commissionService.GetUnpaidAsync();
        return ApiOk<IReadOnlyList<CommissionReadDto>>(list, "Unpaid commissions retrieved successfully.");
    }

    [HttpPost("{commissionId:int}/mark-paid")]
    public async Task<ActionResult<ApiResponse<CommissionReadDto>>> MarkPaid(int commissionId)
    {
        var updated = await _commissionService.MarkPaidAsync(commissionId);
        return ApiOk(updated, "Commission marked as paid.");
    }

    [HttpGet("metrics")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<TrainerCommissionMetricsDto>>>> Metrics()
    {
        var metrics = await _commissionService.GetTrainerMetricsAsync();
        return ApiOk<IReadOnlyList<TrainerCommissionMetricsDto>>(metrics, "Commission metrics retrieved successfully.");
    }
}
