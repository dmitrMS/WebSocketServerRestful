using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WebSocketClient
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;

            while (true)
            {
                using (var client = new ClientWebSocket())
                {
                    Uri serverUri = new Uri("ws://localhost:5207/ws");

                    try
                    {
                        await client.ConnectAsync(serverUri, cancellationToken);
                        Console.WriteLine("Подключено к серверу!");

                        var receiveTask = ReceiveMessages(client, cancellationToken);

                        // Ожидание завершения задачи получения сообщений
                        await receiveTask;

                        // Если соединение разорвано
                        if (client.State != WebSocketState.Open)
                        {
                            Console.WriteLine("Соединение разорвано. Попытка переподключения...");
                            await Task.Delay(5000);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Ошибка соединения: {ex.Message}. Повторная попытка через 5 секунд...");
                        await Task.Delay(5000);
                    }
                }
            }
        }

        private static async Task ReceiveMessages(ClientWebSocket client, CancellationToken cancellationToken)
        {
            var buffer = new byte[1024];

            while (client.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var result = await client.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        Console.WriteLine("Сервер закрыл соединение.");
                        await client.CloseAsync(WebSocketCloseStatus.NormalClosure, "Сервер закрыл соединение", cancellationToken);
                    }
                    else
                    {
                        string message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        Console.WriteLine($"Получено сообщение от сервера: {message}");
                    }
                }
                catch (WebSocketException ex)
                {
                    Console.WriteLine($"Ошибка получения данных: {ex.Message}");
                    break;
                }
            }
        }
    }
}
