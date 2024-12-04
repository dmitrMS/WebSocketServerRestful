using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServerRestful;
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
        private readonly WebSocketDbContext _dbContext;

        public WebSocketController(WebSocketDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        private static readonly ConcurrentDictionary<string, WebSocket> Clients = new();

        // ����� ��� ����������� �������
        [HttpGet("connect")]
        public async Task ConnectClient()
        {
            if (HttpContext.Request.Headers["Upgrade"] == "websocket")
            {
                var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
                var clientId = Guid.NewGuid().ToString(); // ��������� ����������� �������������� ��� �������

                // ����������� �������
                RegisterClient(clientId, webSocket);

                // ����������� ��������� ����������� � ���� ������
                var client = new WebSocketClient
                {
                    ClientId = long.Parse(clientId),  // ����������� ��������� ������������� � long ��� ��
                    ConnectionState = "Connected",
                    ConnectedAt = DateOnly.FromDateTime(DateTime.Now) // ���������� ����� �����������
                };

                // ��������� ������� � ���� ������
                _dbContext.WebSocketClients.Add(client);
                await _dbContext.SaveChangesAsync();

                // ������� ��������� ��������� �� �������
                await HandleWebSocketAsync(webSocket, clientId);
            }
            else
            {
                HttpContext.Response.StatusCode = 400;
            }
        }

        // ����� ��� ��������� WebSocket ��������� �� �������
        private async Task HandleWebSocketAsync(WebSocket webSocket, string clientId)
        {
            var buffer = new byte[1024 * 4];

            while (webSocket.State == WebSocketState.Open)
            {
                var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                    RemoveClient(clientId);
                }
                else
                {
                    var message = Encoding.UTF8.GetString(buffer, 0, result.Count);

                    // �������� ���������� ��������� � ���� ������
                    var client = await _dbContext.WebSocketClients
                        .FirstOrDefaultAsync(c => c.ClientId.ToString() == clientId);
                    if (client != null)
                    {
                        client.LastMessage = message;
                        client.DisconnectedAt = DateOnly.FromDateTime(DateTime.Now); // ���������� ����� ���������� ���������
                        await _dbContext.SaveChangesAsync();
                    }
                }
            }
        }

        // ����� ��� ����������� �������
        public static void RegisterClient(string clientId, WebSocket webSocket)
        {
            Clients[clientId] = webSocket;
        }

        // ����� ��� �������� �������
        public static void RemoveClient(string clientId)
        {
            Clients.TryRemove(clientId, out _);
        }

        // ����� ��� ��������� ������ ���� ��������
        [HttpGet("clients")]
        public IActionResult GetClients()
        {
            // �������� ���� �������� �� ���� ������
            var clients = _dbContext.WebSocketClients.ToList();

            if (clients.Count == 0)
            {
                Console.WriteLine("��� �������� � ���� ������.");
            }

            return Ok(clients);
        }

        // ����� ��� �������� ��������� �������
        [HttpPost("send")]
        public async Task<IActionResult> SendMessage([FromQuery] string clientId, [FromBody] string message)
        {
            if (Clients.TryGetValue(clientId, out var webSocket) && webSocket.State == WebSocketState.Open)
            {
                var buffer = Encoding.UTF8.GetBytes(message);
                await webSocket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);

                // �������� ��������� � ���� ������
                var client = await _dbContext.WebSocketClients
                    .FirstOrDefaultAsync(c => c.ClientId.ToString() == clientId);
                if (client != null)
                {
                    client.LastMessage = message;
                    client.DisconnectedAt = DateOnly.FromDateTime(DateTime.Now);  // ���������� ����� ���������
                    await _dbContext.SaveChangesAsync();
                }

                return Ok($"��������� '{message}' ���������� ������� {clientId}");
            }

            return BadRequest($"������ � ID {clientId} �� ������ ��� ���������� �������.");
        }
    }
}
