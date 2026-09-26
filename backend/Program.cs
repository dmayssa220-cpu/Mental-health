using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MentalHealth.API.Data;
using MentalHealth.API.Services;

var builder = WebApplication.CreateBuilder(args);

// Base de données MySQL
var connString = builder.Configuration.GetConnectionString("DefaultConnection")!;
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(connString, ServerVersion.AutoDetect(connString)));

// Services
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();

// HttpClient pour le service IA
builder.Services.AddHttpClient<IAIService, AIService>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["AIService:BaseUrl"]!);
    client.Timeout = TimeSpan.FromSeconds(30);
});

// Authentification JWT
var jwtKey = builder.Configuration["Jwt:Key"]!;

 builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };

       
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });   

builder.Services.AddAuthorization();
builder.Services.AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddHostedService<ReminderService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IStripeService, StripeService>();
builder.Services.AddHostedService<ReminderService>();
builder.Services.AddSignalR();
// CORS (dev)

builder.Services.AddCors(o => o.AddPolicy("SignalRPolicy", p =>
    p.WithOrigins("http://localhost", "http://localhost:5173", "http://localhost:80")
     .AllowAnyHeader()
     .AllowAnyMethod()
     .AllowCredentials()));    

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Mental Health API", Version = "v1" });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization : Bearer {token}",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// Migrations automatiques au démarrage
using (var scope = app.Services.CreateScope())
{
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.Migrate();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Erreur migration : {ex.Message}");
    }
}

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("SignalRPolicy");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHub<MentalHealth.API.Hubs.ChatHub>("/hubs/chat");
app.MapHub<MentalHealth.API.Hubs.NotificationHub>("/hubs/notifications");
app.Run();