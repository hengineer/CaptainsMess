using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System;
using System.Collections;

public class ExamplePlayerScript : CaptainsMessPlayer {

	public Image image;
	public Text nameField;
	public Text readyField;

	[SyncVar]
	public Color myColour;

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
		OnClientReady(IsReady());
	}

	void OnGUI()
	{
		if (isLocalPlayer)
		{
			if (GUI.Button(new Rect(Screen.width*0.5f - 150, Screen.height*0.7f - 50, 300, 100), IsReady() ? "Not ready" : "Ready"))
			{
				if (IsReady()) {
					SendNotReadyToBeginMessage();
				} else {
					SendReadyToBeginMessage();
				}
			}
    	}
	}
}
