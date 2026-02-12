using System.Security.Claims;
using GymManagementSystem.Application.DTOs;
using GymManagementSystem.Application.Interfaces;
using GymManagementSystem.WebUI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymManagementSystem.WebUI.Controllers;

[Authorize(Roles = "Admin")]
public class SaasAdminController : BaseController
{
    private readonly IRevenueMetricsService _revenueMetricsService;
    private readonly IBranchService _branchService;
    private readonly ICommissionService _commissionService;
    private readonly IMembershipService _membershipService;
    private readonly IMembershipPlanService _membershipPlanService;
    private readonly IMemberService _memberService;
    private readonly ITrainerService _trainerService;

    public SaasAdminController(
        IRevenueMetricsService revenueMetricsService,
        IBranchService branchService,
        ICommissionService commissionService,
        IMembershipService membershipService,
        IMembershipPlanService membershipPlanService,
        IMemberService memberService,
        ITrainerService trainerService,
        Microsoft.AspNetCore.Identity.UserManager<GymManagementSystem.Domain.Entities.ApplicationUser> userManager)
        : base(userManager)
    {
        _revenueMetricsService = revenueMetricsService;
        _branchService = branchService;
        _commissionService = commissionService;
        _membershipService = membershipService;
        _membershipPlanService = membershipPlanService;
        _memberService = memberService;
        _trainerService = trainerService;
    }

