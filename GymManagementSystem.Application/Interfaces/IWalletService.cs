using GymManagementSystem.Application.DTOs;

namespace GymManagementSystem.Application.Interfaces;

public interface IWalletService
{
    Task<WalletBalanceDto> AdminTopUpWalletAsync(AdminWalletTopUpDto dto);
}
