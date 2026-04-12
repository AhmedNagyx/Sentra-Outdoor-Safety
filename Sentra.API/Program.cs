using Microsoft.EntityFrameworkCore;
using Sentra.API.Data;
using Sentra.API.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Sentra.API.Hubs;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// ===== DATABASE =====
builder.Services.AddDbContext<SentraDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));

// ===== CONTROLLERS =====
builder.Services.AddControllers();

// ===== HTTP CLIENT (for AI service calls) =====
builder.Services.AddHttpClient();

// ===== SIGNALR =====
builder.Services.AddSignalR();

// ===== NOTIFICATIONS =====
builder.Services.AddScoped<INotificationService, NotificationService>();

// ===== FIREBASE =====
FirebaseInitializer.Initialize(builder.Configuration);

// ===== JWT SERVICE =====
builder.Services.AddScoped<IJwtService, JwtService>();

// ===== JWT AUTHENTICATION =====
var jwt = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwt["SecretKey"]
    ?? throw new Exception("JwtSettings:SecretKey missing in appsettings.json");
var key = Encoding.UTF8.GetBytes(secretKey);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt["Issuer"],
            ValidAudience = jwt["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

// ===== SWAGGER =====
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Sentra Outdoor Safety API",
        Version = "v1"
    });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter JWT token like: Bearer {your token}"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id   = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

// ===== CORS =====
var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontends", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// ===== MIDDLEWARE PIPELINE =====
app.UseSwagger();
app.UseSwaggerUI();
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseCors("AllowFrontends");
app.UseMiddleware<Sentra.API.Middleware.ApiKeyMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
app.MapHub<AlertHub>("/hubs/alerts");
app.MapControllers();
app.Run();