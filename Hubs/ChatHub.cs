using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using chat_dotnet.Data;
using chat_dotnet.Models;
using chat_dotnet.Models.DTOs;
using System.Security.Claims;

namespace chat_dotnet.Hubs;

[Authorize]
public class ChatHub : Hub
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ChatHub> _logger;

    public ChatHub(ApplicationDbContext context, ILogger<ChatHub> logger)
    {
        _context = context;
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        var userIdValue = Context.UserIdentifier;
        if (userIdValue != null && int.TryParse(userIdValue, out var userId))
        {
            // Track connection
            var connection = new Connection
            {
                ConnectionId = Context.ConnectionId,
                UserId = userId,
                ConnectedAt = DateTime.UtcNow
            };
            _context.Connections.Add(connection);

            // Update user online status
            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                user.IsOnline = true;
                user.LastSeen = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            // Notify others
            await Clients.Others.SendAsync("UserOnline", userId);

            _logger.LogInformation($"User {userId} connected with ConnectionId {Context.ConnectionId}");
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userIdValue = Context.UserIdentifier;
        if (userIdValue != null && int.TryParse(userIdValue, out var userId))
        {
            // Remove connection
            var connection = await _context.Connections
                .FirstOrDefaultAsync(c => c.ConnectionId == Context.ConnectionId);
            
            if (connection != null)
            {
                _context.Connections.Remove(connection);
            }

            // Check if user has other active connections
            var hasOtherConnections = await _context.Connections
                .AnyAsync(c => c.UserId == userId && c.ConnectionId != Context.ConnectionId);

            if (!hasOtherConnections)
            {
                // Update user offline status
                var user = await _context.Users.FindAsync(userId);
                if (user != null)
                {
                    user.IsOnline = false;
                    user.LastSeen = DateTime.UtcNow;
                }

                // Notify others
                await Clients.Others.SendAsync("UserOffline", userId);
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation($"User {userId} disconnected");
        }

        await base.OnDisconnectedAsync(exception);
    }

    public async Task SendPrivateMessage(int receiverId, string content)
    {
        var senderIdValue = Context.UserIdentifier;
        if (senderIdValue == null || !int.TryParse(senderIdValue, out var senderId)) return;

        try
        {
            // Get or create conversation
            var conversation = await _context.Conversations
                .FirstOrDefaultAsync(c =>
                    (c.User1Id == senderId && c.User2Id == receiverId) ||
                    (c.User1Id == receiverId && c.User2Id == senderId));

            if (conversation == null)
            {
                conversation = new Conversation
                {
                    User1Id = senderId,
                    User2Id = receiverId,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Conversations.Add(conversation);
                await _context.SaveChangesAsync();
            }

            // Create message
            var message = new Message
            {
                Content = content,
                SenderId = senderId,
                ConversationId = conversation.Id,
                SentAt = DateTime.UtcNow,
                IsRead = false,
                MessageType = MessageType.Text
            };

            _context.Messages.Add(message);
            conversation.LastMessageAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // Get sender info
            var sender = await _context.Users.FindAsync(senderId);

            var messageDto = new MessageDto
            {
                Id = message.Id,
                Content = message.Content,
                SenderId = senderId,
                SenderName = sender?.UserName ?? "Unknown",
                SentAt = message.SentAt,
                IsRead = message.IsRead,
                ConversationId = conversation.Id,
                MessageType = message.MessageType,
                Media = new List<MediaDto>()
            };

            // Send to receiver
            await Clients.User(receiverId.ToString()).SendAsync("ReceivePrivateMessage", messageDto);

            // Send confirmation to sender
            await Clients.Caller.SendAsync("MessageSent", messageDto);

            _logger.LogInformation($"Message sent from {senderId} to {receiverId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending private message");
            await Clients.Caller.SendAsync("Error", "Failed to send message");
        }
    }

    public async Task SendGroupMessage(int groupId, string content)
    {
        var senderIdValue = Context.UserIdentifier;
        if (senderIdValue == null || !int.TryParse(senderIdValue, out var senderId)) return;

        try
        {
            // Verify user is member of group
            var isMember = await _context.GroupMembers
                .AnyAsync(gm => gm.GroupId == groupId && gm.UserId == senderId);

            if (!isMember)
            {
                await Clients.Caller.SendAsync("Error", "You are not a member of this group");
                return;
            }

            // Create message
            var message = new Message
            {
                Content = content,
                SenderId = senderId,
                GroupId = groupId,
                SentAt = DateTime.UtcNow,
                MessageType = MessageType.Text
            };

            _context.Messages.Add(message);
            await _context.SaveChangesAsync();

            // Get sender info
            var sender = await _context.Users.FindAsync(senderId);

            var messageDto = new MessageDto
            {
                Id = message.Id,
                Content = message.Content,
                SenderId = senderId,
                SenderName = sender?.UserName ?? "Unknown",
                SentAt = message.SentAt,
                GroupId = groupId,
                MessageType = message.MessageType,
                Media = new List<MediaDto>()
            };

            // Get all group members
            var memberIds = await _context.GroupMembers
                .Where(gm => gm.GroupId == groupId)
                .Select(gm => gm.UserId.ToString())
                .ToListAsync();

            // Send to all group members
            await Clients.Users(memberIds).SendAsync("ReceiveGroupMessage", messageDto);

            _logger.LogInformation($"Group message sent by {senderId} to group {groupId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending group message");
            await Clients.Caller.SendAsync("Error", "Failed to send group message");
        }
    }

    public async Task MarkMessageAsRead(int messageId)
    {
        var userIdValue = Context.UserIdentifier;
        if (userIdValue == null || !int.TryParse(userIdValue, out var userId)) return;

        try
        {
            var message = await _context.Messages
                .Include(m => m.Conversation)
                .FirstOrDefaultAsync(m => m.Id == messageId);

            if (message != null && message.Conversation != null)
            {
                // Verify user is participant
                if (message.Conversation.User1Id == userId || message.Conversation.User2Id == userId)
                {
                    if (!message.IsRead && message.SenderId != userId)
                    {
                        message.IsRead = true;
                        message.ReadAt = DateTime.UtcNow;
                        await _context.SaveChangesAsync();

                        // Notify sender
                        await Clients.User(message.SenderId.ToString()).SendAsync("MessageRead", messageId);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking message as read");
        }
    }

    public async Task JoinGroup(string groupId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, groupId);
        _logger.LogInformation($"User joined group {groupId}");
    }

    public async Task LeaveGroup(string groupId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupId);
        _logger.LogInformation($"User left group {groupId}");
    }

    public async Task Typing(int receiverId)
    {
        var senderId = Context.UserIdentifier;
        await Clients.User(receiverId.ToString()).SendAsync("UserTyping", senderId);
    }

    public async Task StopTyping(int receiverId)
    {
        var senderId = Context.UserIdentifier;
        await Clients.User(receiverId.ToString()).SendAsync("UserStoppedTyping", senderId);
    }

    // Enhanced message sending with media support
    public async Task SendPrivateMessageWithMedia(int receiverId, string content, List<int> mediaIds)
    {
        var senderIdValue = Context.UserIdentifier;
        if (senderIdValue == null || !int.TryParse(senderIdValue, out var senderId)) return;

        try
        {
            // Get or create conversation
            var conversation = await _context.Conversations
                .FirstOrDefaultAsync(c =>
                    (c.User1Id == senderId && c.User2Id == receiverId) ||
                    (c.User1Id == receiverId && c.User2Id == senderId));

            if (conversation == null)
            {
                conversation = new Conversation
                {
                    User1Id = senderId,
                    User2Id = receiverId,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Conversations.Add(conversation);
                await _context.SaveChangesAsync();
            }

            // Create message
            var message = new Message
            {
                Content = content,
                SenderId = senderId,
                ConversationId = conversation.Id,
                SentAt = DateTime.UtcNow,
                IsRead = false,
                MessageType = mediaIds.Any() ? MessageType.Media : MessageType.Text
            };

            _context.Messages.Add(message);
            await _context.SaveChangesAsync();

            // Attach media if provided
            var mediaList = new List<MediaDto>();
            if (mediaIds.Any())
            {
                var mediaItems = await _context.Media
                    .Where(m => mediaIds.Contains(m.Id) && m.UploadedBy == senderId)
                    .Include(m => m.Uploader)
                    .ToListAsync();

                foreach (var media in mediaItems)
                {
                    var messageMedia = new MessageMedia
                    {
                        MessageId = message.Id,
                        MediaId = media.Id
                    };
                    _context.MessageMedia.Add(messageMedia);

                    mediaList.Add(new MediaDto
                    {
                        Id = media.Id,
                        FileName = media.FileName,
                        FilePath = media.FilePath,
                        ContentType = media.ContentType,
                        FileSize = media.FileSize,
                        MediaType = media.MediaType,
                        ThumbnailPath = media.ThumbnailPath,
                        UploadedAt = media.UploadedAt,
                        UploadedBy = media.UploadedBy,
                        UploaderName = media.Uploader.UserName ?? "Unknown"
                    });
                }

                await _context.SaveChangesAsync();
            }

            conversation.LastMessageAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // Get sender info
            var sender = await _context.Users.FindAsync(senderId);

            var messageDto = new MessageDto
            {
                Id = message.Id,
                Content = message.Content,
                SenderId = senderId,
                SenderName = sender?.UserName ?? "Unknown",
                SentAt = message.SentAt,
                IsRead = message.IsRead,
                ConversationId = conversation.Id,
                MessageType = message.MessageType,
                Media = mediaList
            };

            // Send to receiver
            await Clients.User(receiverId.ToString()).SendAsync("ReceivePrivateMessage", messageDto);

            // Send confirmation to sender
            await Clients.Caller.SendAsync("MessageSent", messageDto);

            // Create notification
            await CreateNotification(receiverId, senderId, "New Message", 
                mediaIds.Any() ? "Sent you media files" : content, 
                NotificationType.NewMessage, message.Id.ToString());

            _logger.LogInformation($"Message with media sent from {senderId} to {receiverId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending private message with media");
            await Clients.Caller.SendAsync("Error", "Failed to send message");
        }
    }

    public async Task SendGroupMessageWithMedia(int groupId, string content, List<int> mediaIds)
    {
        var senderIdValue = Context.UserIdentifier;
        if (senderIdValue == null || !int.TryParse(senderIdValue, out var senderId)) return;

        try
        {
            // Verify user is member of group
            var isMember = await _context.GroupMembers
                .AnyAsync(gm => gm.GroupId == groupId && gm.UserId == senderId);

            if (!isMember)
            {
                await Clients.Caller.SendAsync("Error", "You are not a member of this group");
                return;
            }

            // Create message
            var message = new Message
            {
                Content = content,
                SenderId = senderId,
                GroupId = groupId,
                SentAt = DateTime.UtcNow,
                MessageType = mediaIds.Any() ? MessageType.Media : MessageType.Text
            };

            _context.Messages.Add(message);
            await _context.SaveChangesAsync();

            // Attach media if provided
            var mediaList = new List<MediaDto>();
            if (mediaIds.Any())
            {
                var mediaItems = await _context.Media
                    .Where(m => mediaIds.Contains(m.Id) && m.UploadedBy == senderId)
                    .Include(m => m.Uploader)
                    .ToListAsync();

                foreach (var media in mediaItems)
                {
                    var messageMedia = new MessageMedia
                    {
                        MessageId = message.Id,
                        MediaId = media.Id
                    };
                    _context.MessageMedia.Add(messageMedia);

                    mediaList.Add(new MediaDto
                    {
                        Id = media.Id,
                        FileName = media.FileName,
                        FilePath = media.FilePath,
                        ContentType = media.ContentType,
                        FileSize = media.FileSize,
                        MediaType = media.MediaType,
                        ThumbnailPath = media.ThumbnailPath,
                        UploadedAt = media.UploadedAt,
                        UploadedBy = media.UploadedBy,
                        UploaderName = media.Uploader.UserName ?? "Unknown"
                    });
                }

                await _context.SaveChangesAsync();
            }

            // Get sender info
            var sender = await _context.Users.FindAsync(senderId);

            var messageDto = new MessageDto
            {
                Id = message.Id,
                Content = message.Content,
                SenderId = senderId,
                SenderName = sender?.UserName ?? "Unknown",
                SentAt = message.SentAt,
                GroupId = groupId,
                MessageType = message.MessageType,
                Media = mediaList
            };

            // Send to all group members
            await Clients.Group($"group_{groupId}").SendAsync("ReceiveGroupMessage", messageDto);

            // Create notifications for group members
            var groupMembers = await _context.GroupMembers
                .Where(gm => gm.GroupId == groupId && gm.UserId != senderId)
                .ToListAsync();

            foreach (var member in groupMembers)
            {
                await CreateNotification(member.UserId, senderId, "New Group Message",
                    mediaIds.Any() ? "Sent media files to the group" : content,
                    NotificationType.NewGroupMessage, groupId.ToString());
            }

            _logger.LogInformation($"Group message with media sent from {senderId} to group {groupId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending group message with media");
            await Clients.Caller.SendAsync("Error", "Failed to send group message");
        }
    }

    // Notification methods
    public async Task SendNotification(int userId, NotificationDto notification)
    {
        await Clients.User(userId.ToString()).SendAsync("ReceiveNotification", notification);
    }

    private async Task CreateNotification(int userId, int? fromUserId, string title, string? message, 
        NotificationType type, string? relatedEntityId = null)
    {
        try
        {
            var notification = new Notification
            {
                UserId = userId,
                FromUserId = fromUserId,
                Title = title,
                Message = message,
                Type = type,
                RelatedEntityId = relatedEntityId,
                CreatedAt = DateTime.UtcNow
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            // Get from user info
            string? fromUserName = null;
            if (fromUserId != null)
            {
                var fromUser = await _context.Users.FindAsync(fromUserId);
                fromUserName = fromUser?.UserName;
            }

            var notificationDto = new NotificationDto
            {
                Id = notification.Id,
                Title = notification.Title,
                Message = notification.Message,
                Type = notification.Type,
                IsRead = notification.IsRead,
                CreatedAt = notification.CreatedAt,
                RelatedEntityId = notification.RelatedEntityId,
                FromUserId = notification.FromUserId,
                FromUserName = fromUserName
            };

            // Send real-time notification
            await Clients.User(userId.ToString()).SendAsync("ReceiveNotification", notificationDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating notification");
        }
    }
}
