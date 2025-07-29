using System.Text.Json;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace ohs_sharedmouse_ws
{
    internal class MsgHandler : WebSocketBehavior
    {
        // string = session id (this.ID, Sessions[]), int = logical room id
        private static Dictionary<string, int> rooms = new();
        private static int[] roomCounts = new int[10001]; // to support 0-10000 inclusive, like client does
        private static Mutex mutRoom = new(false); //must own mut to interact with rooms

        private static List<TextBoxCreate> textBoxes = new(); // holds the text boxes for all rooms until deleted
        private static Mutex mutBoxes = new(false);

        private void MouseMoveHandler(object? data)
        {
            string message = (string)data!;
            int mm = JsonSerializer.Deserialize<MouseMove>(message)!.id;
            mutRoom.WaitOne();
            foreach (var k in rooms)
            {
                if (k.Value == mm)
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

        private void RoomMoveHandler(object? data)
        {
            string message = (string)data!;
            RoomUpdate ru = JsonSerializer.Deserialize<RoomUpdate>(message)!;
            int newID = ru.id;
            if (ru.oldID == newID) { return; } // don't change anything if there isn't an actual change requested
            mutRoom.WaitOne();
            rooms[ID] = newID; //update the room ID
            mutRoom.ReleaseMutex();

            // sending saved text boxes section
            if (newID == 0) { return; } // can't have boxes on id 0 so don't bother running this code
            List<string> boxesJSON = [];
            mutBoxes.WaitOne();
            List<TextBoxCreate> roomBoxes = textBoxes.FindAll(x => { return x.id == newID; }); //gets the boxes in the current room
            if (roomBoxes.Count == 0) { mutBoxes.ReleaseMutex(); return; } // if there aren't any, stop
            roomBoxes[0].msgtype = 5; // first one is type 5 so the client knows to reset it's box array
            foreach (var tbc in roomBoxes)
            {
                boxesJSON.Add(JsonSerializer.Serialize<TextBoxCreate>(tbc));
            }
            mutBoxes.ReleaseMutex();

            // send the box creation messages to the client
            mutRoom.WaitOne();
            if (Sessions.TryGetSession(ID, out _))
            {
                foreach (var oMessage in boxesJSON)
                {
                    Sessions.SendTo(oMessage, ID);
                }
            }
            else // if the session has gone away between the start of the function and now, don't send and dispose of it
            {
                rooms.Remove(ID);
            }
            mutRoom.ReleaseMutex();
        }

        private void BoxCreateHandler(object? data)
        {
#if DEBUG
            Console.WriteLine("Got to box create handler");
#endif
            string message = (string)data!;
            TextBoxCreate tbc = JsonSerializer.Deserialize<TextBoxCreate>(message)!;
            int id = tbc.id;
            if (id == 0) { return; } //don't allow box creation on global room
            mutBoxes.WaitOne();
            textBoxes.Add(tbc);
            mutBoxes.ReleaseMutex();
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

        private void BoxDeleteHandler(object? data)
        {
#if DEBUG
            Console.WriteLine("Got to box delete handler");
#endif
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

        private void InitialMessageHandler(object? data)
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
                    //unused on recieve side because RoomMove uses it as special signal to the client(s)
                    break;
            }
        }

        protected override void OnClose(CloseEventArgs e)
        {
            bool clean = true;
            mutRoom.WaitOne();
#if DEBUG
            Console.WriteLine("Removed session {0}", ID);
#endif
            try {
                roomCounts[rooms[ID]]--;
                if (roomCounts[rooms[ID]] < 0) { roomCounts[rooms[ID]] = 0; }
                rooms.Remove(ID);
            } catch (KeyNotFoundException) //can happen when trying to access a session id that has already been cleaned by another function, I think.
            {
#if DEBUG
                Console.WriteLine("Key not found: {0}", ID);
#endif
                clean = false;
            }
            mutRoom.ReleaseMutex();
            if (clean)
            {
                new Thread(RoomCleaner).Start();
            }
        }

        //cleans the boxes out of rooms that are empty. only triggered when a client disconnects.
        private void RoomCleaner()
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
            wssv.KeepClean = true; // ensures old sessions are disconnected
            wssv.ReuseAddress = true; // I think this is necessary for it to be accessible on Azure
            wssv.Start();
            if (wssv.IsListening)
            {
                Console.WriteLine("Listening on {0}:{1}, and providing WebSocket services:", wssv.Address, wssv.Port);
            } else
            {
                Console.WriteLine("Error: server not listening!!");
            }
            
            Console.WriteLine("Kill this process to close server...");
            Thread.Sleep(Timeout.Infinite); // keeps the process running forever in the background, but in a way that doesn't use 100% CPU.

            // Stop the server.
            wssv.Stop();
        }
    }
}
