using FluentValidation;

namespace GymManagementSystem.Application.DTOs.Validators
{
    internal class CreateTrainingPlanDtoValidator : AbstractValidator<CreateTrainingPlanDto>
    {
        public CreateTrainingPlanDtoValidator()
        {
            RuleFor(x => x.MemberId).NotEmpty();
            RuleFor(x => x.TrainerId).NotEmpty();
            RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
            RuleForEach(x => x.Items).SetValidator(new CreateTrainingPlanItemDtoValidator());
        }
    }

    internal class CreateTrainingPlanItemDtoValidator : AbstractValidator<CreateTrainingPlanItemDto>
    {
        public CreateTrainingPlanItemDtoValidator()
        {
            RuleFor(x => x.ExerciseName).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Sets).GreaterThan(0).LessThanOrEqualTo(20);
            RuleFor(x => x.Reps).GreaterThan(0).LessThanOrEqualTo(100);
        }
    }

    internal class CreateNutritionPlanDtoValidator : AbstractValidator<CreateNutritionPlanDto>
    {
        public CreateNutritionPlanDtoValidator()
        {
            RuleFor(x => x.MemberId).NotEmpty();
            RuleFor(x => x.TrainerId).NotEmpty();
            RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
            RuleForEach(x => x.Items).SetValidator(new CreateNutritionPlanItemDtoValidator());
        }
    }

    internal class CreateNutritionPlanItemDtoValidator : AbstractValidator<CreateNutritionPlanItemDto>
    {
        public CreateNutritionPlanItemDtoValidator()
        {
            RuleFor(x => x.MealType).NotEmpty().MaximumLength(100);
            RuleFor(x => x.FoodDescription).NotEmpty().MaximumLength(500);
            RuleFor(x => x.Calories).GreaterThanOrEqualTo(0).LessThanOrEqualTo(5000);
        }
    }
}

