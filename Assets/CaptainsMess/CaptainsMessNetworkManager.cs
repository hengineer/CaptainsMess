using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Networking.NetworkSystem;
using UnityEngine.SceneManagement;

public class CaptainsMessNetworkManager : CaptainsMessLobbyManager
{
    public string broadcastIdentifier = "CM";
    public string deviceId;
    public string peerId;
    public float startHostingDelay = 2; // Look for a server for this many seconds before starting one ourself
    public float allReadyCountdownDuration = 4; // Wait for this many seconds after people are ready before starting the game
    public bool verboseLogging = false;

    public CaptainsMessServer discoveryServer;
    public CaptainsMessClient discoveryClient;
    public CaptainsMessListener listener;
    public CaptainsMessPlayer localPlayer;

    public float allReadyCountdown = 0;
    public bool forceServer = false;

    private string maybeStartHostingFunction;
    private bool gameHasStarted = false;
    private bool joinedLobby = false;

    public virtual void Start ()
    {
        deviceId = GetUniqueDeviceId();
        peerId = deviceId.Substring(0, 8);

        discoveryServer.Setup(this);
        discoveryClient.Setup(this);

        if (singleton != this)
        {
            Debug.LogWarning("#CaptainsMess# DUPLICATE CAPTAINS MESS!");
            Destroy(gameObject);
        }

        Debug.Log(String.Format("#CaptainsMess# Initialized peer {0}, \'{1}\', {2}-{3} players",
            peerId, broadcastIdentifier, minPlayers, maxPlayers));
    }

    string GetUniqueDeviceId()
    {
        // NOTE: Don't use SystemInfo.deviceUniqueIdentifier here because it causes Android
        // to ask for permission to "make and manage phone calls?" which sounds suspicious.
        // Using this alternate method means that the ID isn't truly unique. Each time the app is
        // uninstalled/reinstalled a new ID will be generated.
        // This won't matter for normal connections but might matter if you want to use the ID
        // to track unique players met (eg. Spaceteam has achievements for number of players met).

        string savedId = PlayerPrefs.GetString("CaptainsMessDeviceId");
        if (string.IsNullOrEmpty(savedId))
        {
            savedId = GenerateNewUniqueID();
            PlayerPrefs.SetString("CaptainsMessDeviceId", savedId);
        }
        return savedId;
    }

    string GenerateNewUniqueID()
    {
        return Guid.NewGuid().ToString().Substring(0,8);
    }

    public void InitNetworkTransport()
    {
        NetworkTransport.Init();
    }

    public void ShutdownNetworkTransport()
    {
        NetworkTransport.Shutdown();
    }

    public void StartHosting()
    {
        if (verboseLogging) {
            Debug.Log("#CaptainsMess# StartHosting");
        }

        // Stop the broadcast server so we can start a regular hosting server
        StopServer();

        // Delay briefly to let things settle down
        CancelInvoke("StartHostingInternal");
        Invoke("StartHostingInternal", 0.5f);

        discoveryServer.isOpen = true;
        discoveryServer.RestartBroadcast();
    }

    private void StartHostingInternal()
    {
        if (StartHost() != null) {
            SendServerCreatedMessage();
        } else {
            Debug.LogError("#CaptainsMess# Failed to start hosting!");
        }
    }

    public void StartLocalGameForDebugging()
    {
        if (StartHost() != null) {
            SendServerCreatedMessage();
        } else {
            Debug.LogError("#CaptainsMess# Failed to start hosting!");
        }
    }

    public void StartBroadcasting()
    {
        if (!isNetworkActive)
        {
            // Must also start network server so the broadcast is sent properly
            if (!StartServer())
            {
                Debug.LogError("#CaptainsMess# Failed to start broadcasting!");
                return;
            }
        }

        const int HIGH_SERVER_SCORE = 999;

        discoveryServer.isOpen = false;
        discoveryServer.serverScore = forceServer ? HIGH_SERVER_SCORE : 1;
        discoveryServer.RestartBroadcast();
    }

    public void StartJoining()
    {
        discoveryClient.StartJoining();

        SendStartConnectingMessage();
    }

    public void AutoConnect()
    {
        StartBroadcasting();
        StartJoining();

        // Start hosting if we don't find anything for a while...
        maybeStartHostingFunction = "MaybeStartHosting";
        Invoke(maybeStartHostingFunction, startHostingDelay);
    }

