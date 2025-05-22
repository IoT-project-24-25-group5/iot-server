using System.Net.Security;
using iot_server_cs;
using Microsoft.EntityFrameworkCore;
using System.Net.WebSockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Server.Kestrel.Https;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddDbContext<AppDbContext>(options =>
    options
        .UseSqlite("Data Source=app.db")
        .EnableSensitiveDataLogging()
        .LogTo(Console.WriteLine, LogLevel.Information));

builder.Services.AddSingleton<WebSocketMessageHandler>();
builder.WebHost.ConfigureKestrel(options =>
{
    options.ConfigureHttpsDefaults(httpsOptions =>
    {
        httpsOptions.ClientCertificateMode = ClientCertificateMode.RequireCertificate;
        httpsOptions.CheckCertificateRevocation = false;
        httpsOptions.ClientCertificateValidation = (X509Certificate2 certificate, X509Chain chain, SslPolicyErrors  sslPolicyErrors) =>
        {
            // Custom validation logic
            return true; // Accept all certificates for demonstration purposes
        };
    });
});


var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.UseWebSockets(new WebSocketOptions
{
    KeepAliveInterval = TimeSpan.FromSeconds(30)
});

app.Use( async (context, next) =>
{
    if (context.WebSockets.IsWebSocketRequest)
    {
        using var scope = app.Services.CreateScope();
        var wsm = scope.ServiceProvider.GetRequiredService<WebSocketMessageHandler>();
        if (context.Request.Path == "/")
        {
            var cert = context.Connection.ClientCertificate;
            if (cert == null)
            {
                Console.WriteLine("no cert");
            }
            else
            {
                Console.WriteLine("public key: ");
                Console.WriteLine(cert.GetPublicKeyString());
            }
            WebSocket ws = await context.WebSockets.AcceptWebSocketAsync();
            WebsocketStore.AddClient(ws);
            // JsonSerializer.Serialize(db.GetDbDto());
            WebsocketStore.sendText(ws, JsonSerializer.Serialize(wsm._db.GetDbDto()));
            
            
            var buffer = new byte[4096];
            var messageBuilder = new StringBuilder();

            while (ws.State == WebSocketState.Open)
            {
                messageBuilder.Clear();
                WebSocketReceiveResult result;
                do
                {
                    result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                    var chunk = System.Text.Encoding.UTF8.GetString(buffer, 0, result.Count);
                    messageBuilder.Append(chunk);
                }
                while (!result.EndOfMessage); // 🔁 keep reading until entire message is received

                var fullMessage = messageBuilder.ToString();
                Console.WriteLine("📦 Full message received:");
                Console.WriteLine(fullMessage);

                wsm.handleWebsocketMessage(ws, fullMessage);
            }
            
            
            // var buffer = new byte[1024 * 4];
            // while (ws.State == WebSocketState.Open)
            // {
            //     var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            //     if (result.MessageType == WebSocketMessageType.Text)
            //     {
            //         var message = System.Text.Encoding.UTF8.GetString(buffer, 0, result.Count);
            //         Console.WriteLine(message);
            //         wsm.handleWebsocketMessage(ws, message);
            //     }
            // }
            
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


