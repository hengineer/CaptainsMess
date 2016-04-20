using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System;
using System.Collections;

public class ExamplePlayerScript : CaptainsMessPlayer
{
	public Image image;
	public Text nameField;
	public Text readyField;
	public Text rollResultField;
	public Text totalPointsField;

	[SyncVar]
	public Color myColour;

	// Simple game states for a dice-rolling game

	[SyncVar]
	public int rollResult;

	[SyncVar]
	public int totalPoints;


	public override void OnStartLocalPlayer()
	{
		base.OnStartLocalPlayer();

		// Send custom player info
		// This is an example of sending additional information to the server that might be needed in the lobby (eg. colour, player image, personal settings, etc.)

		myColour = UnityEngine.Random.ColorHSV(0,1,1,1,1,1);
		CmdSetCustomPlayerInfo(myColour);
	}

	[Command]
	public void CmdSetCustomPlayerInfo(Color aColour)
	{
		myColour = aColour;
	}

	[Command]
	public void CmdRollDie()
	{
		rollResult = UnityEngine.Random.Range(1, 7);
	}

	[Command]
	public void CmdPlayAgain()
	{
		ExampleGameSession.instance.PlayAgain();
	}

	public override void OnClientEnterLobby()
	{
		base.OnClientEnterLobby();

		// Brief delay to let SyncVars propagate
		Invoke("ShowPlayer", 0.5f);
	}

	public override void OnClientReady(bool readyState)
	{
		if (readyState)
		{
			readyField.text = "READY!";
			readyField.color = Color.green;
		}
		else
		{
			readyField.text = "not ready";
			readyField.color = Color.red;
		}
	}

	void ShowPlayer()
	{
		transform.SetParent(GameObject.Find("Canvas/PlayerContainer").transform, false);

		image.color = myColour;	
		nameField.text = deviceName;
		readyField.gameObject.SetActive(true);

		rollResultField.gameObject.SetActive(false);
		totalPointsField.gameObject.SetActive(false);

		OnClientReady(IsReady());
	}

	public void Update()
	{
		totalPointsField.text = "Points: " + totalPoints.ToString();
		if (rollResult > 0) {
			rollResultField.text = "Roll: " + rollResult.ToString();
		} else {
			rollResultField.text = "";
		}
	}

	[ClientRpc]
	public void RpcOnStartedGame()
	{
		readyField.gameObject.SetActive(false);

		rollResultField.gameObject.SetActive(true);
		totalPointsField.gameObject.SetActive(true);
	}

	void OnGUI()
	{
		if (isLocalPlayer)
		{
			GUILayout.BeginArea(new Rect(0, Screen.height * 0.8f, Screen.width, 100));
			GUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();

			ExampleGameSession gameSession = ExampleGameSession.instance;
			if (gameSession)
			{
				if (gameSession.gameState == GameState.Lobby ||
					gameSession.gameState == GameState.Countdown)
				{
					if (GUILayout.Button(IsReady() ? "Not ready" : "Ready", GUILayout.Width(Screen.width * 0.3f), GUILayout.Height(100)))
					{
						if (IsReady()) {
							SendNotReadyToBeginMessage();
						} else {
							SendReadyToBeginMessage();
						}
					}
				}
				else if (gameSession.gameState == GameState.WaitingForRolls)
				{
					if (rollResult == 0)
					{
						if (GUILayout.Button("Roll Die", GUILayout.Width(Screen.width * 0.3f), GUILayout.Height(100)))
						{
							CmdRollDie();
						}
					}
				}
				else if (gameSession.gameState == GameState.GameOver)
				{
					if (isServer)
					{
						if (GUILayout.Button("Play Again", GUILayout.Width(Screen.width * 0.3f), GUILayout.Height(100)))
						{
							CmdPlayAgain();
						}
					}
				}
			}

			GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal();
			GUILayout.EndArea();
    	}
	}
}
