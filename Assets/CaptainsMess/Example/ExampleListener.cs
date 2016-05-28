using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class ExampleListener : CaptainsMessListener
{
	public enum NetworkState
	{
		Init,
		Offline,
		Connecting,
		Connected,
		Disrupted
	};
	[HideInInspector]
	public NetworkState networkState = NetworkState.Init;
	public Text networkStateField;
	
	public GameObject gameSessionPrefab;
	public ExampleGameSession gameSession;

	public void Start()
	{
		networkState = NetworkState.Offline;

		ClientScene.RegisterPrefab(gameSessionPrefab);
	}

	public override void OnStartConnecting()
	{
		networkState = NetworkState.Connecting;
	}

	public override void OnStopConnecting()
	{
		networkState = NetworkState.Offline;
	}

	public override void OnServerCreated()
	{
		// Create game session
		ExampleGameSession oldSession = FindObjectOfType<ExampleGameSession>();
		if (oldSession == null)
		{
			GameObject serverSession = Instantiate(gameSessionPrefab);
			NetworkServer.Spawn(serverSession);
		}
		else
		{
			Debug.LogError("GameSession already exists!");
		}
	}

	public override void OnJoinedLobby()
	{
		networkState = NetworkState.Connected;

		gameSession = FindObjectOfType<ExampleGameSession>();
		if (gameSession) {
			gameSession.OnJoinedLobby();
		}
	}

	public override void OnLeftLobby()
	{
		networkState = NetworkState.Offline;

		gameSession.OnLeftLobby();
	}

	public override void OnCountdownStarted()
	{
		gameSession.OnCountdownStarted();
	}

	public override void OnCountdownCancelled()
	{
		gameSession.OnCountdownCancelled();
	}

	public override void OnStartGame(List<CaptainsMessPlayer> aStartingPlayers)
	{
		Debug.Log("GO!");
		gameSession.OnStartGame(aStartingPlayers);
	}

	public override void OnAbortGame()
	{
		Debug.Log("ABORT!");
		gameSession.OnAbortGame();
	}

	void Update()
	{
		networkStateField.text = networkState.ToString();	
	}
}
