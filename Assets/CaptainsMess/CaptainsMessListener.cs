using UnityEngine;
using UnityEngine.Networking;
using System.Collections.Generic;

public class CaptainsMessListener : MonoBehaviour
{
	[HideInInspector]
	public CaptainsMess mess;

	public void Awake()
	{
		mess = FindObjectOfType(typeof(CaptainsMess)) as CaptainsMess;
	}

	public virtual void OnStartConnecting()
	{
		// Override
	}

	public virtual void OnStopConnecting()
	{
		// Override
	}

	public virtual void OnServerCreated()
	{
		// Override
	}

	public virtual void OnReceivedBroadcast(string aFromAddress, string aData)
	{
		// Override
	}

	public virtual void OnDiscoveredServer(DiscoveredServer aServer)
	{
		// Override
	}

	public virtual void OnJoinedLobby()
	{
		// Override
	}

	public virtual void OnLeftLobby()
	{
		// Override
	}

	public virtual void OnCountdownStarted()
	{
		// Override
	}

	public virtual void OnCountdownCancelled()
	{
		// Override
	}

	public virtual void OnStartGame(List<CaptainsMessPlayer> aStartingPlayers)
	{
		// Override
	}

	public virtual void OnAbortGame()
	{
		// Override
	}
}
