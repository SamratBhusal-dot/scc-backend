using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using SmartCampusConnectBackend.Services;
using SmartCampusConnectBackend.Models;
using Microsoft.Extensions.FileProviders;
using SmartCampusConnectBackend.Hubs;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

// For Render deployment, typically remove explicit UseUrls.
// Kestrel (ASP.NET Core's web server) will automatically bind to 0.0.0.0
// and the port specified by Render's PORT environment variable.
// If you still want to explicitly set it (e.g., for local testing on a specific port),
// you can use: builder.WebHost.UseUrls("http://0.0.0.0:5005");
// However, for production on Render, it's often best to let Render manage it.


// Add services to the container.
builder.Services.Configure<MongoDBSettings>(
    builder.Configuration.GetSection("MongoDB"));

builder.Services.AddSingleton<MongoDBService>();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure JWT Authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)),
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        RoleClaimType = ClaimTypes.Role
    };
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(accessToken) &&
                (path.StartsWithSegments("/chatHub")))
            {
                context.Token = accessToken;
            }
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("admin"));
});

builder.Services.AddSignalR();

// Configure CORS - CRUCIAL: For Render, add your deployed frontend domain.
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigin",
        builder => builder.WithOrigins(
                                // Keep these for local development/testing from your Mac/local network
                                "http://127.0.0.1:5500",
                                "http://localhost:5500",
                                "http://10.30.1.117:5500", // Your Mac's local network IP for frontend

                                // ADD YOUR DEPLOYED RENDER FRONTEND DOMAIN HERE (e.g., "https://smartcampusconnect.onrender.com")
                                "https://smartcampusconnect.onrender.com" // Example: Replace with your actual Render frontend URL
                           )
                          .AllowAnyHeader()
                          .AllowAnyMethod()
                          .AllowCredentials());
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
        Path.Combine(builder.Environment.WebRootPath, "images")),
    RequestPath = "/images"
});

// IMPORTANT: UseCors must be placed BEFORE UseAuthentication and UseAuthorization
app.UseCors("AllowSpecificOrigin");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
// For Render, clients will connect to ws://your-backend-domain.onrender.com/chatHub
app.MapHub<ChatHub>("/chatHub");

app.Run();
