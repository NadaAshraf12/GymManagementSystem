using GymManagementSystem.Domain.Entities.Base;
using GymManagementSystem.Domain.Enums;

namespace GymManagementSystem.Domain.Entities
{
    public class WalletTransaction : BaseEntity
    {
        public string MemberId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public WalletTransactionType Type { get; set; }
        public int? ReferenceId { get; set; }
        public string Description { get; set; } = string.Empty;
        public string? CreatedByUserId { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();

        public virtual Member Member { get; set; } = null!;
    }
}
