using GymManagementSystem.Application.DTOs;
using GymManagementSystem.Application.Exceptions;
using GymManagementSystem.Application.Interfaces;
using GymManagementSystem.Domain.Entities;
using GymManagementSystem.Domain.Enums;
using Mapster;
using Microsoft.EntityFrameworkCore;

namespace GymManagementSystem.Application.Services;

public class BranchService : IBranchService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAppAuthorizationService _authorizationService;

    public BranchService(IUnitOfWork unitOfWork, IAppAuthorizationService authorizationService)
    {
        _unitOfWork = unitOfWork;
        _authorizationService = authorizationService;
    }

    public async Task<BranchReadDto> CreateAsync(CreateBranchDto dto)
    {
        await _authorizationService.EnsureAdminFullAccessAsync();

        var repo = _unitOfWork.Repository<Branch>();
        var exists = await repo.AnyAsync(x => x.Name == dto.Name);
        if (exists)
        {
            throw new AppValidationException("Branch name already exists.");
        }

        var branch = dto.Adapt<Branch>();
        await repo.AddAsync(branch);
        await _unitOfWork.SaveChangesAsync();
        return branch.Adapt<BranchReadDto>();
    }

    public async Task<IReadOnlyList<BranchReadDto>> GetAllAsync()
    {
        var repo = _unitOfWork.Repository<Branch>();
        var branches = await repo.ToListAsync(repo.Query().AsNoTracking().OrderBy(x => x.Name));
        return branches.Adapt<List<BranchReadDto>>();
    }

    public async Task AssignMemberAsync(AssignUserBranchDto dto)
    {
        await _authorizationService.EnsureAdminFullAccessAsync();

        var branchRepo = _unitOfWork.Repository<Branch>();
        var memberRepo = _unitOfWork.Repository<Member>();

        var branchExists = await branchRepo.AnyAsync(x => x.Id == dto.BranchId && x.IsActive);
        if (!branchExists)
        {
            throw new NotFoundException("Branch not found.");
        }

        var member = await memberRepo.FirstOrDefaultAsync(memberRepo.Query().Where(x => x.Id == dto.UserId));
        if (member == null)
        {
            throw new NotFoundException("Member not found.");
        }

        member.BranchId = dto.BranchId;
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task AssignTrainerAsync(AssignUserBranchDto dto)
    {
        await _authorizationService.EnsureAdminFullAccessAsync();

        var branchRepo = _unitOfWork.Repository<Branch>();
        var trainerRepo = _unitOfWork.Repository<Trainer>();

        var branchExists = await branchRepo.AnyAsync(x => x.Id == dto.BranchId && x.IsActive);
        if (!branchExists)
        {
            throw new NotFoundException("Branch not found.");
        }

        var trainer = await trainerRepo.FirstOrDefaultAsync(trainerRepo.Query().Where(x => x.Id == dto.UserId));
        if (trainer == null)
        {
            throw new NotFoundException("Trainer not found.");
        }

        trainer.BranchId = dto.BranchId;
        await _unitOfWork.SaveChangesAsync();
    }
}

