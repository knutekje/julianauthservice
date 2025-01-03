using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using AuthService.Data;
using AuthService.Interfaces;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<IAuthService, AuthService.Services.AuthService>();
builder.Services.AddControllers();


builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy =>
        {
            policy
                .WithOrigins(
                    "http://192.168.100.109:8081",
                    "http://localhost:8081",
                    "http://localhost:3000",
                    "http://localhost:3001",
                    "http://localhost:5174",
                    "http://localhost:5172")
                .AllowAnyHeader()
                .AllowAnyMethod()
                ;
        });
});


builder.Configuration.AddEnvironmentVariables();





Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("Logs/log-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();



builder.Services.AddDbContext<AuthDbContext>(
    options =>
    {
        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
            if (!builder.Environment.IsDevelopment())
        {
            var password = Environment.GetEnvironmentVariable("DB_PASSWORD");
            Console.WriteLine(password);
            connectionString = string.Format(connectionString, password);
            Console.WriteLine(connectionString);
        }
        
        options.UseNpgsql(connectionString);

    });

builder.Logging.AddConsole();
builder.Services.AddDbContext<AuthDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"));
    options.EnableSensitiveDataLogging(); 
    options.LogTo(Console.WriteLine); 
});


var secret = Environment.GetEnvironmentVariable("JWT_SECRET");
var jwtSecret = builder.Configuration["JwtSettings:Secret"];
if (string.IsNullOrEmpty(jwtSecret) || jwtSecret.Length < 16)
{
    throw new ArgumentException("The JWT secret key must be at least 16 characters long and set via the environment variable 'JwtSettings__Secret'.");
}

var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));

Console.WriteLine(secret);


builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["JwtSettings:Issuer"],
            ValidAudience = builder.Configuration["JwtSettings:Audience"],
            IssuerSigningKey = key
        };
    });


var app = builder.Build();
// Apply migrations and create database at startup
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
    dbContext.Database.Migrate();
}


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapControllers();

app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

app.Run();

