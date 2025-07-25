import { useState, type JSX } from "react";
import { zodResolver } from "@hookform/resolvers/zod";
import { useForm } from "react-hook-form";
import { z } from "zod";
import { Button } from "@/components/ui/button";
import { Form, FormControl, FormField, FormItem, FormLabel, FormMessage } from "@/components/ui/form";
import { Input } from "@/components/ui/input";
import { DropdownMenu, DropdownMenuContent, DropdownMenuRadioGroup, DropdownMenuRadioItem, DropdownMenuTrigger } from "@/components/ui/dropdown-menu";
import { ContextMenu, ContextMenuContent, ContextMenuItem, ContextMenuTrigger } from "@/components/ui/context-menu";
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle, DialogTrigger } from "@/components/ui/dialog";
import { MouseMove, BaseMessage, TextBoxCreate, TextBoxDelete, RoomUpdate } from "./structs.ts";
//import "./App.css";

function TextBox({ id, x, y, name, color, content, ukey }: { id: number; x: number; y: number; name: string; color: string; content: string; ukey: number }) {
	function deleteTextBox() {
		//console.log("button deleted ukey: %s", ukey);
		webSocket.send(JSON.stringify(new TextBoxDelete(id, ukey)));
	}

	return (
		<>
			<div style={{ position: "relative", top: y, left: x }}>
				<svg width="220" height="220" viewBox="0 0 220 220" xmlns="http://www.w3.org/2000/svg">
					<rect width="220" height="220" fill="#1E1E1E" />
					<rect x="0.5" y="0.5" width="219" height="219" fill="#949494" stroke={color} />
					<rect y="20" width="219" height="1" fill="white" />
					<text y="15" x="2" textRendering="optimizeLegibility" fill="white">
						{name}
					</text>
					<text y="40" x="2" textRendering="optimizeLegibility" fill="white">
						{content}
					</text>
				</svg>
				<Button style={{ position: "relative", top: -222, left: 195, width: 20, height: 20 }} size="sm" onClick={deleteTextBox}>
					x
				</Button>
			</div>
		</>
	);
}

function MouseFollower({ x, y, name, color }: { x: number; y: number; name: string; color: string }) {
	const nameClassString = "text-xs text-" + color.toLowerCase() + "-600";
	return (
		<>
			<div style={{ position: "relative", top: y, left: x }}>
				<svg fill={color} height="16" width="16" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 391.751 391.751" transform="matrix(-1, 0, 0, 1, 0, 0)">
					<path d="M389.2,2.803c-2.4-2.4-5.6-2.8-8.8-1.6L4.8,154.403c-2.8,1.6-4.8,4.4-4.8,8c0,3.6,2.4,6.4,5.6,7.2l125.2,40.4l-94.8,94.8 c-4.8,4.8-7.2,10.8-7.2,17.2s2.4,12.4,7.2,17.2l16.8,16.8c4.8,4.8,10.8,7.2,17.2,7.2c6.4,0,12.4-2.4,17.2-7.2l94.4-94.4l40,124 c1.2,3.2,4,5.6,7.2,5.6h0.4c3.2,0,6-2,7.2-4.8l154.8-374.4C392.4,8.403,391.6,5.203,389.2,2.803z M229.6,359.603l-37.2-115.6 c-0.8-2.8-3.2-4.8-5.6-5.2c-0.8,0-1.2-0.4-2-0.4c-2,0-4,0.8-5.6,2.4l-103.6,103.6c-3.2,3.2-8.4,3.2-11.6,0l-16.8-16.8 c-1.6-1.6-2.4-3.6-2.4-5.6s0.8-4,2.4-5.6l104-104c2-2,2.8-4.8,2-7.6c-0.8-2.8-2.8-4.8-5.2-5.6l-116.8-37.6l337.6-138 L229.6,359.603z" />
				</svg>
				<p className={nameClassString}>{name}</p>
			</div>
		</>
	);
}

let prevX = 0;
let prevY = 0;
//const webSocket = new WebSocket("ws://thelettuceclub.myddns.me:58324/");
const webSocket = new WebSocket("ws://192.168.2.116:58324/"); //for local testing

webSocket.onopen = function () {
	this.send(JSON.stringify(new BaseMessage(0, 0)));
};

webSocket.onerror = function () {
	console.log("WS error");
	this.close();
};

let peers: MouseMove[] = [];
let boxes: TextBoxCreate[] = [];

