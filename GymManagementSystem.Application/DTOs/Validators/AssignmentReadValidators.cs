using FluentValidation;

namespace GymManagementSystem.Application.DTOs.Validators
{
    internal class TrainerAssignmentDtoValidator : AbstractValidator<TrainerAssignmentDto>
    {
        public TrainerAssignmentDtoValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0);
            RuleFor(x => x.TrainerId).NotEmpty();
            RuleFor(x => x.MemberId).NotEmpty();
        }
    }

    internal class TrainerAssignmentDetailDtoValidator : AbstractValidator<TrainerAssignmentDetailDto>
    {
        public TrainerAssignmentDetailDtoValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0);
            RuleFor(x => x.MemberId).NotEmpty();
            RuleFor(x => x.MemberName).NotEmpty();
        }
    }

    internal class AssignmentResultDtoValidator : AbstractValidator<AssignmentResultDto>
    {
        public AssignmentResultDtoValidator()
        {
            RuleFor(x => x.Message).NotEmpty();
        }
    }

    internal class OperationResultDtoValidator : AbstractValidator<OperationResultDto>
    {
        public OperationResultDtoValidator()
        {
            RuleFor(x => x.Message).NotEmpty();
        }
    }
}