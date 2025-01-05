using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using AuthService.Data;
using AuthService.Interfaces;
using AuthService.Models;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<IAuthService, AuthService.Services.AuthService>();
builder.Services.AddControllers();
builder.Configuration.AddEnvironmentVariables();


builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy =>
        {
            policy
                .WithOrigins(
                    "http://192.168.100.109:8081",
                    "http://192.168.0.240:3000",
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



builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole(Roles.Admin));
    options.AddPolicy("ReceptionistOnly", policy => policy.RequireRole(Roles.Receptionist));
    options.AddPolicy("HousekeepingOnly", policy => policy.RequireRole(Roles.Housekeeping));
    options.AddPolicy("FAndBOnly", policy => policy.RequireRole(Roles.FAndB));
    options.AddPolicy("MaintenanceOnly", policy => policy.RequireRole(Roles.Maintenance));
});






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


builder.WebHost.ConfigureKestrel((context, options) =>
{
    var env = context.HostingEnvironment;

    if (env.IsDevelopment())
    {
        options.ListenAnyIP(8080);
        options.ListenAnyIP(8443, listenOptions =>
        {
            listenOptions.UseHttps(); 
        });
    }
    else
    {
        var certPath = context.Configuration["MYAPP_CERT_PATH"];
        var certPassword = context.Configuration["MYAPP_CERT_PASSWORD"];


        if (string.IsNullOrEmpty(certPath) || string.IsNullOrEmpty(certPassword))
        {
            throw new InvalidOperationException(
                $"Certificate path or password is not configured. Path: {certPath}, Password: {(string.IsNullOrEmpty(certPassword) ? "Not Provided" : "Provided")}");
        }

        options.ListenAnyIP(8080); 
        options.ListenAnyIP(8443, listenOptions =>
        {
            listenOptions.UseHttps(certPath, certPassword);
        });
    }
});



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

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<AuthDbContext>();

    context.Database.Migrate();

    DbInitializer.SeedUsers(context);
}



// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseAuthentication();
app.UseAuthorization();
app.UseHttpsRedirection();
app.MapControllers();

app.UseCors("AllowAll");



app.Run();

