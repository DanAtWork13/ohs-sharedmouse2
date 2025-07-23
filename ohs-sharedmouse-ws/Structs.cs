namespace ohs_sharedmouse_ws
{
    public class BaseMessage //type = 0
    {
        public int msgtype { get; set; } //message type, so we can deserialize to this, then know what to deserialize the rest to, later
        public int id { get; set; } // room id, because it's gonna be in every message anyways
    }

    public class RoomUpdate : BaseMessage // type = 2
    {
        public int oldID { get; set; }
    }

    public class MouseMove : BaseMessage //type = 1
    {
        public required string name { get; set; }
        public required string color { get; set; }
        public float x { get; set; }
        public float y { get; set; }
    }

    public class TextBoxCreate : BaseMessage //type = 3 or 5 when broadcast
    {
        public int key { get; set; }
        public float x { get; set; }
        public float y { get; set; }
        public required string creator { get; set; }
        public required string color { get; set; }
        public required string content { get; set; }
    }

    public class TextBoxDelete : BaseMessage //type = 4
    {
        public int key { get; set; }
    }
}