let lastRCX = 0;
let lastRCY = 0;

function MouseCaptureZone({ name, color, id }: { name: string; color: string; id: number }) {
	const [x, setX] = useState(0);
	const [y, setY] = useState(0);
	const [recMsgCnt, setRecMsgCnt] = useState(0);
	const [open, setOpen] = useState(false);

	//pseudo function to send current x/y whenever it changes
	if (prevX != x || prevY != y) {
		//console.log("sending new coords");
		webSocket.send(JSON.stringify(new MouseMove(id, name, color, x, y)));
		prevX = x;
		prevY = y;
	}

	function MouseMoveHandler(e: string) {
		const data = JSON.parse(e) as MouseMove;
		const idx = peers.findIndex((elem) => elem.name === data.name);
		if (data.id === id) {
			if (idx != -1) {
				peers[idx] = data;
			} else {
				peers.push(data);
			}
			setRecMsgCnt(recMsgCnt + 1);
		}
	}

	function TextBoxCreateHandler(e: string) {
		console.log("got to text box create handler");
		const data = JSON.parse(e) as TextBoxCreate;
		boxes.push(data);
		setRecMsgCnt(recMsgCnt + 1);
	}

	function TextBoxDeleteHandler(e: string) {
		console.log("got to text box delete handler");
		const data = JSON.parse(e) as TextBoxDelete;
		console.log(boxes);
		boxes.splice(
			boxes.findIndex((value) => {
				return value.key == data.key;
			}),
			1,
		);
		console.log(boxes);
		setRecMsgCnt(recMsgCnt + 1);
	}

	webSocket.onmessage = function (e) {
		//console.log("WS recieved: " + e.data);
		const data = JSON.parse(e.data as string) as BaseMessage;
		switch (data.msgtype) {
			case 1:
				setTimeout(() => {
					MouseMoveHandler(e.data as string);
				}, 1);
				break;

			case 3:
				setTimeout(() => {
					TextBoxCreateHandler(e.data as string);
				}, 1);
				break;

			case 4:
				setTimeout(() => {
					TextBoxDeleteHandler(e.data as string);
				}, 1);
				break;

			case 5: //the first message of a box broadcast clears the current boxes
				setTimeout(() => {
					boxes = [];
					TextBoxCreateHandler(e.data as string);
				}, 1);
				break;
		}
	};

	function handlePointMove(ev: React.PointerEvent<HTMLDivElement>) {
		//console.log("clientX: %d, clientY: %d", ev.clientX, ev.clientY);
		setX(ev.clientX);
		setY(ev.clientY);
	}

	function peerToMouse() {
		const rows: JSX.Element[] = [];
		peers.forEach((peer) => {
			if (peer.name !== name) {
				rows.push(<MouseFollower x={peer.x} y={peer.y - 40} name={peer.name} color={peer.color} />);
			}
		});
		return rows;
	}

	function spawnTextBoxDialog(event: React.MouseEvent<HTMLDivElement, MouseEvent>) {
		console.log("right-clicked at x: %d, y: %d", event.clientX, event.clientY);
		lastRCX = event.clientX;
		lastRCY = event.clientY;
	}

	function spawnTextBox() {
		const text = document.getElementById("textField")!.value; //ignore this error
		if (text !== null) {
			console.log("sending new box. text: %s", text);
			webSocket.send(JSON.stringify(new TextBoxCreate(id, getRandomInt(15386), lastRCX, lastRCY, name, color, text)));
		}
		setOpen(false);
	}

	function textToBox() {
		const rows: JSX.Element[] = [];
		boxes.forEach((box) => {
			//console.log("rendering box %d", box.key);
			rows.push(<TextBox id={id} x={box.x - 64} y={box.y - 278} ukey={box.key} color={box.color} content={box.content} name={box.creator} />);
		});
		return rows;
	}

	//disable normal cursor by adding cursor-none to className
	return (
		<>
			<Dialog open={open} onOpenChange={setOpen}>
				<ContextMenu>
					<ContextMenuTrigger>
						<div onPointerMove={handlePointMove} style={{ backgroundColor: "lightyellow" }} className="h-dvh">
							<MouseFollower x={x} y={y} name={name} color={color} />
							{peerToMouse()}
							{textToBox()}
						</div>
					</ContextMenuTrigger>
					<ContextMenuContent className="w-52">
						<DialogTrigger asChild>
							<ContextMenuItem inset onClick={spawnTextBoxDialog}>
								Create text box
							</ContextMenuItem>
						</DialogTrigger>
					</ContextMenuContent>
				</ContextMenu>
				<DialogContent>
					<DialogHeader>
						<DialogTitle>Create text box</DialogTitle>
						<DialogDescription>Type the text you want visible here</DialogDescription>
					</DialogHeader>
					<Input id="textField" />
					<DialogFooter>
						<Button type="submit" onClick={spawnTextBox}>
							Confirm
						</Button>
					</DialogFooter>
				</DialogContent>
			</Dialog>
		</>
	);
}

