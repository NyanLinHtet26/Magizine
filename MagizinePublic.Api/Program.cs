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

// The GlobalExceptionHandler writes its body with HttpResponse.WriteAsJsonAsync, which reads
// Http.Json's JsonOptions - a DIFFERENT type from the MVC options configured above. Without
// this second block a 500 serialises as camelCase + numeric enums while a 200 is PascalCase
// + string enums, and clients reading result.RespCode get undefined on errors.
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
    options.SerializerOptions.PropertyNamingPolicy = null;
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
