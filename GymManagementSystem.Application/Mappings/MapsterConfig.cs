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
                .Map(dest => dest.BranchId, src => src.BranchId)
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
                .Map(dest => dest.Address, src => src.Address)
                .Map(dest => dest.BranchId, src => src.BranchId);

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
                .Map(dest => dest.BranchId, src => src.BranchId)
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
                .Map(dest => dest.BranchId, src => src.BranchId)
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
                .Map(dest => dest.BranchId, src => src.BranchId)
                .Map(dest => dest.Title, src => src.Title)
                .Map(dest => dest.Description, src => src.Description)
                .Map(dest => dest.SessionDate, src => src.SessionDate)
                .Map(dest => dest.Price, src => src.Price)
                .Map(dest => dest.StartTime, src => src.StartTime)
                .Map(dest => dest.EndTime, src => src.EndTime)
                .Map(dest => dest.MaxParticipants, src => src.MaxParticipants);

            TypeAdapterConfig<UpdateWorkoutSessionDto, WorkoutSession>
                .NewConfig()
                .IgnoreNonMapped(true)
                .Map(dest => dest.BranchId, src => src.BranchId)
                .Map(dest => dest.Title, src => src.Title)
                .Map(dest => dest.Description, src => src.Description)
                .Map(dest => dest.SessionDate, src => src.SessionDate)
                .Map(dest => dest.Price, src => src.Price)
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
                .Map(dest => dest.BranchId, src => src.BranchId)
                .Map(dest => dest.Title, src => src.Title)
                .Map(dest => dest.Description, src => src.Description)
                .Map(dest => dest.SessionDate, src => src.SessionDate)
                .Map(dest => dest.Price, src => src.Price)
                .Map(dest => dest.StartTime, src => src.StartTime)
                .Map(dest => dest.EndTime, src => src.EndTime)
                .Map(dest => dest.MaxParticipants, src => src.MaxParticipants)
                .Map(dest => dest.CurrentParticipants, src => src.CurrentParticipants);

            TypeAdapterConfig<WorkoutSession, SessionDto>
                .NewConfig()
                .IgnoreNonMapped(true)
                .Map(dest => dest.Id, src => src.Id)
                .Map(dest => dest.TrainerId, src => src.TrainerId)
                .Map(dest => dest.BranchId, src => src.BranchId)
                .Map(dest => dest.Title, src => src.Title)
                .Map(dest => dest.Description, src => src.Description)
                .Map(dest => dest.SessionDate, src => src.SessionDate)
                .Map(dest => dest.Price, src => src.Price)
                .Map(dest => dest.StartTime, src => src.StartTime)
                .Map(dest => dest.EndTime, src => src.EndTime)
                .Map(dest => dest.MaxParticipants, src => src.MaxParticipants)
                .Map(dest => dest.CurrentParticipants, src => src.CurrentParticipants);

            TypeAdapterConfig<SessionDto, WorkoutSession>
                .NewConfig()
                .IgnoreNonMapped(true)
                .Map(dest => dest.Id, src => src.Id)
                .Map(dest => dest.TrainerId, src => src.TrainerId)
                .Map(dest => dest.BranchId, src => src.BranchId)
                .Map(dest => dest.Title, src => src.Title)
                .Map(dest => dest.Description, src => src.Description)
                .Map(dest => dest.SessionDate, src => src.SessionDate)
                .Map(dest => dest.Price, src => src.Price)
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
                .Map(dest => dest.BranchId, src => src.BranchId)
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
                .Map(dest => dest.BranchId, src => src.BranchId)
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
                .Map(dest => dest.BranchId, src => src.BranchId)
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
                .Map(dest => dest.BranchId, src => src.BranchId)
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

            TypeAdapterConfig<CreateMembershipPlanDto, MembershipPlan>
                .NewConfig()
                .IgnoreNonMapped(true)
                .Map(dest => dest.Name, src => src.Name)
                .Map(dest => dest.DurationInDays, src => src.DurationInDays)
                .Map(dest => dest.Price, src => src.Price)
                .Map(dest => dest.CommissionRate, src => src.CommissionRate)
                .Map(dest => dest.IncludedSessionsPerMonth, src => src.IncludedSessionsPerMonth)
                .Map(dest => dest.SessionDiscountPercentage, src => src.DiscountPercentage)
                .Map(dest => dest.PriorityBooking, src => src.PriorityBooking)
                .Map(dest => dest.AddOnAccess, src => src.AddOnAccess)
                .Map(dest => dest.Description, src => src.Description)
                .Map(dest => dest.IsActive, src => src.IsActive);

            TypeAdapterConfig<UpdateMembershipPlanDto, MembershipPlan>
                .NewConfig()
                .IgnoreNonMapped(true)
                .Map(dest => dest.Name, src => src.Name)
                .Map(dest => dest.DurationInDays, src => src.DurationInDays)
                .Map(dest => dest.Price, src => src.Price)
                .Map(dest => dest.CommissionRate, src => src.CommissionRate)
                .Map(dest => dest.IncludedSessionsPerMonth, src => src.IncludedSessionsPerMonth)
                .Map(dest => dest.SessionDiscountPercentage, src => src.DiscountPercentage)
                .Map(dest => dest.PriorityBooking, src => src.PriorityBooking)
                .Map(dest => dest.AddOnAccess, src => src.AddOnAccess)
                .Map(dest => dest.Description, src => src.Description)
                .Map(dest => dest.IsActive, src => src.IsActive);

            TypeAdapterConfig<MembershipPlan, MembershipPlanReadDto>
                .NewConfig()
                .IgnoreNonMapped(true)
                .Map(dest => dest.Id, src => src.Id)
                .Map(dest => dest.Name, src => src.Name)
                .Map(dest => dest.DurationInDays, src => src.DurationInDays)
                .Map(dest => dest.Price, src => src.Price)
                .Map(dest => dest.CommissionRate, src => src.CommissionRate)
                .Map(dest => dest.IncludedSessionsPerMonth, src => src.IncludedSessionsPerMonth)
                .Map(dest => dest.DiscountPercentage, src => src.SessionDiscountPercentage)
                .Map(dest => dest.PriorityBooking, src => src.PriorityBooking)
                .Map(dest => dest.AddOnAccess, src => src.AddOnAccess)
                .Map(dest => dest.Description, src => src.Description)
                .Map(dest => dest.IsActive, src => src.IsActive);

            TypeAdapterConfig<Payment, PaymentReadDto>
                .NewConfig()
                .IgnoreNonMapped(true)
                .Map(dest => dest.Id, src => src.Id)
                .Map(dest => dest.MembershipId, src => src.MembershipId)
                .Map(dest => dest.Amount, src => src.Amount)
                .Map(dest => dest.PaymentMethod, src => src.PaymentMethod)
                .Map(dest => dest.PaymentStatus, src => src.PaymentStatus)
                .Map(dest => dest.PaymentProofUrl, src => src.PaymentProofUrl)
                .Map(dest => dest.PaidAt, src => src.PaidAt)
                .Map(dest => dest.ReviewedAt, src => src.ReviewedAt)
                .Map(dest => dest.RejectionReason, src => src.RejectionReason)
                .Map(dest => dest.ConfirmedByAdminId, src => src.ConfirmedByAdminId);

            TypeAdapterConfig<Membership, MembershipReadDto>
                .NewConfig()
                .IgnoreNonMapped(true)
                .Map(dest => dest.Id, src => src.Id)
                .Map(dest => dest.MemberId, src => src.MemberId)
                .Map(dest => dest.BranchId, src => src.BranchId)
                .Map(dest => dest.MembershipPlanId, src => src.MembershipPlanId)
                .Map(dest => dest.MembershipPlanName, src => src.MembershipPlan.Name)
                .Map(dest => dest.StartDate, src => src.StartDate)
                .Map(dest => dest.EndDate, src => src.EndDate)
                .Map(dest => dest.Status, src => src.Status)
                .Map(dest => dest.Source, src => src.Source)
                .Map(dest => dest.AutoRenewEnabled, src => src.AutoRenewEnabled)
                .Map(dest => dest.FreezeStartDate, src => src.FreezeStartDate)
                .Map(dest => dest.FreezeEndDate, src => src.FreezeEndDate)
                .Map(dest => dest.TotalPaid, src => src.TotalPaid)
                .Map(dest => dest.RemainingBalanceUsedFromWallet, src => src.RemainingBalanceUsedFromWallet)
                .Map(dest => dest.Payments, src => src.Payments);

            TypeAdapterConfig<Payment, PendingPaymentReadDto>
                .NewConfig()
                .IgnoreNonMapped(true)
                .Map(dest => dest.PaymentId, src => src.Id)
                .Map(dest => dest.MembershipId, src => src.MembershipId)
                .Map(dest => dest.MemberId, src => src.Membership.MemberId)
                .Map(dest => dest.Amount, src => src.Amount)
                .Map(dest => dest.PaymentProofUrl, src => src.PaymentProofUrl)
                .Map(dest => dest.PaidAt, src => src.PaidAt);

            TypeAdapterConfig<WalletTransaction, WalletTransactionReadDto>
                .NewConfig()
                .IgnoreNonMapped(true)
                .Map(dest => dest.Id, src => src.Id)
                .Map(dest => dest.MemberId, src => src.MemberId)
                .Map(dest => dest.Amount, src => src.Amount)
                .Map(dest => dest.Type, src => src.Type)
                .Map(dest => dest.ReferenceId, src => src.ReferenceId)
                .Map(dest => dest.Description, src => src.Description)
                .Map(dest => dest.CreatedAt, src => src.CreatedAt)
                .Map(dest => dest.CreatedByUserId, src => src.CreatedByUserId);

            TypeAdapterConfig<CreateBranchDto, Branch>
                .NewConfig()
                .IgnoreNonMapped(true)
                .Map(dest => dest.Name, src => src.Name)
                .Map(dest => dest.Address, src => src.Address)
                .Map(dest => dest.IsActive, src => src.IsActive);

            TypeAdapterConfig<Branch, BranchReadDto>
                .NewConfig()
                .IgnoreNonMapped(true)
                .Map(dest => dest.Id, src => src.Id)
                .Map(dest => dest.Name, src => src.Name)
                .Map(dest => dest.Address, src => src.Address)
                .Map(dest => dest.IsActive, src => src.IsActive);

            TypeAdapterConfig<Commission, CommissionReadDto>
                .NewConfig()
                .IgnoreNonMapped(true)
                .Map(dest => dest.Id, src => src.Id)
                .Map(dest => dest.TrainerId, src => src.TrainerId)
                .Map(dest => dest.MembershipId, src => src.MembershipId)
                .Map(dest => dest.BranchId, src => src.BranchId)
                .Map(dest => dest.Source, src => src.Source.ToString())
                .Map(dest => dest.Status, src => src.IsPaid ? "Paid" : "Generated")
                .Map(dest => dest.Percentage, src => src.Percentage)
                .Map(dest => dest.CalculatedAmount, src => src.CalculatedAmount)
                .Map(dest => dest.IsPaid, src => src.IsPaid)
                .Map(dest => dest.CreatedAt, src => src.CreatedAt)
                .Map(dest => dest.PaidAt, src => src.PaidAt)
                .Map(dest => dest.PaidByAdminId, src => src.PaidByAdminId);

            TypeAdapterConfig<Invoice, InvoiceReadDto>
                .NewConfig()
                .IgnoreNonMapped(true)
                .Map(dest => dest.Id, src => src.Id)
                .Map(dest => dest.InvoiceNumber, src => src.InvoiceNumber)
                .Map(dest => dest.MembershipId, src => src.MembershipId)
                .Map(dest => dest.AddOnId, src => src.AddOnId)
                .Map(dest => dest.PaymentId, src => src.PaymentId)
                .Map(dest => dest.MemberId, src => src.MemberId)
                .Map(dest => dest.Amount, src => src.Amount)
                .Map(dest => dest.FilePath, src => src.FilePath)
                .Map(dest => dest.Type, src => src.Type)
                .Map(dest => dest.CreatedAt, src => src.CreatedAt);

            TypeAdapterConfig<Notification, NotificationReadDto>
                .NewConfig()
                .IgnoreNonMapped(true)
                .Map(dest => dest.Id, src => src.Id)
                .Map(dest => dest.Title, src => src.Title)
                .Map(dest => dest.Message, src => src.Message)
                .Map(dest => dest.IsRead, src => src.IsRead)
                .Map(dest => dest.CreatedAt, src => src.CreatedAt);

            TypeAdapterConfig<CreateAddOnDto, AddOn>
                .NewConfig()
                .IgnoreNonMapped(true)
                .Map(dest => dest.Name, src => src.Name)
                .Map(dest => dest.Price, src => src.Price)
                .Map(dest => dest.BranchId, src => src.BranchId)
                .Map(dest => dest.RequiresActiveMembership, src => src.RequiresActiveMembership);

            TypeAdapterConfig<AddOn, AddOnReadDto>
                .NewConfig()
                .IgnoreNonMapped(true)
                .Map(dest => dest.Id, src => src.Id)
                .Map(dest => dest.Name, src => src.Name)
                .Map(dest => dest.Price, src => src.Price)
                .Map(dest => dest.BranchId, src => src.BranchId)
                .Map(dest => dest.RequiresActiveMembership, src => src.RequiresActiveMembership);
        }
    }
}

