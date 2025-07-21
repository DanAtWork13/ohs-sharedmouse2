namespace ohs_sharedmouse_ws
{
    public class BaseMessage
    {
        public int msgtype { get; set; } //message type, so we can deserialize to this, then know what to deserialize the rest to, later
        public required string id { get; set; } // room id, because it's gonna be in every message anyways
    }

    public class RoomUpdate : BaseMessage
    {
        public required string oldID { get; set; }
    }

    public class MouseMove : BaseMessage
    {
        public required string name { get; set; }
        public required string color { get; set; }
        public float x { get; set; }
        public float y { get; set; }
    }
}