    public void Cancel()
    {
        if (verboseLogging) {
            Debug.Log("#CaptainsMess# Cancelling!");
        }

        if (gameHasStarted)
        {
            if (IsHost()) {
                SendAbortGameMessage();
            }
            gameHasStarted = false;
            Invoke("Cancel", 0.1f);
            return;
        }

        // NOTE: Calling CancelInvoke(maybeStartHostingFunction) here crashes the game in certain cases
        // so I'm using the more general version instead.
        CancelInvoke();

        if (discoveryClient.running && discoveryClient.hostId != -1) {
            discoveryClient.StopBroadcast();
        }
        discoveryClient.Reset();

        if (discoveryServer.running && discoveryServer.hostId != -1) {
            discoveryServer.StopBroadcast();
        }
        discoveryServer.Reset();

        StopClient();
        StopServer();

        if (NetworkServer.active) {
            NetworkServer.Reset();
        }
    }

    public void FinishGame()
    {
        gameHasStarted = false;
    }

    public void OnReceivedBroadcast(string aFromAddress, string aData)
    {
        SendReceivedBroadcastMessage(aFromAddress, aData);
    }

    public void OnDiscoveredServer(DiscoveredServer aServer)
    {
        if (verboseLogging) {
            Debug.Log("#CaptainsMess# Discovered " + aServer.rawData);
        }

        if (discoveryServer.isOpen) {
            Debug.Log("#CaptainsMess# Already hosting a server, ignoring " + aServer.rawData);
            return;
        }

        SendDiscoveredServerMessage(aServer);

        bool shouldJoin = false;
        bool isMe = (aServer.peerId == peerId);
        if (!isMe)
        {
            if (aServer.isOpen && aServer.numPlayers < maxPlayers)
            {
                if (aServer.privateTeamKey == discoveryServer.privateTeamKey)
                {
                    if (aServer.numPlayers > 0) {
                        shouldJoin = true; // Pick the first server that already has players
                    } else if (BestHostingCandidate() == aServer.peerId) {
                        shouldJoin = true;
                    }
                }
            }
        }

        if (shouldJoin)
        {
            if (verboseLogging) {
                Debug.Log("#CaptainsMess# Should join!");
            }

            // We found something! Cancel hosting...
            CancelInvoke(maybeStartHostingFunction);

            if (client == null)
            {
                if (discoveryClient.autoJoin)
                {
                    JoinServer(aServer.ipAddress, networkPort);
                }
                else
                {
                    if (verboseLogging) {
                        Debug.Log("#CaptainsMess# JOIN CANCELED: Auto join disabled.");
                    }
                }
            }
            else
            {
                if (verboseLogging) {
                    Debug.Log("#CaptainsMess# JOIN CANCELED: Already have client.");
                }
            }
        }
        else
        {
            if (verboseLogging) {
                Debug.Log("#CaptainsMess# Should NOT join.");
            }
        }
    }

    void MaybeStartHosting()
    {
        // If I'm the best candidate, start hosting!
        int numCandidates = GetHostingCandidates().Count;
        bool enoughPlayers = (numCandidates >= minPlayers);

        if (verboseLogging) {
            Debug.Log("#CaptainsMess# MaybeStartHosting? Found " + numCandidates + "/" + minPlayers + " candidates");
        }

        if (enoughPlayers && BestHostingCandidate() == peerId)
        {
            StartHosting();
        }
        else
        {
            // Wait, then try again...
            Invoke(maybeStartHostingFunction, startHostingDelay);
        }
    }

    List<DiscoveredServer> GetHostingCandidates()
    {
        var candidates = new List<DiscoveredServer>();

        // Grab server peer IDs
        foreach (DiscoveredServer server in discoveryClient.discoveredServers.Values)
        {
            if (server.numPlayers < 2 && !server.isOpen && (server.privateTeamKey == discoveryServer.privateTeamKey))
            {
                if (!candidates.Exists(x => x.peerId == server.peerId)) {
                    candidates.Add(server);
                }
                else
                {
                    if (verboseLogging) {
                        Debug.LogWarning("#CaptainsMess# Discovered duplicate server!");
                    }
                }
            }
        }

        if (verboseLogging) {
            Debug.Log("#CaptainsMess# Hosting candidates: " + String.Join(",", candidates.Select(x => x.ToString()).ToArray()));
        }
        return candidates;
    }

    string BestHostingCandidate()
    {
        var allCandidates = GetHostingCandidates();
        if (allCandidates.Count < minPlayers) {
            return "";
        }

        allCandidates.Sort((x,y) => {
            if (x.serverScore != y.serverScore)
            {
                // Pick highest serverScore first
                return y.serverScore.CompareTo(x.serverScore);
            }
            else
            {
                // Otherwise, pick lowest peer ID (arbitrary, maybe change to fastest/best device?)
                return x.peerId.CompareTo(y.peerId);
            }
        });
        string bestCandidate = allCandidates[0].peerId;

        if (verboseLogging) {
            Debug.Log("#CaptainsMess# Picked " + bestCandidate + " as best candidate");
        }
        return bestCandidate;
    }

