using System;
using UnityEngine;
using UnityEngine.Networking;

[System.Serializable]
public class BroadcastData
{
	public int version = 1;
	public string peerId;
	public bool isOpen;
	public int numPlayers;

	public override string ToString()
	{
		return String.Format("{0}:{1}:{2}:{3}", version, peerId, isOpen ? 1 : 0, numPlayers);
	}

	public void FromString(string aString)
	{
		var items = aString.Split(':');
		version = Convert.ToInt32(items[0]);
		peerId = items[1];
		isOpen = (Convert.ToInt32(items[2]) != 0);
		numPlayers = Convert.ToInt32(items[3]); 
	}
}

public class CaptainsMessServer : NetworkDiscovery
{
	public CaptainsMessNetworkManager networkManager;
	public BroadcastData broadcastDataObject;
	public bool isOpen		{ get { return broadcastDataObject.isOpen; } set { broadcastDataObject.isOpen = value; } }
	public int numPlayers	{ get { return broadcastDataObject.numPlayers; } set { broadcastDataObject.numPlayers = value; } }

	void Start()
	{
		showGUI = false;
		useNetworkManager = false;
	}

	public void Setup(CaptainsMessNetworkManager aNetworkManager)
	{
		networkManager = aNetworkManager;
		broadcastKey = aNetworkManager.broadcastIdentifier.GetHashCode(); // Make sure broadcastKey matches client
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
		if (!StartAsServer()) {
			Debug.LogError("#CaptainsMess# Unable to broadcast!");
		}
	}
}
