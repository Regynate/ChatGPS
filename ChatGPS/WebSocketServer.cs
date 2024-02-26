using Fleck;
using GTA.Math;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Timers;
using System.Windows.Forms;

namespace ChatGPS
{
    internal class WebSocketServer
    {
        private readonly List<IWebSocketConnection> connections = new List<IWebSocketConnection>();

        public event EventHandler<EventArgs> ConnectionOpen;

        private readonly Fleck.WebSocketServer wss;

        private WebSocketServer()
        {
            try
            {
                wss = new Fleck.WebSocketServer($"ws://0.0.0.0:9873");
                // Set the websocket listeners
                wss.Start(connection =>
                {
                    connection.OnOpen += () => OnWsConnectionOpen(connection);
                    connection.OnClose += () => OnWSConnectionClose(connection);
                });
            }
            catch (Exception)
            {

            }
        }

        ~WebSocketServer()
        {
            wss.Dispose();
        }

        private void Broadcast(string message)
        {
            connections.ForEach(connection =>
            {
                // If the connection is not available for some reason, we just close it
                if (!connection.IsAvailable)
                {
                    connection.Close();
                }
                else
                {
                    connection.Send(message);
                }
            });
        }

        private void OnWSConnectionClose(IWebSocketConnection connection)
        {
            try
            {
                connections.Remove(connection);
            }
            catch (Exception)
            {

            }
        }

        private void OnWsConnectionOpen(IWebSocketConnection connection)
        {
            try
            {
                connections.Add(connection);
                ConnectionOpen?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception)
            {

            }
        }

        private void Broadcast(object message)
        {
            Broadcast(JsonConvert.SerializeObject(message));
        }

        private long GetTimeMs()
        {
            return (long) DateTime.Now.ToUniversalTime().Subtract(
                new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                ).TotalMilliseconds;
        }

        public void SendPlayerLocation(Vector2 location, float direction, bool small)
        {
            Broadcast(new { type = "playerLocation", x = location.X, y = location.Y, direction, small, timestamp = GetTimeMs() });
        }

        public void SendDestinationLocation(Vector2 position)
        {
            Broadcast(new { type = "destinationLocation", x = position.X, y = position.Y, timestamp = GetTimeMs() });
        }

        public void ClearDestination()
        {
            Broadcast(new { type = "clearDestination", timestamp = GetTimeMs() });
        }

        public void SendPicture(string filepath)
        {
            Broadcast(new { type = "picture", filepath, timestamp = GetTimeMs() });
        }

        public void ClearPicture()
        {
            SendPicture("");
        }

        public bool HasMessage { get; private set; }

        public void SendMessage(string message, int time)
        {
            Broadcast(new { type = "message", message, time, timestamp = GetTimeMs() });
            if (time > 0)
            {
                HasMessage = true;
                System.Timers.Timer timer = new System.Timers.Timer(time);
                timer.AutoReset = false;
                timer.Elapsed += (_, args) => HasMessage = false;
                timer.Start();
            }
        }

        public void SendCountdown(int countdown)
        {
            Broadcast(new { type = "message", message = $" {countdown} ", time = 1500, timestamp = GetTimeMs() });
        }

        public void SendMapInfo(string filepath, PointF mapPoint0, PointF picPoint0, PointF mapPoint1, PointF picPoint1)
        {
            Broadcast(new
            {
                type = "mapInfo",
                filepath,
                mapx0 = mapPoint0.X,
                mapy0 = mapPoint0.Y,
                picx0 = picPoint0.X,
                picy0 = picPoint0.Y,
                mapx1 = mapPoint1.X,
                mapy1 = mapPoint1.Y,
                picx1 = picPoint1.X,
                picy1 = picPoint1.Y,
            });
        }

        public void SendMapInfo(MapInfo mapInfo)
        {
            SendMapInfo(mapInfo.Filepath, mapInfo.MapPoint0, mapInfo.PicPoint0, mapInfo.MapPoint1, mapInfo.PicPoint1);
        }

        private static readonly WebSocketServer instance = new WebSocketServer();
        public static WebSocketServer Instance => instance;
    }
}
