using System.ComponentModel.DataAnnotations;

namespace GymManagementSystem.WebUI.Models
{
    public class ProfileViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string? ProfilePicture { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; }
        public string Role { get; set; } = string.Empty;
    }

    public class EditProfileViewModel
    {
        [Required(ErrorMessage = "First name is required")]
        [StringLength(50, ErrorMessage = "First name cannot exceed 50 characters")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Last name is required")]
        [StringLength(50, ErrorMessage = "Last name cannot exceed 50 characters")]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Invalid phone number format")]
        public string? PhoneNumber { get; set; }

        public string? ProfilePicture { get; set; }

        [Display(Name = "Profile Picture")]
        public IFormFile? ProfileImageFile { get; set; }
    }

    public class MemberFinancialProfileViewModel
    {
        public decimal WalletBalance { get; set; }
        public List<MemberFinancialWalletRowViewModel> WalletTransactions { get; set; } = new();
        public List<MemberFinancialPurchaseRowViewModel> Purchases { get; set; } = new();
        public List<MemberFinancialMembershipRowViewModel> MembershipHistory { get; set; } = new();
    }

    public class MemberFinancialWalletRowViewModel
    {
        public DateTime Date { get; set; }
        public string Type { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Description { get; set; } = string.Empty;
        public decimal RunningBalance { get; set; }
    }

    public class MemberFinancialPurchaseRowViewModel
    {
        public DateTime Date { get; set; }
        public string Category { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Description { get; set; } = string.Empty;
        public string? InvoiceNumber { get; set; }
    }

    public class MemberFinancialMembershipRowViewModel
    {
        public int MembershipId { get; set; }
        public string PlanName { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
    }
}
