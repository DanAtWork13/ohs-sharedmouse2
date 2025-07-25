using System.Text.Json;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace ohs_sharedmouse_ws
{
    internal class MsgHandler : WebSocketBehavior
    {
        // string = session id (this.ID, Sessions[]), int = logical room id
        private static Dictionary<string, int> rooms = new();
        private static int[] roomCounts = new int[10000];
        private static Mutex mutRoom = new(false); //must own mut to interact with rooms

        private static List<TextBoxCreate> textBoxes = new(); // holds the text boxes for all rooms until deleted
        private static Mutex mutBoxes = new(false);

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

            if (newID == 0) { return; }
            List<string> boxesJSON = [];
            mutBoxes.WaitOne();
            List<TextBoxCreate> sessionBoxes = textBoxes.FindAll(x => { return x.id == newID; });
            if (sessionBoxes.Count == 0) { mutBoxes.ReleaseMutex(); return; }
            sessionBoxes[0].msgtype = 5;
            foreach (var tbc in sessionBoxes)
            {
                boxesJSON.Add(JsonSerializer.Serialize<TextBoxCreate>(tbc));
            }
            mutBoxes.ReleaseMutex();

            mutRoom.WaitOne();
            if (Sessions.TryGetSession(ID, out _))
            {
                foreach (var oMessage in boxesJSON)
                {
                    Sessions.SendTo(oMessage, ID);
                }
            }
            else
            {
                rooms.Remove(ID);
            }
            mutRoom.ReleaseMutex();
        }

        internal void BoxCreateHandler(object? data)
        {
            //Console.WriteLine("Got to box create handler");
            string message = (string)data!;
            TextBoxCreate tbc = JsonSerializer.Deserialize<TextBoxCreate>(message)!;
            if (tbc.id == 0) { return; } //don't allow box creation on global room
            mutBoxes.WaitOne();
            textBoxes.Add(tbc);
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
                    }
                }
            }
            mutRoom.ReleaseMutex();
        }

        internal void BoxDeleteHandler(object? data)
        {
            //Console.WriteLine("Got to box delete handler");
            string message = (string)data!;
            TextBoxDelete tbd = JsonSerializer.Deserialize<TextBoxDelete>(message)!;
            mutBoxes.WaitOne();
            textBoxes.RemoveAll(x => { return x.key == tbd.key; });
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
                    }
                }
            }
            mutRoom.ReleaseMutex();
        }

        internal void InitialMessageHandler(object? data)
        {
            BaseMessage bm = (BaseMessage)data!;
            mutRoom.WaitOne();
            rooms.Add(ID, bm.id);
            roomCounts[bm.id]++;
            mutRoom.ReleaseMutex();
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
                    new Thread(InitialMessageHandler).Start(bm);
                    break;

                case 1:
                    // mouse movement. broadcast the message to the other users in the same room id
                    new Thread(MouseMoveHandler).Start(e.Data);
                    break;

                case 2:
                    // room change, ensures room ids change correctly
                    new Thread(RoomMoveHandler).Start(e.Data);
                    break;

                case 3:
                    // text box creation
                    new Thread(BoxCreateHandler).Start(e.Data);
                    break;

                case 4:
                    // text box deletion
                    new Thread(BoxDeleteHandler).Start(e.Data);
                    break;

                case 5:
                    //unused on recieve side because broadcaster uses it as special signal
                    break;
            }
        }

        protected override void OnClose(CloseEventArgs e)
        {
            bool clean = true;
            mutRoom.WaitOne();
            //Console.WriteLine("Removed session {0}", ID);
            try {
                roomCounts[rooms[ID]]--;
                if (roomCounts[rooms[ID]] < 0) { roomCounts[rooms[ID]] = 0; }
                rooms.Remove(ID);
            } catch (KeyNotFoundException)
            {
                //Console.WriteLine("Key not found: {0}", ID);
                clean = false;
            }
            mutRoom.ReleaseMutex();
            if (clean)
            {
                new Thread(RoomCleaner).Start();
            }
        }

        internal void RoomCleaner()
        {
            Thread.Sleep(Random.Shared.Next(50, 550));
            mutRoom.WaitOne();
            for (int i = 0; i < roomCounts.Length; i++)
            {
                if (roomCounts[i] == 0)
                {
                    mutBoxes.WaitOne();
                    textBoxes.RemoveAll(x => { return x.id == i; });
                    mutBoxes.ReleaseMutex();
                }
            }
            mutRoom.ReleaseMutex();
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
            } else
            {
                Console.WriteLine("Error: server not listening!!");
            }
            
            Console.WriteLine("Kill this process to close server...");
            Thread.Sleep(Timeout.Infinite);

            // Stop the server.
            wssv.Stop();
        }
    }
}