public class CommissionService : ICommissionService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAppAuthorizationService _authorizationService;
    private readonly ICurrentUserService _currentUserService;

    public CommissionService(IUnitOfWork unitOfWork, IAppAuthorizationService authorizationService, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _authorizationService = authorizationService;
        _currentUserService = currentUserService;
    }

    public async Task<IReadOnlyList<CommissionReadDto>> GetUnpaidAsync()
    {
        await _authorizationService.EnsureAdminFullAccessAsync();
        var repo = _unitOfWork.Repository<Commission>();
        var list = await repo.ToListAsync(
            repo.Query()
                .AsNoTracking()
                .Where(x => !x.IsPaid)
                .OrderByDescending(x => x.CreatedAt));
        return list.Adapt<List<CommissionReadDto>>();
    }

    public async Task<CommissionReadDto> MarkPaidAsync(int commissionId)
    {
        await _authorizationService.EnsureAdminFullAccessAsync();
        var repo = _unitOfWork.Repository<Commission>();
        var commission = await repo.FirstOrDefaultAsync(repo.Query().Where(x => x.Id == commissionId));
        if (commission == null)
        {
            throw new NotFoundException("Commission not found.");
        }

        if (commission.IsPaid)
        {
            throw new AppValidationException("Commission is already paid.");
        }

        commission.IsPaid = true;
        commission.PaidAt = DateTime.UtcNow;
        commission.PaidByAdminId = _currentUserService.UserId;
        await _unitOfWork.SaveChangesAsync();
        return commission.Adapt<CommissionReadDto>();
    }

    public async Task<IReadOnlyList<TrainerCommissionMetricsDto>> GetTrainerMetricsAsync()
    {
        await _authorizationService.EnsureAdminFullAccessAsync();
        var repo = _unitOfWork.Repository<Commission>();
        var list = await repo.Query()
            .AsNoTracking()
            .GroupBy(x => x.TrainerId)
            .Select(g => new TrainerCommissionMetricsDto
            {
                TrainerId = g.Key,
                TotalOwed = g.Where(x => !x.IsPaid).Sum(x => x.CalculatedAmount),
                TotalPaid = g.Where(x => x.IsPaid).Sum(x => x.CalculatedAmount)
            })
            .OrderBy(x => x.TrainerId)
            .ToListAsync();

        return list;
    }

    public async Task<TrainerCommissionDashboardDto> GetMyDashboardAsync()
    {
        var trainerId = _currentUserService.UserId;
        if (string.IsNullOrWhiteSpace(trainerId) || !_currentUserService.IsInRole("Trainer"))
        {
            throw new UnauthorizedException("Trainer access required.");
        }

        var repo = _unitOfWork.Repository<Commission>();
        var query = repo.Query()
            .AsNoTracking()
            .Where(x => x.TrainerId == trainerId);

        var totalOwed = await query
            .Where(x => !x.IsPaid)
            .Select(x => (decimal?)x.CalculatedAmount)
            .SumAsync() ?? 0m;
        var totalPaid = await query
            .Where(x => x.IsPaid)
            .Select(x => (decimal?)x.CalculatedAmount)
            .SumAsync() ?? 0m;

        var recent = await repo.ToListAsync(
            query.OrderByDescending(x => x.CreatedAt).Take(20));

        return new TrainerCommissionDashboardDto
        {
            TrainerId = trainerId,
            TotalOwed = totalOwed,
            TotalPaid = totalPaid,
            RecentCommissions = recent.Adapt<List<CommissionReadDto>>()
        };
    }
}

public class InvoiceService : IInvoiceService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAppAuthorizationService _authorizationService;
    private readonly ICurrentUserService _currentUserService;

    public InvoiceService(IUnitOfWork unitOfWork, IAppAuthorizationService authorizationService, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _authorizationService = authorizationService;
        _currentUserService = currentUserService;
    }

    public async Task<IReadOnlyList<InvoiceReadDto>> GetMemberInvoicesAsync(string memberId)
    {
        if (_currentUserService.IsInRole("Admin"))
        {
            // allowed
        }
        else
        {
            await _authorizationService.EnsureMemberOwnsResourceAsync(memberId);
        }

        var repo = _unitOfWork.Repository<Invoice>();
        var list = await repo.ToListAsync(
            repo.Query()
                .AsNoTracking()
                .Where(x => x.MemberId == memberId)
                .OrderByDescending(x => x.CreatedAt));

        return list.Adapt<List<InvoiceReadDto>>();
    }

    public async Task<InvoiceReadDto> GetByIdAsync(int id)
    {
        var repo = _unitOfWork.Repository<Invoice>();
        var invoice = await repo.FirstOrDefaultAsync(repo.Query().AsNoTracking().Where(x => x.Id == id));
        if (invoice == null)
        {
            throw new NotFoundException("Invoice not found.");
        }

        if (!_currentUserService.IsInRole("Admin"))
        {
            await _authorizationService.EnsureMemberOwnsResourceAsync(invoice.MemberId);
        }

        return invoice.Adapt<InvoiceReadDto>();
    }

    public async Task<(byte[] Content, string FileName, string ContentType)> DownloadAsync(int id)
    {
        var invoice = await GetByIdAsync(id);
        if (!File.Exists(invoice.FilePath))
        {
            throw new NotFoundException("Invoice file not found.");
        }

        var bytes = await File.ReadAllBytesAsync(invoice.FilePath);
        var name = Path.GetFileName(invoice.FilePath);
        return (bytes, name, "application/pdf");
    }
}

