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
                case "offer":
                    WebsocketStore.waitingRTCAnswer.Add(client);
                    try
                    {
                        WebsocketStore.sendText(WebsocketStore.devBoard, message);
                        WebsocketStore.sendText(client, "{\"type\": \"acknowledge\", \"message\": \"waiting for answer\"}");
                    }
                    catch (Exception e)
                    {
                        WebsocketStore.sendText(client, "{\"type\": \"error\", \"message\": \" devBoard not connected \"}");
                    }
                    break;
                case "answer":
                    if (WebsocketStore.devBoard == client)
                    {
                        var recepiant = WebsocketStore.waitingRTCAnswer[0];
                        WebsocketStore.sendText(recepiant, message);
                        WebsocketStore.waitingRTCAnswer.Remove(recepiant);
                    }
                    break;
                case "sensors":
                    foreach (var sensor in deserializedMessage["value"].EnumerateObject())
                    {
                        _db.AddSensorData(sensor.Name, sensor.Value.ToString());
                    }

                    break;
                default:
                    Console.WriteLine("unknown message type");
                    Console.WriteLine(message);
                    break;
            }
        }
        catch (Exception e)
        {
            try
            {
                Console.WriteLine("----- ACTUAL MESSAGE BEFORE PARSE -----");
                Console.WriteLine("StartsWith: " + message.Substring(0, Math.Min(10, message.Length)));
                Console.WriteLine("Full: " + message);
                Console.WriteLine("Is JSON? " + message.TrimStart().StartsWith("{"));
                Dictionary<string, string> deserializedMessage =
                    JsonSerializer.Deserialize<Dictionary<string, string>>(message);
                switch (deserializedMessage["type"])
                {
                    case "offer":
                        WebsocketStore.waitingRTCAnswer.Add(client);
                        try
                        {
                            WebsocketStore.sendText(WebsocketStore.devBoard, message);
                            WebsocketStore.sendText(client, "{\"type\": \"acknowledge\", \"message\": \"waiting for answer\"}");
                        }
                        catch (Exception e2)
                        {
                            WebsocketStore.sendText(client, "{\"type\": \"error\", \"message\": \" devBoard not connected \"}");
                        }
                        break;
                    case "answer":
                        if (WebsocketStore.devBoard == client)
                        {
                            var recepiant = WebsocketStore.waitingRTCAnswer[0];
                            WebsocketStore.sendText(recepiant, message);
                            WebsocketStore.waitingRTCAnswer.Remove(recepiant);
                        }
                        break;
                    default:
                        WebsocketStore.sendText(client, "{\"type\": \"error\", \"message\": \"line145 " + e.Message + "\"}");
                        break;
                }
            }
            catch (Exception e2)
            {
                WebsocketStore.sendText(client, "{\"type\": \"error\", \"message\": \" line 151" + e2.Message + "\"}");
            }
        }
    }
}