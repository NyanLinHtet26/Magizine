using System.Text.Json.Serialization;
using MagizinePublic.Api;

var builder = WebApplication.CreateBuilder(args);

// Every service, the database, and the CORS policy are registered from one place.
builder.AddModularService();

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

// CORS runs after UseRouting (inserted automatically by WebApplication once endpoints are mapped)
// and before authorization, so a preflight is answered by the CORS middleware instead of falling
// through to a controller that rejects OPTIONS with 405. With an empty Cors:AllowedOrigins list
// this permits no cross-origin browser calls, which is the intended state until a frontend exists.
app.UseCors();

app.UseAuthorization();

app.MapControllers();

app.Run();
