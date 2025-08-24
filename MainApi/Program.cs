using MainApi.Controllers.User;

var builder = WebApplication.CreateBuilder(args);

//Add needed services to dependency injection
#region DbContext

builder.Services.AddDbContext<MechanicContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("TransactionConnection") ??
        throw new InvalidOperationException("Connection string 'DefaultConnection' not found.")
    )
);

#endregion

// Options to add controllers and configure JSON to avoid reference loops
builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    });

//Adding Swagger services for API documentation
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

//Enabling CORS to allow access from different domains
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularFrontend",
        builder =>
        {
            builder.WithOrigins("http://localhost:4200")
                   .AllowAnyHeader()
                   .AllowAnyMethod();
        });
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowNetlify",
        builder =>
        {
            builder.WithOrigins("https://mechanic-umz.netlify.app")
                   .AllowAnyHeader()
                   .AllowAnyMethod();
        });
});

builder.Services
.AddAuthentication(options =>
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
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? "default_secret_key"))
    };
});

builder.Services.AddSingleton<RateLimitService>();

var app = builder.Build();

//Configuring the HTTP Request Pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseRouting();

//Using CORS
app.UseCors("AllowAngularFrontend");
app.UseCors("AllowNetlify");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

//https://mechanic-umz.liara.run/
app.UseStaticFiles();


app.Run();