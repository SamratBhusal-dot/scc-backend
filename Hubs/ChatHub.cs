using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization; // For [Authorize] attribute
using System.Security.Claims; // For accessing user claims
using SmartCampusConnectBackend.Models;
using SmartCampusConnectBackend.Services;
using MongoDB.Driver; // For MongoDB operations

namespace SmartCampusConnectBackend.Hubs
{
    // [Authorize] ensures only authenticated users can connect to this hub
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly MongoDBService _mongoService;

        public ChatHub(MongoDBService mongoService)
        {
            _mongoService = mongoService;
        }

        // This method is called by clients to send a message
        public async Task SendMessage(string message)
        {
            // Get user information from the JWT token claims
            var userId = Context.User?.FindFirst("userId")?.Value;
            var username = Context.User?.FindFirst("username")?.Value;

            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(username))
            {
                // This should ideally not happen due to [Authorize]
                // but as a fallback, disconnect or log error
                Console.WriteLine("Unauthorized chat message attempt.");
                return;
            }

            // Create a new chat message object
            var chatMessage = new ChatMessage
            {
                UserId = userId,
                Username = username,
                Message = message,
                Timestamp = DateTime.UtcNow // Use UTC for consistency
            };

            // Save the message to MongoDB
            await _mongoService.ChatMessages.InsertOneAsync(chatMessage);

            // Broadcast the message to all connected clients (including the sender)
            // The "ReceiveMessage" method will be called on all clients
            await Clients.All.SendAsync("ReceiveMessage", username, message, chatMessage.Timestamp);
        }

        // Override OnConnectedAsync to send chat history when a user connects
        public override async Task OnConnectedAsync()
        {
            // Get the last 50 messages (or more/less as desired)
            // IMPORTANT: Avoid orderBy() with Firestore, but with MongoDB, it's supported.
            // However, for simplicity and to avoid potential index issues if not configured,
            // we'll fetch and sort in memory for a small history.
            // For large histories, consider proper indexing on 'Timestamp' in MongoDB.
            var history = await _mongoService.ChatMessages
                                             .Find(_ => true) // Find all messages
                                             .SortByDescending(m => m.Timestamp) // Sort by timestamp descending
                                             .Limit(50) // Get the last 50 messages
                                             .ToListAsync();

            // Reverse the list to send in chronological order (oldest first)
            history.Reverse();

            // Send the history only to the connecting client
            await Clients.Caller.SendAsync("ReceiveChatHistory", history);

            // Optionally, broadcast a "user connected" message to all others
            var username = Context.User?.FindFirst("username")?.Value ?? "A user";
            await Clients.All.SendAsync("ReceiveMessage", "System", $"{username} has joined the chat.", DateTime.UtcNow);

            await base.OnConnectedAsync();
        }

        // Override OnDisconnectedAsync to broadcast a "user disconnected" message
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var username = Context.User?.FindFirst("username")?.Value ?? "A user";
            await Clients.All.SendAsync("ReceiveMessage", "System", $"{username} has left the chat.", DateTime.UtcNow);

            await base.OnDisconnectedAsync(exception);
        }
    }
}