    [HttpGet]
    public async Task<IActionResult> Dashboard(int? branchId, CancellationToken cancellationToken)
    {
        var metrics = await _revenueMetricsService.GetDashboardMetricsAsync(branchId, cancellationToken);
        var branches = await _branchService.GetAllAsync();

        var vm = new AdminDashboardViewModel
        {
            SelectedBranchId = branchId,
            Branches = branches.Select(x => new BranchOptionViewModel { Id = x.Id, Name = x.Name }).ToList(),
            TotalRevenue = metrics.TotalRevenue,
            TotalSessionRevenue = metrics.TotalSessionRevenue,
            TotalMembershipRevenue = metrics.TotalMembershipRevenue,
            TotalAddOnRevenue = metrics.TotalAddOnRevenue,
            MonthlyRecurringRevenue = metrics.MonthlyRecurringRevenue,
            TotalWalletBalance = metrics.TotalWalletBalance,
            ActiveMemberships = metrics.ActiveMemberships,
            ExpiringSoonMemberships = metrics.ExpiringSoonMemberships,
            ExpiredMemberships = metrics.ExpiredMemberships,
            TotalCommissionsOwed = metrics.TotalCommissionsOwed,
            TotalCommissionsPaid = metrics.TotalCommissionsPaid,
            WalletTotalCredits = metrics.WalletTotalCredits,
            WalletTotalDebits = metrics.WalletTotalDebits
        };

        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> Memberships()
    {
        var pending = await _membershipService.GetPendingPaymentsAsync();
        var members = await _memberService.GetAllMembersAsync();
        var branches = await _branchService.GetAllAsync();
        var plans = await _membershipPlanService.GetAllAsync();
        var plansById = plans.ToDictionary(p => p.Id);
        var branchById = branches.ToDictionary(b => b.Id, b => b.Name);

        var statusRows = new List<MembershipStatusItemViewModel>();
        foreach (var member in members.Take(80))
        {
            var memberships = await _membershipService.GetMembershipsForMemberAsync(member.Id);
            var wallet = await _membershipService.GetWalletBalanceAsync(member.Id);
            foreach (var item in memberships)
            {
                statusRows.Add(new MembershipStatusItemViewModel
                {
                    MembershipId = item.Id,
                    MemberId = member.Id,
                    MemberDisplayName = $"{member.MemberCode} - {member.FirstName} {member.LastName}",
                    MembershipPlanName = item.MembershipPlanName,
                    BranchName = member.BranchId.HasValue && branchById.TryGetValue(member.BranchId.Value, out var branchName)
                        ? branchName
                        : "N/A",
                    Status = item.Status.ToString(),
                    StatusBadgeClass = GetMembershipStatusBadgeClass(item.Status.ToString()),
                    EndDate = item.EndDate,
                    WalletBalance = wallet.WalletBalance
                });
            }
        }
        var statusByMembershipId = statusRows
            .OrderByDescending(s => s.MembershipId)
            .ToDictionary(s => s.MembershipId, s => s);

        var vm = new MembershipManagementViewModel
        {
            PendingPayments = pending.Select(x => new PendingPaymentItemViewModel
            {
                PaymentId = x.PaymentId,
                MembershipId = x.MembershipId,
                MemberId = x.MemberId,
                MemberDisplayName = statusByMembershipId.TryGetValue(x.MembershipId, out var row) ? row.MemberDisplayName : x.MemberId,
                MembershipPlanName = statusByMembershipId.TryGetValue(x.MembershipId, out row) ? row.MembershipPlanName : $"Plan #{x.MembershipId}",
                BranchName = statusByMembershipId.TryGetValue(x.MembershipId, out row) ? row.BranchName : "N/A",
                Amount = x.Amount,
                PaidAt = x.PaidAt,
                PaymentProofUrl = x.PaymentProofUrl
            }).ToList(),
            Members = members.Select(m => new SimpleUserOptionViewModel
            {
                Id = m.Id,
                Display = $"{m.MemberCode} - {m.FirstName} {m.LastName}"
            }).ToList(),
            MembershipStatuses = statusRows,
            ActiveMemberships = statusRows.Where(x => x.Status == "Active").OrderBy(x => x.EndDate).ToList(),
            FrozenMemberships = statusRows.Where(x => x.Status == "Frozen").OrderBy(x => x.EndDate).ToList(),
            ExpiredMemberships = statusRows.Where(x => x.Status == "Expired").OrderByDescending(x => x.EndDate).ToList(),
            CancelledMemberships = statusRows.Where(x => x.Status == "Cancelled").OrderByDescending(x => x.EndDate).ToList(),
            MembershipPlans = plans.Select(p => new PlanCardViewModel
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
                CommissionRate = $"{p.CommissionRate:0.##}%",
                IsActive = p.IsActive
            }).OrderBy(p => p.Price).ToList()
        };
        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> CreateMembership()
    {
        var vm = await BuildCreateMembershipViewModelAsync();
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateMembership(CreateMembershipViewModel form)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(form.MemberId))
            {
                TempData["Error"] = "Please select a member.";
                return RedirectToAction(nameof(CreateMembership));
            }

            if (form.MembershipPlanId <= 0)
            {
                TempData["Error"] = "Please select a membership plan.";
                return RedirectToAction(nameof(CreateMembership));
            }

            var plans = await _membershipPlanService.GetActiveAsync();
            var plan = plans.FirstOrDefault(p => p.Id == form.MembershipPlanId);
            if (plan == null)
            {
                TempData["Error"] = "Selected plan was not found.";
                return RedirectToAction(nameof(CreateMembership));
            }

            await _membershipService.CreateDirectMembershipAsync(new CreateDirectMembershipDto
            {
                MemberId = form.MemberId,
                BranchId = form.BranchId,
                MembershipPlanId = form.MembershipPlanId,
                StartDate = form.StartDate,
                PaymentAmount = plan.Price,
                WalletAmountToUse = 0m
            });

            TempData["Success"] = "Membership created successfully.";
            return RedirectToAction(nameof(Memberships));
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(CreateMembership));
        }
    }

    private async Task<CreateMembershipViewModel> BuildCreateMembershipViewModelAsync()
    {
        var members = await _memberService.GetAllMembersAsync();
        var branches = await _branchService.GetAllAsync();
        var plans = await _membershipPlanService.GetActiveAsync();

        return new CreateMembershipViewModel
        {
            Members = members.Select(m => new SimpleUserOptionViewModel
            {
                Id = m.Id,
                Display = $"{m.MemberCode} - {m.FirstName} {m.LastName}"
            }).ToList(),
            Branches = branches.Select(b => new BranchOptionViewModel
            {
                Id = b.Id,
                Name = b.Name
            }).ToList(),
            Plans = plans.Select(p => new MembershipPlanOptionViewModel
            {
                Id = p.Id,
                Label = p.Name,
                Price = p.Price,
                DurationInDays = p.DurationInDays,
                DiscountPercentage = p.DiscountPercentage,
                IncludedSessionsPerMonth = p.IncludedSessionsPerMonth,
                PriorityBooking = p.PriorityBooking,
                AddOnAccess = p.AddOnAccess,
                CommissionRate = $"{p.CommissionRate:0.##}%"
            }).ToList()
        };
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmPayment(int paymentId, decimal? confirmedAmount)
    {
        try
        {
            await _membershipService.ConfirmPaymentAsync(paymentId, new ConfirmPaymentDto { ConfirmedAmount = confirmedAmount });
            TempData["Success"] = "Payment confirmed.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Memberships));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RejectPayment(int paymentId, string rejectionReason)
    {
        try
        {
            await _membershipService.ReviewPaymentAsync(paymentId, new ReviewPaymentDto
            {
                Approve = false,
                RejectionReason = rejectionReason
            });
            TempData["Success"] = "Payment rejected.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Memberships));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> FreezeMembership(FreezeMembershipFormViewModel form)
    {
        try
        {
            await _membershipService.FreezeMembershipAsync(form.MembershipId, new FreezeMembershipDto
            {
                FreezeStartDate = form.FreezeStartDate
            });
            TempData["Success"] = "Membership frozen.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Memberships));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResumeMembership(ResumeMembershipFormViewModel form)
    {
        try
        {
            await _membershipService.ResumeMembershipAsync(form.MembershipId);
            TempData["Success"] = "Membership resumed.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Memberships));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AdjustWallet(WalletAdjustFormViewModel form)
    {
        try
        {
            await _membershipService.AdjustWalletAsync(new AdjustWalletDto
            {
                MemberId = form.MemberId,
                Amount = form.Amount
            });
            TempData["Success"] = "Wallet adjusted.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Memberships));
    }

    [HttpGet]
    public async Task<IActionResult> Branches()
    {
        var branches = await _branchService.GetAllAsync();
        var members = await _memberService.GetAllMembersAsync();
        var trainers = await _trainerService.GetAllAsync();

        var vm = new BranchManagementViewModel
        {
            Branches = branches.Select(x => new BranchReadItemViewModel
            {
                Id = x.Id,
                Name = x.Name,
                Address = x.Address,
                IsActive = x.IsActive
            }).ToList(),
            Members = members.Select(x => new SimpleUserOptionViewModel
            {
                Id = x.Id,
                Display = $"{x.MemberCode} - {x.FirstName} {x.LastName}"
            }).ToList(),
            Trainers = trainers.Select(x => new SimpleUserOptionViewModel
            {
                Id = x.Id,
                Display = $"{x.FirstName} {x.LastName}"
            }).ToList()
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateBranch(CreateBranchFormViewModel form)
    {
        try
        {
            await _branchService.CreateAsync(new CreateBranchDto
            {
                Name = form.Name,
                Address = form.Address,
                IsActive = form.IsActive
            });
            TempData["Success"] = "Branch created.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Branches));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignMember(AssignBranchFormViewModel form)
    {
        try
        {
            await _branchService.AssignMemberAsync(new AssignUserBranchDto { UserId = form.UserId, BranchId = form.BranchId });
            TempData["Success"] = "Member assigned to branch.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Branches));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignTrainer(AssignBranchFormViewModel form)
    {
        try
        {
            await _branchService.AssignTrainerAsync(new AssignUserBranchDto { UserId = form.UserId, BranchId = form.BranchId });
            TempData["Success"] = "Trainer assigned to branch.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Branches));
    }

    [HttpGet]
    public async Task<IActionResult> Commissions()
    {
        var unpaid = await _commissionService.GetUnpaidAsync();
        var metrics = await _commissionService.GetTrainerMetricsAsync();
        var membershipsByMember = new Dictionary<int, string>();
        var memberNameByMembership = new Dictionary<int, string>();
        var plansByMembership = new Dictionary<int, string>();
        var members = await _memberService.GetAllMembersAsync();
        var memberNameById = members.ToDictionary(m => m.Id, m => $"{m.MemberCode} - {m.FirstName} {m.LastName}");

        foreach (var member in members.Take(120))
        {
            var list = await _membershipService.GetMembershipsForMemberAsync(member.Id);
            foreach (var m in list)
            {
                memberNameByMembership[m.Id] = memberNameById[member.Id];
                plansByMembership[m.Id] = m.MembershipPlanName;
            }
        }

        var vm = new CommissionCenterViewModel
        {
            UnpaidCommissions = unpaid.Select(x => new CommissionItemViewModel
            {
                Id = x.Id,
                TrainerId = x.TrainerId,
                MembershipId = x.MembershipId,
                MemberName = memberNameByMembership.TryGetValue(x.MembershipId, out var n) ? n : "N/A",
                MembershipPlanName = plansByMembership.TryGetValue(x.MembershipId, out var p) ? p : "N/A",
                BranchId = x.BranchId,
                Source = x.Source,
                Status = x.Status,
                SourceBadgeClass = x.Source == "Activation" ? "bg-primary" : "bg-info text-dark",
                StatusBadgeClass = x.Status == "Paid" ? "bg-success" : "bg-warning text-dark",
                Percentage = x.Percentage,
                CalculatedAmount = x.CalculatedAmount,
                CreatedAt = x.CreatedAt
            }).ToList(),
            Metrics = metrics.Select(x => new TrainerCommissionMetricsItemViewModel
            {
                TrainerId = x.TrainerId,
                TotalOwed = x.TotalOwed,
                TotalPaid = x.TotalPaid
            }).ToList()
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkCommissionPaid(int commissionId)
    {
        try
        {
            await _commissionService.MarkPaidAsync(commissionId);
            TempData["Success"] = "Commission marked as paid.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Commissions));
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
