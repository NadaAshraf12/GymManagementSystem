using FluentValidation;

namespace GymManagementSystem.Application.DTOs.Validators
{
    internal class AssignTrainerDtoValidator : AbstractValidator<AssignTrainerDto>
    {
        public AssignTrainerDtoValidator()
        {
            RuleFor(x => x.TrainerId).NotEmpty();
            RuleFor(x => x.MemberId).NotEmpty();
            RuleFor(x => x.Notes).MaximumLength(500);
        }
    }

    internal class UpdateTrainerAssignmentDtoValidator : AbstractValidator<UpdateTrainerAssignmentDto>
    {
        public UpdateTrainerAssignmentDtoValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0);
            RuleFor(x => x.Notes).MaximumLength(500);
        }
    }
}

