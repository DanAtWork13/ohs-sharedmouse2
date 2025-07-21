using System.Text.Json;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace ohs_sharedmouse_ws
{
    internal class MsgHandler : WebSocketBehavior
    {
        // string = session id (this.ID, Sessions[]), int = logical room id
        private static Dictionary<string, int> rooms = new();
        private static Mutex mutRoom = new(); //must own mut to interact with rooms

        private static List<TextBoxCreate> textBoxes = []; // holds the text boxes for all rooms until deleted
        private static Mutex mutBoxes = new();

        private Thread boxBroadCaster;

        public MsgHandler()
        {
            boxBroadCaster = new(BoxBroadcaster);
            boxBroadCaster.Start();
        }

        internal void MouseMoveHandler(object? data)
        {
            string message = (string)data!;
            MouseMove mm = JsonSerializer.Deserialize<MouseMove>(message)!;
            int id = mm.id;
            mutRoom.WaitOne();
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
            mutRoom.ReleaseMutex();
        }

        internal void RoomMoveHandler(object? data)
        {
            string message = (string)data!;
            RoomUpdate ru = JsonSerializer.Deserialize<RoomUpdate>(message)!;
            int newID = ru.id;
            int oldID = ru.oldID;
            if (oldID == newID) { return; }
            mutRoom.WaitOne();
            rooms[ID] = newID;
            mutRoom.ReleaseMutex();
        }

        internal void BoxCreateHandler(object? data)
        {
            string message = (string)data!;
            TextBoxCreate tbc = JsonSerializer.Deserialize<TextBoxCreate>(message)!;
            mutBoxes.WaitOne();
            textBoxes.Append(tbc);
            mutBoxes.ReleaseMutex();
            int id = tbc.id;
            mutRoom.WaitOne();
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
            mutRoom.ReleaseMutex();
        }

        internal void BoxDeleteHandler(object? data)
        {
            string message = (string)data!;
            TextBoxDelete tbd = JsonSerializer.Deserialize<TextBoxDelete>(message)!;
            mutBoxes.WaitOne();
            textBoxes.Remove(textBoxes.Find(x =>
                            {
                                if (x.key == tbd.key)
                                {
                                    return true;
                                }
                                return false;
                            }));
            mutBoxes.ReleaseMutex();
            int id = tbd.id;
            mutRoom.WaitOne();
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
            mutRoom.ReleaseMutex();
        }

        internal void BoxBroadcaster(object? data)
        {
            // occasionally tells all connected sessions about any boxes that are in their session.
            while (true)
            {
                Thread.Sleep(550);
                if (textBoxes.Count == 0) { continue; }
                mutRoom.WaitOne();
                foreach (var k in rooms)
                {
                    int id = k.Value;
                    string[] boxesJSON = [];
                    mutBoxes.WaitOne();
                    List<TextBoxCreate> sessionBoxes = textBoxes.FindAll(x =>
                    {
                        if (x.key == id)
                        {
                            return true;
                        }
                        return false;
                    });
                    sessionBoxes[0].key = 5;
                    foreach (var tbc in sessionBoxes)
                    {
                        _ = boxesJSON.Append(JsonSerializer.Serialize<TextBoxCreate>(tbc));
                    }
                    mutBoxes.ReleaseMutex();

                    if (Sessions.TryGetSession(k.Key, out _))
                    {
                        foreach (var message in boxesJSON)
                        {
                            Sessions.SendTo(message, k.Key);
                        }
                    }
                    else
                    {
                        rooms.Remove(k.Key);
                        Sessions.Sweep();
                    }
                }
                mutRoom.ReleaseMutex();
            }
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
                    rooms.Add(ID, bm.id);
                    break;

                case 1:
                    // mouse movement. broadcast the message to the other users in the same room id
                    new Thread(MouseMoveHandler).Start(e.Data);
                    break;

                case 2:
                    new Thread(RoomMoveHandler).Start(e.Data);
                    break;

                case 3:
                    new Thread(BoxCreateHandler).Start(e.Data);
                    break;

                case 4:
                    new Thread(BoxDeleteHandler).Start(e.Data);
                    break;
            }
        }

        protected override void OnClose(CloseEventArgs e)
        {
            mutRoom.WaitOne();
            //Console.WriteLine("Removed session {0}", ID);
            rooms.Remove(ID);
            mutRoom.ReleaseMutex();
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
