using System.Net.WebSockets;
using System.Text.Json;
using iot_server_cs.Controllers;

namespace iot_server_cs;

public class WebSocketMessageHandler
{
    public readonly AppDbContext _db;
    
    public WebSocketMessageHandler(IServiceProvider serviceProvider)
    {
        _db = serviceProvider.GetRequiredService<AppDbContext>();
    }

    // public async Task HandleWebSocketRequest(HttpContext context)
    // {
    //     if (context.Request.Path == "/")
    //     {
    //         WebSocket ws = context.WebSockets.AcceptWebSocketAsync().Result;
    //         WebsocketStore.AddClient(ws);
    //         // JsonSerializer.Serialize(db.GetDbDto());
    //         WebsocketStore.sendText(ws, JsonSerializer.Serialize(_db.GetDbDto()));
    //         
    //         var buffer = new byte[1024 * 4];
    //         while (ws.State == WebSocketState.Open)
    //         {
    //             var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
    //             if (result.MessageType == WebSocketMessageType.Text)
    //             {
    //                 var message = System.Text.Encoding.UTF8.GetString(buffer, 0, result.Count);
    //                 handleWebsocketMessage(ws, message);
    //             }
    //         }
    //         WebsocketStore.RemoveClient(ws);
    //     }
    // }

    public void handleWebsocketMessage(WebSocket client, string message)
    {
        if (message == "pytrack")
        {
            if (WebsocketStore.pytrack != null)
            {
                WebsocketStore.sendText(client, "{\"type\": \"error\", \"message\": \"there already is a pytrack connected\"}");
            }
            else
            {
                WebsocketStore.UpgradeClientToPytrack(client);
                WebsocketStore.sendText(client, "{\"type\": \"acknowledge\", \"message\": \"upgraded to pytrack\"}");
            }

            return;

        }
        else if (message == "devBoard")
        {
            if (WebsocketStore.devBoard != null)
            {
                WebsocketStore.sendText(client, "{\"type\": \"error\", \"message\": \"there already is a devboard connected\"}");
            }
            else
            {
                WebsocketStore.UpgradeClientToDevBoard(client);
                WebsocketStore.sendText(client, "{\"type\": \"acknowledge\", \"message\": \"upgraded to devboard\"}");
            }

            return;
        }

        try
        {
            Dictionary<string, JsonElement> deserializedMessage =
                JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(message);
            if (deserializedMessage == null)
            {
                WebsocketStore.sendText(client, "{\"type\": \"error\", \"message\": \"invalid message\"}");
                return;
            }
            
            switch (deserializedMessage["type"].GetString())
            {
                case "sensor":
                    _db.AddSensorData(deserializedMessage["name"].GetString()!, deserializedMessage["value"].GetString()!);
                    WebsocketStore.sendText(client, "{\"type\": \"acknowledge\", \"message\": \"sensor data set\"}");
                    break;
                case "location":
                    _db.SetLocation(
                        new LocationDto{
                            latitude = deserializedMessage["latitude"].GetDouble(),
                            longitude = deserializedMessage["longitude"].GetDouble()
                        }
                    );
                    WebsocketStore.sendText(client, "{\"type\": \"acknowledge\", \"message\": \"location set\"}");
                    break;
                case "locationrange":
                    _db.SetLocationRange(deserializedMessage["value"].GetDouble());
                    WebsocketStore.sendText(client, "{\"type\": \"acknowledge\", \"message\": \"location range set\"}");
                    break;
                case "locationcenter":
                    _db.SetLocationRangeCenter(
                        deserializedMessage["latitude"].GetDouble(),
                        deserializedMessage["longitude"].GetDouble()
                    );
                    WebsocketStore.sendText(client, "{\"type\": \"acknowledge\", \"message\": \"locationcenter set\"}");
                    break;
                case "getstate":
                    _db.sendState(client);
                    break;
                case "getvideo":
                    WebsocketStore.sendText(client, "{\"type\": \"error\", \"message\": \"invalid message\"}");
                    break;
            }
        }
        catch (Exception e)
        {
            WebsocketStore.sendText(client, "{\"type\": \"error\", \"message\": \"" + e.Message + "\"}");
        }
    }
}