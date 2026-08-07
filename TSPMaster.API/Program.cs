using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using NLog;
using NLog.Web;
using System.Text;
using TSPMaster.API.Data;
using TSPMaster.API.Helpers;
using TSPMaster.API.Models;
using TSPMaster.API.Services;

// ─── NLog: initialize before anything else ───────────────────────────────────
var logger = LogManager.Setup()
    .LoadConfigurationFromAppSettings()
    .GetCurrentClassLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // NLog: swap default logging for NLog
    builder.Logging.ClearProviders();
    builder.Host.UseNLog();

    // ─── Database ────────────────────────────────────────────────────────────
    builder.Services.AddDbContextPool<ApplicationDbContext>(options =>
        options.UseSqlServer(
            builder.Configuration.GetConnectionString("DefaultConnection"),
            sqlOptions => sqlOptions.EnableRetryOnFailure(3)));

    // ─── Identity ────────────────────────────────────────────────────────────
    builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.Password.RequiredLength = 8;
        options.Password.RequireDigit = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = false;
        options.User.RequireUniqueEmail = true;
        options.SignIn.RequireConfirmedEmail = false;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

    // ─── Caching ──────────────────────────────────────────────────────────────
    builder.Services.AddMemoryCache();

    // ─── JWT Bearer Auth ──────────────────────────────────────────────────────
    var jwtSection = builder.Configuration.GetSection("JwtSettings");
    var secretKey = jwtSection["SecretKey"]
        ?? throw new InvalidOperationException("JwtSettings:SecretKey is not configured.");

    if (secretKey.Contains("REPLACE_WITH_SECURE_SECRET") && !builder.Environment.IsDevelopment())
    {
        throw new InvalidOperationException("Insecure default JwtSettings:SecretKey detected in non-Development environment.");
    }

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
            ValidIssuer = jwtSection["Issuer"],
            ValidAudience = jwtSection["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
            ClockSkew = TimeSpan.Zero
        };
    });

    builder.Services.AddAuthorization();

    // ─── CORS ─────────────────────────────────────────────────────────────────
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowClient", policy =>
        {
            policy.SetIsOriginAllowed(origin =>
            {
                if (string.IsNullOrEmpty(origin)) return false;
                if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri)) return false;
                var host = uri.Host;
                return host.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
                       host.Equals("tspmaster.com", StringComparison.OrdinalIgnoreCase) ||
                       host.EndsWith(".tspmaster.com", StringComparison.OrdinalIgnoreCase);
            })
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
        });
    });

    // ─── HttpClient ───────────────────────────────────────────────────────────
    builder.Services.AddHttpClient("TspClient", client =>
    {
        client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
        client.Timeout = TimeSpan.FromSeconds(60);
    });

    builder.Services.AddHttpClient("MarketDataClient", client =>
    {
        client.Timeout = TimeSpan.FromSeconds(15);
    });

    // ─── Application Services ─────────────────────────────────────────────────
    builder.Services.AddScoped<ITokenService, TokenService>();
    builder.Services.AddScoped<ITspDataService, TspDataService>();
    builder.Services.AddScoped<IIntradayMarketService, IntradayMarketService>();
    builder.Services.AddScoped<IAllocationService, AllocationService>();
    builder.Services.AddScoped<IPortfolioService, PortfolioService>();
    builder.Services.AddScoped<IAnalysisService, AnalysisService>();

    var emailProvider = builder.Configuration["EmailProvider"] ?? "Smtp";
    if (emailProvider.Equals("SES", StringComparison.OrdinalIgnoreCase))
    {
        builder.Services.AddScoped<IEmailService, SesEmailService>();
    }
    else
    {
        builder.Services.AddScoped<IEmailService, SmtpEmailService>();
    }


    // ─── Background Services ──────────────────────────────────────────────────
    builder.Services.AddHostedService<TspPriceSyncService>();
    builder.Services.AddHostedService<TspDailyRecommendationService>();

    // ─── Controllers + Swagger ────────────────────────────────────────────────
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "TSP Master API",
            Version = "v1",
            Description = "API for TSP fund analysis, user allocations, and AI-powered investment recommendations."
        });

        c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Description = "JWT Authorization header using the Bearer scheme. Example: 'Bearer {token}'",
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
                    Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
                },
                Array.Empty<string>()
            }
        });
    });

    // ─── Build App ────────────────────────────────────────────────────────────
    var app = builder.Build();

    // ─── Seed Database ────────────────────────────────────────────────────────
    using (var scope = app.Services.CreateScope())
    {
        await DbInitializer.InitializeAsync(scope.ServiceProvider);
    }

    // ─── Middleware Pipeline ──────────────────────────────────────────────────
    app.UseExceptionHandler(errApp => errApp.Run(async ctx =>
    {
        ctx.Response.StatusCode = 500;
        ctx.Response.ContentType = "application/json";
        var ex = ctx.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>()?.Error;
        var logger2 = ctx.RequestServices.GetRequiredService<ILogger<Program>>();
        logger2.LogError(ex, "Unhandled exception on {Method} {Path}", ctx.Request.Method, ctx.Request.Path);
        var detail = app.Environment.IsDevelopment() ? ex?.Message : null;
        await ctx.Response.WriteAsJsonAsync(new { error = "An internal server error occurred.", detail });
    }));

    app.UseCors("AllowClient");

    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "TSP Master API v1"));

    app.UseAuthentication();
    app.UseAuthorization();


    app.MapGet("/health", () => Results.Ok(new { status = "Healthy", service = "TSPMaster.API", domain = "api.tspmaster.com" }));
    app.MapControllers();


    await app.RunAsync();
}
catch (Exception ex)
{
    logger.Error(ex, "TSP Master API stopped due to an unhandled exception.");
    throw;
}
finally
{
    LogManager.Shutdown();
}
