export class BaseMessage //type = 0
{
    msgtype: number = 0; //message type, so we can deserialize to this, then know what to deserialize the rest to, later
    id: number = 0; // room id, because it's gonna be in every message anyways
}

export class RoomUpdate extends BaseMessage // type = 2
{
    oldID: number = 0;
}

export class MouseMove extends BaseMessage //type = 1
{
    name: string = "";
    color: string = "";
    x: number = 0;
    y: number = 0;
}

export class TextBoxCreate extends BaseMessage //type = 3 or 5 when broadcast
{
    key: number = 0;
    x: number = 0;
    y: number = 0;
    creator: string = "";
    color: string = "";
    content: string = "";
}

export class TextBoxDelete extends BaseMessage //type = 4
{
    key: number = 0;
}