public class NotificationService : INotificationService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public NotificationService(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<IReadOnlyList<NotificationReadDto>> GetMyNotificationsAsync()
    {
        var userId = _currentUserService.UserId;
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new UnauthorizedException("Authentication required.");
        }

        var repo = _unitOfWork.Repository<Notification>();
        var list = await repo.ToListAsync(
            repo.Query()
                .AsNoTracking()
                .Where(x => x.UserId == userId)
                .OrderByDescending(x => x.CreatedAt));
        return list.Adapt<List<NotificationReadDto>>();
    }

    public async Task MarkReadAsync(MarkNotificationReadDto dto)
    {
        var userId = _currentUserService.UserId;
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new UnauthorizedException("Authentication required.");
        }

        var repo = _unitOfWork.Repository<Notification>();
        var notification = await repo.FirstOrDefaultAsync(repo.Query().Where(x => x.Id == dto.NotificationId && x.UserId == userId));
        if (notification == null)
        {
            throw new NotFoundException("Notification not found.");
        }

        notification.IsRead = true;
        notification.ReadAt = DateTime.UtcNow;
        await _unitOfWork.SaveChangesAsync();
    }
}

public class AddOnService : IAddOnService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAppAuthorizationService _authorizationService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IInvoicePdfGenerator _invoicePdfGenerator;

    public AddOnService(
        IUnitOfWork unitOfWork,
        IAppAuthorizationService authorizationService,
        ICurrentUserService currentUserService,
        IInvoicePdfGenerator invoicePdfGenerator)
    {
        _unitOfWork = unitOfWork;
        _authorizationService = authorizationService;
        _currentUserService = currentUserService;
        _invoicePdfGenerator = invoicePdfGenerator;
    }

    public async Task<AddOnReadDto> CreateAsync(CreateAddOnDto dto)
    {
        await _authorizationService.EnsureAdminFullAccessAsync();
        var repo = _unitOfWork.Repository<AddOn>();
        var addOn = dto.Adapt<AddOn>();
        await repo.AddAsync(addOn);
        await _unitOfWork.SaveChangesAsync();
        return addOn.Adapt<AddOnReadDto>();
    }

    public async Task<IReadOnlyList<AddOnReadDto>> GetAvailableForMemberAsync(string memberId)
    {
        await EnsureCanAccessMemberAsync(memberId);

        var memberRepo = _unitOfWork.Repository<Member>();
        var member = await memberRepo.FirstOrDefaultAsync(memberRepo.Query().AsNoTracking().Where(x => x.Id == memberId));
        if (member == null)
        {
            throw new NotFoundException("Member not found.");
        }

        var activeMembership = await GetActiveMembershipWithBenefitsAsync(memberId);
        var hasAddOnAccess = activeMembership?.MembershipPlan.AddOnAccess ?? false;

        var addOnRepo = _unitOfWork.Repository<AddOn>();
        var list = await addOnRepo.ToListAsync(
            addOnRepo.Query()
                .AsNoTracking()
                .Where(x =>
                    x.IsActive &&
                    (!x.BranchId.HasValue || x.BranchId == member.BranchId) &&
                    (!x.RequiresActiveMembership || hasAddOnAccess))
                .OrderBy(x => x.Name));
        return list.Adapt<List<AddOnReadDto>>();
    }

    public async Task<InvoiceReadDto> PurchaseAsync(PurchaseAddOnDto dto)
    {
        await EnsureCanAccessMemberAsync(dto.MemberId);

        return await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var addOnRepo = _unitOfWork.Repository<AddOn>();
            var memberRepo = _unitOfWork.Repository<Member>();
            var walletRepo = _unitOfWork.Repository<WalletTransaction>();
            var invoiceRepo = _unitOfWork.Repository<Invoice>();
            var notificationRepo = _unitOfWork.Repository<Notification>();

            var addOn = await addOnRepo.FirstOrDefaultAsync(addOnRepo.Query().Where(x => x.Id == dto.AddOnId && x.IsActive));
            if (addOn == null)
            {
                throw new NotFoundException("Add-on not found.");
            }

            var member = await memberRepo.FirstOrDefaultAsync(memberRepo.Query().Where(x => x.Id == dto.MemberId));
            if (member == null)
            {
                throw new NotFoundException("Member not found.");
            }

            if (addOn.BranchId.HasValue && member.BranchId.HasValue && addOn.BranchId.Value != member.BranchId.Value)
            {
                throw new UnauthorizedException("Cannot purchase add-on from another branch.");
            }

            var activeMembership = await GetActiveMembershipWithBenefitsAsync(dto.MemberId);
            var hasAddOnAccess = activeMembership?.MembershipPlan.AddOnAccess ?? false;
            if (addOn.RequiresActiveMembership && !hasAddOnAccess)
            {
                throw new AppValidationException("This add-on requires an active membership with add-on access.");
            }

            var balance = await walletRepo.Query()
                .Where(t => t.MemberId == dto.MemberId)
                .Select(t => (decimal?)t.Amount)
                .SumAsync() ?? 0m;
            if (balance < addOn.Price)
            {
                throw new AppValidationException("Insufficient wallet balance.");
            }

            await walletRepo.AddAsync(new WalletTransaction
            {
                MemberId = dto.MemberId,
                Amount = -addOn.Price,
                Type = WalletTransactionType.AddOnPurchase,
                ReferenceId = addOn.Id,
                Description = $"Wallet debit for add-on purchase #{addOn.Id}.",
                CreatedByUserId = _currentUserService.UserId
            });
            await _unitOfWork.SaveChangesAsync();

            member.WalletBalance = await walletRepo.Query()
                .Where(t => t.MemberId == dto.MemberId)
                .Select(t => (decimal?)t.Amount)
                .SumAsync() ?? 0m;

            var invoice = new Invoice
            {
                InvoiceNumber = $"INV-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}".Substring(0, 32),
                MembershipId = null,
                AddOnId = addOn.Id,
                PaymentId = null,
                MemberId = dto.MemberId,
                Amount = addOn.Price,
                Type = "AddOnPurchase",
                FilePath = string.Empty
            };
            await invoiceRepo.AddAsync(invoice);
            await _unitOfWork.SaveChangesAsync();

            var invoiceDto = invoice.Adapt<InvoiceReadDto>();
            var filePath = await _invoicePdfGenerator.GenerateInvoicePdfAsync(invoiceDto);
            invoice.FilePath = filePath;

            await notificationRepo.AddAsync(new Notification
            {
                UserId = dto.MemberId,
                Title = "Add-On Purchased",
                Message = $"Add-on '{addOn.Name}' purchased successfully.",
                IsRead = false
            });

            await _unitOfWork.SaveChangesAsync();
            return invoice.Adapt<InvoiceReadDto>();
        });
    }

    private async Task EnsureCanAccessMemberAsync(string memberId)
    {
        if (_currentUserService.IsInRole("Admin"))
        {
            return;
        }

        await _authorizationService.EnsureMemberOwnsResourceAsync(memberId);
    }

    private async Task<Membership?> GetActiveMembershipWithBenefitsAsync(string memberId)
    {
        var membershipRepo = _unitOfWork.Repository<Membership>();
        return await membershipRepo.FirstOrDefaultAsync(
            membershipRepo.Query()
                .AsNoTracking()
                .Include(m => m.MembershipPlan)
                .Where(m =>
                    m.MemberId == memberId &&
                    m.Status == MembershipStatus.Active &&
                    m.StartDate <= DateTime.UtcNow &&
                    m.EndDate >= DateTime.UtcNow)
                .OrderByDescending(m => m.EndDate));
    }
}

public class ManualVodafoneCashGateway : IPaymentGateway
{
    public string Name => "manual";

    public Task<(GymManagementSystem.Domain.Enums.PaymentMethod Method, GymManagementSystem.Domain.Enums.PaymentStatus Status)> PrepareAsync(
        GymManagementSystem.Domain.Enums.MembershipSource source,
        CancellationToken cancellationToken = default)
    {
        var status = source == GymManagementSystem.Domain.Enums.MembershipSource.InGym
            ? GymManagementSystem.Domain.Enums.PaymentStatus.Confirmed
            : GymManagementSystem.Domain.Enums.PaymentStatus.Pending;
        return Task.FromResult((GymManagementSystem.Domain.Enums.PaymentMethod.VodafoneCash, status));
    }
}

public class FutureOnlineGateway : IPaymentGateway
{
    public string Name => "future-online";

    public Task<(GymManagementSystem.Domain.Enums.PaymentMethod Method, GymManagementSystem.Domain.Enums.PaymentStatus Status)> PrepareAsync(
        GymManagementSystem.Domain.Enums.MembershipSource source,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult((GymManagementSystem.Domain.Enums.PaymentMethod.VodafoneCash, GymManagementSystem.Domain.Enums.PaymentStatus.Pending));
    }
}