    void JoinServer(string aAddress, int aPort)
    {
        if (verboseLogging) {
            Debug.Log("#CaptainsMess# Joining " + aAddress + " : " + aPort);
        }

        // Stop being a server
        StopHost();
        networkAddress = aAddress;
        networkPort = aPort;

        // Delay briefly to let things settle down
        CancelInvoke("JoinServerInternal");
        Invoke("JoinServerInternal", 0.5f);
    }

    private void JoinServerInternal()
    {
        StartClient();
    }

    public bool AreAllPlayersReady()
    {
        return (NumReadyPlayers() == NumPlayers() && NumPlayers() >= minPlayers);
    }

    public bool AreAllPlayersCompatible()
    {
        int highestVersion = HighestConnectedVersion();
        foreach (var player in LobbyPlayers())
        {
            if (player.version != highestVersion) {
                return false;
            }
        }
        return true;
    }

    public int HighestConnectedVersion()
    {
        int highestVersion = 0;
        foreach (CaptainsMessPlayer p in LobbyPlayers()) {
            highestVersion = Math.Max(highestVersion, p.version);
        }
        return highestVersion;
    }

    public List<CaptainsMessPlayer> LobbyPlayers()
    {
        var lobbyPlayers = new List<CaptainsMessPlayer>();
        foreach (var player in lobbySlots)
        {
            if (player != null) {
                lobbyPlayers.Add(player);
            }
        }
        return lobbyPlayers;
    }

    public int NumReadyPlayers()
    {
        int readyCount = 0;
        foreach (var player in LobbyPlayers())
        {
            if (player.ready) {
                readyCount += 1;
            }
        }
        return readyCount;
    }

    public int NumPlayers()
    {
        return LobbyPlayers().Count;
    }

    public bool IsHost()
    {
        if (localPlayer != null)
        {
            NetworkIdentity networkObject = localPlayer.GetComponent<NetworkIdentity>();
            return (networkObject != null && networkObject.isServer && IsClientConnected() && NumPlayers() >= minPlayers);
        }
        else
        {
            return false;
        }
    }

    public void Update()
    {
        CheckAllReady();
    }

    public void CheckAllReady()
    {
        if (!gameHasStarted && allReadyCountdown > 0)
        {
            if (AreAllPlayersReady() && AreAllPlayersCompatible())
            {
                allReadyCountdown -= Time.deltaTime;
                if (allReadyCountdown <= 0)
                {
                    // Stop the broadcast so no more players join
                    if (discoveryServer.running) {
                        discoveryServer.StopBroadcast();
                    }

                    // Finalize player list
                    gameHasStarted = true;

                    if (IsHost()) {
                        SendStartGameMessage(LobbyPlayers());
                    }
                }
            }
            else
            {
                // Cancel the countdown
                allReadyCountdown = 0;

                if (IsHost()) {
                    SendCountdownCancelledMessage();
                }
            }
        }
    }

    public bool IsBroadcasting()
    {
        return discoveryServer.running;
    }

    public bool IsJoining()
    {
        return discoveryClient.running;
    }

    public bool IsConnected()
    {
        return IsClientConnected();
    }

    public void CheckReadyToBegin()
    {
        if (AreAllPlayersReady() && AreAllPlayersCompatible()) {
            OnLobbyServerPlayersReady();
        }
    }

    public override bool HasGameStarted()
    {
        return gameHasStarted;
    }

    public void SetPrivateTeamKey(string key)
    {
        discoveryServer.privateTeamKey = key;
    }

    // ------------------------ lobby server virtuals ------------------------

    public override void OnLobbyServerConnect(NetworkConnection conn)
    {
        if (verboseLogging) {
            Debug.Log("#CaptainsMess# OnLobbyServerConnect (num players = " + NumPlayers() + ")");
        }

        // If we've reached max players, stop the broadcast
        if (NumPlayers()+1 >= maxPlayers)
        {
            if (verboseLogging) {
                Debug.Log("#CaptainsMess# Max players reached, stopping broadcast");
            }

            if (discoveryServer.running) {
                discoveryServer.StopBroadcast();
            }
        }
        else
        {
            // Update player count for broadcast
            discoveryServer.numPlayers = NumPlayers() + 1;
            discoveryServer.RestartBroadcast();
        }
    }

