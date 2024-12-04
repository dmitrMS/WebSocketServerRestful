using System;
using System.Collections.Generic;

namespace ServerRestful;

public partial class WebSocketClient
{
    public string? ConnectionState { get; set; }

    public string? LastMessage { get; set; }

    public DateOnly? ConnectedAt { get; set; }

    public DateOnly? DisconnectedAt { get; set; }

    public long ClientId { get; set; }
}
