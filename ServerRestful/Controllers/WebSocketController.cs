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

        // Метод для подключения клиента
        [HttpGet("connect")]
        public async Task ConnectClient()
        {
            if (HttpContext.Request.Headers["Upgrade"] == "websocket")
            {
                var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
                var clientId = Guid.NewGuid().ToString(); // Генерация уникального идентификатора для клиента

                // Регистрация клиента
                RegisterClient(clientId, webSocket);

                // Логирование успешного подключения в базу данных
                var client = new WebSocketClient
                {
                    ClientId = long.Parse(clientId),  // Преобразуем строковый идентификатор в long для БД
                    ConnectionState = "Connected",
                    ConnectedAt = DateOnly.FromDateTime(DateTime.Now) // Записываем время подключения
                };

                // Сохраняем клиента в базу данных
                _dbContext.WebSocketClients.Add(client);
                await _dbContext.SaveChangesAsync();

                // Ожидаем получения сообщений от клиента
                await HandleWebSocketAsync(webSocket, clientId);
            }
            else
            {
                HttpContext.Response.StatusCode = 400;
            }
        }

        // Метод для обработки WebSocket сообщений от клиента
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

                    // Логируем полученное сообщение в базу данных
                    var client = await _dbContext.WebSocketClients
                        .FirstOrDefaultAsync(c => c.ClientId.ToString() == clientId);
                    if (client != null)
                    {
                        client.LastMessage = message;
                        client.DisconnectedAt = DateOnly.FromDateTime(DateTime.Now); // Записываем время последнего сообщения
                        await _dbContext.SaveChangesAsync();
                    }
                }
            }
        }

        // Метод для регистрации клиента
        public static void RegisterClient(string clientId, WebSocket webSocket)
        {
            Clients[clientId] = webSocket;
        }

        // Метод для удаления клиента
        public static void RemoveClient(string clientId)
        {
            Clients.TryRemove(clientId, out _);
        }

        // Метод для получения списка всех клиентов
        [HttpGet("clients")]
        public IActionResult GetClients()
        {
            // Получаем всех клиентов из базы данных
            var clients = _dbContext.WebSocketClients.ToList();

            if (clients.Count == 0)
            {
                Console.WriteLine("Нет клиентов в базе данных.");
            }

            return Ok(clients);
        }

        // Метод для отправки сообщений клиенту
        [HttpPost("send")]
        public async Task<IActionResult> SendMessage([FromQuery] string clientId, [FromBody] string message)
        {
            if (Clients.TryGetValue(clientId, out var webSocket) && webSocket.State == WebSocketState.Open)
            {
                var buffer = Encoding.UTF8.GetBytes(message);
                await webSocket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);

                // Логируем сообщение в базе данных
                var client = await _dbContext.WebSocketClients
                    .FirstOrDefaultAsync(c => c.ClientId.ToString() == clientId);
                if (client != null)
                {
                    client.LastMessage = message;
                    client.DisconnectedAt = DateOnly.FromDateTime(DateTime.Now);  // Записываем время сообщения
                    await _dbContext.SaveChangesAsync();
                }

                return Ok($"Сообщение '{message}' отправлено клиенту {clientId}");
            }

            return BadRequest($"Клиент с ID {clientId} не найден или соединение закрыто.");
        }
    }
}
