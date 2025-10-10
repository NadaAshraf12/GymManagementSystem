using GymManagementSystem.Application.DTOs;
using GymManagementSystem.Domain.Entities;
using Mapster;

namespace GymManagementSystem.Application.Mappings
{
    public static class MapsterConfig
    {
        public static void Register()
        {
            TypeAdapterConfig<Member, MemberDto>
                .NewConfig()
                .Map(dest => dest.Id, src => src.Id)
                .Map(dest => dest.FirstName, src => src.FirstName)
                .Map(dest => dest.LastName, src => src.LastName)
                .Map(dest => dest.Email, src => src.Email)
                .Map(dest => dest.PhoneNumber, src => src.PhoneNumber)
                .Map(dest => dest.DateOfBirth, src => src.DateOfBirth)
                .Map(dest => dest.Gender, src => src.Gender)
                .Map(dest => dest.Address, src => src.Address)
                .Map(dest => dest.MemberCode, src => src.MemberCode)
                .Map(dest => dest.IsActive, src => src.IsActive);

            TypeAdapterConfig<CreateTrainingPlanDto, TrainingPlan>
                .NewConfig()
                .Map(dest => dest.MemberId, src => src.MemberId)
                .Map(dest => dest.TrainerId, src => src.TrainerId)
                .Map(dest => dest.Title, src => src.Title)
                .Map(dest => dest.Notes, src => src.Notes);

            TypeAdapterConfig<CreateTrainingPlanItemDto, TrainingPlanItem>
                .NewConfig()
                .Map(dest => dest.DayOfWeek, src => src.DayOfWeek)
                .Map(dest => dest.ExerciseName, src => src.ExerciseName)
                .Map(dest => dest.Sets, src => src.Sets)
                .Map(dest => dest.Reps, src => src.Reps)
                .Map(dest => dest.Notes, src => src.Notes);
        }
    }
}

