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
		myColour = UnityEngine.Random.ColorHSV(0,1,1,1,1,1);
		CmdSetCustomPlayerInfo(myColour);
	}

	[Command]
	public void CmdSetCustomPlayerInfo(Color aColour)
	{
		myColour = aColour;
	}

	// Update is called once per frame
	public override void Update ()
	{
		base.Update();

		UpdateReadyStatus();
	}

	void UpdateReadyStatus()
	{
		if (IsReady())
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

	public override void OnClientEnterLobby()
	{
		base.OnClientEnterLobby();

		// Brief delay to let SyncVars propagate
		Invoke("ShowPlayer", 0.5f);
	}

	void ShowPlayer()
	{
		transform.SetParent(GameObject.Find("Canvas/PlayerContainer").transform, false);

		image.color = myColour;	
		nameField.text = deviceName;
		UpdateReadyStatus();
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
