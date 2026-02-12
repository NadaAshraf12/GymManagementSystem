using GymManagementSystem.Application.DTOs;
using GymManagementSystem.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymManagementSystem.WebUI.Controllers;

[Authorize]
public class InvoicesController : BaseApiController
{
    private readonly IInvoiceService _invoiceService;
    private readonly ICurrentUserService _currentUserService;

    public InvoicesController(IInvoiceService invoiceService, ICurrentUserService currentUserService)
    {
        _invoiceService = invoiceService;
        _currentUserService = currentUserService;
    }

    [HttpGet("me")]
    [Authorize(Policy = "MemberReadOnly")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<InvoiceReadDto>>>> Me()
    {
        var memberId = _currentUserService.UserId ?? string.Empty;
        var invoices = await _invoiceService.GetMemberInvoicesAsync(memberId);
        return ApiOk<IReadOnlyList<InvoiceReadDto>>(invoices, "Invoices retrieved successfully.");
    }

    [HttpGet("member/{memberId}")]
    [Authorize(Policy = "AdminFullAccess")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<InvoiceReadDto>>>> ByMember(string memberId)
    {
        var invoices = await _invoiceService.GetMemberInvoicesAsync(memberId);
        return ApiOk<IReadOnlyList<InvoiceReadDto>>(invoices, "Invoices retrieved successfully.");
    }

    [HttpGet("{invoiceId:int}/download")]
    public async Task<IActionResult> Download(int invoiceId)
    {
        var file = await _invoiceService.DownloadAsync(invoiceId);
        return File(file.Content, file.ContentType, file.FileName);
    }
}
