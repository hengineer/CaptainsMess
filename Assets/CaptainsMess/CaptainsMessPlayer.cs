using System;
using UnityEngine;
using UnityEngine.Networking;

public class CaptainsMessPlayer : NetworkLobbyPlayer {

	[SyncVar]
	public string deviceName;
	[SyncVar]
	public string deviceId;
	[SyncVar]
	public string peerId;
	[SyncVar]
	public int playerIndex;

	// This is needed due to a bug in NetworkLobbyPlayer where the initial state of the readyToBegin flag isn't synced properly
	[SyncVar]
	public bool ready;

	// public string deviceModel;
	// public int memory;
	// public int processorFrequency;
	// public string operatingSystem;
	
	protected CaptainsMessNetworkManager networkManager;

	public override void OnStartClient()
	{
		base.OnStartClient();

		networkManager = NetworkManager.singleton as CaptainsMessNetworkManager;
	}

	public override void OnStartLocalPlayer()
	{
		#if UNITY_ANDROID
			deviceName = SystemInfo.deviceModel;
		#else
			deviceName = SystemInfo.deviceName;
		#endif

		deviceId = networkManager.deviceId;
		peerId = networkManager.peerId;
		playerIndex = slot;

		// deviceModel = SystemInfo.deviceModel;
		// memory = SystemInfo.systemMemorySize;
		// processorFrequency = SystemInfo.processorFrequency;
		// operatingSystem = SystemInfo.operatingSystem;
		// Debug.Log(String.Format("Device specs: {0}, {1}, {2} proc, {3} mem", deviceModel, operatingSystem, processorFrequency, memory));

		CmdSetBasePlayerInfo(deviceName, deviceId, peerId, playerIndex);
	}

	[Command]
	public virtual void CmdSetBasePlayerInfo(string aDeviceName, string aDeviceId, string aPeerId, int aPlayerIndex)
	{
		deviceName = aDeviceName;
		deviceId = aDeviceId;
		peerId = aPeerId;
		playerIndex = aPlayerIndex;
	}

	public bool IsReady()
	{
		return isLocalPlayer ? readyToBegin : ready;
	}

	[ServerCallback]
	public virtual void Update()
	{
		// This is needed due to a bug in NetworkLobbyPlayer where the initial state of the readyToBegin flag isn't synced properly
		ready = readyToBegin;
	}
}
