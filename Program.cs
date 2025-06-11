using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WebHS.Data;
using WebHS.Models;
using WebHS.Services;
using WebHS.Services.Enhanced;
using Serilog;
using ILogger = Microsoft.Extensions.Logging.ILogger;

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/webhs-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);

// Add Serilog
builder.Host.UseSerilog();

// Add Entity Framework
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add Identity
builder.Services.AddIdentity<User, IdentityRole>(options =>
{
    // Password settings - Relaxed for development
    options.Password.RequiredLength = 6;
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    
    // User settings
    options.User.RequireUniqueEmail = true;
    options.SignIn.RequireConfirmedEmail = false; // Disable for development

    // Lockout settings
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(30);
    options.Lockout.MaxFailedAccessAttempts = 5;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// Add Authentication - Configure for both Cookie (MVC) and JWT (API)
builder.Services.AddAuthentication(options =>
    {
        // For MVC controllers, use cookies by default
        options.DefaultAuthenticateScheme = IdentityConstants.ApplicationScheme;
        options.DefaultChallengeScheme = IdentityConstants.ApplicationScheme;
        options.DefaultSignInScheme = IdentityConstants.ApplicationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "WebHS",
            ValidAudience = builder.Configuration["Jwt:Audience"] ?? "WebHS.Users",
            IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
                System.Text.Encoding.UTF8.GetBytes(builder.Configuration["Jwt:SecretKey"] ?? "WebHS_Super_Secret_Key_2024!@#$%^&*"))
        };
    })
    .AddGoogle(options =>
    {
        IConfigurationSection googleAuthSection = builder.Configuration.GetSection("Authentication:Google");
        options.ClientId = googleAuthSection["ClientId"] ?? "";
        options.ClientSecret = googleAuthSection["ClientSecret"] ?? "";
    })
    .AddFacebook(options =>
    {
        IConfigurationSection facebookAuthSection = builder.Configuration.GetSection("Authentication:Facebook");
        options.AppId = facebookAuthSection["AppId"] ?? "";
        options.AppSecret = facebookAuthSection["AppSecret"] ?? "";
    });

// Configure Email Settings
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));

// Add HttpClient for external API calls
builder.Services.AddHttpClient<GeocodingService>();
builder.Services.AddHttpClient<WebHS.Services.Enhanced.EnhancedGeocodingService>();
builder.Services.AddHttpClient<IWeatherService, WeatherService>();
builder.Services.AddHttpClient<IUnsplashService, UnsplashService>();

// Register services
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IHomestayService, HomestayService>();
builder.Services.AddScoped<IBookingService, BookingService>();
builder.Services.AddScoped<IFileUploadService, FileUploadService>();
builder.Services.AddScoped<IPricingService, PricingService>();
builder.Services.AddScoped<IWeatherService, WeatherService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IUnsplashService, UnsplashService>();
builder.Services.AddScoped<GeocodingService>();
builder.Services.AddScoped<WebHS.Services.Enhanced.EnhancedGeocodingService>();
// Register the data seeder service
builder.Services.AddScoped<DataSeederServiceFixed>();

// Register new professional services
builder.Services.AddScoped<ICacheService, CacheService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IBackgroundJobService, BackgroundJobService>();
builder.Services.AddScoped<ISeoService, SeoService>();

// Register background service
builder.Services.AddHostedService<BackgroundJobHostedService>();

// Add logging for services
builder.Services.AddLogging();
builder.Services.AddSingleton<ILogger<HomestayService>>(provider =>
    provider.GetRequiredService<ILoggerFactory>().CreateLogger<HomestayService>());
builder.Services.AddSingleton<ILogger<BookingService>>(provider =>
    provider.GetRequiredService<ILoggerFactory>().CreateLogger<BookingService>());

// Add services to the container.
builder.Services.AddControllersWithViews();

// Add API Controllers
builder.Services.AddControllers();

// Add Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() 
    {
        Title = "WebHS Homestay Booking API",
        Version = "v1",
        Description = "Professional homestay booking system API",
        Contact = new()
        {
            Name = "WebHS Support",
            Email = "support@webhs.com"
        }
    });
});

// Add Memory Cache
builder.Services.AddMemoryCache();

// Add Response Caching
builder.Services.AddResponseCaching();

// Add Health Checks
builder.Services.AddHealthChecks();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("DefaultPolicy", policy =>
    {
        policy.WithOrigins("http://localhost:3000", "https://localhost:3001")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Add session support
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
else
{
    // Enable Swagger in development
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "WebHS API v1");
        c.RoutePrefix = "api-docs";
    });
    
    // Enable detailed error page in development
    app.UseDeveloperExceptionPage();
}

