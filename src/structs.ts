export class BaseMessage {
	//type = 0
	msgtype: number = 0; //message type, so we can deserialize to this, then know what to deserialize the rest to, later
	id: number = 0; // room id, because it's gonna be in every message anyways

	constructor(msgtype: number, id: number) {
		this.msgtype = msgtype;
		this.id = id;
	}
}

export class RoomUpdate extends BaseMessage {
	// type = 2
	oldID: number = 0;

	constructor(id: number, oldID: number) {
		super(2, id);
		this.oldID = oldID;
	}
}

export class MouseMove extends BaseMessage {
	//type = 1
	name: string = "";
	color: string = "";
	x: number = 0;
	y: number = 0;

	constructor(id: number, name: string, color: string, x: number, y: number) {
		super(1, id);
		this.name = name;
		this.color = color;
		this.x = x;
		this.y = y;
	}
}

export class TextBoxCreate extends BaseMessage {
	//type = 3 or 5 when broadcast (recieve from server only)
	key: number = 0;
	x: number = 0;
	y: number = 0;
	creator: string = "";
	color: string = "";
	content: string = "";

	constructor(id: number, key: number, x: number, y: number, creator: string, color: string, content: string) {
		super(3, id);
		this.key = key;
		this.x = x;
		this.y = y;
		this.creator = creator;
		this.color = color;
		this.content = content;
	}
}

export class TextBoxDelete extends BaseMessage {
	//type = 4
	key: number = 0;

	constructor(id: number, key: number) {
		super(4, id);
		this.key = key;
	}
}
