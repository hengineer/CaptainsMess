// A simplified version of Unity's NetworkLobbyManager to prevent it changing scenes on connection

using System;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Networking.NetworkSystem;

public class CaptainsMessLobbyManager : NetworkManager
{
    public int minPlayers = 2;
    public int maxPlayers = 4;
    public CaptainsMessPlayer[] lobbySlots;
    public int maxPlayersPerConnection = 1;
    
    public void SetMaxPlayers(int value)
    {
        maxPlayers = value;
        maxConnections = maxPlayers;
    }

    // ==================== SERVER ====================

    public override void OnStartServer()
    {
        if (lobbySlots == null || lobbySlots.Length == 0)
        {
            lobbySlots = new CaptainsMessPlayer[maxPlayers];
        }
    }

    public override void OnServerConnect(NetworkConnection conn)
    {
        if (numPlayers >= maxPlayers)
        {
            conn.Disconnect();
            return;
        }

        // cannot join game in progress
        if (HasGameStarted())
        {
            conn.Disconnect();
            return;
        }

        base.OnServerConnect(conn);
        OnLobbyServerConnect(conn);
    }

    public override void OnServerDisconnect(NetworkConnection conn)
    {
        base.OnServerDisconnect(conn);

        // if lobbyplayer for this connection has not been destroyed by now, then destroy it here
        for (int i = 0; i < lobbySlots.Length; i++)
        {
            var player = lobbySlots[i];
            if (player == null)
                continue;

            if (player.connectionToClient == conn)
            {
                lobbySlots[i] = null;
                NetworkServer.Destroy(player.gameObject);
            }
        }

        OnLobbyServerDisconnect(conn);
    }

    Byte FindSlot()
    {
        for (byte i = 0; i < maxPlayers; i++)
        {
            if (lobbySlots[i] == null)
            {
                return i;
            }
        }
        return Byte.MaxValue;
    }

    public override void OnServerAddPlayer(NetworkConnection conn, short playerControllerId)
    {
        // check MaxPlayersPerConnection
        int numPlayersForConnection = 0;
        foreach (var player in conn.playerControllers)
        {
            if (player.IsValid)
                numPlayersForConnection += 1;
        }

        if (numPlayersForConnection >= maxPlayersPerConnection)
        {
            if (LogFilter.logWarn) { Debug.LogWarning("NetworkLobbyManager no more players for this connection."); }

            var errorMsg = new EmptyMessage();
            conn.Send(MsgType.LobbyAddPlayerFailed, errorMsg);
            return;
        }

        byte slot = FindSlot();
        if (slot == Byte.MaxValue)
        {
            if (LogFilter.logWarn) { Debug.LogWarning("NetworkLobbyManager no space for more players"); }

            var errorMsg = new EmptyMessage();
            conn.Send(MsgType.LobbyAddPlayerFailed, errorMsg);
            return;
        }

        var newLobbyGameObject = OnLobbyServerCreateLobbyPlayer(conn, playerControllerId);
        if (newLobbyGameObject == null)
        {
            newLobbyGameObject = (GameObject)Instantiate(playerPrefab.gameObject, Vector3.zero, Quaternion.identity);
        }

        var newLobbyPlayer = newLobbyGameObject.GetComponent<CaptainsMessPlayer>();
        newLobbyPlayer.slot = slot;
        lobbySlots[slot] = newLobbyPlayer;

        NetworkServer.AddPlayerForConnection(conn, newLobbyGameObject, playerControllerId);
    }

    public override void OnServerRemovePlayer(NetworkConnection conn, PlayerController player)
    {
        byte slot = player.gameObject.GetComponent<CaptainsMessPlayer>().slot;
        lobbySlots[slot] = null;
        base.OnServerRemovePlayer(conn, player);
    }

    // ==================== CLIENT ====================

    public override void OnStartClient(NetworkClient lobbyClient)
    {
        if (lobbySlots == null || lobbySlots.Length == 0)
        {
            lobbySlots = new CaptainsMessPlayer[maxPlayers];
        }
    }

    public override void OnClientConnect(NetworkConnection conn)
    {
        CallOnClientEnterLobby();
        base.OnClientConnect(conn);
    }

    public override void OnClientDisconnect(NetworkConnection conn)
    {
        base.OnClientDisconnect(conn);
    }

    public override void OnStopClient()
    {
        CallOnClientExitLobby();
    }

    void CallOnClientEnterLobby()
    {
        OnLobbyClientEnter();
        foreach (CaptainsMessPlayer player in lobbySlots)
        {
            if (player == null)
                continue;

            player.OnClientEnterLobby();
        }
    }

    void CallOnClientExitLobby()
    {
        OnLobbyClientExit();
        foreach (CaptainsMessPlayer player in lobbySlots)
        {
            if (player == null)
                continue;

            player.OnClientExitLobby();
        }
    }

    public virtual bool HasGameStarted()
    {
        return false;
    }

    // ------------------------ lobby server virtuals ------------------------

    public virtual void OnLobbyServerConnect(NetworkConnection conn)
    {
    }

    public virtual void OnLobbyServerDisconnect(NetworkConnection conn)
    {
    }

    public virtual GameObject OnLobbyServerCreateLobbyPlayer(NetworkConnection conn, short playerControllerId)
    {
        return null;
    }

    public virtual void OnLobbyServerPlayersReady()
    {
    }

    // ------------------------ lobby client virtuals ------------------------

    public virtual void OnLobbyClientEnter()
    {
    }

    public virtual void OnLobbyClientExit()
    {
    }
}
