using FluentValidation;

namespace GymManagementSystem.Application.DTOs.Validators
{
    internal class LoginAuditDtoValidator : AbstractValidator<LoginAuditDto>
    {
        public LoginAuditDtoValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0);
            RuleFor(x => x.Email).NotEmpty().EmailAddress();
        }
    }

    internal class ActiveSessionDtoValidator : AbstractValidator<ActiveSessionDto>
    {
        public ActiveSessionDtoValidator()
        {
            RuleFor(x => x.AuditId).GreaterThan(0);
            RuleFor(x => x.Email).NotEmpty().EmailAddress();
        }
    }

    internal class UserLookupDtoValidator : AbstractValidator<UserLookupDto>
    {
        public UserLookupDtoValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.DisplayName).NotEmpty();
        }
    }

    internal class AssignTrainerLookupDtoValidator : AbstractValidator<AssignTrainerLookupDto>
    {
        public AssignTrainerLookupDtoValidator()
        {
            RuleFor(x => x.Trainers).NotNull();
            RuleFor(x => x.Members).NotNull();
        }
    }

    internal class PaginatedResultValidator<T> : AbstractValidator<PaginatedResult<T>>
    {
        public PaginatedResultValidator()
        {
            RuleFor(x => x.Items).NotNull();
            RuleFor(x => x.TotalItems).GreaterThanOrEqualTo(0);
            RuleFor(x => x.CurrentPage).GreaterThan(0);
            RuleFor(x => x.PageSize).GreaterThanOrEqualTo(0);
            RuleFor(x => x.TotalPages).GreaterThanOrEqualTo(0);
        }
    }
}