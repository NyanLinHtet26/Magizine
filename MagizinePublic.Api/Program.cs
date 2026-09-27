using System.Text.Json.Serialization;
using Magizine.DataBase;
using MagizinePublic.Api;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Enums travel as names ("Success") rather than ordinals, so the wire
        // contract survives any future reordering of EnumRespType.
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());

        // Match the reference project: property names stay PascalCase on the wire.
        options.JsonSerializerOptions.PropertyNamingPolicy = null;
    });

//DataBase Config 
builder.Services.AddMagizineDatabase(builder.Configuration);

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// empty delegate => use the IExceptionHandler implementations registered above
app.UseExceptionHandler(_ => { });

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
