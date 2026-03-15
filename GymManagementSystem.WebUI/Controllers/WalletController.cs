using GymManagementSystem.Application.DTOs;
using GymManagementSystem.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace GymManagementSystem.WebUI.Controllers;

[Authorize]
public class WalletController : BaseApiController
{
    private readonly IMembershipService _membershipService;
    private readonly IWalletService _walletService;
    private readonly ICurrentUserService _currentUserService;

    public WalletController(IMembershipService membershipService, IWalletService walletService, ICurrentUserService currentUserService)
    {
        _membershipService = membershipService;
        _walletService = walletService;
        _currentUserService = currentUserService;
    }

    [HttpGet("me")]
    [Authorize(Policy = "MemberReadOnly")]
    public async Task<ActionResult<ApiResponse<WalletBalanceDto>>> Me()
    {
        var memberId = _currentUserService.UserId ?? string.Empty;
        var balance = await _membershipService.GetWalletBalanceAsync(memberId);
        return ApiOk(balance, "Wallet balance retrieved successfully.");
    }

    [HttpGet("{memberId}")]
    [Authorize(Policy = "AdminFullAccess")]
    public async Task<ActionResult<ApiResponse<WalletBalanceDto>>> GetByMemberId(string memberId)
    {
        var balance = await _membershipService.GetWalletBalanceAsync(memberId);
        return ApiOk(balance, "Wallet balance retrieved successfully.");
    }

    [HttpPost("adjust")]
    [Authorize(Policy = "AdminFullAccess")]
    [EnableRateLimiting("wallet-adjust")]
    public async Task<ActionResult<ApiResponse<WalletBalanceDto>>> Adjust(AdjustWalletDto dto)
    {
        var balance = await _membershipService.AdjustWalletAsync(dto);
        return ApiOk(balance, "Wallet adjusted successfully.");
    }

    [HttpPost("topup")]
    [Authorize(Policy = "AdminFullAccess")]
    [EnableRateLimiting("wallet-adjust")]
    public async Task<ActionResult<ApiResponse<WalletBalanceDto>>> TopUp(AdminWalletTopUpDto dto)
    {
        var balance = await _walletService.AdminTopUpWalletAsync(dto);
        return ApiOk(balance, "Wallet topped up successfully.");
    }

    [HttpGet("transactions/{memberId}")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<WalletTransactionReadDto>>>> Transactions(string memberId)
    {
        var transactions = await _membershipService.GetWalletTransactionsAsync(memberId);
        return ApiOk<IReadOnlyList<WalletTransactionReadDto>>(transactions, "Wallet transactions retrieved successfully.");
    }

    [HttpGet("trainer/{memberId}")]
    [Authorize(Policy = "TrainerOwnsResource")]
    public async Task<ActionResult<ApiResponse<WalletBalanceDto>>> GetForTrainerAssignedMember(string memberId)
    {
        var balance = await _membershipService.GetWalletBalanceAsync(memberId);
        return ApiOk(balance, "Wallet balance retrieved successfully.");
    }

    [HttpPost("use-for-session")]
    [Authorize(Policy = "MemberReadOnly")]
    public async Task<ActionResult<ApiResponse<WalletBalanceDto>>> UseForSession(UseWalletForSessionBookingDto dto)
    {
        var balance = await _membershipService.UseWalletForSessionBookingAsync(dto);
        return ApiOk(balance, "Wallet balance deducted for session booking.");
    }
}
