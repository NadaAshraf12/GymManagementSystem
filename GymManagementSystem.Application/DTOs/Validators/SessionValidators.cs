using FluentValidation;

namespace GymManagementSystem.Application.DTOs.Validators
{
    internal class CreateWorkoutSessionDtoValidator : AbstractValidator<CreateWorkoutSessionDto>
    {
        public CreateWorkoutSessionDtoValidator()
        {
            RuleFor(x => x.TrainerId).NotEmpty();
            RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Description).NotEmpty().MaximumLength(500);
            RuleFor(x => x.SessionDate).GreaterThanOrEqualTo(DateTime.UtcNow.Date);
            RuleFor(x => x.StartTime).LessThan(x => x.EndTime);
            RuleFor(x => x.MaxParticipants).GreaterThan(0).LessThanOrEqualTo(100);
        }
    }

    internal class BookMemberToSessionDtoValidator : AbstractValidator<BookMemberToSessionDto>
    {
        public BookMemberToSessionDtoValidator()
        {
            RuleFor(x => x.MemberId).NotEmpty();
            RuleFor(x => x.WorkoutSessionId).GreaterThan(0);
        }
    }

    internal class UpdateWorkoutSessionDtoValidator : AbstractValidator<UpdateWorkoutSessionDto>
    {
        public UpdateWorkoutSessionDtoValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0);
            RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Description).NotEmpty().MaximumLength(500);
            RuleFor(x => x.SessionDate).GreaterThanOrEqualTo(DateTime.UtcNow.Date);
            RuleFor(x => x.StartTime).LessThan(x => x.EndTime);
            RuleFor(x => x.MaxParticipants).GreaterThan(0).LessThanOrEqualTo(100);
        }
    }
}

