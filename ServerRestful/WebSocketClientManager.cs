using Microsoft.EntityFrameworkCore;
using ServerRestful;
using System;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Threading.Tasks;

namespace WebSocketRestApi
{
    public class ClientManager
    {
        private readonly WebSocketDbContext _dbContext;
        private static readonly ConcurrentDictionary<string, WebSocket> Clients = new();

        public ClientManager(WebSocketDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task AddClientAsync(string clientId, WebSocket webSocket)
        {
            Clients[clientId] = webSocket;

            var client = new WebSocketClient
            {
                ClientId = Convert.ToInt64(clientId), // Преобразуем строку в long
                ConnectionState = "Connected",
                //ConnectedAt = DateOnly.FromDateTime(DateTime.Now).ToDateTime(TimeOnly.MinValue) // Преобразуем DateOnly в DateTime
                ConnectedAt = DateOnly.FromDateTime(DateTime.Now)
        };

            _dbContext.WebSocketClients.Add(client);
            await _dbContext.SaveChangesAsync();
        }

        public async Task RemoveClientAsync(string clientId)
        {
            Clients.TryRemove(clientId, out _);

            var clientIdLong = Convert.ToInt64(clientId); // Преобразуем строку в long

            var client = await _dbContext.WebSocketClients
                .FirstOrDefaultAsync(c => c.ClientId == clientIdLong); // Сравниваем как long
            if (client != null)
            {
                // Преобразуем DateTime в DateOnly для поля DisconnectedAt
                client.DisconnectedAt = DateOnly.FromDateTime(DateTime.Now);

                client.ConnectionState = "Disconnected";
                await _dbContext.SaveChangesAsync();
            }
        }


        public ConcurrentDictionary<string, WebSocket> GetClients()
        {
            return Clients;
        }
    }
}
