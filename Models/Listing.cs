using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace SmartCampusConnectBackend.Models
{
    public class Listing
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        public string Title { get; set; } = null!;
        public string Description { get; set; } = null!;
        public double Price { get; set; }
        public string Category { get; set; } = null!;
        public string? ImageUrl { get; set; } // URL from your image storage
        public string SellerId { get; set; } = null!; // MongoDB ObjectId of the user who posted
        public string SellerUsername { get; set; } = null!; // Denormalized for easier display
        public string Status { get; set; } = "available"; // e.g., "available", "sold"
        public DateTime PostedAt { get; set; } = DateTime.UtcNow;
    }
}