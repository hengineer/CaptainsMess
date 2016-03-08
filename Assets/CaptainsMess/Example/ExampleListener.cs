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
	public NetworkState networkState = NetworkState.Init;
	public Text networkStateField;
	public ExampleGameSession gameSession;

	public void Start()
	{
		networkState = NetworkState.Offline;
	}

	public override void OnStartConnecting()
	{
		networkState = NetworkState.Connecting;
	}

	public override void OnStopConnecting()
	{
		networkState = NetworkState.Offline;
	}

	public override void OnJoinedLobby()
	{
		networkState = NetworkState.Connected;

		gameSession.OnJoinedLobby();
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
