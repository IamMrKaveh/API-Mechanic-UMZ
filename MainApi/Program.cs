var builder = WebApplication.CreateBuilder(args);

// DbContext
builder.Services.AddDbContext<MechanicContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("PoolerConnection") ??
        throw new InvalidOperationException("Connection string 'PoolerConnection' not found.")
    )
);

// Controllers with JSON options
builder.Services.AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles
    );

// Swagger
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{
    try
    {
        // Optional: configure Swagger here
    }
    catch (Exception ex)
    {
        Console.WriteLine("Swagger generation error:");
        Console.WriteLine(ex);
    }
});

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", b =>
        b.WithOrigins(
            "https://mechanic-umz.netlify.app"
        )
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials()
    );
});

// Authentication
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
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? "default_secret_key"))
    };
});

// Services
builder.Services.AddSingleton<RateLimitService>();
builder.Services.AddScoped<ICartService, CartService>();

var app = builder.Build();

// Enable Swagger only in Development
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Developer exception page
app.UseDeveloperExceptionPage();

// Global exception logging and Swagger-specific logging
app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (Exception ex)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("Unhandled exception:");
        Console.WriteLine(ex.ToString());
        Console.ResetColor();

        context.Response.StatusCode = 500;

        if (context.Request.Path.StartsWithSegments("/swagger"))
        {
            context.Response.ContentType = "text/plain";
            await context.Response.WriteAsync("Swagger generation failed:\n\n");
            await context.Response.WriteAsync(ex.ToString());
        }
        else
        {
            await context.Response.WriteAsJsonAsync(new
            {
                error = "Internal Server Error",
                message = ex.Message,
                stackTrace = ex.StackTrace
            });
        }
    }
});

app.UseHttpsRedirection();
app.UseRouting();

// Apply CORS
app.UseCors("AllowAll");

// Auth
app.UseAuthentication();
app.UseAuthorization();

// Map controllers
app.MapControllers();

// Serve static files if any
app.UseStaticFiles();

// Run app
app.Run();
