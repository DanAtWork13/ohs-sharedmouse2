using WebSocketSharp;
using WebSocketSharp.Server;

namespace ohs_sharedmouse_ws
{
    internal struct Message
    {
        string name;
        int x;
        int y;
    }

    internal class MsgHandler : WebSocketBehavior
    {
        protected override void OnMessage(MessageEventArgs e)
        {
            Console.WriteLine("recieved: {0}", e.Data);
            Sessions.BroadcastAsync(e.Data,null);
        }
    }

    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello, World!");
            WebSocketServer wssv = new(58324);
            //wssv.Log.Level = LogLevel.Trace;
            wssv.AddWebSocketService<MsgHandler>("/");
            wssv.Start();
            if (wssv.IsListening)
            {
                var fmt = "Listening on port {0}, and providing WebSocket services:";

                Console.WriteLine(fmt, wssv.Port);

                foreach (var path in wssv.WebSocketServices.Paths)
                    Console.WriteLine("- {0}", path);
            }

            Console.WriteLine("\nPress Enter key to stop the server...");

            Console.ReadLine();

            // Stop the server.
            wssv.Stop();
        }
    }
}
