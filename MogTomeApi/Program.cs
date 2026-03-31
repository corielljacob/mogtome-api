using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using MogTomeApi.Features.Chronicle;
using MogTomeApi.Shared;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using System.Text;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

BsonSerializer.RegisterSerializer(new GuidSerializer(GuidRepresentation.Standard));

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowMogTome", policy =>
    {
        policy.WithOrigins("https://mogtome.com")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(config =>
{
    config.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "JWT Header",
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT"
    });

    config.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document)] = Array.Empty<string>().ToList()
    });

});

builder.Services.AddSignalR();
builder.Services.AddSingleton(new HttpClient());
builder.Services.AddDistributedMemoryCache();
builder.Services.AddMediatR(cfg => {
    cfg.RegisterServicesFromAssemblyContaining<Program>();
    cfg.LicenseKey = Environment.GetEnvironmentVariable(Constants.LuckyPennyLicense, EnvironmentVariableTarget.Machine);
});

builder.Services.AddSingleton<IMongoClient>(sp =>
{
    var connectionString = Environment.GetEnvironmentVariable(Constants.ConnectionStringId, EnvironmentVariableTarget.Machine);

    if (string.IsNullOrWhiteSpace(connectionString))
        connectionString = Environment.GetEnvironmentVariable(Constants.ConnectionStringId);

    return new MongoClient(connectionString);
});

builder.Services.AddScoped(sp =>
{
    var client = sp.GetRequiredService<IMongoClient>();
    return client.GetDatabase(Constants.KupoLifeDatabase);
});

builder.Services.AddSession(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.None;
    options.Cookie.Domain = builder.Configuration["Authentication:CookieDomain"];
    options.Cookie.Path = "/";
});

var secretKey = Environment.GetEnvironmentVariable("MogTomeApiSigningSecret", EnvironmentVariableTarget.Process);
var signingkey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Authentication:Host"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Authentication:Audience"],
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = signingkey
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

app.UseSession();

app.UseCors("AllowMogTome");

app.UseSession();

app.UseStaticFiles();

// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.InjectJavascript("/discord-login.js");
});

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.MapHub<EventsHub>("/eventsHub");

app.Run();
