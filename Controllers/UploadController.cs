using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization; // If image upload requires authentication

namespace SmartCampusConnectBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    // [Authorize] // Uncomment this if image uploads should only be allowed for logged-in users
    public class UploadController : ControllerBase
    {
        private readonly IWebHostEnvironment _env;

        public UploadController(IWebHostEnvironment env)
        {
            _env = env;
        }

        [HttpPost("image")]
        public async Task<IActionResult> UploadImage(IFormFile image)
        {
            if (image == null || image.Length == 0)
            {
                return BadRequest(new { error = "No image file provided." });
            }

            // Validate file type (optional but recommended)
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
            var fileExtension = Path.GetExtension(image.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(fileExtension))
            {
                return BadRequest(new { error = "Invalid image file type. Only JPG, JPEG, PNG, GIF are allowed." });
            }

            // Define the uploads folder within wwwroot
            // Ensure you have a 'wwwroot' folder at your project root, and an 'images' subfolder within it.
            var uploadsFolder = Path.Combine(_env.WebRootPath, "images");
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            var uniqueFileName = Guid.NewGuid().ToString() + fileExtension;
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            try
            {
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await image.CopyToAsync(fileStream);
                }

                // Construct the URL to access the image
                // Request.Scheme will be "http" or "https"
                // Request.Host will be "localhost:port" or your deployed domain
                var imageUrl = $"{Request.Scheme}://{Request.Host}/images/{uniqueFileName}";
                return Ok(new { imageUrl });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Image upload failed: {ex.Message}" });
            }
        }
    }
}