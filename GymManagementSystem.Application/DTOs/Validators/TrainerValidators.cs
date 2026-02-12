using FluentValidation;

namespace GymManagementSystem.Application.DTOs.Validators
{
    internal class CreateTrainerDtoValidator : AbstractValidator<CreateTrainerDto>
    {
        public CreateTrainerDtoValidator()
        {
            RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
            RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
            RuleFor(x => x.Email).NotEmpty().EmailAddress();
            RuleFor(x => x.PhoneNumber).NotEmpty().MaximumLength(20);
            RuleFor(x => x.Specialty).NotEmpty().MaximumLength(100);
            RuleFor(x => x.Certification).MaximumLength(200);
            RuleFor(x => x.Experience).MaximumLength(200);
            RuleFor(x => x.Salary).GreaterThanOrEqualTo(0);
            RuleFor(x => x.BankAccount).MaximumLength(100);
            RuleFor(x => x.BranchId).GreaterThan(0).When(x => x.BranchId.HasValue);
        }
    }

    internal class UpdateTrainerDtoValidator : AbstractValidator<UpdateTrainerDto>
    {
        public UpdateTrainerDtoValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
            RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
            RuleFor(x => x.Email).NotEmpty().EmailAddress();
            RuleFor(x => x.PhoneNumber).NotEmpty().MaximumLength(20);
            RuleFor(x => x.Specialty).NotEmpty().MaximumLength(100);
            RuleFor(x => x.Certification).MaximumLength(200);
            RuleFor(x => x.Experience).MaximumLength(200);
            RuleFor(x => x.Salary).GreaterThanOrEqualTo(0);
            RuleFor(x => x.BankAccount).MaximumLength(100);
            RuleFor(x => x.BranchId).GreaterThan(0).When(x => x.BranchId.HasValue);
        }
    }

    internal class TrainerDtoValidator : AbstractValidator<TrainerDto>
    {
        public TrainerDtoValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
            RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
            RuleFor(x => x.Email).NotEmpty().EmailAddress();
            RuleFor(x => x.BranchId).GreaterThan(0).When(x => x.BranchId.HasValue);
        }
    }
}
