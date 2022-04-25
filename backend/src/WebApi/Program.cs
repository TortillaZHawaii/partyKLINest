using PartyKlinest.Infrastructure;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
const string CORS_POLICY_DEV = "CorsPolicyDev";
const string CORS_POLICY_PROD = "CorsPolicyProd";
string[] frontend_urls = new string[] { "" }; // TODO

builder.Services.AddCors(options =>
{
    options.AddPolicy(CORS_POLICY_DEV,
        builder =>
        {
            builder.AllowAnyOrigin(); // unsafe, okay for testing
            builder.AllowAnyHeader();
            builder.AllowAnyMethod();
        });
    options.AddPolicy(CORS_POLICY_PROD,
        builder =>
        {
            builder.WithOrigins(frontend_urls);
            builder.AllowAnyHeader();
            builder.AllowAnyMethod();
        });
});

Dependencies.ConfigureServices(builder.Configuration, builder.Services);

builder.Services.AddControllers()
    .AddJsonOptions(j =>
    {
        // Convert C# enums (int wrappers) to strings
        j.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });


// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("ClientsOnly", policy => policy.RequireClaim("Client"));
    options.AddPolicy("CleanersOnly", policy => policy.RequireClaim("Cleaner"));
    options.AddPolicy("AdministratorsOnly", policy => policy.RequireClaim("Administrator"));
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

if (app.Environment.IsDevelopment())
{
    app.UseCors(CORS_POLICY_DEV);
}
else
{
    app.UseCors(CORS_POLICY_PROD);
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
