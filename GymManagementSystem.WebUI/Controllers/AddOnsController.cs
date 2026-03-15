using GymManagementSystem.Application.DTOs;
using GymManagementSystem.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymManagementSystem.WebUI.Controllers;

[Authorize]
public class AddOnsController : BaseApiController
{
    private readonly IAddOnService _addOnService;
    private readonly ICurrentUserService _currentUserService;

    public AddOnsController(IAddOnService addOnService, ICurrentUserService currentUserService)
    {
        _addOnService = addOnService;
        _currentUserService = currentUserService;
    }

    [HttpPost]
    [Authorize(Policy = "AdminFullAccess")]
    public async Task<ActionResult<ApiResponse<AddOnReadDto>>> Create(CreateAddOnDto dto)
    {
        var result = await _addOnService.CreateAsync(dto);
        return ApiCreated(result, "Add-on created successfully.");
    }

    [HttpGet("me")]
    [Authorize(Policy = "MemberReadOnly")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<AddOnReadDto>>>> GetForMe()
    {
        var memberId = _currentUserService.UserId ?? string.Empty;
        var list = await _addOnService.GetAvailableForMemberAsync(memberId);
        return ApiOk<IReadOnlyList<AddOnReadDto>>(list, "Add-ons retrieved successfully.");
    }

    [HttpPost("purchase")]
    [Authorize(Policy = "MemberReadOnly")]
    public async Task<ActionResult<ApiResponse<InvoiceReadDto>>> Purchase(PurchaseAddOnDto dto)
    {
        var invoice = await _addOnService.PurchaseAsync(dto);
        return ApiOk(invoice, "Add-on purchased successfully.");
    }
}
