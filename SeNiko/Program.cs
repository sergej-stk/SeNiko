var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();
builder.Host.UseSerilog();
try
{
    Log.Information("Starting web host");
    Log.Debug($"- Environment: {builder.Environment.EnvironmentName}");
    Log.Debug($"- Arguments: {args}");
    
    Log.Information("Serilog configured");

    builder.Services.AddRateLimiter(rateLimiterOptions => rateLimiterOptions
        .AddFixedWindowLimiter(policyName: "fixed", options =>
        {
            options.PermitLimit = 4;
            options.Window = TimeSpan.FromSeconds(12);
            options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            options.QueueLimit = 2;
            Log.Information($"RateLimiter configured");
            Log.Debug($"- PermitLimit: {options.PermitLimit}");
            Log.Debug($"- Window: {options.Window}");
            Log.Debug($"- QueueProcessingOrder: {options.QueueProcessingOrder}");
            Log.Debug($"- QueueLimit: {options.QueueLimit}");
        }));

    builder.Services.AddApiVersioning(options =>
    {
        options.DefaultApiVersion = new ApiVersion(1, 0);
        options.ReportApiVersions = true;
        options.AssumeDefaultVersionWhenUnspecified = true;
        options.ApiVersionReader = new UrlSegmentApiVersionReader();
        Log.Information("ApiVersioning configured");
        Log.Debug($"- DefaultApiVersion: {options.DefaultApiVersion}");
        Log.Debug($"- ReportApiVersions: {options.ReportApiVersions}");
        Log.Debug($"- AssumeDefaultVersionWhenUnspecified: {options.AssumeDefaultVersionWhenUnspecified}");
        Log.Debug($"- ApiVersionReader: {options.ApiVersionReader}");
    });

    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new OpenApiInfo()
        {
            Version = "1",
            Title = "v1 API",
            Description = "v1 API Description",

        });

        var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
        options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));
    });

    builder.Services.AddControllers();
    // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    var app = builder.Build();

    app.UseSerilogRequestLogging();

    app.UseHttpsRedirection();

    app.UseAuthorization();

    app.UseRateLimiter();

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
            c.SwaggerEndpoint($"/swagger/v1/swagger.json", $"v1"));
    }

    app.MapControllers();

    app.Run();
}
catch (Exception e)
{
    Log.Fatal(e, $"Host terminated unexpectedly");
}