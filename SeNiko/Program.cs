using Marten;
using Weasel.Core;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();
builder.Host.UseSerilog();
try
{
    Log.Information("Starting application initialization");
    Log.Information("Configuring application environment");
    Log.Debug("Environment Configuration Details:");
    Log.Debug($"- Environment Name: {builder.Environment.EnvironmentName}");
    Log.Debug($"- Application Name: {builder.Environment.ApplicationName}");
    Log.Debug($"- Content Root Path: {builder.Environment.ContentRootPath}");
    Log.Debug($"- Command Line Arguments: {string.Join(" ", args)}");
    
    var configSources = builder.Configuration.Sources
        .OfType<JsonConfigurationSource>()
        .Select(source => source.Path)
        .ToList();
    Log.Information($"- Configuration files: {configSources}");
    
    Log.Information("Configuring Rate Limiter");
    builder.Services.AddRateLimiter(rateLimiterOptions => rateLimiterOptions
        .AddFixedWindowLimiter(policyName: "FixedWindowThrottlingPolicy", options =>
        {
            Log.Debug("Configuring Fixed Window Limiter Settings:");
            
            options.PermitLimit = 4;
            Log.Debug($"- Request Permit Limit: {options.PermitLimit} requests");
            
            options.Window = TimeSpan.FromSeconds(12);
            Log.Debug($"- Time Window: {options.Window.TotalSeconds} seconds");
            
            options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            Log.Debug($"- Queue Processing Strategy: {options.QueueProcessingOrder}");
            
            options.QueueLimit = 2;
            Log.Debug($"- Maximum Queue Size: {options.QueueLimit} requests");
        }));
    Log.Information("Rate Limiter configuration completed");
    
    Log.Information("Configuring URL formatting options");
    builder.Services.Configure<RouteOptions>(options =>
    {
        Log.Debug("URL Formatting Configuration:");
        
        options.LowercaseUrls = true;        
        Log.Debug("- URLs will be converted to lowercase");
        
        options.LowercaseQueryStrings = true;
        Log.Debug("- Query strings will be converted to lowercase");
    });
    Log.Information("URL formatting configuration completed");

    Log.Information("Configuring JWT Authentication");
    var jwtSettings = builder.Configuration.GetSection("JwtSettings");
    try 
    {
        var secretKey = Encoding.UTF8.GetBytes(jwtSettings["SecretKey"] ??
                                               throw new InvalidOperationException("JWT SecretKey is not configured"));
        Log.Debug("JWT Configuration Details:");
        Log.Debug($"- Issuer: {jwtSettings["Issuer"]}");
        Log.Debug($"- Audience: {jwtSettings["Audience"]}");
        
        builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                Log.Debug($"- Authentication Scheme: {JwtBearerDefaults.AuthenticationScheme}");
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidIssuer = jwtSettings["Issuer"],
                    ValidAudience = jwtSettings["Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(secretKey),
                    ClockSkew = TimeSpan.Zero
                };
                Log.Debug("Token Validation Parameters configured");
            });
        Log.Information("JWT Authentication configuration completed");
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Failed to configure JWT Authentication");
        throw;
    }

    Log.Information("Configuring API Versioning");
    builder.Services.AddApiVersioning(options =>
    {
        options.DefaultApiVersion = new ApiVersion(1);
        options.ReportApiVersions = true;
        options.AssumeDefaultVersionWhenUnspecified = true;
        options.ApiVersionReader = new UrlSegmentApiVersionReader();
        Log.Debug("API Versioning Configuration:");
        Log.Debug($"- Default API Version: v{options.DefaultApiVersion}");
        Log.Debug($"- API Versions will be reported in response headers");
        Log.Debug($"- Unspecified versions will default to v{options.DefaultApiVersion}");
        Log.Debug($"- Version Reader Type: {options.ApiVersionReader.GetType().Name}");
    }).AddApiExplorer(options =>
    {
        options.GroupNameFormat = "'v'V";
        options.SubstituteApiVersionInUrl = true;
        Log.Debug("API Explorer Configuration:");
        Log.Debug($"- Group Name Format: {options.GroupNameFormat}");
        Log.Debug("- API Version will be substituted in URL");
    });
    
    Log.Information("Configuring Swagger Documentation");
    builder.Services.AddSwaggerGen(options =>
    {
        options.EnableAnnotations();
        
        Log.Debug("Configuring Swagger Security Definitions");
        options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            In = ParameterLocation.Header,
            Description = "Please enter a valid token",
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            BearerFormat = "JWT",
            Scheme = "Bearer"
        });

        options.AddSecurityRequirement(new OpenApiSecurityRequirement
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
                new string[] { }
            }
        });

        options.SwaggerDoc("v1", new OpenApiInfo()
        {
            Version = "1",
            Title = "ꌗєиเк๏-คקเ",
            Description = "SeNiko-API is a test monolith API designed to showcase and experiment with API development concepts. It provides a simple structure for learning and testing purposes.",
        });
        Log.Debug("Swagger API Documentation configured for v1");

        try
        {
            var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFilename);
            options.IncludeXmlComments(xmlPath);
            Log.Debug($"Included XML Comments from: {xmlPath}");
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to load XML documentation file");
        }
    });

    builder.Services.AddMarten(options => {
        options.Connection("Server=127.0.0.1;Port=5432;Database=postgres;User Id=postgres;Password=;");
        options.AutoCreateSchemaObjects = AutoCreate.All;
    });
    
    Log.Information("Configuring ASP.NET Core Services");
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    Log.Information("Building application");
    var app = builder.Build();

    Log.Information("Configuring HTTP Request Pipeline");
    app.UseSerilogRequestLogging(options =>
    {
        options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
    });

    app.UseHttpsRedirection();
    Log.Debug("HTTPS Redirection enabled");

    app.UseAuthentication();
    Log.Debug("Authentication middleware enabled");

    app.UseAuthorization();
    Log.Debug("Authorization middleware enabled");

    app.UseRateLimiter();
    Log.Debug("Rate Limiter middleware enabled");

    if (app.Environment.IsDevelopment())
    {
        Log.Information("Configuring Swagger UI for Development environment");
        app.UseSwagger();
        app.UseSwaggerUI(c => 
        {
            IApiVersionDescriptionProvider provider = app.Services.GetRequiredService<IApiVersionDescriptionProvider>();

            foreach (var item in provider.ApiVersionDescriptions)
            {
                c.SwaggerEndpoint($"/swagger/{item.GroupName}/swagger.json", $"API {item.GroupName}");
                Log.Debug($"Added Swagger Endpoint for API {item.GroupName}");
            }

            c.EnableDeepLinking();
            Log.Debug("Swagger UI Deep Linking enabled");
        });
    }

    app.MapControllers();
    Log.Debug("Controller endpoints mapped");

    Log.Information("Application configuration completed. Starting application...");
    app.Run();
}
catch (Exception e)
{
    Log.Fatal(e, "Application terminated unexpectedly");
    throw;
}
finally
{
    Log.Information("Shutting down application");
    Log.CloseAndFlush();
}