function getRandomInt(max: number) {
	return Math.floor(Math.random() * max);
}

const FormSchema = z.object({
	name: z.string().min(2, {
		message: "Username must be at least 2 characters.",
	}),
	id: z.string().check((ctx) => {
		console.log("got to formschema id check");
		console.log(ctx.value);
		console.log(typeof ctx.value);
		console.log(ctx.issues);
		const input: number = parseInt(ctx.value);
		if (input == null || isNaN(input)) {
			ctx.issues.push({
				code: "invalid_element",
				message: "Number entry only field",
				input: ctx.value,
				origin: "map",
				key: "ie",
				issues: [],
			});
		}
		if (input < 0) {
			ctx.issues.push({
				code: "too_small",
				message: "Cannot enter rooms < 0",
				input: input,
				origin: "map",
				key: "ts",
				issues: [],
				minimum: 0,
			});
		}
		if (input > 10000) {
			ctx.issues.push({
				code: "too_big",
				message: "Cannot enter rooms > 10000",
				input: input,
				origin: "map",
				key: "tb",
				issues: [],
				maximum: 10000,
			});
		}
	}),
});

//z.number().gt(0, { message: "id must be greater than 0" }).lt(10000, "id must be less than 10000"),

function App() {
	const [name, setName] = useState("No Name" + getRandomInt(10000));
	const [color, setColor] = useState("Red");
	const [id, setID] = useState(0);

	const form = useForm<z.infer<typeof FormSchema>>({
		resolver: zodResolver(FormSchema),
		defaultValues: {
			name: name,
			id: id.toString(),
		},
	});

	function onSubmit(data: z.infer<typeof FormSchema>) {
		//console.log("submitted with name = %s, color = %s, id = %d", data.name, color, data.id);
		setName(data.name);
		webSocket.send(JSON.stringify(new RoomUpdate(parseInt(data.id), id)));
		setID(parseInt(data.id));
		peers = [];
		boxes = [];
	}

	window.onbeforeunload = () => {
		webSocket.close();
	};

	return (
		<>
			<div className="p-2 flex gap-2">
				<Form {...form}>
					<form onSubmit={form.handleSubmit(onSubmit)} className="flex">
						<FormField
							control={form.control}
							name="name"
							render={({ field }) => (
								<FormItem className="flex">
									<FormLabel>Name:</FormLabel>
									<FormControl>
										<Input {...field} />
									</FormControl>
									<FormMessage />
								</FormItem>
							)}
						/>
						<FormField
							control={form.control}
							name="id"
							render={({ field }) => (
								<FormItem className="flex">
									<FormLabel>ID:</FormLabel>
									<FormControl>
										<Input type="number" {...field} />
									</FormControl>
									<FormMessage />
								</FormItem>
							)}
						/>
						<Button type="submit">Set Name/Room</Button>
					</form>
				</Form>
				<DropdownMenu>
					<DropdownMenuTrigger asChild>
						<Button variant="outline">Set Color</Button>
					</DropdownMenuTrigger>
					<DropdownMenuContent className="w-56">
						<DropdownMenuRadioGroup value={color} onValueChange={setColor}>
							<DropdownMenuRadioItem value="Red">Red</DropdownMenuRadioItem>
							<DropdownMenuRadioItem value="Blue">Blue</DropdownMenuRadioItem>
							<DropdownMenuRadioItem value="Green">Green</DropdownMenuRadioItem>
							<DropdownMenuRadioItem value="Black">Black</DropdownMenuRadioItem>
						</DropdownMenuRadioGroup>
					</DropdownMenuContent>
				</DropdownMenu>
				<p>Scroll down to align your real mouse to the virtual one!</p>
			</div>
			<MouseCaptureZone name={name} color={color} id={id} />
		</>
	);
}

export default App;
