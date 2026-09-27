using System.Text.Json.Serialization;
using MagizineAdmin.Api;

var builder = WebApplication.CreateBuilder(args);

// Every service, the database, and JWT auth are registered from one place.
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

// Authentication must run BEFORE authorization: [Authorize] needs the principal that
// UseAuthentication populates. Reversed, every policy evaluates as anonymous and
// endpoints behind [Authorize] return 401 even with a valid token.
app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();
