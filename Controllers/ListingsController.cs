using Microsoft.AspNetCore.Mvc;
using SmartCampusConnectBackend.Models;
using SmartCampusConnectBackend.Services;
using MongoDB.Driver;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims; // To get user ID from token

namespace SmartCampusConnectBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // All listing operations require authentication
    public class ListingsController : ControllerBase
    {
        private readonly MongoDBService _mongoService;

        public ListingsController(MongoDBService mongoService)
        {
            _mongoService = mongoService;
        }

        [HttpGet]
        public async Task<IActionResult> GetListings([FromQuery] string? search, [FromQuery] string? category, [FromQuery] double? minPrice, [FromQuery] double? maxPrice)
        {
            var filterBuilder = Builders<Listing>.Filter;
            var filter = filterBuilder.Empty; // Start with an empty filter

            if (!string.IsNullOrWhiteSpace(search))
            {
                // Case-insensitive search on Title and Description
                filter &= filterBuilder.Where(l => l.Title.ToLower().Contains(search.ToLower()) || l.Description.ToLower().Contains(search.ToLower()));
            }

            if (!string.IsNullOrWhiteSpace(category))
            {
                filter &= filterBuilder.Eq(l => l.Category, category);
            }

            if (minPrice.HasValue)
            {
                filter &= filterBuilder.Gte(l => l.Price, minPrice.Value);
            }

            if (maxPrice.HasValue)
            {
                filter &= filterBuilder.Lte(l => l.Price, maxPrice.Value);
            }

            var listings = await _mongoService.Listings.Find(filter).ToListAsync();
            return Ok(new { listings });
        }

        [HttpPost]
        public async Task<IActionResult> CreateListing([FromBody] ListingRequest request)
        {
            // Get user ID and username from JWT claims
            var userId = User.FindFirst("userId")?.Value;
            var username = User.FindFirst("username")?.Value;

            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(username))
            {
                // This should ideally not happen if [Authorize] works correctly
                return Unauthorized(new { error = "User information missing from token. Please log in again." });
            }

            // Basic validation
            if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Description) || string.IsNullOrWhiteSpace(request.Category) || request.Price <= 0)
            {
                return BadRequest(new { error = "Title, Description, Price, and Category are required, and Price must be positive." });
            }

            var listing = new Listing
            {
                Title = request.Title,
                Description = request.Description,
                Price = request.Price,
                Category = request.Category,
                ImageUrl = request.ImageUrl, // This URL comes from the image upload endpoint
                SellerId = userId,
                SellerUsername = username,
                PostedAt = DateTime.UtcNow,
                Status = "available"
            };

            await _mongoService.Listings.InsertOneAsync(listing);
            return StatusCode(201, new { message = "Listing created successfully!", listing });
        }

        // Example: Get listing by ID
        [HttpGet("{id}")]
        public async Task<IActionResult> GetListingById(string id)
        {
            var listing = await _mongoService.Listings.Find(l => l.Id == id).FirstOrDefaultAsync();
            if (listing == null)
            {
                return NotFound(new { error = "Listing not found." });
            }
            return Ok(listing);
        }

        // Example: Update listing
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateListing(string id, [FromBody] ListingRequest request)
        {
            var userId = User.FindFirst("userId")?.Value;
            var existingListing = await _mongoService.Listings.Find(l => l.Id == id).FirstOrDefaultAsync();

            if (existingListing == null)
            {
                return NotFound(new { error = "Listing not found." });
            }

            // Ensure only the seller can update their listing
            if (existingListing.SellerId != userId)
            {
                return StatusCode(403, new { error = "You are not authorized to update this listing." });
            }

            // Update fields
            existingListing.Title = request.Title;
            existingListing.Description = request.Description;
            existingListing.Price = request.Price;
            existingListing.Category = request.Category;
            existingListing.ImageUrl = request.ImageUrl; // Allow image URL to be updated

            await _mongoService.Listings.ReplaceOneAsync(l => l.Id == id, existingListing);
            return Ok(new { message = "Listing updated successfully!" });
        }

        // Example: Delete listing
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteListing(string id)
        {
            var userId = User.FindFirst("userId")?.Value;
            var existingListing = await _mongoService.Listings.Find(l => l.Id == id).FirstOrDefaultAsync();

            if (existingListing == null)
            {
                return NotFound(new { error = "Listing not found." });
            }

            // Ensure only the seller can delete their listing
            if (existingListing.SellerId != userId)
            {
                return StatusCode(403, new { error = "You are not authorized to delete this listing." });
            }

            await _mongoService.Listings.DeleteOneAsync(l => l.Id == id);
            return Ok(new { message = "Listing deleted successfully!" });
        }
    } // This is the closing brace for the ListingsController class

    // Request Model for Listing creation/update - NOW OUTSIDE THE CONTROLLER CLASS
    public class ListingRequest
    {
        public string Title { get; set; } = null!;
        public string Description { get; set; } = null!;
        public double Price { get; set; }
        public string Category { get; set; } = null!;
        public string? ImageUrl { get; set; }
    }
}