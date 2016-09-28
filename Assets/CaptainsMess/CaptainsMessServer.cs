using System;
using UnityEngine;
using UnityEngine.Networking;

[System.Serializable]
public class BroadcastData
{
	public static int VERSION = 1;

	public int version = VERSION;
	public string peerId;
	public bool isOpen;
	public int numPlayers;
	public int serverScore;
	public string privateTeamKey = "";

	public override string ToString()
	{
		// IMPORTANT: I'm adding a token at the end of this string (in this case two colons "::") so I can tell when the
		// data ends because Unity doesn't seem to clear the broadcastData buffer when changing it. This led to a weird
		// bug where the final element would get overwritten. eg. Previously, before adding the token, if the final
		// value was 999 but then changed to 1, it would be received as 199 because Unity would write the new value over
		// the previous value without clearing it. This way, the garbage data at the end will just get ignored.
		//
		// TODO: Find a better way of dealing with this!
		return String.Format("{0}:{1}:{2}:{3}:{4}:{5}::", version, peerId, isOpen ? 1 : 0, numPlayers, serverScore, privateTeamKey);
	}

	public void FromString(string aString)
	{
		var items = aString.Split(':');
		version = Convert.ToInt32(items[0]);
		peerId = items[1];
		isOpen = (Convert.ToInt32(items[2]) != 0);
		numPlayers = Convert.ToInt32(items[3]);

		if (items.Length > 4) {
			serverScore = Convert.ToInt32(items[4]);
		} else {
			serverScore = 1;
		}

		if (items.Length > 5) {
			privateTeamKey = items[5];
		}
	}
}

public class CaptainsMessServer : NetworkDiscovery
{
	public CaptainsMessNetworkManager networkManager;
	public BroadcastData broadcastDataObject;
	public bool isOpen				{ get { return broadcastDataObject.isOpen; } set { broadcastDataObject.isOpen = value; } }
	public int numPlayers			{ get { return broadcastDataObject.numPlayers; } set { broadcastDataObject.numPlayers = value; } }
	public int serverScore			{ get { return broadcastDataObject.serverScore; } set { broadcastDataObject.serverScore = value; } }
	public string privateTeamKey	{ get { return broadcastDataObject.privateTeamKey; } set { broadcastDataObject.privateTeamKey = value; } }

	void Start()
	{
		showGUI = false;
		useNetworkManager = false;
	}

	public void Setup(CaptainsMessNetworkManager aNetworkManager)
	{
		networkManager = aNetworkManager;
		broadcastKey = Mathf.Abs(aNetworkManager.broadcastIdentifier.GetHashCode()); // Make sure broadcastKey matches client
		isOpen = false;
		numPlayers = 0;

		broadcastDataObject = new BroadcastData();
		broadcastDataObject.peerId = networkManager.peerId;
		UpdateBroadcastData();
	}

	public void UpdateBroadcastData()
	{
		broadcastData = broadcastDataObject.ToString();
	}

	public void Reset()
	{
		isOpen = false;
		numPlayers = 0;
		UpdateBroadcastData();
	}

	public void RestartBroadcast()
	{
		if (running)
		{
			StopBroadcast();
		}

		// Delay briefly to let things settle down
		CancelInvoke("RestartBroadcastInternal");
		Invoke("RestartBroadcastInternal", 0.5f);
	}

	private void RestartBroadcastInternal()
	{
		UpdateBroadcastData();

		if (networkManager.verboseLogging) {
			Debug.Log("#CaptainsMess# Restarting server with data: " + broadcastData);
		}

		// You can't update broadcastData while the server is running so I have to reinitialize and restart it
		// I think Unity is fixing this

		if (!Initialize()) {
			Debug.LogError("#CaptainsMess# Network port is unavailable!");
		}
		if (!StartAsServer())
		{
			Debug.LogError("#CaptainsMess# Unable to broadcast!");

			// Clean up some data that Unity seems not to
			if (hostId != -1)
			{
				if (isServer) {
					NetworkTransport.StopBroadcastDiscovery();
				}
            	NetworkTransport.RemoveHost(hostId);
            	hostId = -1;
            }
		}
	}
}
