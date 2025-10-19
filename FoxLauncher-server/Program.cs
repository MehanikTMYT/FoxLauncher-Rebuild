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
        // 1. --- ��������� Serilog ---
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .WriteTo.File("logs/foxlauncher-.txt", rollingInterval: RollingInterval.Day) // ���� � ���� � ��������
            .MinimumLevel.Debug() // ������� �����������
            .CreateBootstrapLogger(); // �������� ���������� �������

        var builder = WebApplication.CreateBuilder(args);

        // �������� ASP.NET Core ������������ Serilog
        builder.Host.UseSerilog((context, configuration) => configuration
            .ReadFrom.Configuration(context.Configuration)); // ������ ��������� �� appsettings.json (�����������)

        // 2. --- ������������ ---
        builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

        // 3. --- ��������� DbContext ---
        builder.Services.AddDbContext<AuthDbContext>(options =>
            options.UseMySql(builder.Configuration.GetConnectionString("AuthDbConnection"),
                             ServerVersion.Parse("8.0.30-mysql")));

        builder.Services.AddDbContext<ProfileDbContext>(options =>
            options.UseMySql(builder.Configuration.GetConnectionString("AdminDbConnection"),
                     ServerVersion.Parse("8.0.30-mysql")));

        builder.Services.AddDbContext<FileDbContext>(options =>
            options.UseMySql(builder.Configuration.GetConnectionString("FileDbConnection"),
                     ServerVersion.Parse("8.0.30-mysql")));

        // 4. --- ��������� Identity � ������ ---
        builder.Services.AddIdentity<User, IdentityRole<int>>()
            .AddEntityFrameworkStores<AuthDbContext>()
            .AddDefaultTokenProviders(); // ���������� ������� (��������, ��� ������������� email)

        // 5. --- ����������� �������� ---

        builder.Services.AddScoped<IAuthService, AuthService>();
        builder.Services.AddScoped<IAuthlibKeyService, AuthlibKeyService>();
        builder.Services.AddScoped<ITextureService, TextureService>();
        builder.Services.AddScoped<IFileService, FileService>();
        builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
        builder.Services.AddScoped<IEmailConfirmationService, EmailConfirmationService>();

        // 6. --- ���������� MVC, API Explorer, Swagger ---
        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();

        // --- ��������� Swagger ---
        builder.Services.AddSwaggerGen(c =>
        {
            c.EnableAnnotations(); // �������� ��������� Swashbuckle

            // --- ��������� XML-������������ ---
            var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            // ���������, ��� XML-������������ ������������ (��. FoxLauncher-server.csproj)
            c.IncludeXmlComments(xmlPath);

            // --- ���������� �� API ---
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Version = "v1",
                Title = "FoxLauncher API",
                Description = "API ��� ��������������, ���������� ���������, �������� � ������� � FoxLauncher.",
                Contact = new OpenApiContact
                {
                    Name = "FoxLauncher", 
                },
    
            });
        });

        // 7. --- ��������� Authentication � JWT ---
        var jwtSettings = builder.Configuration.GetSection("Jwt"); // ������ �� ������ Jwt � appsettings.json
        var jwtSecret = jwtSettings["Key"];
        var jwtIssuer = jwtSettings["Issuer"] ?? "FoxLauncher"; // �������� �� ���������
        var jwtAudience = jwtSettings["Audience"] ?? "FoxLauncher"; // �������� �� ���������

        if (string.IsNullOrEmpty(jwtSecret))
        {
            throw new InvalidOperationException("Jwt:Key is not configured in appsettings.json");
        }

        var signingKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));

        builder.Services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            // options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme; // �������������� ������
        })
        .AddJwtBearer("JwtBearer", jwtOptions =>
        {
            jwtOptions.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = signingKey, // ���������� ��������� ����
                ValidateIssuer = true, // ���������� � true � ������� Issuer
                ValidIssuer = jwtIssuer,
                ValidateAudience = true, // ���������� � true � ������� Audience
                ValidAudience = jwtAudience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero // ������� ����� �������
            };
        });

        // 8. --- ���������� ���������� ---
        var app = builder.Build();

        // 9. --- ��������� HTTP pipeline ---
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();
        app.UseAuthentication(); // ����� ��� JWT
        app.UseAuthorization();

        app.MapControllers();

        // 10. --- ������ ���������� ---
        app.Run();
    }
}