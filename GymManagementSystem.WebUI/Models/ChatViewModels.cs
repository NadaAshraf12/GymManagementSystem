using GymManagementSystem.Application.DTOs;

namespace GymManagementSystem.WebUI.Models
{
    public class ChatIndexViewModel
    {
        public string CurrentUserId { get; set; } = string.Empty;
        public List<ChatConversationDto> Conversations { get; set; } = new();
    }
}
