////using System.Net.WebSockets;
////using System.Text;
////using WebSocketRestApi.Controllers;

////var builder = WebApplication.CreateBuilder(args);

////builder.Services.AddControllers();
////builder.Services.AddSingleton<WebSocketClientManager>(); // Регистрируем как Singleton
////builder.Services.AddSwaggerGen();

////var app = builder.Build();

////app.UseSwagger();
////app.UseSwaggerUI();

////app.UseWebSockets();
////app.UseRouting();

////app.UseEndpoints(endpoints =>
////{
////    endpoints.MapControllers();
////});

////app.Run();
//using System.Net.WebSockets;
//using System.Text;
//using Microsoft.EntityFrameworkCore;
//using ServerRestful;
////using WebSocketRestApi.Data;
//using WebSocketRestApi.Controllers;
////using WebSocketRestApi.Data;

//var builder = WebApplication.CreateBuilder(args);

//// Добавляем контроллеры
//builder.Services.AddControllers();

//// Настраиваем DbContext для PostgreSQL
//builder.Services.AddDbContext<WebSocketDbContext>(options =>
//    options.UseNpgsql(builder.Configuration.GetConnectionString("PostgresConnection")));

//// Добавляем сервис управления клиентами
//builder.Services.AddSingleton<WebSocketClientManager>();

//var app = builder.Build();

//app.UseWebSockets();
//app.UseRouting();
//app.UseEndpoints(endpoints =>
//{
//    endpoints.MapControllers();
//});

//app.Run();
using Microsoft.EntityFrameworkCore;
using ServerRestful;
using System.Net.WebSockets;
using System.Text;
using WebSocketRestApi.Controllers;

var builder = WebApplication.CreateBuilder(args);

// Добавляем конфигурацию и строку подключения
builder.Services.AddDbContext<WebSocketDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("WebSocketDbConnection")));

// Добавляем контроллеры и API
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Добавляем Swagger
app.UseSwagger();
app.UseSwaggerUI();

// Включаем маршрутизацию и обработку WebSocket
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

            // Регистрация клиента
            WebSocketController.RegisterClient(clientId, webSocket);

            try
            {
                await HandleWebSocketConnection(webSocket, clientId);
            }
            finally
            {
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
            Console.WriteLine($"Сообщение от клиента ({clientId}): {message}");
        }
        else if (result.MessageType == WebSocketMessageType.Close)
        {
            await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Соединение закрыто", cancellationToken);
        }
    }
}

app.Run();