    public override void OnLobbyServerDisconnect(NetworkConnection conn)
    {
        if (verboseLogging) {
            Debug.Log("#CaptainsMess# OnLobbyServerDisconnect (num players = " + NumPlayers() + ")");
        }

        if (gameHasStarted)
        {
            // Always cancel the game if it has started. We don't support re-joining a game in progress.
            Cancel();
        }
        else
        {
            // If we're below the minimum required players, close the lobby
            if (NumPlayers() < minPlayers)
            {
                if (verboseLogging) {
                    Debug.Log("#CaptainsMess# Not enough players, cancelling game");
                }
                Cancel();
            }
            else
            {
                if (allReadyCountdown > 0)
                {
                    // Cancel the countdown
                    allReadyCountdown = 0;
                    SendCountdownCancelledMessage();
                }

                // Update player count for broadcast
                discoveryServer.numPlayers = NumPlayers();
                discoveryServer.RestartBroadcast();
            }
        }
    }

    public override GameObject OnLobbyServerCreateLobbyPlayer(NetworkConnection conn, short playerControllerId)
    {
        if (verboseLogging) {
            Debug.Log("#CaptainsMess# OnLobbyServerCreateLobbyPlayer (num players " + NumPlayers() + ")");
        }

        GameObject newLobbyPlayer = Instantiate(playerPrefab.gameObject, Vector3.zero, Quaternion.identity) as GameObject;
        return newLobbyPlayer;
    }

    public override void OnLobbyServerPlayersReady()
    {
        if (verboseLogging) {
            Debug.Log("#CaptainsMess# OnLobbyServerPlayersReady (num players " + NumPlayers() + ")");
        }

        if (AreAllPlayersReady() && AreAllPlayersCompatible())
        {
            if (allReadyCountdownDuration > 0)
            {
                // Start all ready countdown
                allReadyCountdown = allReadyCountdownDuration;
                SendCountdownStartedMessage();
            }
            else
            {
                // Stop the broadcast so no more players join
                if (discoveryServer.running) {
                    discoveryServer.StopBroadcast();
                }

                // Start game immediately
                gameHasStarted = true;
                SendStartGameMessage(LobbyPlayers());
            }
        }
    }

    // ------------------------ lobby client virtuals ------------------------

    public override void OnLobbyClientEnter()
    {
        if (verboseLogging) {
            Debug.Log("#CaptainsMess# OnLobbyClientEnter " + listener);
        }

        // Stop listening for other servers
        if (discoveryClient.running) {
            discoveryClient.StopBroadcast();
        }

        // Stop broadcasting as a server
        if (discoveryServer.running) {
            discoveryServer.StopBroadcast();
        }

        SendJoinedLobbyMessage();

        joinedLobby = true;
    }

    public override void OnLobbyClientExit()
    {
        if (verboseLogging) {
            Debug.Log("#CaptainsMess# OnLobbyClientExit (num players = " + numPlayers + ")");
        }

        if (gameHasStarted)
        {
            if (IsHost()) {
                SendAbortGameMessage();
            }
            gameHasStarted = false;
        }

        // Check to see if we've actually joined a lobby
        if (joinedLobby)
        {
            SendLeftLobbyMessage();
        }
        else
        {
            SendStopConnectingMessage();
        }
        joinedLobby = false;
    }

    ////////////////////////////////////////////////////////////////////////////////
    //
    //  API messages
    //

    public void SendServerCreatedMessage()
    {
        listener.OnServerCreated();
    }

    public void SendStartConnectingMessage()
    {
        listener.OnStartConnecting();
    }

    public void SendStopConnectingMessage()
    {
        listener.OnStopConnecting();
    }

    public void SendReceivedBroadcastMessage(string aFromAddress, string aData)
    {
        listener.OnReceivedBroadcast(aFromAddress, aData);
    }

    public void SendDiscoveredServerMessage(DiscoveredServer aServer)
    {
        listener.OnDiscoveredServer(aServer);
    }

    public void SendJoinedLobbyMessage()
    {
        listener.OnJoinedLobby();
    }

    public void SendLeftLobbyMessage()
    {
        listener.OnLeftLobby();
    }

    public void SendCountdownStartedMessage()
    {
        listener.OnCountdownStarted();
    }

    public void SendCountdownCancelledMessage()
    {
        listener.OnCountdownCancelled();
    }

    public void SendStartGameMessage(List<CaptainsMessPlayer> aStartingPlayers)
    {
        listener.OnStartGame(aStartingPlayers);
    }

    public void SendAbortGameMessage()
    {
        listener.OnAbortGame();
    }
}
