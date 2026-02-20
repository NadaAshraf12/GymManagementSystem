using GymManagementSystem.Application.DTOs;
using GymManagementSystem.Application.Interfaces;
using GymManagementSystem.Domain.Enums;
using GymManagementSystem.WebUI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymManagementSystem.WebUI.Controllers;

[Authorize(Roles = "Member,Admin")]
public class PortalController : BaseController
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IMembershipService _membershipService;
    private readonly IMembershipPlanService _membershipPlanService;
    private readonly IInvoiceService _invoiceService;
    private readonly INotificationService _notificationService;
    private readonly ISessionService _sessionService;
    private readonly IAddOnService _addOnService;

    public PortalController(
        ICurrentUserService currentUserService,
        IMembershipService membershipService,
        IMembershipPlanService membershipPlanService,
        IInvoiceService invoiceService,
        INotificationService notificationService,
        ISessionService sessionService,
        IAddOnService addOnService,
        Microsoft.AspNetCore.Identity.UserManager<GymManagementSystem.Domain.Entities.ApplicationUser> userManager)
        : base(userManager)
    {
        _currentUserService = currentUserService;
        _membershipService = membershipService;
        _membershipPlanService = membershipPlanService;
        _invoiceService = invoiceService;
        _notificationService = notificationService;
        _sessionService = sessionService;
        _addOnService = addOnService;
    }

    [HttpGet]
    [Authorize(Roles = "Member")]
    public async Task<IActionResult> Index(int page = 1, int pageSize = 10)
    {
        var memberId = _currentUserService.UserId ?? string.Empty;
        var memberships = await _membershipService.GetMembershipsForMemberAsync(memberId);
        var wallet = await _membershipService.GetWalletBalanceAsync(memberId);
        var transactions = await _membershipService.GetWalletTransactionsAsync(memberId);
        var invoices = await _invoiceService.GetMemberInvoicesAsync(memberId);
        var notifications = await _notificationService.GetMyNotificationsAsync();
        var sessions = await _sessionService.GetAvailableForMemberAsync(memberId);
        var pricingPreviews = await _sessionService.GetSessionPricingPreviewsAsync(memberId);
        var benefits = await _sessionService.GetMembershipBenefitsSnapshotAsync(memberId);
        var plans = await _membershipPlanService.GetActiveAsync();
        var addOns = await _addOnService.GetAvailableForMemberAsync(memberId);
        var pricingBySessionId = pricingPreviews.ToDictionary(x => x.WorkoutSessionId);
        SessionPricingPreviewDto? GetPreview(int sessionId) =>
            pricingBySessionId.TryGetValue(sessionId, out var preview) ? preview : null;

        if (page < 1) page = 1;
        var total = transactions.Count;
        var pagedTransactions = transactions
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var vm = new MemberPortalViewModel
        {
            Memberships = memberships.Select(x => new MembershipListItemViewModel
            {
                Id = x.Id,
                MembershipPlanId = x.MembershipPlanId,
                MembershipPlanName = x.MembershipPlanName,
                Status = x.Status.ToString(),
                StatusBadgeClass = GetMembershipStatusBadgeClass(x.Status.ToString()),
                StartDate = x.StartDate,
                EndDate = x.EndDate,
                TotalPaid = x.TotalPaid
            }).ToList(),
            WalletBalance = wallet.WalletBalance,
            WalletTransactions = pagedTransactions.Select(x => new WalletTransactionItemViewModel
            {
                Id = x.Id,
                Amount = x.Amount,
                Type = x.Type.ToString(),
                Description = x.Description,
                CreatedAt = x.CreatedAt
            }).ToList(),
            WalletTransactionsPage = page,
            WalletTransactionsPageSize = pageSize,
            WalletTransactionsTotalCount = total,
            Invoices = invoices.Select(x => new InvoiceItemViewModel
            {
                Id = x.Id,
                InvoiceNumber = x.InvoiceNumber,
                Amount = x.Amount,
                Type = x.Type,
                CreatedAt = x.CreatedAt
            }).ToList(),
            Notifications = notifications.Select(x => new NotificationItemViewModel
            {
                Id = x.Id,
                Title = x.Title,
                Message = x.Message,
                IsRead = x.IsRead,
                CreatedAt = x.CreatedAt
            }).ToList(),
            Benefits = new MembershipBenefitsViewModel
            {
                HasActiveMembership = benefits.HasActiveMembership,
                ActivePlanName = benefits.ActivePlanName,
                RemainingFreeSessionsThisMonth = benefits.RemainingIncludedSessionsThisMonth,
                SessionDiscountPercentage = benefits.SessionDiscountPercentage,
                PriorityBooking = benefits.PriorityBooking,
                AddOnAccess = benefits.AddOnAccess
            },
            AvailablePaidSessions = sessions
                .Where(s => s.Price > 0)
                .Select(s =>
                {
                    var preview = GetPreview(s.Id);
                    return new WorkoutSessionOptionViewModel
                    {
                        Id = s.Id,
                        Price = s.Price,
                        EffectivePrice = preview?.FinalPrice ?? s.Price,
                        IncludedApplied = preview?.IsIncludedSessionApplied ?? false,
                        DiscountPercentage = preview?.DiscountPercentage ?? 0,
                        Label = $"{s.Title} ({s.SessionDate:yyyy-MM-dd})"
                    };
                }).ToList(),
            UpgradePlans = plans.Select(p => new MembershipPlanOptionViewModel
            {
                Id = p.Id,
                Label = $"{p.Name} - {p.Price:0.00}"
            }).ToList(),
            AvailablePlans = plans.Select(p => new PlanCardViewModel
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description ?? string.Empty,
                DurationInDays = p.DurationInDays,
                Price = p.Price,
                DiscountPercentage = p.DiscountPercentage,
                IncludedSessionsPerMonth = p.IncludedSessionsPerMonth,
                PriorityBooking = p.PriorityBooking,
                AddOnAccess = p.AddOnAccess,
                IsActive = p.IsActive
            }).ToList(),
            AvailableAddOns = addOns.Select(a => new AddOnOptionViewModel
            {
                Id = a.Id,
                Price = a.Price,
                RequiresActiveMembership = a.RequiresActiveMembership,
                Label = $"{a.Name} - {a.Price:0.00}"
            }).ToList()
        };
        vm.CurrentMembership = vm.Memberships
            .OrderByDescending(m => m.StartDate)
            .FirstOrDefault(m => m.Status is "PendingPayment" or "Active" or "Frozen" or "Expired" or "Cancelled");

        return View(vm);
    }

    [HttpGet]
    [Authorize(Roles = "Member,Admin")]
    public async Task<IActionResult> MembershipPlans()
    {
        var plans = await _membershipPlanService.GetActiveAsync();
        var vm = plans.Select(p => new PlanCardViewModel
        {
            Id = p.Id,
            Name = p.Name,
            Description = p.Description ?? string.Empty,
            DurationInDays = p.DurationInDays,
            Price = p.Price,
            DiscountPercentage = p.DiscountPercentage,
            IncludedSessionsPerMonth = p.IncludedSessionsPerMonth,
            PriorityBooking = p.PriorityBooking,
            AddOnAccess = p.AddOnAccess,
            IsActive = p.IsActive
        }).ToList();

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Member")]
    public async Task<IActionResult> MarkNotificationRead(int notificationId)
    {
        try
        {
            await _notificationService.MarkReadAsync(new MarkNotificationReadDto { NotificationId = notificationId });
            TempData["Success"] = "Notification marked as read.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Member")]
    public async Task<IActionResult> BuyPaidSession(int workoutSessionId)
    {
        var memberId = _currentUserService.UserId ?? string.Empty;
        try
        {
            var result = await _sessionService.BookPaidSessionAsync(new PaidSessionBookingDto
            {
                MemberId = memberId,
                WorkoutSessionId = workoutSessionId
            });
            TempData[result.Success ? "Success" : "Error"] = result.Message;
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Member")]
    public async Task<IActionResult> UpgradeMembership(int newPlanId)
    {
        var memberId = _currentUserService.UserId ?? string.Empty;
        try
        {
            await _membershipService.UpgradeMembershipAsync(new UpgradeMembershipDto
            {
                MemberId = memberId,
                NewMembershipPlanId = newPlanId
            });
            TempData["Success"] = "Membership upgraded successfully.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Member")]
    public async Task<IActionResult> PurchaseAddOn(int addOnId)
    {
        var memberId = _currentUserService.UserId ?? string.Empty;
        try
        {
            await _addOnService.PurchaseAsync(new PurchaseAddOnDto
            {
                MemberId = memberId,
                AddOnId = addOnId
            });
            TempData["Success"] = "Add-on purchased successfully.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Member")]
    public async Task<IActionResult> SubscribePlan(int planId, PaymentMethod paymentMethod = PaymentMethod.Proof)
    {
        var memberId = _currentUserService.UserId ?? string.Empty;
        try
        {
            if (paymentMethod == PaymentMethod.Cash)
            {
                TempData["Error"] = "Cash is not available in the portal.";
                return RedirectToAction(nameof(Index));
            }

            var plans = await _membershipPlanService.GetActiveAsync();
            var plan = plans.FirstOrDefault(p => p.Id == planId);
            if (plan == null)
            {
                TempData["Error"] = "Plan not found.";
                return RedirectToAction(nameof(Index));
            }

            await _membershipService.RequestSubscriptionAsync(new RequestSubscriptionDto
            {
                MemberId = memberId,
                MembershipPlanId = planId,
                StartDate = DateTime.UtcNow.Date,
                PaymentAmount = plan.Price,
                WalletAmountToUse = 0m,
                PaymentMethod = paymentMethod
            });
            TempData["Success"] = paymentMethod == PaymentMethod.Wallet
                ? "Subscription activated successfully."
                : "Subscription request submitted. Awaiting admin confirmation.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Member")]
    public async Task<IActionResult> RenewMembership(int planId)
    {
        return await SubscribePlan(planId);
    }

    private static string GetMembershipStatusBadgeClass(string status) => status switch
    {
        "PendingPayment" => "bg-warning text-dark",
        "Active" => "bg-success",
        "Frozen" => "bg-info text-dark",
        "Expired" => "bg-secondary",
        "Cancelled" => "bg-dark",
        _ => "bg-secondary"
    };
}
