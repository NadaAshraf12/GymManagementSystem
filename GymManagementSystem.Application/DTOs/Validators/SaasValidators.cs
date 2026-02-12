using FluentValidation;

namespace GymManagementSystem.Application.DTOs.Validators;

internal class CreateBranchDtoValidator : AbstractValidator<CreateBranchDto>
{
    public CreateBranchDtoValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Address).NotEmpty().MaximumLength(500);
    }
}

internal class AssignUserBranchDtoValidator : AbstractValidator<AssignUserBranchDto>
{
    public AssignUserBranchDtoValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.BranchId).GreaterThan(0);
    }
}

internal class MarkNotificationReadDtoValidator : AbstractValidator<MarkNotificationReadDto>
{
    public MarkNotificationReadDtoValidator()
    {
        RuleFor(x => x.NotificationId).GreaterThan(0);
    }
}

internal class CreateAddOnDtoValidator : AbstractValidator<CreateAddOnDto>
{
    public CreateAddOnDtoValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Price).GreaterThan(0);
        RuleFor(x => x.BranchId).GreaterThan(0).When(x => x.BranchId.HasValue);
    }
}

internal class PurchaseAddOnDtoValidator : AbstractValidator<PurchaseAddOnDto>
{
    public PurchaseAddOnDtoValidator()
    {
        RuleFor(x => x.MemberId).NotEmpty();
        RuleFor(x => x.AddOnId).GreaterThan(0);
    }
}
