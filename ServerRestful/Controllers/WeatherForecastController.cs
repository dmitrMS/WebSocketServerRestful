using Microsoft.AspNetCore.Mvc;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WebSocketRestApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class WebSocketController : ControllerBase
    {
        private static readonly ConcurrentDictionary<string, WebSocket> Clients = new();

        [HttpGet("clients")]
        public IActionResult GetClients()
        {
            return Ok(Clients.Keys);
        }

        [HttpPost("send")]
        public async Task<IActionResult> SendMessage([FromQuery] string clientId, [FromBody] string message)
        {
            if (Clients.TryGetValue(clientId, out var webSocket) && webSocket.State == WebSocketState.Open)
            {
                var buffer = Encoding.UTF8.GetBytes(message);
                await webSocket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);
                return Ok($"Сообщение '{message}' отправлено клиенту {clientId}");
            }

            return BadRequest($"Клиент с ID {clientId} не найден или соединение закрыто.");
        }

        public static void RegisterClient(string clientId, WebSocket webSocket)
        {
            Clients[clientId] = webSocket;
        }

        public static void RemoveClient(string clientId)
        {
            Clients.TryRemove(clientId, out _);
        }
    }
}
