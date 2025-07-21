using System.Text.Json;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace ohs_sharedmouse_ws
{
    internal class MsgHandler : WebSocketBehavior
    {
        // string = session id (this.ID, Sessions[]), int = logical room id
        private static Dictionary<string, int> rooms = new();
        private static Mutex mut = new(); //must own mut to interact with rooms

        internal void MouseMoveHandler(object? data)
        {
            string message = (string)data!;
            MouseMove mm = JsonSerializer.Deserialize<MouseMove>(message)!;
            int id = Int32.Parse(mm.id);
            mut.WaitOne();
            foreach (var k in rooms)
            {
                if (k.Value == id)
                {
                    if (Sessions.TryGetSession(k.Key, out _))
                    {
                        Sessions.SendTo(message, k.Key);
                    }
                    else
                    {
                        rooms.Remove(k.Key);
                        Sessions.Sweep();
                    }
                }
            }
            mut.ReleaseMutex();
        }

        internal void RoomMoveHandler(object? data)
        {
            string message = (string)data!;
            RoomUpdate ru = JsonSerializer.Deserialize<RoomUpdate>(message)!;
            int newID = Int32.Parse(ru.id);
            int oldID = Int32.Parse(ru.oldID);
            if (oldID == newID) { return; }
            mut.WaitOne();
            rooms[ID] = newID;
            mut.ReleaseMutex();
        }


        protected override void OnMessage(MessageEventArgs e)
        {
            //Console.WriteLine("recieved: {0}", e.Data);
            //deserialize the inital part of the message to determine the type
            BaseMessage bm = JsonSerializer.Deserialize<BaseMessage>(e.Data)!;
            switch (bm.msgtype)
            {
                case 0:
                    //new user, put their session info into the id field's room
                    rooms.Add(ID, Int32.Parse(bm.id));
                    break;

                case 1:
                    // mouse movement. broadcast the message to the other users in the same room id
                    new Thread(MouseMoveHandler).Start(e.Data);
                    break;

                case 2:
                    new Thread(RoomMoveHandler).Start(e.Data);
                    break;
            }
        }

        protected override void OnClose(CloseEventArgs e)
        {
            mut.WaitOne();
            //Console.WriteLine("Removed session {0}", ID);
            rooms.Remove(ID);
            mut.ReleaseMutex();
            base.OnClose(e);
        }
    }

    internal class Program
    {
        static void Main(string[] args)
        {
            WebSocketServer wssv = new(58324);
            //wssv.Log.Level = LogLevel.Trace;
            wssv.AddWebSocketService<MsgHandler>("/");
            wssv.KeepClean = true;
            wssv.ReuseAddress = true;
            wssv.Start();
            if (wssv.IsListening)
            {
                Console.WriteLine("Listening on {0}:{1}, and providing WebSocket services:", wssv.Address, wssv.Port);

                foreach (string path in wssv.WebSocketServices.Paths)
                {
                    Console.WriteLine("- {0}", path);
                }
            }

            Console.WriteLine("Kill this process to close server...");

            //Console.ReadLine();
            while (true) { Thread.Sleep(Timeout.Infinite); }

            // Stop the server. (Unreachable)
            //wssv.Stop();
        }
    }
}
