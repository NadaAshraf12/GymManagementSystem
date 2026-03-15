using FluentValidation;

namespace GymManagementSystem.Application.DTOs.Validators
{
    internal class MemberReadDtoValidator : AbstractValidator<MemberReadDto>
    {
        public MemberReadDtoValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.Email).NotEmpty().EmailAddress();
            RuleFor(x => x.MemberCode).NotEmpty();
        }
    }

    internal class TrainerReadDtoValidator : AbstractValidator<TrainerReadDto>
    {
        public TrainerReadDtoValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.Email).NotEmpty().EmailAddress();
        }
    }

    internal class WorkoutSessionDtoValidator : AbstractValidator<WorkoutSessionDto>
    {
        public WorkoutSessionDtoValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0);
            RuleFor(x => x.TrainerId).NotEmpty();
            RuleFor(x => x.Title).NotEmpty();
        }
    }

    internal class SessionDtoValidator : AbstractValidator<SessionDto>
    {
        public SessionDtoValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0);
            RuleFor(x => x.TrainerId).NotEmpty();
            RuleFor(x => x.Title).NotEmpty();
        }
    }

    internal class TrainingPlanDtoValidator : AbstractValidator<TrainingPlanDto>
    {
        public TrainingPlanDtoValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0);
            RuleFor(x => x.MemberId).NotEmpty();
            RuleFor(x => x.TrainerId).NotEmpty();
            RuleFor(x => x.Title).NotEmpty();
        }
    }

    internal class TrainingPlanItemDtoValidator : AbstractValidator<TrainingPlanItemDto>
    {
        public TrainingPlanItemDtoValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0);
            RuleFor(x => x.ExerciseName).NotEmpty();
        }
    }

    internal class NutritionPlanDtoValidator : AbstractValidator<NutritionPlanDto>
    {
        public NutritionPlanDtoValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0);
            RuleFor(x => x.MemberId).NotEmpty();
            RuleFor(x => x.TrainerId).NotEmpty();
            RuleFor(x => x.Title).NotEmpty();
        }
    }

    internal class NutritionPlanItemDtoValidator : AbstractValidator<NutritionPlanItemDto>
    {
        public NutritionPlanItemDtoValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0);
            RuleFor(x => x.MealType).NotEmpty();
            RuleFor(x => x.FoodDescription).NotEmpty();
        }
    }

    internal class ApiResponseValidator<T> : AbstractValidator<ApiResponse<T>>
    {
        public ApiResponseValidator()
        {
            RuleFor(x => x.StatusCode).GreaterThanOrEqualTo(0);
            RuleFor(x => x.Message).NotEmpty();
        }
    }
}
