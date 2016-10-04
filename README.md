# CaptainsMess
A local multiplayer networking library for making games like [Spaceteam](http://sleepingbeastgames.com/spaceteam) in Unity (5.1+). Tested on iOS, Android, and Mac but probably runs on other Unity platforms. Hopefully it will help other developers make more local multiplayer games!

The library is designed for local multiplayer (LAN only) games where you play with other people in the same room. The main idea is to allow "one button" connections where you just hit Play and the game automatically connects with other games around it. No IP addresses, connection dialogs, or decisions about Hosting or Joining.

It's currently built on top of the **[Unity Networking HLAPI](http://docs.unity3d.com/Manual/UNetUsingHLAPI.html)** (High Level API) introduced in Unity 5.1, specifically the `NetworkDiscovery` and ~~NetworkLobbyManager~~ `NetworkManager` classes. CaptainsMess only handles the initial discovery and connection. After that you should use the built-in Unity Networking features to communicate between devices like **SyncVars**, **Commands**, and **ClientRpcs**.

It's not finished yet. Please help if you can!

So far it only supports Wifi. I may have to drop down to the Low Level API to add Bluetooth support.

_(For more background, see my blog post: [http://www.sleepingbeastgames.com/blog/the-spaceteam-networking-post/](http://www.sleepingbeastgames.com/blog/the-spaceteam-networking-post/))_

## Todo list
- [x] Prevent it from switching scenes while connecting - DONE!
- [ ] Bluetooth support
- [ ] Lots of testing

## Version History
### 0.5 (October 4, 2016)
- Android no longer asks for permission to "make and manage phone calls?"
	- NOTE: This was fixed by no longer using `System.deviceUniqueIdentifier`. I now use a different system which is not technically unique to the device. It will be regenerated if the app is uninstalled/reinstalled. This is fine for normal connections but won't work if you actually want to track unique devices (eg. Spaceteam has achievements for number of players met)
- Version support (game will not start if players have incompatible versions)
- Private Team support (only connect to players with the same private team password/code)
- Fixed various connection bugs
- Unity 5.4.1 upgrade

### 0.4 (June 25, 2016)
- New `FinishGame` API call. Call it when the game is over so you can go back to the lobby with the same players.
- Added internal support for `serverScore`. This may be used in the future to pick the server based on device performance, eg. to prefer the "fastest" device as the server.
- Fixed some warnings.

### 0.3 (April 20, 2016)
- The scene is no longer reloaded/changed when connecting.
<br>*Because of this I would be wary of using [Scene Objects](http://docs.unity3d.com/Manual/UNetSceneObjects.html) (objects with NetworkIdentity that already exist in the scene) since they no longer seem to get synced properly. Instead you should spawn network objects.*
- New `OnServerCreated` callback (eg. for spawning initial network objects)

### 0.2 (March 8, 2016)
- Added example dice-rolling game
- CaptainsMessListener is no longer a NetworkBehaviour
- `OnLeftLobby` callback is called properly
- New `OnStopConnecting` callback
- Debug buttons now show and hide depending on connection state

### 0.1 (February 18, 2016)
- Initial public version

## Example Project

![Captains Mess Screenshot](http://www.sleepingbeastgames.com/files/CaptainsMessScreenshot.jpg)

- In the Assets/CaptainsMess/**Example** folder there is an implementation of a simple dice-rolling game
- Run it on at least two devices, press **Auto Connect**, and after a few seconds you should see both players connect to a "lobby"
- Press Ready on all devices and the game should start
- Check out **[ExamplePlayerScript.cs](Assets/CaptainsMess/Example/ExamplePlayerScript.cs)**, **[ExampleListener.cs](Assets/CaptainsMess/Example/ExampleListener.cs)**, and **[ExampleGameSession.cs](Assets/CaptainsMess/Example/ExampleGameSession.cs)** to see the details

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
     - **Force Server**: Force this device to be the server (internally: gives the device a very high `serverScore`, ensuring that it will be picked first)
- Now you should be able to connect to other devices using the API calls or the Debug GUI
- After the players are connected you can use the built-in Unity Networking features to communicate between devices like **SyncVars**, **Commands**, and **ClientRpcs**.

## API

#### CaptainsMess API
- `AutoConnect()` starts joining or hosting as appropriate. This is what Spaceteam does when you dial "Play".
- `StartHosting()` starts hosting a game as the server
- `StartJoining()` starts joining any servers it finds, but will not host
- `Cancel()` aborts the connection or disconnect from a lobby
- `Players()` returns a list of all the connected players
- `LocalPlayer()` returns the player associated with the local device
- `AreAllPlayersReady()` returns true if all players have marked themselves ready
- `CountdownTimer()` returns the remaining time in the countdown (or 0 if the countdown is not running)
- `IsConnected()` returns true if connected to other devices
- `IsHost()` returns true if this device is the host/server
- `StartLocalGameForDebugging()` is the same as StartHosting but temporarily sets minPlayers to 1 so that you can start a single-player game
- `FinishGame()` call this when your game is over if you want to return to the lobby with the same players
- `ForceServer(bool forceServer)` give the device a high 'serverScore' so that it will be preferred when choosing a server

#### CaptainsMessPlayer API
- `SendReadyToBeginMessage()` Tell the server that this player is ready
- `SendNotReadyToBeginMessage()` Tell the server that this player is NOT ready
- `OnClientReady(bool readyState)` Called when the player's ready state changes
- `OnClientEnterLobby()` Called when the player enters a lobby
- `OnClientExitLobby()` Called when the player exits a lobby

#### CaptainsMessListener callbacks
- `OnServerCreated()` *(Server only)* Called after the server has started so you can do additional setup (eg. spawn network objects)
- `OnStartGame(List<CaptainsMessPlayer> aStartingPlayers)` *(Server only)* Called when all players are ready and the countdown has finished. This is when your game should take over do whatever it needs to.
- `OnAbortGame()` *(Server only)* Called if any player disconnects after the game has started. You should end your game at this point. At the moment there is no support for disconnecting/reconnecting while a game is in progress.
- `OnCountdownStarted()` *(Server only)* Called when all players have marked themselves ready
- `OnCountdownCancelled()` *(Server only)* Called if any player stops being ready during the countdown
- `OnJoinedLobby()` Called when the player joins a lobby
- `OnLeftLobby()` Called when the player leaves a lobby or is disconnected
- `OnStartConnecting()` You can override this if you want to show feedback that the game has started the connection process
- `OnStopConnecting()` You can override this if you want to show feedback that the game has stopped the connection process
- `OnReceivedBroadcast()` You can override this if you want to show feedback about the network traffic
- `OnDiscoveredServer()` You can override this if you want to show feedback about the network traffic.

## Limitations
- I only use a single Scene for Offline/Online. Unity Networking supports different scenes for these states so this functionality can be added back if needed.
- I don't support disconnecting and re-connecting to a game already in progress. If any player disconnects after a game has started, the game is aborted.

---
Please check it out and let me know if you have any problems or if you can help improve it!

I've created a **discussion thread** on my forum here: [http://spaceteamadmiralsclub.com/forum/discussion/218/captainsmess-first-release#latest](http://spaceteamadmiralsclub.com/forum/discussion/218/captainsmess-first-release#latest)

And if you want to support me please consider joining the **[Spaceteam Admiral's Club](http://spaceteamadmiralsclub.com/forum/plugin/page/membersArea)**.

Thanks,

Henry (aka Captain Spaceteam) \[ [email](mailto:henry@sleepingbeastgames.com) | [twitter](https://twitter.com/hengineer) \]
