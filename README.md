# CaptainsMess
A local multiplayer networking library for making games like [Spaceteam](http://sleepingbeastgames.com/spaceteam) in Unity. Tested on iOS, Android, and Mac but probably runs on other Unity platforms. Hopefully it will help other developers make more local multiplayer games!

The library is designed for local multiplayer (LAN only) games where you play with other people in the same room. The main idea is to allow "one button" connections where you just hit Play and the game automatically connects with other games around it. No IP addresses, connection dialogs, or decisions about Hosting or Joining.

It's currently built on top of the **Unity Networking HLAPI** (High Level API), specifically the `NetworkDiscovery` and `NetworkLobbyManager` classes. CaptainsMess only handles the initial discovery and connection. After that you should use the built-in Unity Networking features to communicate between devices like **SyncVars**, **Commands**, and **ClientRpcs**.

It's not finished yet. Please help if you can!

So far it only supports Wifi. I may have to drop down to the Low Level API to add Bluetooth support.

![Captains Mess Screenshot](http://www.sleepingbeastgames.com/files/CaptainsMessScreenshot.jpg)

## Todo list
- [ ] Bluetooth support
- [ ] Lots of testing

## Example Project
- In the Assets/CaptainsMess/**Example** folder there is a simple scene with a very basic implementation of the system
- Run it on at least two devices, press **Auto Connect**, and after a few seconds you should see both players connect to a "lobby"
- Check out **[ExamplePlayerScript.cs](Assets/CaptainsMess/Example/ExamplePlayerScript.cs)** and **[ExampleListener.cs](Assets/CaptainsMess/Example/ExampleListener.cs)** to see the details

## Usage
- Move Assets/**CaptainsMess** into your own Assets folder
- Create an object called "CaptainsMess" in your Scene and add the **CaptainsMess** component
- Make a subclass of **CaptainsMessPlayer** and add it to your player prefab. This object will be spawned for each player that joins a game.
- Make a subclass of **CaptainsMessListener** and add it to your listener object. This object will receive callbacks from CaptainsMess.
- Click on the CaptainsMess object you created (the inspector should look something like this screenshot)
![Captains Mess Inspector](http://www.sleepingbeastgames.com/files/CaptainsMessInspector.png)
- Customize the settings for your game:
     - **Broadcast Identifier**: Your game will only connect to games matching the same Broadcast Identifier (eg. "Spaceteam" for Spaceteam games). Usually you should use the name of your own game here.
     - **Min Players**: The minimum number of players required to start a game (eg. 2 for Spaceteam). A lobby won't be created until at least this many players find each other. You can set this to 1 for testing purposes.
     - **Max Players**: The maximum number of players supported by your game (eg. 8 for Spaceteam). Lobbies will not allow more than this number of players to join.
     - **Player Prefab**: An object of this type will be created for each player that joins. It must have the CaptainsMessPlayer component or a subclass.
     - **Countdown Duration**: The optional delay (in seconds) before starting a game once all players are ready. Can be zero.
     - **Listener**: The object that will receive callbacks from CaptainsMess like: OnJoinedLobby, OnStartGame, etc. Must have the CaptainsMessListener component or a subclass.
     - **Verbose Logging**: Shows lots of log messages for debugging
     - **Use Debug GUI**: Shows debug buttons and some debug info for quick testing.
- Now you should be able to connect to other devices using the API calls or the Debug GUI
- After the players are connected you can use the built-in Unity Networking features to communicate between devices like **SyncVars**, **Commands**, and **ClientRpcs**.

## API

#### CaptainsMess API
- `AutoConnect()` starts joining or hosting as appropriate. This is what Spaceteam does when you dial "Play".
- `StartHosting()` starts hosting a game as the server
- `StartJoining()` starts joining any servers it finds, but will not host
- `Cancel()` will abort the connection or disconnect from a lobby
- `Players()` will return a list of all the connected players
- `AreAllPlayersReady()` will return true if all players have marked themselves ready
- `CountdownTimer()` will return the remaining time in the countdown (or 0 if the countdown is not running)
- `StartLocalGameForDebugging()` is the same as StartHosting but temporarily sets minPlayers to 1 so that you can start a single-player game

#### CaptainsMessPlayer API
These are built-in Unity Networking commands defined in the [NetworkLobbyPlayer](http://docs.unity3d.com/ScriptReference/Networking.NetworkLobbyPlayer.html) base class. See the documentation for more info.
- `SendReadyToBeginMessage()` Tell the server that this player is ready
- `SendNotReadyToBeginMessage()` Tell the server that this player is NOT ready
- `OnClientReady(bool readyState)` Called when the player's ready state changes
- `OnClientEnterLobby()` Called when the player enters a lobby
- `OnClientExitLobby()` Called when the player exits a lobby

#### CaptainsMessListener callbacks
- `OnStartGame(List<CaptainsMessPlayer> aStartingPlayers)` *(Server only)* Called when all players are ready and the countdown has finished. This is when your game should take over do whatever it needs to.
- `OnAbortGame()` *(Server only)* Called if any player disconnects after the game has started. You should end your game at this point. At the moment there is no support for disconnecting/reconnecting while a game is in progress.
- `OnCountdownStarted()` *(Server only)* Called when all players have marked themselves ready
- `OnCountdownCancelled()` *(Server only)* Called if any player stops being ready during the countdown
- `OnJoinedLobby()` Called when the player joins a lobby
- `OnLeftLobby()` Called when the player leaves a lobby or is disconnected
- `OnStartConnecting()` You can override this if you want to show feedback that the game has started the connection process
- `OnReceivedBroadcast()` You can override this if you want to show feedback about the network traffic
- `OnDiscoveredServer()` You can override this if you want to show feedback about the network traffic.

## Limitations
- I only use a single Scene for Lobby/Game/Offline/Online. Unity Networking supports different scenes for these states so this functionality can be added back if needed.
- I don't support disconnecting and re-connecting to a game already in progress. If any player disconnects after a game has started, the game is aborted.

---
Please check it out and let me know if you have any problems or if you can help improve it! And if you want to support me please consider joining the **[Spaceteam Admiral's Club](http://spaceteamadmiralsclub.com/forum/plugin/page/membersArea)**.

Thanks, Henry (aka Captain Spaceteam) \[ [email](mailto:henry@sleepingbeastgames.com) | [twitter](https://twitter.com/hengineer) \]
