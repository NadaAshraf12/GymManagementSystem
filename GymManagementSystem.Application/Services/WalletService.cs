using GymManagementSystem.Application.DTOs;
using GymManagementSystem.Application.Exceptions;
using GymManagementSystem.Application.Interfaces;
using GymManagementSystem.Domain.Entities;
using GymManagementSystem.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace GymManagementSystem.Application.Services;

public class WalletService : IWalletService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAppAuthorizationService _authorizationService;
    private readonly ICurrentUserService _currentUserService;

    public WalletService(
        IUnitOfWork unitOfWork,
        IAppAuthorizationService authorizationService,
        ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _authorizationService = authorizationService;
        _currentUserService = currentUserService;
    }

    public async Task<WalletBalanceDto> AdminTopUpWalletAsync(AdminWalletTopUpDto dto)
    {
        await _authorizationService.EnsureAdminFullAccessAsync();

        return await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            if (dto.Amount <= 0)
            {
                throw new AppValidationException("Top-up amount must be greater than zero.");
            }

            var memberRepo = _unitOfWork.Repository<Member>();
            var member = await memberRepo.FirstOrDefaultAsync(
                memberRepo.Query().Where(x => x.Id == dto.MemberId && x.IsActive));
            if (member == null)
            {
                throw new NotFoundException("Member not found.");
            }

            var notes = string.IsNullOrWhiteSpace(dto.Notes) ? "N/A" : dto.Notes.Trim();
            var txRepo = _unitOfWork.Repository<WalletTransaction>();
            await txRepo.AddAsync(new WalletTransaction
            {
                MemberId = dto.MemberId,
                Amount = dto.Amount,
                Type = WalletTransactionType.Credit,
                ReferenceId = null,
                Description = $"Admin cash top-up. Source=AdminCash; PaymentMethod=Cash; PaymentStatus=Confirmed; Notes={notes}",
                CreatedByUserId = _currentUserService.UserId
            });

            await _unitOfWork.SaveChangesAsync();

            member.WalletBalance = await txRepo.Query()
                .Where(t => t.MemberId == dto.MemberId)
                .Select(t => (decimal?)t.Amount)
                .SumAsync() ?? 0m;
            await _unitOfWork.SaveChangesAsync();

            return new WalletBalanceDto
            {
                MemberId = dto.MemberId,
                WalletBalance = member.WalletBalance
            };
        });
    }
}