// Register global exception handling middleware only for non-development
if (!app.Environment.IsDevelopment())
{
    app.UseMiddleware<WebHS.Middleware.GlobalExceptionMiddleware>();
}

// Add CORS
app.UseCors("DefaultPolicy");

// Add security middleware
app.UseMiddleware<WebHS.Middleware.SecurityHeadersMiddleware>();
app.UseMiddleware<WebHS.Middleware.RateLimitingMiddleware>();

// Add Response Caching
app.UseResponseCaching();

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseSession();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// Map Health Checks
app.MapHealthChecks("/health");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Map API Controllers
app.MapControllers();

// Seed database with initial data
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    
    try
    {        
        logger.LogInformation("Starting database basic initialization...");
        // Apply database migrations
        var dbContext = services.GetRequiredService<ApplicationDbContext>();
        // Apply all pending migrations
        await dbContext.Database.MigrateAsync();
        
        // Add basic admin user if needed
        var userManager = services.GetRequiredService<UserManager<User>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        
        // Create roles if they don't exist
        if (!await roleManager.RoleExistsAsync("Admin"))
        {
            await roleManager.CreateAsync(new IdentityRole("Admin"));
            logger.LogInformation("Admin role created successfully");
        }
        
        if (!await roleManager.RoleExistsAsync("Host"))
        {
            await roleManager.CreateAsync(new IdentityRole("Host"));
            logger.LogInformation("Host role created successfully");
        }
        
        if (!await roleManager.RoleExistsAsync("User"))
        {
            await roleManager.CreateAsync(new IdentityRole("User"));
            logger.LogInformation("User role created successfully");
        }
        
        // Create admin user
        await CreateAdminUser(userManager, logger);
          // Create host users for data seeding
        await CreateHostUsers(userManager, logger);
        
        // Create regular users for data seeding
        await CreateRegularUsers(userManager, logger);
        
        // Seed sample data (homestays, amenities, etc.)
        logger.LogInformation("Starting sample data seeding...");
        var dataSeeder = services.GetRequiredService<DataSeederServiceFixed>();
        await dataSeeder.SeedDataAsync();
        logger.LogInformation("Sample data seeding completed successfully.");
        
        logger.LogInformation("Database initialization completed successfully.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred during database initialization");
    }
}

app.Run();

// Helper method for creating admin user
static async Task CreateAdminUser(UserManager<User> userManager, ILogger logger)
{
    const string adminEmail = "admin1@webhs.com";
    
    var existingUser = await userManager.FindByEmailAsync(adminEmail);
    if (existingUser == null)
    {
        var adminUser = new User
        {
            UserName = adminEmail,
            Email = adminEmail,
            FirstName = "System",
            LastName = "Administrator",
            PhoneNumber = "0123456789",
            IsHost = false,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(adminUser, adminEmail);
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(adminUser, "Admin");
            logger.LogInformation("Admin user created successfully");
        }
        else
        {
            logger.LogError($"Failed to create admin user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }
    }
}

// Helper method for creating host users
static async Task CreateHostUsers(UserManager<User> userManager, ILogger logger)
{
    var hostEmails = new[]
    {
        "host1@webhs.com",
        "host2@webhs.com",
        "host3@webhs.com"
    };

    foreach (var email in hostEmails)
    {
        var existingUser = await userManager.FindByEmailAsync(email);
        if (existingUser == null)
        {
            var hostUser = new User
            {
                UserName = email,
                Email = email,
                FirstName = "Host",
                LastName = email.Split('@')[0],
                PhoneNumber = "0987654321",
                IsHost = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(hostUser, email);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(hostUser, "Host");
                logger.LogInformation($"Host user {email} created successfully");
            }
        }
    }
}

// Helper method for creating regular users
static async Task CreateRegularUsers(UserManager<User> userManager, ILogger logger)
{
    var userEmails = new[]
    {
        "user1@webhs.com",
        "user2@webhs.com",
        "user3@webhs.com"
    };

    foreach (var email in userEmails)
    {
        var existingUser = await userManager.FindByEmailAsync(email);
        if (existingUser == null)
        {
            var regularUser = new User
            {
                UserName = email,
                Email = email,
                FirstName = "User",
                LastName = email.Split('@')[0],
                PhoneNumber = "0123456789",
                IsHost = false,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(regularUser, email);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(regularUser, "User");
                logger.LogInformation($"Regular user {email} created successfully");
            }
        }
    }
}

