using Microsoft.AspNetCore.Mvc;
using System.Net.WebSockets;
using System.Text;

namespace WebSocketRestApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class WebSocketController : ControllerBase
    {
        private readonly WebSocketClientManager _clientManager;

        public WebSocketController(WebSocketClientManager clientManager)
        {
            _clientManager = clientManager;
        }

        /// �������� ������ ���� ��������.
        [HttpGet("clients")]
        public IActionResult GetClients()
        {
            return Ok(_clientManager.GetAllClientIds());
        }

        /// ��������� ��������� �������.
        [HttpPost("send")]
        public async Task<IActionResult> SendMessage([FromQuery] string clientId, [FromBody] string message)
        {
            if (_clientManager.TryGetClient(clientId, out var webSocket) && webSocket.State == WebSocketState.Open)
            {
                var buffer = Encoding.UTF8.GetBytes(message);
                await webSocket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);
                return Ok($"��������� '{message}' ���������� ������� {clientId}");
            }

            return NotFound($"������ � ID {clientId} �� ������ ��� ���������� �������.");
        }

        /// WebSocket ����������.
        [HttpGet("ws")]
        public async Task<IActionResult> WebSocketHandler()
        {
            if (HttpContext.WebSockets.IsWebSocketRequest)
            {
                var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
                string clientId = Guid.NewGuid().ToString();

                _clientManager.RegisterClient(clientId, webSocket);

                try
                {
                    await HandleWebSocketConnection(webSocket, clientId);
                }
                finally
                {
                    _clientManager.RemoveClient(clientId);
                }

                return new EmptyResult();
            }
            else
            {
                return BadRequest("��� endpoint ��� WebSocket-����������.");
            }
        }

        private static async Task HandleWebSocketConnection(WebSocket webSocket, string clientId)
        {
            var buffer = new byte[1024 * 4];
            var cancellationToken = CancellationToken.None;

            while (webSocket.State == WebSocketState.Open)
            {
                var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);

                if (result.MessageType == WebSocketMessageType.Text)
                {
                    var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    Console.WriteLine($"��������� �� ������� ({clientId}): {message}");
                }
                else if (result.MessageType == WebSocketMessageType.Close)
                {
                    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "���������� �������", cancellationToken);
                }
            }
        }
    }
}
