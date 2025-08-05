using Microsoft.Extensions.Options;
using MongoDB.Driver;
using SmartCampusConnectBackend.Models;

namespace SmartCampusConnectBackend.Services
{
    public class MongoDBService
    {
        private readonly IMongoCollection<User> _usersCollection;
        private readonly IMongoCollection<Listing> _listingsCollection;
        private readonly IMongoCollection<ChatMessage> _chatMessagesCollection; // New collection for chat messages

        public MongoDBService(IOptions<MongoDBSettings> mongoDBSettings)
        {
            var mongoClient = new MongoClient(
                mongoDBSettings.Value.ConnectionString);

            var mongoDatabase = mongoClient.GetDatabase(
                mongoDBSettings.Value.DatabaseName);

            _usersCollection = mongoDatabase.GetCollection<User>(
                mongoDBSettings.Value.UsersCollectionName);

            _listingsCollection = mongoDatabase.GetCollection<Listing>(
                mongoDBSettings.Value.ListingsCollectionName);

            // Initialize the new chat messages collection
            _chatMessagesCollection = mongoDatabase.GetCollection<ChatMessage>(
                mongoDBSettings.Value.ChatMessagesCollectionName);
        }

        public IMongoCollection<User> Users => _usersCollection;
        public IMongoCollection<Listing> Listings => _listingsCollection;
        public IMongoCollection<ChatMessage> ChatMessages => _chatMessagesCollection; // Expose the new collection
    }

    public class MongoDBSettings
    {
        public string ConnectionString { get; set; } = null!;
        public string DatabaseName { get; set; } = null!;
        public string UsersCollectionName { get; set; } = null!;
        public string ListingsCollectionName { get; set; } = null!;
        public string ChatMessagesCollectionName { get; set; } = null!; // New setting for chat messages collection name
    }
}
