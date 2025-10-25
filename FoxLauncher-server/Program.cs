using FoxLauncher.Modules.AuthModule.Data;
using FoxLauncher.Modules.AuthModule.Models;
using FoxLauncher.Modules.AuthModule.Services;
using FoxLauncher.Modules.AuthModule.Services.Authlib;
using FoxLauncher.Modules.FileModule.Data;
using FoxLauncher.Modules.FileModule.Services;
using FoxLauncher.Modules.ProfileModule.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Microsoft.IdentityModel.Tokens;
using System.Reflection;
using System.Text;
using Serilog;

internal class Program
{
    private static void Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .WriteTo.File("logs/foxlauncher-.txt", rollingInterval: RollingInterval.Day)
            .MinimumLevel.Debug()
            .CreateBootstrapLogger();

        var builder = WebApplication.CreateBuilder(args);

        builder.Host.UseSerilog((context, configuration) => configuration
            .ReadFrom.Configuration(context.Configuration));

        builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

        builder.Services.AddDbContext<AuthDbContext>(options =>
            options.UseMySql(builder.Configuration.GetConnectionString("AuthDbConnection"),
                             ServerVersion.Parse("8.0.30-mysql")));

        builder.Services.AddDbContext<ProfileDbContext>(options =>
            options.UseMySql(builder.Configuration.GetConnectionString("AdminDbConnection"),
                     ServerVersion.Parse("8.0.30-mysql")));

        builder.Services.AddDbContext<FileDbContext>(options =>
            options.UseMySql(builder.Configuration.GetConnectionString("FileDbConnection"),
                     ServerVersion.Parse("8.0.30-mysql")));

        builder.Services.AddIdentity<User, IdentityRole<int>>()
            .AddEntityFrameworkStores<AuthDbContext>()
            .AddDefaultTokenProviders();

        builder.Services.AddScoped<IAuthService, AuthService>();
        builder.Services.AddScoped<IAuthlibKeyService, AuthlibKeyService>();
        builder.Services.AddScoped<ITextureService, TextureService>();
        builder.Services.AddScoped<IFileService, FileService>();
        // Предполагается, что JwtTokenService реализует IJwtTokenService
        // builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
        builder.Services.AddScoped<IEmailConfirmationService, EmailConfirmationService>();

        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(c =>
        {
            c.EnableAnnotations();
            var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            c.IncludeXmlComments(xmlPath);
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Version = "v1",
                Title = "FoxLauncher API",
                Description = "API для аутентификации, управления профилями, версиями и файлами в FoxLauncher.",
                Contact = new OpenApiContact
                {
                    Name = "FoxLauncher",
                },
            });
        });

        // --- Настройка аутентификации с двумя схемами JWT ---
        var jwtSettings = builder.Configuration.GetSection("Jwt");
        var adminSecretKeyBase64 = jwtSettings["AdminSecretKey"];
        var userSecretKeyBase64 = jwtSettings["UserSecretKey"];
        var jwtIssuer = jwtSettings["Issuer"];
        var jwtAudience = jwtSettings["Audience"];

        if (string.IsNullOrEmpty(adminSecretKeyBase64) || string.IsNullOrEmpty(userSecretKeyBase64))
        {
            throw new InvalidOperationException("Jwt:AdminSecretKey or Jwt:UserSecretKey is not configured in appsettings.json");
        }

        byte[] adminKeyBytes = Convert.FromBase64String(adminSecretKeyBase64);
        byte[] userKeyBytes = Convert.FromBase64String(userSecretKeyBase64);

        builder.Services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer("AdminScheme", options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(adminKeyBytes),
                ValidateIssuer = !string.IsNullOrEmpty(jwtIssuer),
                ValidIssuer = jwtIssuer,
                ValidateAudience = !string.IsNullOrEmpty(jwtAudience),
                ValidAudience = jwtAudience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };
        })
        .AddJwtBearer("UserScheme", options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(userKeyBytes),
                ValidateIssuer = !string.IsNullOrEmpty(jwtIssuer),
                ValidIssuer = jwtIssuer,
                ValidateAudience = !string.IsNullOrEmpty(jwtAudience),
                ValidAudience = jwtAudience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };
        });
        // --- Конец настройки аутентификации ---

        var app = builder.Build();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();
        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();

        app.Run();
    }
}