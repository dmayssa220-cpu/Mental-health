using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using MentalHealth.API.Data;
using MentalHealth.API.Models;

namespace MentalHealth.API.Hubs;

[Authorize]
public class ChatHub : Hub
{
    private readonly AppDbContext _db;
    private readonly ILogger<ChatHub> _logger;

    public ChatHub(AppDbContext db, ILogger<ChatHub> logger)
    {
        _db = db;
        _logger = logger;
    }

    private int CurrentUserId =>
        int.Parse(Context.User!.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);

    private string CurrentUserName =>
        Context.User!.FindFirst(System.Security.Claims.ClaimTypes.Name)!.Value ?? "";

  
    public override async Task OnConnectedAsync()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"user-{CurrentUserId}");
        await BroadcastOnlineStatus(true);

     
        await Clients.Others.SendAsync("UserOnline", new { userId = CurrentUserId, name = CurrentUserName });
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user-{CurrentUserId}");
        await BroadcastOnlineStatus(false);
        await Clients.Others.SendAsync("UserOffline", new { userId = CurrentUserId });
        await base.OnDisconnectedAsync(exception);
    }

    private async Task BroadcastOnlineStatus(bool isOnline)
    {
        await Clients.All.SendAsync("UserStatusChanged", new
        {
            userId = CurrentUserId,
            name = CurrentUserName,
            isOnline
        });
    }

    
    public async Task JoinConversation(int conversationId)
    {
        var conv = await _db.Conversations.FindAsync(conversationId);
        if (conv == null) return;

        if (conv.PatientId != CurrentUserId && conv.DoctorId != CurrentUserId)
            throw new HubException("Accès non autorisé à cette conversation.");

        await Groups.AddToGroupAsync(Context.ConnectionId, $"conversation-{conversationId}");
        _logger.LogInformation("User {UserId} joined conversation {ConvId}", CurrentUserId, conversationId);
    }

    public async Task LeaveConversation(int conversationId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"conversation-{conversationId}");
    }

   
    public async Task SendMessage(int conversationId, string content)
    {
        if (string.IsNullOrWhiteSpace(content)) return;

        var conv = await _db.Conversations.FindAsync(conversationId);
        if (conv == null) throw new HubException("Conversation introuvable.");
        if (conv.PatientId != CurrentUserId && conv.DoctorId != CurrentUserId)
            throw new HubException("Accès non autorisé.");

        var msg = new Message
        {
            ConversationId = conversationId,
            SenderId = CurrentUserId,
            Content = content.Trim(),
            SentAt = DateTime.UtcNow,
            IsFromAI = false
        };

        _db.Messages.Add(msg);
        conv.LastMessageAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        var payload = new
        {
            msg.Id,
            msg.Content,
            msg.IsFromAI,
            msg.SentAt,
            msg.ConversationId,
            msg.SenderId,
            SenderName = CurrentUserName
        };

       
        await Clients.Group($"conversation-{conversationId}").SendAsync("ReceiveMessage", payload);

       
        var otherId = conv.PatientId == CurrentUserId ? conv.DoctorId : conv.PatientId;
        await Clients.Group($"user-{otherId}").SendAsync("NewMessageNotification", new
        {
            conversationId,
            preview = content.Length > 60 ? content.Substring(0, 60) + "..." : content,
            from = CurrentUserName,
            sentAt = msg.SentAt
        });
    }

        public async Task Typing(int conversationId, bool isTyping)
    {
        var conv = await _db.Conversations.FindAsync(conversationId);
        if (conv == null) return;
        if (conv.PatientId != CurrentUserId && conv.DoctorId != CurrentUserId) return;

        await Clients.OthersInGroup($"conversation-{conversationId}")
            .SendAsync("UserTyping", new
            {
                conversationId,
                userId = CurrentUserId,
                name = CurrentUserName,
                isTyping
            });
    }

   
    public async Task MarkAsRead(int conversationId)
    {
        var conv = await _db.Conversations.FindAsync(conversationId);
        if (conv == null) return;

        var otherId = conv.PatientId == CurrentUserId ? conv.DoctorId : conv.PatientId;
        await Clients.Group($"user-{otherId}").SendAsync("MessagesRead", new
        {
            conversationId,
            readerId = CurrentUserId
        });
    }
}