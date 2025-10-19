
using FoxLauncher.Modules.AuthModule.Data;
using FoxLauncher.Modules.AuthModule.Models;
using FoxLauncher.Modules.AuthModule.Services;
using FoxLauncher.Modules.FileModule.Data;
using FoxLauncher.Modules.FileModule.Services;
using FoxLauncher.Modules.ProfileModule.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Text;

internal class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Добавьте конфигурацию JWT
        builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

        // Добавьте DbContext для AuthModule
        builder.Services.AddDbContext<AuthDbContext>(options =>
            options.UseMySql(builder.Configuration.GetConnectionString("AuthDbConnection"), // Убедитесь, что строка подключения указана
                             ServerVersion.Parse("8.0.30-mysql"))); // Укажите свою версию MySQL

        builder.Services.AddDbContext<ProfileDbContext>(options =>
            options.UseMySql(builder.Configuration.GetConnectionString("AdminDbConnection"), // Убедитесь, что строка подключения указана
                     ServerVersion.Parse("8.0.30-mysql")));

        builder.Services.AddDbContext<FileDbContext>(options =>
            options.UseMySql(builder.Configuration.GetConnectionString("FileDbConnection"), // Убедитесь, что строка подключения указана
                     ServerVersion.Parse("8.0.30-mysql")));

        // Настройте Identity
        builder.Services.AddIdentity<User, IdentityRole<int>>()
            .AddEntityFrameworkStores<AuthDbContext>()
            .AddDefaultTokenProviders();

        // Регистрация сервисов AuthModule
        builder.Services.AddScoped<IAuthService, AuthService>();
        builder.Services.AddScoped<IAuthlibKeyService, AuthlibKeyService>();
        builder.Services.AddScoped<ITextureService, TextureService>();
        builder.Services.AddScoped<IFileService, FileService>();

        // Добавьте остальные сервисы (Controllers, CORS, JWT и т.д.)
        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        // Добавьте Authentication
        var jwtSecret = builder.Configuration["Jwt:Key"];
        if (string.IsNullOrEmpty(jwtSecret))
        {
            throw new InvalidOperationException("Jwt:Key is not configured");
        }
        builder.Services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = "JwtBearer";
            options.DefaultChallengeScheme = "JwtBearer";
        })
        .AddJwtBearer("JwtBearer", jwtOptions =>
        {
            jwtOptions.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
                ValidateIssuer = false, // Установите в true и укажите Issuer, если нужно
                ValidateAudience = false, // Установите в true и укажите Audience, если нужно
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero // Убираем сдвиг времени
            };
        });

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();
        app.UseAuthentication(); // Важно для JWT
        app.UseAuthorization();

        app.MapControllers();

        app.Run();
    }
}