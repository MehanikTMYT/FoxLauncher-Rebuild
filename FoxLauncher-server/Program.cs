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
using System.Reflection;
using System.Text;
using Serilog; 

internal class Program
{
    private static void Main(string[] args)
    {
        // 1. --- Настройка Serilog ---
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .WriteTo.File("logs/foxlauncher-.txt", rollingInterval: RollingInterval.Day) // Логи в файл с ротацией
            .MinimumLevel.Debug() // Уровень логирования
            .CreateBootstrapLogger(); // Создание начального логгера

        var builder = WebApplication.CreateBuilder(args);

        // Сообщаем ASP.NET Core использовать Serilog
        builder.Host.UseSerilog((context, configuration) => configuration
            .ReadFrom.Configuration(context.Configuration)); // Читаем настройки из appsettings.json (опционально)

        // 2. --- Конфигурация ---
        builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

        // 3. --- Настройка DbContext ---
        builder.Services.AddDbContext<AuthDbContext>(options =>
            options.UseMySql(builder.Configuration.GetConnectionString("AuthDbConnection"),
                             ServerVersion.Parse("8.0.30-mysql")));

        builder.Services.AddDbContext<ProfileDbContext>(options =>
            options.UseMySql(builder.Configuration.GetConnectionString("AdminDbConnection"),
                     ServerVersion.Parse("8.0.30-mysql")));

        builder.Services.AddDbContext<FileDbContext>(options =>
            options.UseMySql(builder.Configuration.GetConnectionString("FileDbConnection"),
                     ServerVersion.Parse("8.0.30-mysql")));

        // 4. --- Настройка Identity с ролями ---
        builder.Services.AddIdentity<User, IdentityRole<int>>()
            .AddEntityFrameworkStores<AuthDbContext>()
            .AddDefaultTokenProviders(); // Провайдеры токенов (например, для подтверждения email)

        // 5. --- Регистрация сервисов ---

        builder.Services.AddScoped<IAuthService, AuthService>();
        builder.Services.AddScoped<IAuthlibKeyService, AuthlibKeyService>();
        builder.Services.AddScoped<ITextureService, TextureService>();
        builder.Services.AddScoped<IFileService, FileService>();
        builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
        builder.Services.AddScoped<IEmailConfirmationService, EmailConfirmationService>();

        // 6. --- Добавление MVC, API Explorer, Swagger ---
        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();

        // --- Настройка Swagger ---
        builder.Services.AddSwaggerGen(c =>
        {
            c.EnableAnnotations(); // Включаем аннотации Swashbuckle

            // --- Настройка XML-документации ---
            var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            // Убедитесь, что XML-документация генерируется (см. FoxLauncher-server.csproj)
            c.IncludeXmlComments(xmlPath);

            // --- Информация об API ---
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

        // 7. --- Настройка Authentication и JWT ---
        var jwtSettings = builder.Configuration.GetSection("Jwt"); // Читаем из секции Jwt в appsettings.json
        var jwtSecret = jwtSettings["Key"];
        var jwtIssuer = jwtSettings["Issuer"] ?? "FoxLauncher"; // Значение по умолчанию
        var jwtAudience = jwtSettings["Audience"] ?? "FoxLauncher"; // Значение по умолчанию

        if (string.IsNullOrEmpty(jwtSecret))
        {
            throw new InvalidOperationException("Jwt:Key is not configured in appsettings.json");
        }

        var signingKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));

        builder.Services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            // options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme; // Альтернативный способ
        })
        .AddJwtBearer("JwtBearer", jwtOptions =>
        {
            jwtOptions.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = signingKey, // Используем созданный ключ
                ValidateIssuer = true, // Установите в true и укажите Issuer
                ValidIssuer = jwtIssuer,
                ValidateAudience = true, // Установите в true и укажите Audience
                ValidAudience = jwtAudience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero // Убираем сдвиг времени
            };
        });

        // 8. --- Построение приложения ---
        var app = builder.Build();

        // 9. --- Настройка HTTP pipeline ---
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();
        app.UseAuthentication(); // Важно для JWT
        app.UseAuthorization();

        app.MapControllers();

        // 10. --- Запуск приложения ---
        app.Run();
    }
}