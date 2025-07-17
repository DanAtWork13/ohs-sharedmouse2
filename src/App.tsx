import { useState } from "react";
import { Button } from "@/components/ui/button";
//import "./App.css";

function App() {
	const [count, setCount] = useState("Click me");

	async function runRequest(): string {
		const response = await fetch("/api/hellorld");
		const data = await response.json();
		return data.message;
	}


	return (
		<>
			<h1>Vercel Functions test</h1>
			<div className="card">
				<Button onClick={() => setCount(runRequest())}>{count}</Button>
				<p>
					Click to test function
				</p>
			</div>
		</>
	);
}

export default App;
