using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace SmartCampusConnectBackend.Models
{
    public class ChatMessage
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; } // MongoDB ObjectId

        public string UserId { get; set; } = null!; // ID of the user who sent the message
        public string Username { get; set; } = null!; // Username of the sender
        public string Message { get; set; } = null!; // The message content
        public DateTime Timestamp { get; set; } = DateTime.UtcNow; // When the message was sent
    }
}
