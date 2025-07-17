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
  This is open-ended, so feel free to get creative. When it's ready, send us a link to the live demo (Vercel) and your GitHub repo.

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
