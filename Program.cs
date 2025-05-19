using iot_server_cs;
using Microsoft.EntityFrameworkCore;
using System.Net.WebSockets;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddDbContext<AppDbContext>(options =>
    options
        .UseSqlite("Data Source=app.db")
        .EnableSensitiveDataLogging()
        .LogTo(Console.WriteLine, LogLevel.Information));

builder.Services.AddSingleton<WebSocketMessageHandler>();


var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.UseWebSockets();

app.Use( async (context, next) =>
{
    if (context.WebSockets.IsWebSocketRequest)
    {
        using var scope = app.Services.CreateScope();
        var wsm = scope.ServiceProvider.GetRequiredService<WebSocketMessageHandler>();
        if (context.Request.Path == "/")
        {
            WebSocket ws = await context.WebSockets.AcceptWebSocketAsync();
            WebsocketStore.AddClient(ws);
            // JsonSerializer.Serialize(db.GetDbDto());
            WebsocketStore.sendText(ws, JsonSerializer.Serialize(wsm._db.GetDbDto()));
            
            var buffer = new byte[1024 * 4];
            while (ws.State == WebSocketState.Open)
            {
                var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                if (result.MessageType == WebSocketMessageType.Text)
                {
                    var message = System.Text.Encoding.UTF8.GetString(buffer, 0, result.Count);
                    Console.WriteLine(message);
                    wsm.handleWebsocketMessage(ws, message);
                }
            }
            
            WebsocketStore.RemoveClient(ws);
        }
    }
    else
    {
        await next();
    }
}); 



app.MapControllers();

app.UseDefaultFiles();
app.UseStaticFiles();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated(); // Or use Migrate() if using migrations
}

Console.WriteLine("app has started");

app.Run();


