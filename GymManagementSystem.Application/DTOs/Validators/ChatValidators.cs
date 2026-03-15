using FluentValidation;

namespace GymManagementSystem.Application.DTOs.Validators
{
    internal class ChatMessageDtoValidator : AbstractValidator<ChatMessageDto>
    {
        public ChatMessageDtoValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0);
            RuleFor(x => x.SenderId).NotEmpty();
            RuleFor(x => x.ReceiverId).NotEmpty();
            RuleFor(x => x.Message).NotEmpty();
        }
    }

    internal class ChatConversationDtoValidator : AbstractValidator<ChatConversationDto>
    {
        public ChatConversationDtoValidator()
        {
            RuleFor(x => x.UserId).NotEmpty();
            RuleFor(x => x.DisplayName).NotEmpty();
        }
    }

    internal class ChatUploadResultDtoValidator : AbstractValidator<ChatUploadResultDto>
    {
        public ChatUploadResultDtoValidator()
        {
            RuleFor(x => x.Url).NotEmpty();
        }
    }
}