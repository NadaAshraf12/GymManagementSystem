using FluentValidation;

namespace GymManagementSystem.Application.DTOs.Validators
{
    internal class CreateMemberDtoValidator : AbstractValidator<CreateMemberDto>
    {
        public CreateMemberDtoValidator()
        {
            RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
            RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
            RuleFor(x => x.Email).NotEmpty().EmailAddress();
            RuleFor(x => x.PhoneNumber).NotEmpty().MaximumLength(20);
            RuleFor(x => x.DateOfBirth).LessThan(DateTime.UtcNow.Date);
            RuleFor(x => x.Gender).NotEmpty().MaximumLength(20);
            RuleFor(x => x.Address).NotEmpty().MaximumLength(250);
            RuleFor(x => x.BranchId).GreaterThan(0).When(x => x.BranchId.HasValue);
        }
    }

    internal class UpdateMemberDtoValidator : AbstractValidator<UpdateMemberDto>
    {
        public UpdateMemberDtoValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
            RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
            RuleFor(x => x.Email).NotEmpty().EmailAddress();
            RuleFor(x => x.PhoneNumber).NotEmpty().MaximumLength(20);
            RuleFor(x => x.DateOfBirth).LessThan(DateTime.UtcNow.Date);
            RuleFor(x => x.Gender).NotEmpty().MaximumLength(20);
            RuleFor(x => x.Address).NotEmpty().MaximumLength(250);
            RuleFor(x => x.BranchId).GreaterThan(0).When(x => x.BranchId.HasValue);
        }
    }

    internal class MemberDtoValidator : AbstractValidator<MemberDto>
    {
        public MemberDtoValidator()
        {
            RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
            RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
            RuleFor(x => x.Email).NotEmpty().EmailAddress();
            RuleFor(x => x.PhoneNumber).NotEmpty().MaximumLength(20);
            RuleFor(x => x.DateOfBirth).LessThan(DateTime.UtcNow.Date);
            RuleFor(x => x.Gender).NotEmpty().MaximumLength(20);
            RuleFor(x => x.Address).NotEmpty().MaximumLength(250);
            RuleFor(x => x.BranchId).GreaterThan(0).When(x => x.BranchId.HasValue);
        }
    }
}

