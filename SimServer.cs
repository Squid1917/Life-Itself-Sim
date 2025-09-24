using System;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Linq;

public class SimServer
{
    private static readonly List<WebSocket> _clients = new List<WebSocket>();
    private static readonly SemaphoreSlim _clientLock = new SemaphoreSlim(1, 1);
    private SimManager _simManager;
    private readonly string _serverUri;
    private readonly HttpListener _httpListener;
    private readonly CancellationTokenSource _cts = new CancellationTokenSource();
    
    // New constructor to accept a port number
    public SimServer(int port)
    {
        _serverUri = $"http://+:{port}/";
        // The SimManager is now created outside of this class
        _httpListener = new HttpListener();
        _httpListener.Prefixes.Add(_serverUri);
    }

    public void SetSimManager(SimManager simManager)
    {
        // This method allows the SimManager to be set after the SimServer is instantiated.
        // It's a common pattern to handle circular dependencies.
        _simManager = simManager;
    }

    public async Task StartAsync()
    {
        try
        {
            _httpListener.Start();
            Console.WriteLine($"WebSocket server listening on {_serverUri}");

            // The simulation is now started by the Program class,
            // so we don't need to do it here.

            // Accept incoming connections.
            while (true)
            {
                var context = await _httpListener.GetContextAsync();
                if (context.Request.IsWebSocketRequest)
                {
                    await ProcessWebSocketRequestAsync(context);
                }
                else
                {
                    // Handle non-WebSocket requests, if any.
                    context.Response.StatusCode = 400;
                    context.Response.Close();
                }
            }
        }
        catch (HttpListenerException ex)
        {
            Console.WriteLine($"HttpListener error: {ex.Message}");
        }
        finally
        {
            if (_httpListener.IsListening)
            {
                _httpListener.Stop();
            }
        }
    }

    private async Task ProcessWebSocketRequestAsync(HttpListenerContext context)
    {
        var webSocketContext = await context.AcceptWebSocketAsync(null);
        var webSocket = webSocketContext.WebSocket;

        await _clientLock.WaitAsync();
        try
        {
            _clients.Add(webSocket);
            Console.WriteLine($"Client connected. Total clients: {_clients.Count}");
        }
        finally
        {
            _clientLock.Release();
        }

        while (webSocket.State == WebSocketState.Open)
        {
            // The server primarily broadcasts, so this loop keeps the connection alive
            // and can be used to read client messages if needed in the future.
            var buffer = new byte[1024];
            var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            if (result.MessageType == WebSocketMessageType.Close)
            {
                await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
            }
        }

        await _clientLock.WaitAsync();
        try
        {
            _clients.Remove(webSocket);
            Console.WriteLine($"Client disconnected. Total clients: {_clients.Count}");
        }
        finally
        {
            _clientLock.Release();
        }
    }

    public async Task BroadcastStateAsync(SimSaveState state)
    {
        var json = JsonConvert.SerializeObject(state);
        var buffer = Encoding.UTF8.GetBytes(json);

        await _clientLock.WaitAsync();
        try
        {
            var clientsToSend = new List<WebSocket>(_clients);
            foreach (var client in clientsToSend)
            {
                if (client.State == WebSocketState.Open)
                {
                    try
                    {
                        await client.SendAsync(
                            new ArraySegment<byte>(buffer, 0, buffer.Length),
                            WebSocketMessageType.Text,
                            true,
                            CancellationToken.None
                        );
                    }
                    catch (WebSocketException ex)
                    {
                        Console.WriteLine($"Error sending to client: {ex.Message}");
                    }
                }
            }
        }
        finally
        {
            _clientLock.Release();
        }
    }
}
