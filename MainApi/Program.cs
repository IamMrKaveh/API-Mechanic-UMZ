var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("PoolerConnection") ??
                       throw new InvalidOperationException("Connection string 'PoolerConnection' not found.");

builder.Services.AddDbContext<MechanicContext>(options =>
    options.UseNpgsql(connectionString)
);

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter 'Bearer' [space] and then your token in the text input below.\n\nExample: \"Bearer 12345abcdef\""
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
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
            new string[] {}
        }
    });
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", b =>
        b.WithOrigins(
            "https://mechanic-umz.netlify.app",
            "http://localhost:4200"
        )
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials()
    );
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
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? "default_secret_key_that_is_long_enough_for_hs256"))
    };
});

builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
    ConnectionMultiplexer.Connect(builder.Configuration.GetConnectionString("Redis") ?? "localhost")
);

builder.Services.AddSingleton<IRateLimitService, RateLimitService>();
builder.Services.AddScoped<ICartService, CartService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseDeveloperExceptionPage();

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
                message = "An unexpected error occurred.",
                details = ex.Message
            });
        }
    }
});

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();