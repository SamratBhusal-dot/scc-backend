using Microsoft.AspNetCore.Mvc;
using SmartCampusConnectBackend.Models;
using SmartCampusConnectBackend.Services;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using MongoDB.Driver;
using System.Security.Cryptography; // For hashing passwords
using Microsoft.Extensions.Configuration;

namespace SmartCampusConnectBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly MongoDBService _mongoService;
        private readonly IConfiguration _configuration;

        public AuthController(MongoDBService mongoService, IConfiguration configuration)
        {
            _mongoService = mongoService;
            _configuration = configuration;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            // Basic validation
            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(new { error = "Username, Email, and Password are required." });
            }

            // Check if username or email already exists
            var existingUser = await _mongoService.Users.Find(u => u.Username == request.Username || u.Email == request.Email).FirstOrDefaultAsync();
            if (existingUser != null)
            {
                return BadRequest(new { error = "Username or Email already exists." });
            }

            var passwordHash = HashPassword(request.Password); // Hash the password (USE A STRONGER HASHING LIB IN PROD)

            var user = new User
            {
                Username = request.Username,
                Email = request.Email,
                PasswordHash = passwordHash,
                CreatedAt = DateTime.UtcNow
            };

            await _mongoService.Users.InsertOneAsync(user);

            return Ok(new { message = "Registration successful!" });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            // Basic validation
            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(new { error = "Username and Password are required." });
            }

            var user = await _mongoService.Users.Find(u => u.Username == request.Username).FirstOrDefaultAsync();

            if (user == null || !VerifyPassword(request.Password, user.PasswordHash))
            {
                return Unauthorized(new { error = "Invalid credentials." });
            }

            var token = GenerateJwtToken(user);
            return Ok(new { token });
        }

        // IMPORTANT: For production, use a dedicated password hashing library like BCrypt.Net or Argon2.
        // This SHA256 example is for demonstration purposes only and NOT secure for production passwords.
        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return BitConverter.ToString(hashedBytes).Replace("-", "").ToLower();
            }
        }

        private bool VerifyPassword(string providedPassword, string storedHash)
        {
            return HashPassword(providedPassword) == storedHash;
        }

        private string GenerateJwtToken(User user)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Username),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim("userId", user.Id!), // Include MongoDB ObjectId as a claim
                new Claim("username", user.Username) // Include username as a claim
            };

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddHours(2), // Token expiration
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }

    // Request Models (Can be in a DTOs folder or here for simplicity)
    public class RegisterRequest
    {
        public string Username { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Password { get; set; } = null!;
    }

    public class LoginRequest
    {
        public string Username { get; set; } = null!;
        public string Password { get; set; } = null!;
    }
}