using GymManagementSystem.Application.DTOs;
using GymManagementSystem.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace GymManagementSystem.WebUI.Controllers;

[Authorize]
public class MembershipsController : BaseApiController
{
    private readonly IMembershipService _membershipService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IWebHostEnvironment _environment;

    public MembershipsController(
        IMembershipService membershipService,
        ICurrentUserService currentUserService,
        IWebHostEnvironment environment)
    {
        _membershipService = membershipService;
        _currentUserService = currentUserService;
        _environment = environment;
    }

    [HttpPost("subscribe")]
    [Authorize(Policy = "MemberReadOnly")]
    public async Task<ActionResult<ApiResponse<MembershipReadDto>>> Subscribe(RequestSubscriptionDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.MemberId))
        {
            dto.MemberId = _currentUserService.UserId ?? string.Empty;
        }
        var result = await _membershipService.RequestSubscriptionAsync(dto);
        return ApiCreated(result, "Subscription request created successfully.");
    }

    [HttpPost("{membershipId:int}/confirm")]
    [Authorize(Policy = "AdminFullAccess")]
    [EnableRateLimiting("payment-review")]
    public async Task<ActionResult<ApiResponse<MembershipReadDto>>> ConfirmMembership(int membershipId)
    {
        var result = await _membershipService.ActivatePendingMembershipAsync(membershipId);
        return ApiOk(result, "Membership activated successfully.");
    }

    [HttpPost("direct-create")]
    [Authorize(Policy = "AdminFullAccess")]
    public async Task<ActionResult<ApiResponse<MembershipReadDto>>> DirectCreate(CreateDirectMembershipDto dto)
    {
        var result = await _membershipService.CreateDirectMembershipAsync(dto);
        return ApiCreated(result, "Direct membership created successfully.");
    }

    [HttpPost("subscribe/online")]
    [Authorize(Policy = "MemberReadOnly")]
    public async Task<ActionResult<ApiResponse<MembershipReadDto>>> SubscribeOnline(CreateMembershipDto dto)
    {
        var result = await _membershipService.RequestSubscriptionAsync(new RequestSubscriptionDto
        {
            MemberId = dto.MemberId,
            BranchId = dto.BranchId,
            MembershipPlanId = dto.MembershipPlanId,
            StartDate = dto.StartDate,
            AutoRenewEnabled = dto.AutoRenewEnabled,
            PaymentAmount = dto.PaymentAmount,
            WalletAmountToUse = dto.WalletAmountToUse,
            PaymentMethod = dto.PaymentMethod
        });
        return ApiCreated(result, "Online membership request created successfully.");
    }

    [HttpPost("subscribe/ingym")]
    [Authorize(Policy = "AdminFullAccess")]
    public async Task<ActionResult<ApiResponse<MembershipReadDto>>> SubscribeInGym(CreateMembershipDto dto)
    {
        var result = await _membershipService.CreateDirectMembershipAsync(new CreateDirectMembershipDto
        {
            MemberId = dto.MemberId,
            BranchId = dto.BranchId,
            MembershipPlanId = dto.MembershipPlanId,
            StartDate = dto.StartDate,
            AutoRenewEnabled = dto.AutoRenewEnabled,
            PaymentAmount = dto.PaymentAmount,
            WalletAmountToUse = dto.WalletAmountToUse,
            PaymentMethod = dto.PaymentMethod
        });
        return ApiCreated(result, "In-gym membership created successfully.");
    }

    [HttpPost("upgrade")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<MembershipReadDto>>> Upgrade(UpgradeMembershipDto dto)
    {
        var result = await _membershipService.UpgradeMembershipAsync(dto);
        return ApiOk(result, "Membership upgraded successfully.");
    }

    [HttpPost("payments/{paymentId:int}/confirm")]
    [Authorize(Policy = "AdminFullAccess")]
    [EnableRateLimiting("payment-review")]
    public async Task<ActionResult<ApiResponse<MembershipReadDto>>> ConfirmPayment(int paymentId, ConfirmPaymentDto dto)
    {
        var result = await _membershipService.ConfirmPaymentAsync(paymentId, dto);
        return ApiOk(result, "Payment confirmed successfully.");
    }

    [HttpPost("payments/{paymentId:int}/reject")]
    [Authorize(Policy = "AdminFullAccess")]
    [EnableRateLimiting("payment-review")]
    public async Task<ActionResult<ApiResponse<MembershipReadDto>>> RejectPayment(int paymentId)
    {
        var result = await _membershipService.RejectPaymentAsync(paymentId);
        return ApiOk(result, "Payment rejected successfully.");
    }

    [HttpPost("payments/{paymentId:int}/proof")]
    [Authorize(Policy = "MemberReadOnly")]
    public async Task<ActionResult<ApiResponse<PaymentReadDto>>> UploadPaymentProof(int paymentId, IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return ApiBadRequest<PaymentReadDto>("Receipt file is required.");
        }

        var uploadsDir = Path.Combine(_environment.WebRootPath ?? "wwwroot", "uploads", "payment-proofs");
        Directory.CreateDirectory(uploadsDir);

        var extension = Path.GetExtension(file.FileName);
        var fileName = $"{Guid.NewGuid():N}_{DateTime.UtcNow:yyyyMMddHHmmss}{extension}";
        var filePath = Path.Combine(uploadsDir, fileName);

        await using (var stream = System.IO.File.Create(filePath))
        {
            await file.CopyToAsync(stream);
        }

        var relativePath = $"/uploads/payment-proofs/{fileName}";
        var result = await _membershipService.UploadPaymentProofAsync(new UploadPaymentProofDto
        {
            PaymentId = paymentId,
            PaymentProofUrl = relativePath
        });

        return ApiOk(result, "Payment proof uploaded successfully.");
    }

    [HttpGet("payments/pending")]
    [Authorize(Policy = "AdminFullAccess")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<PendingPaymentReadDto>>>> PendingPayments()
    {
        var pending = await _membershipService.GetPendingPaymentsAsync();
        return ApiOk<IReadOnlyList<PendingPaymentReadDto>>(pending, "Pending payments retrieved successfully.");
    }

    [HttpPost("payments/{paymentId:int}/review")]
    [Authorize(Policy = "AdminFullAccess")]
    [EnableRateLimiting("payment-review")]
    public async Task<ActionResult<ApiResponse<MembershipReadDto>>> ReviewPayment(int paymentId, ReviewPaymentDto dto)
    {
        var result = await _membershipService.ReviewPaymentAsync(paymentId, dto);
        return ApiOk(result, dto.Approve ? "Payment approved successfully." : "Payment rejected successfully.");
    }

    [HttpGet("my")]
    [Authorize(Policy = "MemberReadOnly")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<MembershipReadDto>>>> MyMemberships()
    {
        var memberId = _currentUserService.UserId ?? string.Empty;
        var memberships = await _membershipService.GetMembershipsForMemberAsync(memberId);
        return ApiOk<IReadOnlyList<MembershipReadDto>>(memberships, "Memberships retrieved successfully.");
    }

    [HttpGet("member/{memberId}")]
    [Authorize(Policy = "TrainerOwnsResource")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<MembershipReadDto>>>> MemberMemberships(string memberId)
    {
        var memberships = await _membershipService.GetMembershipsForMemberAsync(memberId);
        return ApiOk<IReadOnlyList<MembershipReadDto>>(memberships, "Memberships retrieved successfully.");
    }

    [HttpGet("wallet/my")]
    [Authorize(Policy = "MemberReadOnly")]
    public async Task<ActionResult<ApiResponse<WalletBalanceDto>>> MyWallet()
    {
        var memberId = _currentUserService.UserId ?? string.Empty;
        var wallet = await _membershipService.GetWalletBalanceAsync(memberId);
        return ApiOk(wallet, "Wallet balance retrieved successfully.");
    }

    [HttpGet("wallet/{memberId}")]
    [Authorize(Policy = "AdminFullAccess")]
    public async Task<ActionResult<ApiResponse<WalletBalanceDto>>> MemberWallet(string memberId)
    {
        var wallet = await _membershipService.GetWalletBalanceAsync(memberId);
        return ApiOk(wallet, "Wallet balance retrieved successfully.");
    }

    [HttpPost("wallet/adjust")]
    [Authorize(Policy = "AdminFullAccess")]
    [EnableRateLimiting("wallet-adjust")]
    public async Task<ActionResult<ApiResponse<WalletBalanceDto>>> AdjustWallet(AdjustWalletDto dto)
    {
        var wallet = await _membershipService.AdjustWalletAsync(dto);
        return ApiOk(wallet, "Wallet adjusted successfully.");
    }

    [HttpPost("wallet/use-for-session")]
    [Authorize(Policy = "MemberReadOnly")]
    public async Task<ActionResult<ApiResponse<WalletBalanceDto>>> UseWalletForSession(UseWalletForSessionBookingDto dto)
    {
        var wallet = await _membershipService.UseWalletForSessionBookingAsync(dto);
        return ApiOk(wallet, "Wallet balance deducted for session booking.");
    }

    [HttpPost("{membershipId:int}/freeze")]
    [Authorize(Policy = "AdminFullAccess")]
    public async Task<ActionResult<ApiResponse<MembershipReadDto>>> Freeze(int membershipId, FreezeMembershipDto dto)
    {
        var membership = await _membershipService.FreezeMembershipAsync(membershipId, dto);
        return ApiOk(membership, "Membership frozen successfully.");
    }

    [HttpPost("{membershipId:int}/resume")]
    [Authorize(Policy = "AdminFullAccess")]
    public async Task<ActionResult<ApiResponse<MembershipReadDto>>> Resume(int membershipId)
    {
        var membership = await _membershipService.ResumeMembershipAsync(membershipId);
        return ApiOk(membership, "Membership resumed successfully.");
    }
}
