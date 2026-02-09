using System.Collections.Concurrent;
using System.Security.Claims;
using GymManagementSystem.Application.Interfaces;
using GymManagementSystem.Application.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace GymManagementSystem.WebUI.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly IChatService _chatService;
        private static readonly ConcurrentDictionary<string, string> ConnectionUsers = new();
        private static readonly ConcurrentDictionary<string, string?> ConnectionOpenWith = new();
        private static readonly ConcurrentDictionary<string, ConcurrentDictionary<string, byte>> UserConnections = new();
        private static readonly ConcurrentDictionary<string, DateTime> LastSeenUtc = new();

        public ChatHub(IChatService chatService)
        {
            _chatService = chatService;
        }

        public override async Task OnConnectedAsync()
        {
            var userId = GetUserId();
            if (!string.IsNullOrWhiteSpace(userId))
            {
                ConnectionUsers[Context.ConnectionId] = userId;
                ConnectionOpenWith[Context.ConnectionId] = null;
                var connections = UserConnections.GetOrAdd(userId, _ => new ConcurrentDictionary<string, byte>());
                connections[Context.ConnectionId] = 0;

                var unread = await _chatService.GetUnreadMessagesAsync(userId);
                if (unread.Count > 0)
                {
                    await Clients.Caller.SendAsync("ReceiveUnreadMessages", unread);
                }

                await BroadcastUserStatusAsync(userId, isOnline: true);
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            if (ConnectionUsers.TryRemove(Context.ConnectionId, out var userId) && !string.IsNullOrWhiteSpace(userId))
            {
                if (UserConnections.TryGetValue(userId, out var connections))
                {
                    connections.TryRemove(Context.ConnectionId, out _);
                    if (connections.IsEmpty)
                    {
                        UserConnections.TryRemove(userId, out _);
                        LastSeenUtc[userId] = DateTime.UtcNow;
                        await BroadcastUserStatusAsync(userId, isOnline: false);
                    }
                }
            }

            ConnectionOpenWith.TryRemove(Context.ConnectionId, out _);
            await base.OnDisconnectedAsync(exception);
        }

        public async Task OpenConversation(string withUserId)
        {
            var currentUserId = GetUserId();
            if (string.IsNullOrWhiteSpace(currentUserId))
            {
                throw new HubException("Unauthorized");
            }

            ConnectionOpenWith[Context.ConnectionId] = withUserId;

            try
            {
                var readIds = await _chatService.MarkAsReadByConversationAsync(withUserId, currentUserId);
                if (readIds.Count > 0)
                {
                    await Clients.User(withUserId).SendAsync("MessageRead", new { messageIds = readIds, readerId = currentUserId });
                }
            }
            catch (InvalidOperationException ex)
            {
                throw new HubException(ex.Message);
            }
        }

        public async Task<object> GetOnlineStatus(string userId)
        {
            var currentUserId = GetUserId();
            if (string.IsNullOrWhiteSpace(currentUserId))
            {
                throw new HubException("Unauthorized");
            }

            if (!string.Equals(currentUserId, userId, StringComparison.Ordinal))
            {
                var allowed = await _chatService.CanChatAsync(currentUserId, userId);
                if (!allowed)
                {
                    throw new HubException("Not allowed");
                }
            }

            var isOnline = UserConnections.ContainsKey(userId);
            LastSeenUtc.TryGetValue(userId, out var lastSeen);
            return new { userId, isOnline, lastSeen = lastSeen == default ? (DateTime?)null : lastSeen };
        }

        public async Task SendMessage(string receiverId, string message)
        {
            var senderId = GetUserId();
            if (string.IsNullOrWhiteSpace(senderId))
            {
                throw new HubException("Unauthorized");
            }

            try
            {
                var dto = await _chatService.SendMessageAsync(senderId, receiverId, message);

                var receiverHasOpenConversation = ConnectionUsers
                    .Where(kvp => kvp.Value == receiverId)
                    .Select(kvp => kvp.Key)
                    .Any(connId =>
                    {
                        if (!ConnectionOpenWith.TryGetValue(connId, out var openWith)) return false;
                        return string.Equals(openWith, senderId, StringComparison.Ordinal);
                    });

                if (receiverHasOpenConversation)
                {
                    var readIds = await _chatService.MarkAsReadByConversationAsync(senderId, receiverId);
                    if (readIds.Contains(dto.Id))
                    {
                        dto.IsRead = true;
                    }

                    if (readIds.Count > 0)
                    {
                        await Clients.User(senderId).SendAsync("MessageRead", new { messageIds = readIds, readerId = receiverId });
                    }
                }

                await Clients.User(receiverId).SendAsync("ReceiveMessage", dto);
                await Clients.Caller.SendAsync("ReceiveMessage", dto);
            }
            catch (InvalidOperationException ex)
            {
                throw new HubException(ex.Message);
            }
        }

        public async Task SendAttachment(string receiverId, string? message, int messageType, string attachmentUrl)
        {
            var senderId = GetUserId();
            if (string.IsNullOrWhiteSpace(senderId))
            {
                throw new HubException("Unauthorized");
            }

            try
            {
                if (messageType != (int)GymManagementSystem.Domain.Enums.MessageType.Image &&
                    messageType != (int)GymManagementSystem.Domain.Enums.MessageType.File)
                {
                    throw new HubException("Invalid message type.");
                }

                if (string.IsNullOrWhiteSpace(attachmentUrl) ||
                    !attachmentUrl.StartsWith("/chat_uploads/", StringComparison.OrdinalIgnoreCase) ||
                    attachmentUrl.Contains("..", StringComparison.Ordinal))
                {
                    throw new HubException("Invalid attachment URL.");
                }

                var dto = await _chatService.SendAttachmentAsync(senderId, receiverId, message, (GymManagementSystem.Domain.Enums.MessageType)messageType, attachmentUrl);

                var receiverHasOpenConversation = ConnectionUsers
                    .Where(kvp => kvp.Value == receiverId)
                    .Select(kvp => kvp.Key)
                    .Any(connId =>
                    {
                        if (!ConnectionOpenWith.TryGetValue(connId, out var openWith)) return false;
                        return string.Equals(openWith, senderId, StringComparison.Ordinal);
                    });

                if (receiverHasOpenConversation)
                {
                    var readIds = await _chatService.MarkAsReadByConversationAsync(senderId, receiverId);
                    if (readIds.Contains(dto.Id))
                    {
                        dto.IsRead = true;
                    }

                    if (readIds.Count > 0)
                    {
                        await Clients.User(senderId).SendAsync("MessageRead", new { messageIds = readIds, readerId = receiverId });
                    }
                }

                await Clients.User(receiverId).SendAsync("ReceiveMessage", dto);
                await Clients.Caller.SendAsync("ReceiveMessage", dto);
            }
            catch (InvalidOperationException ex)
            {
                throw new HubException(ex.Message);
            }
        }

        public async Task UserTyping(string receiverId)
        {
            var senderId = GetUserId();
            if (string.IsNullOrWhiteSpace(senderId))
            {
                throw new HubException("Unauthorized");
            }

            var allowed = await _chatService.CanChatAsync(senderId, receiverId);
            if (!allowed) return;

            var receiverConnectionIds = ConnectionUsers
                .Where(kvp => kvp.Value == receiverId)
                .Select(kvp => kvp.Key)
                .Where(connId =>
                {
                    if (!ConnectionOpenWith.TryGetValue(connId, out var openWith)) return false;
                    return string.Equals(openWith, senderId, StringComparison.Ordinal);
                })
                .ToList();

            if (receiverConnectionIds.Count > 0)
            {
                await Clients.Clients(receiverConnectionIds).SendAsync("UserTyping", new { senderId });
            }
        }

        public async Task MarkAsRead(int messageId)
        {
            var userId = GetUserId();
            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new HubException("Unauthorized");
            }

            await _chatService.MarkAsReadAsync(userId, messageId);
        }

        public async Task<IReadOnlyList<ChatMessageDto>> GetChatHistory(string userId)
        {
            var currentUserId = GetUserId();
            if (string.IsNullOrWhiteSpace(currentUserId))
            {
                throw new HubException("Unauthorized");
            }

            try
            {
                var history = await _chatService.GetChatHistoryAsync(currentUserId, userId);
                return history;
            }
            catch (InvalidOperationException ex)
            {
                throw new HubException(ex.Message);
            }
        }

        private string? GetUserId()
        {
            return Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }

        private async Task BroadcastUserStatusAsync(string userId, bool isOnline)
        {
            var relatedIds = await _chatService.GetRelatedUserIdsAsync(userId);
            if (relatedIds.Count == 0) return;

            LastSeenUtc.TryGetValue(userId, out var lastSeen);
            await Clients.Users(relatedIds).SendAsync("UserStatusChanged", new
            {
                userId,
                isOnline,
                lastSeen = isOnline ? (DateTime?)null : (lastSeen == default ? (DateTime?)null : lastSeen)
            });
        }
    }
}
