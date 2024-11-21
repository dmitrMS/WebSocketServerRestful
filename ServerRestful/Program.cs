using System.Net.WebSockets;
using System.Text;
using WebSocketRestApi.Controllers;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();
app.UseWebSockets();
app.UseRouting();

app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();
    endpoints.Map("/ws", async context =>
    {
        if (context.WebSockets.IsWebSocketRequest)
        {
            var webSocket = await context.WebSockets.AcceptWebSocketAsync();
            string clientId = Guid.NewGuid().ToString();

            // Register the client
            WebSocketController.RegisterClient(clientId, webSocket);

            try
            {
                await HandleWebSocketConnection(webSocket, clientId);
            }
            finally
            {
                // Remove client after closing the connection
                WebSocketController.RemoveClient(clientId);
            }
        }
        else
        {
            context.Response.StatusCode = 400;
            await context.Response.WriteAsync("Это endpoint для WebSocket-соединений.");
        }
    });
});

async Task HandleWebSocketConnection(WebSocket webSocket, string clientId)
{
    var buffer = new byte[1024 * 4];
    var cancellationToken = CancellationToken.None;

    while (webSocket.State == WebSocketState.Open)
    {
        var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);

        if (result.MessageType == WebSocketMessageType.Text)
        {
            var message = Encoding.UTF8.GetString(buffer, 0, result.Count);

            // Обработка других текстовых сообщений
            Console.WriteLine($"Сообщение от клиента ({clientId}): {message}");
        }
        else if (result.MessageType == WebSocketMessageType.Close)
        {
            await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Соединение закрыто", cancellationToken);
        }
    }
}

app.Run();
