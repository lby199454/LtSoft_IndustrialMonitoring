using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace LtSoft_IndustrialMonitoring.Services
{
    /// <summary>
    /// WebSocket π‹¿Ì∆˜
    /// </summary>
    public static class WebSocketManager
    {
        private static readonly List<WebSocket> _sockets = new List<WebSocket>();

        public static void Add(WebSocket socket)
        {
            lock (_sockets)
            {
                _sockets.Add(socket);
            }
        }

        public static void Remove(WebSocket socket)
        {
            lock (_sockets)
            {
                _sockets.Remove(socket);
            }
        }

        public static async Task BroadcastAsync(object data)
        {
            string json = JsonSerializer.Serialize(data);
            byte[] buffer = Encoding.UTF8.GetBytes(json);

            List<WebSocket> toRemove = new List<WebSocket>();
            lock (_sockets)
            {
                foreach (WebSocket socket in _sockets)
                {
                    if (socket.State == WebSocketState.Open)
                    {
                        socket.SendAsync(
                            new ArraySegment<byte>(buffer),
                            WebSocketMessageType.Text,
                            true,
                            CancellationToken.None
                        ).Wait();
                    }
                    else
                    {
                        toRemove.Add(socket);
                    }
                }
                foreach (WebSocket socket in toRemove)
                {
                    _sockets.Remove(socket);
                }
            }
        }
    }
}