import { useState } from "react";
import { Button } from "@/components/ui/button";
//import "./App.css";

function App() {
	const [count, setCount] = useState("Click me");

	function runRequest():string {
		//const response = new Request("/api/hellorld");
		//return (await response.json()).message;
		return "fuck";
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
