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
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        
        // websocketHandling.HandleWebSocketRequest(context);
        if (context.Request.Path == "/")
        {
            WebSocket webSocket = context.WebSockets.AcceptWebSocketAsync().Result;
            WebsocketStore.AddClient(webSocket);
            // JsonSerializer.Serialize(db.GetDbDto());
            WebsocketStore.sendText(webSocket, JsonSerializer.Serialize(db.GetDbDto()));
            
            var buffer = new byte[1024 * 4];
            while (webSocket.State == WebSocketState.Open)
            {
                var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                if (result.MessageType == WebSocketMessageType.Text)
                {
                    var message = System.Text.Encoding.UTF8.GetString(buffer, 0, result.Count);

                    if (message == "pytrack")
                    {
                        WebsocketStore.UpgradeClientToPytrack(webSocket);
                    }
                    else if(message == "devBoard")
                    {
                        WebsocketStore.UpgradeClientToDevBoard(webSocket);
                    }
                    else
                    {
                        Console.WriteLine(message);
                    }
                }
                
            }
            WebsocketStore.RemoveClient(webSocket);
            
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


