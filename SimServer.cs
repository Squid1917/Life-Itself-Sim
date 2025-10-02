using System.Net;
using System.Net.WebSockets;
using System.Text;
using Newtonsoft.Json;
using MessagePack;
using System.IO.Compression;

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
        _httpListener = new HttpListener();
        _httpListener.Prefixes.Add(_serverUri);
    }

    public void SetSimManager(SimManager simManager)
    {
        _simManager = simManager;
    }

    public async Task StartAsync()
    {
        try
        {
            _httpListener.Start();
            Console.WriteLine($"WebSocket server listening on {_serverUri}");

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

        using (var webSocket = webSocketContext.WebSocket)
        {
            await _clientLock.WaitAsync();
            try
            {
                _clients.Add(webSocket);
                Console.WriteLine($"Client connected. Total clients: {_clients.Count}");
            }
            finally
            {
                _ = _clientLock.Release();
            }

            try
            {
                var buffer = new byte[1024];
                // Loop while the socket is open
                while (webSocket.State == WebSocketState.Open)
                {
                    var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        // Client initiated close. Acknowledge and exit loop.
                        await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                        break;
                    }
                }
            }
            catch (WebSocketException ex) when (ex.WebSocketErrorCode == WebSocketError.ConnectionClosedPrematurely)
            {
                // Ignore this common exception when the client closes abruptly.
            }
            catch (Exception ex)
            {
                // Log unexpected errors
                Console.WriteLine($"Error during client communication: {ex.Message}");
            }
            finally
            {
                await _clientLock.WaitAsync();
                try
                {
                    if (_clients.Contains(webSocket))
                    {
                        _ = _clients.Remove(webSocket);
                    }
                    Console.WriteLine($"Client disconnected. Total clients: {_clients.Count}");
                }
                finally
                {
                    _ = _clientLock.Release();
                }
            }
        }
    }
    
    // Helper method to compress a byte array using Gzip
    private static byte[] CompressWithGzip(byte[] data)
    {
        using (var compressedStream = new MemoryStream())
        {
            // The GZipStream compresses the data written to it and writes the result to compressedStream
            using (var gzipStream = new GZipStream(compressedStream, CompressionMode.Compress, true))
            {
                gzipStream.Write(data, 0, data.Length);
            } // gzipStream is disposed here, ensuring all bytes are flushed

            return compressedStream.ToArray();
        }
    }

    // UPDATED: Now takes the final SimSaveState object ready for broadcast.
    public async Task BroadcastStateAsync(SimSaveState broadcastState)
    {
        var sparseJson = JsonConvert.SerializeObject(broadcastState, Formatting.None);
        var sparseBuffer = Encoding.UTF8.GetBytes(sparseJson);
        
        var mpBuffer = MessagePackSerializer.Serialize(broadcastState);

        var finalBuffer = CompressWithGzip(mpBuffer);

        
        // Final buffer to send is the smallest one (OPT 3)
        var bufferToSend = finalBuffer;

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
                            new ArraySegment<byte>(bufferToSend, 0, bufferToSend.Length),
                            WebSocketMessageType.Binary, // Send as the final binary message
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
            _ = _clientLock.Release();
        }
    }
}