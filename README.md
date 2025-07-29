# task 3: shared mouse web app

## original instructions:
Build a collaborative interface (in React.js) where multiple users can see each other's cursors moving on the screen in real time.
Each cursor should display the user's name and a unique color.
When users move their mouse, their cursor position should update for all connected clients using WebSockets.
Some things you might explore or add:
- Unique name and color per user
- "User joined" / "User left" indicators
- Show a message box where users can leave a message
- Smooth animations for cursor movement
- This is open-ended, so feel free to get creative. When it's ready, send us a link to the live demo (Vercel) and your GitHub repo.

## What is done:
- UI/frontend is done. It's functional with a few minor issues.
	- Has all the requisite features of the task, except toast messages
	- 8 total user selectable colors
	- Informative text for confused users
- WS server fully functional
	- able to support all the message types in the project and runs in background correctly
	- used to have thread sync issues, but those seem to have been resolved
- Vercel hosting dropped in favor of Azure-based solution (see notes).
	- Works great, have some scripts to automate production builds and such.

## TODO:
- test stuff with more people
- entering a room with many textboxes from multiple users only shows the most recent box(es)
	- Deleting boxes that aren't on the other screen causes unrelated boxes to be deleted
	- Or no boxes. Box sync appears totally broken if joined after boxes created. Maybe happens w/large number boxes from 3+ users
- having many boxes quickly takes up the entire space
- boxes can easily have their delete buttons covered. Amelia suggested context menu deleter


Old info that isn't relevant:


## My interpretation:
- Figure out how to host an app on vercel
  - Figure out how to run server-side code on vercel/vite/etc
- install prettier, tailwindcss and shadcn components cause they look good
- design api
- write server code to:
  - create WS server that listens for messages from clients, then distributes them to the other connected clients
- create the UI pages: login, usage, admin?


### API
- The 4 types of messages to describe the actions of the client to the server
- By "reflection", i mean the server will recieve the message from one client, then broadcast it out unmodified to all other connected clients
1. login
   - contains client ext IP, username and color. triggers server->client reflection
   - server will store ip and name association, to use later
   - clients will allocate the relevant structs for the new user
   - called only from the login page
2. logout
   - contains ext IP. triggers reflection. both clients and server free structs and stop listening for this client
   - called only from the logout button on the main page
3. local mouse movement
   - called whenever the main page's main component detects a mouse movement within its boundaries
   - contains ext IP and x/y coords of cursor. does not trigger reflection
   - server will translate ip->name, then send remote mouse movement message to other clients
4. remote mouse movement
   - only sent from server to clients after recieving a local mouse move
   - contains the name of the mover, and x/y coords. cannot trigger reflection
   - clients will each individually update the relevant pointer object with the new information


### UI design
- Top row: two text fields with your name and the number of connected clients, a logout button
- rest of screen is uncolored box where local cursor is replaced by colored cursor w/your name attached. remote cursors appear the same way

## Notes
Here I'm just going to summarize what happened on 7/18/25 for posterity.
Determined yesterday that vercel works great for frontend deployment, but cannot support server-based backend required for websockets.
Wrote own WS server using 3rd party library using .Net. Works great in local test environment, but need to put on public internet for hosted frontent to access it.
Eventually decide Microsoft Azure as hosting provider. Fortunately, they have free trial and cheap VMs I can use. Compile WS server for linux, copy to VM.
Attempt to access from hosted frontend: failure. Because frontend hosted over HTTPS, it cannot access an unsecured WS:// url to talk to server.
Try various options of creating self-signed certificate, but none work. Decide to move frontend hosting to the same azure VM.
Install apache web server, npm and frondend on VM, and eventually configure apache to serve the vite build on a dynamic domain name I own (thelettuceclub.myddns.me).
