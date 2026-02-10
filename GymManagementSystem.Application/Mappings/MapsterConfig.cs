using GymManagementSystem.Application.DTOs;
using GymManagementSystem.Domain.Entities;
using Mapster;

namespace GymManagementSystem.Application.Mappings
{
    public static class MapsterConfig
    {
        public static void Register()
        {
            TypeAdapterConfig<Member, MemberReadDto>
                .NewConfig()
                .IgnoreNonMapped(true)
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

            TypeAdapterConfig<CreateMemberDto, Member>
                .NewConfig()
                .IgnoreNonMapped(true)
                .Map(dest => dest.FirstName, src => src.FirstName)
                .Map(dest => dest.LastName, src => src.LastName)
                .Map(dest => dest.Email, src => src.Email)
                .Map(dest => dest.PhoneNumber, src => src.PhoneNumber)
                .Map(dest => dest.DateOfBirth, src => src.DateOfBirth)
                .Map(dest => dest.Gender, src => src.Gender)
                .Map(dest => dest.Address, src => src.Address);

            TypeAdapterConfig<UpdateMemberDto, Member>
                .NewConfig()
                .IgnoreNonMapped(true)
                .Map(dest => dest.FirstName, src => src.FirstName)
                .Map(dest => dest.LastName, src => src.LastName)
                .Map(dest => dest.Email, src => src.Email)
                .Map(dest => dest.PhoneNumber, src => src.PhoneNumber)
                .Map(dest => dest.DateOfBirth, src => src.DateOfBirth)
                .Map(dest => dest.Gender, src => src.Gender)
                .Map(dest => dest.Address, src => src.Address)
                .Map(dest => dest.IsActive, src => src.IsActive);

            TypeAdapterConfig<Member, UpdateMemberDto>
                .NewConfig()
                .IgnoreNonMapped(true)
                .Map(dest => dest.Id, src => src.Id)
                .Map(dest => dest.FirstName, src => src.FirstName)
                .Map(dest => dest.LastName, src => src.LastName)
                .Map(dest => dest.Email, src => src.Email)
                .Map(dest => dest.PhoneNumber, src => src.PhoneNumber)
                .Map(dest => dest.DateOfBirth, src => src.DateOfBirth)
                .Map(dest => dest.Gender, src => src.Gender)
                .Map(dest => dest.Address, src => src.Address)
                .Map(dest => dest.IsActive, src => src.IsActive);

            TypeAdapterConfig<CreateTrainingPlanDto, TrainingPlan>
                .NewConfig()
                .IgnoreNonMapped(true)
                .Map(dest => dest.MemberId, src => src.MemberId)
                .Map(dest => dest.TrainerId, src => src.TrainerId)
                .Map(dest => dest.Title, src => src.Title)
                .Map(dest => dest.Notes, src => src.Notes);

            TypeAdapterConfig<CreateTrainingPlanItemDto, TrainingPlanItem>
                .NewConfig()
                .IgnoreNonMapped(true)
                .Map(dest => dest.DayOfWeek, src => src.DayOfWeek)
                .Map(dest => dest.ExerciseName, src => src.ExerciseName)
                .Map(dest => dest.Sets, src => src.Sets)
                .Map(dest => dest.Reps, src => src.Reps)
                .Map(dest => dest.Notes, src => src.Notes);

            TypeAdapterConfig<UpdateTrainingPlanDto, TrainingPlan>
                .NewConfig()
                .IgnoreNonMapped(true)
                .Map(dest => dest.Title, src => src.Title)
                .Map(dest => dest.Notes, src => src.Notes);

            TypeAdapterConfig<UpdateTrainingPlanItemDto, TrainingPlanItem>
                .NewConfig()
                .IgnoreNonMapped(true)
                .Map(dest => dest.DayOfWeek, src => src.DayOfWeek)
                .Map(dest => dest.ExerciseName, src => src.ExerciseName)
                .Map(dest => dest.Sets, src => src.Sets)
                .Map(dest => dest.Reps, src => src.Reps)
                .Map(dest => dest.Notes, src => src.Notes)
                .Map(dest => dest.IsCompleted, src => src.IsCompleted);
            TypeAdapterConfig<TrainingPlan, TrainingPlanDto>
                .NewConfig()
                .IgnoreNonMapped(true)
                .Map(dest => dest.Id, src => src.Id)
                .Map(dest => dest.MemberId, src => src.MemberId)
                .Map(dest => dest.TrainerId, src => src.TrainerId)
                .Map(dest => dest.Title, src => src.Title)
                .Map(dest => dest.Notes, src => src.Notes)
                .Map(dest => dest.CreatedAt, src => src.CreatedAt)
                .Map(dest => dest.Items, src => src.Items);

            TypeAdapterConfig<TrainingPlanItem, TrainingPlanItemDto>
                .NewConfig()
                .IgnoreNonMapped(true)
                .Map(dest => dest.Id, src => src.Id)
                .Map(dest => dest.TrainingPlanId, src => src.TrainingPlanId)
                .Map(dest => dest.DayOfWeek, src => src.DayOfWeek)
                .Map(dest => dest.ExerciseName, src => src.ExerciseName)
                .Map(dest => dest.Sets, src => src.Sets)
                .Map(dest => dest.Reps, src => src.Reps)
                .Map(dest => dest.Notes, src => src.Notes)
                .Map(dest => dest.IsCompleted, src => src.IsCompleted);

            TypeAdapterConfig<CreateNutritionPlanDto, NutritionPlan>
                .NewConfig()
                .IgnoreNonMapped(true)
                .Map(dest => dest.MemberId, src => src.MemberId)
                .Map(dest => dest.TrainerId, src => src.TrainerId)
                .Map(dest => dest.Title, src => src.Title)
                .Map(dest => dest.Notes, src => src.Notes);

            TypeAdapterConfig<CreateNutritionPlanItemDto, NutritionPlanItem>
                .NewConfig()
                .IgnoreNonMapped(true)
                .Map(dest => dest.DayOfWeek, src => src.DayOfWeek)
                .Map(dest => dest.MealType, src => src.MealType)
                .Map(dest => dest.FoodDescription, src => src.FoodDescription)
                .Map(dest => dest.Calories, src => src.Calories)
                .Map(dest => dest.Notes, src => src.Notes);

            TypeAdapterConfig<UpdateNutritionPlanDto, NutritionPlan>
                .NewConfig()
                .IgnoreNonMapped(true)
                .Map(dest => dest.Title, src => src.Title)
                .Map(dest => dest.Notes, src => src.Notes);

            TypeAdapterConfig<UpdateNutritionPlanItemDto, NutritionPlanItem>
                .NewConfig()
                .IgnoreNonMapped(true)
                .Map(dest => dest.DayOfWeek, src => src.DayOfWeek)
                .Map(dest => dest.MealType, src => src.MealType)
                .Map(dest => dest.FoodDescription, src => src.FoodDescription)
                .Map(dest => dest.Calories, src => src.Calories)
                .Map(dest => dest.Notes, src => src.Notes)
                .Map(dest => dest.IsCompleted, src => src.IsCompleted);
            TypeAdapterConfig<NutritionPlan, NutritionPlanDto>
                .NewConfig()
                .IgnoreNonMapped(true)
                .Map(dest => dest.Id, src => src.Id)
                .Map(dest => dest.MemberId, src => src.MemberId)
                .Map(dest => dest.TrainerId, src => src.TrainerId)
                .Map(dest => dest.Title, src => src.Title)
                .Map(dest => dest.Notes, src => src.Notes)
                .Map(dest => dest.CreatedAt, src => src.CreatedAt)
                .Map(dest => dest.Items, src => src.Items);

            TypeAdapterConfig<NutritionPlanItem, NutritionPlanItemDto>
                .NewConfig()
                .IgnoreNonMapped(true)
                .Map(dest => dest.Id, src => src.Id)
                .Map(dest => dest.NutritionPlanId, src => src.NutritionPlanId)
                .Map(dest => dest.DayOfWeek, src => src.DayOfWeek)
                .Map(dest => dest.MealType, src => src.MealType)
                .Map(dest => dest.FoodDescription, src => src.FoodDescription)
                .Map(dest => dest.Calories, src => src.Calories)
                .Map(dest => dest.Notes, src => src.Notes)
                .Map(dest => dest.IsCompleted, src => src.IsCompleted);

            TypeAdapterConfig<CreateWorkoutSessionDto, WorkoutSession>
                .NewConfig()
                .IgnoreNonMapped(true)
                .Map(dest => dest.TrainerId, src => src.TrainerId)
                .Map(dest => dest.Title, src => src.Title)
                .Map(dest => dest.Description, src => src.Description)
                .Map(dest => dest.SessionDate, src => src.SessionDate)
                .Map(dest => dest.StartTime, src => src.StartTime)
                .Map(dest => dest.EndTime, src => src.EndTime)
                .Map(dest => dest.MaxParticipants, src => src.MaxParticipants);

            TypeAdapterConfig<UpdateWorkoutSessionDto, WorkoutSession>
                .NewConfig()
                .IgnoreNonMapped(true)
                .Map(dest => dest.Title, src => src.Title)
                .Map(dest => dest.Description, src => src.Description)
                .Map(dest => dest.SessionDate, src => src.SessionDate)
                .Map(dest => dest.StartTime, src => src.StartTime)
                .Map(dest => dest.EndTime, src => src.EndTime)
                .Map(dest => dest.MaxParticipants, src => src.MaxParticipants);

            TypeAdapterConfig<AssignTrainerDto, TrainerMemberAssignment>
                .NewConfig()
                .IgnoreNonMapped(true)
                .Map(dest => dest.TrainerId, src => src.TrainerId)
                .Map(dest => dest.MemberId, src => src.MemberId)
                .Map(dest => dest.Notes, src => src.Notes);

            TypeAdapterConfig<UpdateTrainerAssignmentDto, TrainerMemberAssignment>
                .NewConfig()
                .IgnoreNonMapped(true)
                .Map(dest => dest.Notes, src => src.Notes);

            TypeAdapterConfig<WorkoutSession, WorkoutSessionDto>
                .NewConfig()
                .IgnoreNonMapped(true)
                .Map(dest => dest.Id, src => src.Id)
                .Map(dest => dest.TrainerId, src => src.TrainerId)
                .Map(dest => dest.Title, src => src.Title)
                .Map(dest => dest.Description, src => src.Description)
                .Map(dest => dest.SessionDate, src => src.SessionDate)
                .Map(dest => dest.StartTime, src => src.StartTime)
                .Map(dest => dest.EndTime, src => src.EndTime)
                .Map(dest => dest.MaxParticipants, src => src.MaxParticipants)
                .Map(dest => dest.CurrentParticipants, src => src.CurrentParticipants);

            TypeAdapterConfig<WorkoutSession, SessionDto>
                .NewConfig()
                .IgnoreNonMapped(true)
                .Map(dest => dest.Id, src => src.Id)
                .Map(dest => dest.TrainerId, src => src.TrainerId)
                .Map(dest => dest.Title, src => src.Title)
                .Map(dest => dest.Description, src => src.Description)
                .Map(dest => dest.SessionDate, src => src.SessionDate)
                .Map(dest => dest.StartTime, src => src.StartTime)
                .Map(dest => dest.EndTime, src => src.EndTime)
                .Map(dest => dest.MaxParticipants, src => src.MaxParticipants)
                .Map(dest => dest.CurrentParticipants, src => src.CurrentParticipants);

            TypeAdapterConfig<SessionDto, WorkoutSession>
                .NewConfig()
                .IgnoreNonMapped(true)
                .Map(dest => dest.Id, src => src.Id)
                .Map(dest => dest.TrainerId, src => src.TrainerId)
                .Map(dest => dest.Title, src => src.Title)
                .Map(dest => dest.Description, src => src.Description)
                .Map(dest => dest.SessionDate, src => src.SessionDate)
                .Map(dest => dest.StartTime, src => src.StartTime)
                .Map(dest => dest.EndTime, src => src.EndTime)
                .Map(dest => dest.MaxParticipants, src => src.MaxParticipants)
                .Map(dest => dest.CurrentParticipants, src => src.CurrentParticipants);

            TypeAdapterConfig<TrainerMemberAssignment, TrainerAssignmentDto>
                .NewConfig()
                .IgnoreNonMapped(true)
                .Map(dest => dest.Id, src => src.Id)
                .Map(dest => dest.TrainerId, src => src.TrainerId)
                .Map(dest => dest.MemberId, src => src.MemberId)
                .Map(dest => dest.AssignedAt, src => src.AssignedAt)
                .Map(dest => dest.Notes, src => src.Notes);

            TypeAdapterConfig<TrainerMemberAssignment, TrainerAssignmentDetailDto>
                .NewConfig()
                .IgnoreNonMapped(true)
                .Map(dest => dest.Id, src => src.Id)
                .Map(dest => dest.MemberId, src => src.MemberId)
                .Map(dest => dest.MemberCode, src => src.Member != null ? src.Member.MemberCode : string.Empty)
                .Map(dest => dest.MemberName, src => src.Member != null ? $"{src.Member.FirstName} {src.Member.LastName}" : "Unknown")
                .Map(dest => dest.AssignedAt, src => src.AssignedAt)
                .Map(dest => dest.Notes, src => src.Notes);

            TypeAdapterConfig<ChatMessage, ChatMessageDto>
                .NewConfig()
                .IgnoreNonMapped(true)
                .Map(dest => dest.Id, src => src.Id)
                .Map(dest => dest.SenderId, src => src.SenderId)
                .Map(dest => dest.ReceiverId, src => src.ReceiverId)
                .Map(dest => dest.Message, src => src.Message)
                .Map(dest => dest.SentAt, src => src.SentAt)
                .Map(dest => dest.IsRead, src => src.IsRead)
                .Map(dest => dest.Type, src => src.Type)
                .Map(dest => dest.AttachmentUrl, src => src.AttachmentUrl);

            TypeAdapterConfig<Trainer, TrainerReadDto>
                .NewConfig()
                .IgnoreNonMapped(true)
                .Map(dest => dest.Id, src => src.Id)
                .Map(dest => dest.FirstName, src => src.FirstName)
                .Map(dest => dest.LastName, src => src.LastName)
                .Map(dest => dest.Email, src => src.Email ?? string.Empty)
                .Map(dest => dest.PhoneNumber, src => src.PhoneNumber ?? string.Empty)
                .Map(dest => dest.Specialty, src => src.Specialty)
                .Map(dest => dest.Certification, src => src.Certification)
                .Map(dest => dest.Experience, src => src.Experience)
                .Map(dest => dest.Salary, src => src.Salary)
                .Map(dest => dest.BankAccount, src => src.BankAccount)
                .Map(dest => dest.IsActive, src => src.IsActive);

            TypeAdapterConfig<CreateTrainerDto, Trainer>
                .NewConfig()
                .IgnoreNonMapped(true)
                .Map(dest => dest.FirstName, src => src.FirstName)
                .Map(dest => dest.LastName, src => src.LastName)
                .Map(dest => dest.Email, src => src.Email)
                .Map(dest => dest.PhoneNumber, src => src.PhoneNumber)
                .Map(dest => dest.Specialty, src => src.Specialty)
                .Map(dest => dest.Certification, src => src.Certification)
                .Map(dest => dest.Experience, src => src.Experience)
                .Map(dest => dest.Salary, src => src.Salary)
                .Map(dest => dest.BankAccount, src => src.BankAccount)
                .Map(dest => dest.IsActive, src => src.IsActive);

            TypeAdapterConfig<UpdateTrainerDto, Trainer>
                .NewConfig()
                .IgnoreNonMapped(true)
                .Map(dest => dest.FirstName, src => src.FirstName)
                .Map(dest => dest.LastName, src => src.LastName)
                .Map(dest => dest.Email, src => src.Email)
                .Map(dest => dest.PhoneNumber, src => src.PhoneNumber)
                .Map(dest => dest.Specialty, src => src.Specialty)
                .Map(dest => dest.Certification, src => src.Certification)
                .Map(dest => dest.Experience, src => src.Experience)
                .Map(dest => dest.Salary, src => src.Salary)
                .Map(dest => dest.BankAccount, src => src.BankAccount)
                .Map(dest => dest.IsActive, src => src.IsActive);

            TypeAdapterConfig<Trainer, UpdateTrainerDto>
                .NewConfig()
                .IgnoreNonMapped(true)
                .Map(dest => dest.Id, src => src.Id)
                .Map(dest => dest.FirstName, src => src.FirstName)
                .Map(dest => dest.LastName, src => src.LastName)
                .Map(dest => dest.Email, src => src.Email ?? string.Empty)
                .Map(dest => dest.PhoneNumber, src => src.PhoneNumber ?? string.Empty)
                .Map(dest => dest.Specialty, src => src.Specialty)
                .Map(dest => dest.Certification, src => src.Certification)
                .Map(dest => dest.Experience, src => src.Experience)
                .Map(dest => dest.Salary, src => src.Salary)
                .Map(dest => dest.BankAccount, src => src.BankAccount)
                .Map(dest => dest.IsActive, src => src.IsActive);

            TypeAdapterConfig<LoginAudit, LoginAuditDto>
                .NewConfig()
                .IgnoreNonMapped(true)
                .Map(dest => dest.Id, src => src.Id)
                .Map(dest => dest.Email, src => src.Email)
                .Map(dest => dest.UserName, src => src.User != null ? $"{src.User.FirstName} {src.User.LastName}" : "Unknown")
                .Map(dest => dest.IpAddress, src => src.IpAddress ?? "Unknown")
                .Map(dest => dest.LoginTime, src => src.LoginTime)
                .Map(dest => dest.LogoutTime, src => src.LogoutTime)
                .Map(dest => dest.IsSuccessful, src => src.IsSuccessful)
                .Map(dest => dest.FailureReason, src => src.FailureReason);

            TypeAdapterConfig<LoginAudit, ActiveSessionDto>
                .NewConfig()
                .IgnoreNonMapped(true)
                .Map(dest => dest.AuditId, src => src.Id)
                .Map(dest => dest.Email, src => src.Email)
                .Map(dest => dest.UserName, src => src.User != null ? $"{src.User.FirstName} {src.User.LastName}" : "Unknown")
                .Map(dest => dest.IpAddress, src => src.IpAddress ?? "Unknown")
                .Map(dest => dest.UserAgent, src => src.UserAgent)
                .Map(dest => dest.LoginTime, src => src.LoginTime);

            TypeAdapterConfig<Trainer, UserLookupDto>
                .NewConfig()
                .IgnoreNonMapped(true)
                .Map(dest => dest.Id, src => src.Id)
                .Map(dest => dest.DisplayName, src => $"{src.FirstName} {src.LastName}");

            TypeAdapterConfig<Member, UserLookupDto>
                .NewConfig()
                .IgnoreNonMapped(true)
                .Map(dest => dest.Id, src => src.Id)
                .Map(dest => dest.DisplayName, src => $"{src.MemberCode} - {src.FirstName} {src.LastName}")
                .Map(dest => dest.Code, src => src.MemberCode);
        }
    }
}

