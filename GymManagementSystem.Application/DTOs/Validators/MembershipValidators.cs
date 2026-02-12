using FluentValidation;
using GymManagementSystem.Domain.Enums;

namespace GymManagementSystem.Application.DTOs.Validators;

internal class CreateMembershipPlanDtoValidator : AbstractValidator<CreateMembershipPlanDto>
{
    public CreateMembershipPlanDtoValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MinimumLength(3).MaximumLength(100);
        RuleFor(x => x.DurationInDays).InclusiveBetween(1, 366);
        RuleFor(x => x.Price).GreaterThan(0);
        RuleFor(x => x.CommissionRate).GreaterThanOrEqualTo(0);
        RuleFor(x => x.IncludedSessionsPerMonth).GreaterThanOrEqualTo(0).LessThanOrEqualTo(200);
        RuleFor(x => x.DiscountPercentage).InclusiveBetween(0, 100);
        RuleFor(x => x.Description).MaximumLength(1000);
    }
}

internal class UpdateMembershipPlanDtoValidator : AbstractValidator<UpdateMembershipPlanDto>
{
    public UpdateMembershipPlanDtoValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.Name).NotEmpty().MinimumLength(3).MaximumLength(100);
        RuleFor(x => x.DurationInDays).InclusiveBetween(1, 366);
        RuleFor(x => x.Price).GreaterThan(0);
        RuleFor(x => x.CommissionRate).GreaterThanOrEqualTo(0);
        RuleFor(x => x.IncludedSessionsPerMonth).GreaterThanOrEqualTo(0).LessThanOrEqualTo(200);
        RuleFor(x => x.DiscountPercentage).InclusiveBetween(0, 100);
        RuleFor(x => x.Description).MaximumLength(1000);
    }
}

internal class CreateMembershipDtoValidator : AbstractValidator<CreateMembershipDto>
{
    public CreateMembershipDtoValidator()
    {
        RuleFor(x => x.MemberId).NotEmpty();
        RuleFor(x => x.BranchId).GreaterThan(0).When(x => x.BranchId.HasValue);
        RuleFor(x => x.MembershipPlanId).GreaterThan(0);
        RuleFor(x => x.StartDate).Must(d => d.Date >= DateTime.UtcNow.Date)
            .WithMessage("StartDate must be today or in the future.");
        RuleFor(x => x.Source).IsInEnum();
        RuleFor(x => x.PaymentAmount).GreaterThanOrEqualTo(0);
        RuleFor(x => x.WalletAmountToUse).GreaterThanOrEqualTo(0);

        RuleFor(x => x.PaymentAmount)
            .GreaterThan(0)
            .When(x => x.Source == MembershipSource.Online)
            .WithMessage("PaymentAmount must be greater than 0 for online subscriptions.");

        RuleFor(x => x)
            .Must(x => x.PaymentAmount > 0 || x.WalletAmountToUse > 0)
            .WithMessage("Either payment amount or wallet amount must be greater than 0.");
    }
}

internal class ConfirmPaymentDtoValidator : AbstractValidator<ConfirmPaymentDto>
{
    public ConfirmPaymentDtoValidator()
    {
        RuleFor(x => x.ConfirmedAmount)
            .GreaterThan(0)
            .When(x => x.ConfirmedAmount.HasValue);
    }
}

internal class RequestSubscriptionDtoValidator : AbstractValidator<RequestSubscriptionDto>
{
    public RequestSubscriptionDtoValidator()
    {
        RuleFor(x => x.MemberId).NotEmpty();
        RuleFor(x => x.BranchId).GreaterThan(0).When(x => x.BranchId.HasValue);
        RuleFor(x => x.MembershipPlanId).GreaterThan(0);
        RuleFor(x => x.StartDate).Must(d => d.Date >= DateTime.UtcNow.Date)
            .WithMessage("StartDate must be today or in the future.");
        RuleFor(x => x.PaymentAmount).GreaterThanOrEqualTo(0);
        RuleFor(x => x.WalletAmountToUse).GreaterThanOrEqualTo(0);
        RuleFor(x => x.PaymentMethod).Equal(PaymentMethod.VodafoneCash);
        RuleFor(x => x)
            .Must(x => x.PaymentAmount > 0 || x.WalletAmountToUse > 0)
            .WithMessage("Either payment amount or wallet amount must be greater than 0.");
    }
}

internal class CreateDirectMembershipDtoValidator : AbstractValidator<CreateDirectMembershipDto>
{
    public CreateDirectMembershipDtoValidator()
    {
        RuleFor(x => x.MemberId).NotEmpty();
        RuleFor(x => x.BranchId).GreaterThan(0).When(x => x.BranchId.HasValue);
        RuleFor(x => x.MembershipPlanId).GreaterThan(0);
        RuleFor(x => x.StartDate).Must(d => d.Date >= DateTime.UtcNow.Date)
            .WithMessage("StartDate must be today or in the future.");
        RuleFor(x => x.PaymentAmount).GreaterThanOrEqualTo(0);
        RuleFor(x => x.WalletAmountToUse).GreaterThanOrEqualTo(0);
        RuleFor(x => x.PaymentMethod).Equal(PaymentMethod.VodafoneCash);
        RuleFor(x => x)
            .Must(x => x.PaymentAmount > 0 || x.WalletAmountToUse > 0)
            .WithMessage("Either payment amount or wallet amount must be greater than 0.");
    }
}

internal class UploadPaymentProofDtoValidator : AbstractValidator<UploadPaymentProofDto>
{
    public UploadPaymentProofDtoValidator()
    {
        RuleFor(x => x.PaymentId).GreaterThan(0);
        RuleFor(x => x.PaymentProofUrl).NotEmpty().MaximumLength(500);
    }
}

internal class ReviewPaymentDtoValidator : AbstractValidator<ReviewPaymentDto>
{
    public ReviewPaymentDtoValidator()
    {
        RuleFor(x => x.ConfirmedAmount)
            .GreaterThan(0)
            .When(x => x.Approve && x.ConfirmedAmount.HasValue);

        RuleFor(x => x.RejectionReason)
            .NotEmpty()
            .MaximumLength(500)
            .When(x => !x.Approve);
    }
}

internal class AdjustWalletDtoValidator : AbstractValidator<AdjustWalletDto>
{
    public AdjustWalletDtoValidator()
    {
        RuleFor(x => x.MemberId).NotEmpty();
        RuleFor(x => x.Amount).NotEqual(0);
    }
}

internal class UseWalletForSessionBookingDtoValidator : AbstractValidator<UseWalletForSessionBookingDto>
{
    public UseWalletForSessionBookingDtoValidator()
    {
        RuleFor(x => x.MemberId).NotEmpty();
        RuleFor(x => x.WorkoutSessionId).GreaterThan(0);
        RuleFor(x => x.Amount).GreaterThan(0);
    }
}

internal class FreezeMembershipDtoValidator : AbstractValidator<FreezeMembershipDto>
{
    public FreezeMembershipDtoValidator()
    {
        RuleFor(x => x.FreezeStartDate).NotEmpty();
    }
}

internal class UpgradeMembershipDtoValidator : AbstractValidator<UpgradeMembershipDto>
{
    public UpgradeMembershipDtoValidator()
    {
        RuleFor(x => x.MemberId).NotEmpty();
        RuleFor(x => x.NewMembershipPlanId).GreaterThan(0);
    }
}
