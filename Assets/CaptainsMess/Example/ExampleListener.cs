using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class ExampleListener : CaptainsMessListener
{
	public Text messageField;

	[SyncVar]
	public string gameMessage;

	public override void OnStartConnecting()
	{
		gameMessage = "Connecting...";
	}

	public override void OnJoinedLobby()
	{
		gameMessage = "Lobby";
	}

	public override void OnLeftLobby()
	{
		gameMessage = "Offline";
	}

	public override void OnCountdownCancelled()
	{
		gameMessage = "Lobby";
	}

	public override void OnStartGame(List<CaptainsMessPlayer> aStartingPlayers)
	{
		Debug.Log("GO!");
		gameMessage = "GO!";
	}

	public override void OnAbortGame()
	{
		Debug.Log("ABORT!");
		gameMessage = "Offline";
	}

	void Update()
	{
		if (mess.CountdownTimer() > 0)
		{
			gameMessage = "Game Starting in " + Mathf.Ceil(mess.CountdownTimer()) + "...";
		}

		messageField.text = gameMessage;
	}
}
