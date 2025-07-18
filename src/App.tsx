import { useState, type JSX } from "react";
import { zodResolver } from "@hookform/resolvers/zod";
import { useForm } from "react-hook-form";
import { z } from "zod";
import { Button } from "@/components/ui/button";
import { Form, FormControl, FormField, FormItem, FormMessage } from "@/components/ui/form";
import { Input } from "@/components/ui/input";
import { DropdownMenu, DropdownMenuContent, DropdownMenuRadioGroup, DropdownMenuRadioItem, DropdownMenuTrigger } from "@/components/ui/dropdown-menu";
//import "./App.css";


function MouseFollower({ x, y, name, color }: { x: number; y: number; name: string; color: string }) {
	const nameClassString = "text-xs text-"+color.toLowerCase()+"-600";
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
const webSocket = new WebSocket("ws://thelettuceclub.myddns.me:58324/");

webSocket.onerror = function () {
	console.log("WS error");
	this.close();
};

interface Peer {
	name: string;
	color: string;
	x: number;
	y: number;
}

let peers: Peer[] = [];

function MouseCaptureZone({ name, color }: { name: string; color: string }) {
	const [x, setX] = useState(0);
	const [y, setY] = useState(0);
	const [recMsgCnt, setRecMsgCnt] = useState(0);

	//pseudo function to send current x/y whenever it changes
	if (prevX != x || prevY != y) {
		//console.log("sending new coords");
		webSocket.send(JSON.stringify({ name: name, color: color, x: x, y: y }));
		prevX = x;
		prevY = y;
	}

	webSocket.onmessage = function (e) {
		//console.log("WS recieved: " + e.data);
		const data = JSON.parse(e.data as string) as Peer;
		const idx = peers.findIndex((elem) => elem.name === data.name);
		if (idx != -1) {
			peers[idx] = data;
		} else {
			peers.push(data);
		}
		setRecMsgCnt(recMsgCnt+1);
	};
	

	function handlePointMove(ev: React.PointerEvent<HTMLDivElement>) {
		//console.log("clientX: %d, clientY: %d", ev.clientX, ev.clientY);
		setX(ev.clientX);
		setY(ev.clientY);
	}

	function peerToMouse() {
		const rows:JSX.Element[] = [];
		peers.forEach((peer) => {
			if (peer.name !== name) {
				rows.push(<MouseFollower x={peer.x} y={peer.y - 40} name={peer.name} color={peer.color} />);
			}
		});
		return rows;
	}

	//disable normal cursor by adding cursor-none to className
	return (
		<>
			<div onPointerMove={handlePointMove} style={{ backgroundColor: "lightyellow" }} className="h-dvh">
				<MouseFollower x={x} y={y} name={name} color={color} />
				{peerToMouse()}
			</div>
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
});

function App() {
	const [name, setName] = useState("No Name" + getRandomInt(10000));
	const [color, setColor] = useState("Red");

	const form = useForm<z.infer<typeof FormSchema>>({
		resolver: zodResolver(FormSchema),
		defaultValues: {
			name: "",
		},
	});

	function onSubmit(data: z.infer<typeof FormSchema>) {
		//console.log("submitted with name = %s, color = %s", data.name, color);
		setName(data.name);
		peers = [];
	}

	return (
		<>
			<div className="p-2 flex gap-2">
				<Form {...form}>
					<form onSubmit={form.handleSubmit(onSubmit)} className="flex">
						<FormField
							control={form.control}
							name="name"
							render={({ field }) => (
								<FormItem>
									<FormControl>
										<Input placeholder="name" {...field} />
									</FormControl>
									<FormMessage />
								</FormItem>
							)}
						/>
						<Button type="submit">Set Name</Button>
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
			<MouseCaptureZone name={name} color={color} />
		</>
	);
}

export default App;
