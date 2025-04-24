using System.Net.WebSockets;

namespace iot_server_cs;

public static class WebsocketStore
{

    public static WebSocket pytrack;

    public static WebSocket devBoard;
    
    public static List<WebSocket> clients = new List<WebSocket>();

    public static void AddClient(WebSocket client)
    {
        clients.Add(client);
    }
    
    public static void RemoveClient(WebSocket client)
    {
        if (client == pytrack)
        {
            pytrack = null;
        }
        else if (client == devBoard)
        {
            devBoard = null;
        }
        else
        {
            clients.Remove(client);
        }
    }
    
    public static void UpgradeClientToPytrack(WebSocket client)
    {
        if (pytrack != null)
        {
            throw new Exception("Pytrack already connected");
        }
        pytrack = client;
        clients.Remove(client);
    }
    
    public static void UpgradeClientToDevBoard(WebSocket client)
    {
        if (devBoard != null)
        {
            throw new Exception("Devboard already connected");
        }
        devBoard = client;
        clients.Remove(client);
    }

    // public async void HandleWebSocketRequest(HttpContext context)
    // {
    //     if (context.Request.Path == "/")
    //     {
    //         WebSocket webSocket = context.WebSockets.AcceptWebSocketAsync().Result;
    //         AddClient(webSocket);
    //         
    //         var buffer = new byte[1024 * 4];
    //         while (webSocket.State == WebSocketState.Open)
    //         {
    //             var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
    //             if (result.MessageType == WebSocketMessageType.Text)
    //             {
    //                 var message = System.Text.Encoding.UTF8.GetString(buffer, 0, result.Count);
    //
    //                 if (message == "pytrack")
    //                 {
    //                     UpgradeClientToPytrack(webSocket);
    //                 }
    //                 else if(message == "devBoard")
    //                 {
    //                     UpgradeClientToDevBoard(webSocket);
    //                 }
    //                 else
    //                 {
    //                     Console.WriteLine(message);
    //                 }
    //             }
    //             
    //         }
    //         RemoveClient(webSocket);
    //         
    //     }
    // }
    
    public async static void SendToClients(string message)
    {
        var buffer = System.Text.Encoding.UTF8.GetBytes(message);
        var segment = new ArraySegment<byte>(buffer);
        foreach (var client in clients)
        {
            if (client.State == WebSocketState.Open)
            {
                await client.SendAsync(segment, WebSocketMessageType.Text, true, CancellationToken.None);
            }
        }
    }
    
    public static void sendText(WebSocket client, string message)
    {
        var buffer = System.Text.Encoding.UTF8.GetBytes(message);
        var segment = new ArraySegment<byte>(buffer);
        client.SendAsync(segment, WebSocketMessageType.Text, true, CancellationToken.None);
    }
    
}