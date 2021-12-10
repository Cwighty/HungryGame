using HungryHippos;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);
var requestErrorCount = 0L;

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddSingleton<GameLogic>();
builder.Services.AddSingleton<IRandomService, SystemRandomService>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddCors();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = builder.Environment.ApplicationName, Version = "v1" });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}

app.UseCors(builder =>
{
    builder
    .AllowAnyOrigin()
    .AllowAnyMethod()
    .AllowAnyHeader();
});

app.UseStaticFiles();

//THROW_ERRORS middleware
app.Use(async (context, next) =>
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    if(app.Configuration["THROW_ERRORS"] == "true")
    {
        Interlocked.Increment(ref requestErrorCount);
        if(Interlocked.Read(ref requestErrorCount) % 4 == 0)
        {
            logger.LogInformation("THROW_ERRORS enabled...every 4th request dies.");
            context.Response.StatusCode = 500;
            await context.Response.WriteAsync("Every 4th request fails!");
            return;
        }
    }
    await next();
});

app.UseRouting();
app.MapBlazorHub();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", $"{builder.Environment.ApplicationName} v1"));
}
app.MapFallbackToPage("/_Host");

//API endpoints
app.MapGet("/join", (string userName, GameLogic gameInfo) => gameInfo.JoinPlayer(userName));
app.MapGet("/move/left", (string token, GameLogic gameInfo) => gameInfo.Move(token, Direction.Left));
app.MapGet("/move/right", (string token, GameLogic gameInfo) => gameInfo.Move(token, Direction.Right));
app.MapGet("/move/up", (string token, GameLogic gameInfo) => gameInfo.Move(token, Direction.Up));
app.MapGet("/move/down", (string token, GameLogic gameInfo) => gameInfo.Move(token, Direction.Down));
app.MapGet("/users", ([FromServices] GameLogic gameInfo) => gameInfo.GetPlayersByScoreDescending().Select(p => new {p.Name, p.Id, p.Score}));

app.Run();
