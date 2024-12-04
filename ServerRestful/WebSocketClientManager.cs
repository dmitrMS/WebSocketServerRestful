using System.Collections.Concurrent;
using System.Net.WebSockets;

public class WebSocketClientManager
{
    private readonly ConcurrentDictionary<string, WebSocket> _clients = new();

    public IEnumerable<string> GetAllClientIds() => _clients.Keys;

    public bool TryGetClient(string clientId, out WebSocket? webSocket)
    {
        return _clients.TryGetValue(clientId, out webSocket);
    }

    public void RegisterClient(string clientId, WebSocket webSocket)
    {
        _clients[clientId] = webSocket;
    }

    public void RemoveClient(string clientId)
    {
        _clients.TryRemove(clientId, out _);
    }
}