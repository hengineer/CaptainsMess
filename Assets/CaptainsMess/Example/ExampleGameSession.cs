using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public enum GameState
{
	Offline,
	Connecting,
	Lobby,
	Countdown,
	WaitingForRolls,
	Scoring,
	GameOver
}

public class ExampleGameSession : NetworkBehaviour
{
	public Text gameStateField;
	public Text gameRulesField;

	public static ExampleGameSession instance;

	ExampleListener networkListener;
	List<ExamplePlayerScript> players;
	string specialMessage = "";

	[SyncVar]
	public GameState gameState;

	[SyncVar]
	public string message = "";

	public void OnDestroy()
	{
		if (gameStateField != null) {
			gameStateField.text = "";
			gameStateField.gameObject.SetActive(false);
		}
		if (gameRulesField != null) {
			gameRulesField.gameObject.SetActive(false);
		}
	}

	[Server]
	public override void OnStartServer()
	{
		networkListener = FindObjectOfType<ExampleListener>();
		gameState = GameState.Connecting;
	}

	[Server]
	public void OnStartGame(List<CaptainsMessPlayer> aStartingPlayers)
	{
		players = aStartingPlayers.Select(p => p as ExamplePlayerScript).ToList();

		RpcOnStartedGame();
		foreach (ExamplePlayerScript p in players) {
			p.RpcOnStartedGame();
		}

		StartCoroutine(RunGame());
	}

	[Server]
	public void OnAbortGame()
	{
		RpcOnAbortedGame();
	}

	[Client]
	public override void OnStartClient()
	{
		if (instance) {
			Debug.LogError("ERROR: Another GameSession!");
		}
		instance = this;

		networkListener = FindObjectOfType<ExampleListener>();
		networkListener.gameSession = this;

		if (gameState != GameState.Lobby) {
			gameState = GameState.Lobby;
		}
	}

	public void OnJoinedLobby()
	{
		gameState = GameState.Lobby;
	}

	public void OnLeftLobby()
	{
		gameState = GameState.Offline;
	}

	public void OnCountdownStarted()
	{
		gameState = GameState.Countdown;
	}

	public void OnCountdownCancelled()
	{
		gameState = GameState.Lobby;
	}

	[Server]
	IEnumerator RunGame()
	{
		// Reset game
		foreach (ExamplePlayerScript p in players) {
			p.totalPoints = 0;
		}

		while (MaxScore() < 3)
		{
			// Reset rolls
			foreach (ExamplePlayerScript p in players) {
				p.rollResult = 0;
			}

			// Wait for all players to roll
			gameState = GameState.WaitingForRolls;

			while (!AllPlayersHaveRolled()) {
				yield return null;
			}

			// Award point to winner
			gameState = GameState.Scoring;

			List<ExamplePlayerScript> scoringPlayers = PlayersWithHighestRoll();
			if (scoringPlayers.Count == 1)
			{
				scoringPlayers[0].totalPoints += 1;
				specialMessage = scoringPlayers[0].deviceName + " scores 1 point!";
			}
			else
			{
				specialMessage = "TIE! No points awarded.";
			}

			yield return new WaitForSeconds(2);
			specialMessage = "";
		}

		// Declare winner!
		specialMessage = PlayerWithHighestScore().deviceName + " WINS!";
		yield return new WaitForSeconds(3);
		specialMessage = "";

		// Game over
		gameState = GameState.GameOver;
	}

	[Server]
	bool AllPlayersHaveRolled()
	{
		return players.All(p => p.rollResult > 0);
	}

	[Server]
	List<ExamplePlayerScript> PlayersWithHighestRoll()
	{
		int highestRoll = players.Max(p => p.rollResult);
		return players.Where(p => p.rollResult == highestRoll).ToList();
	}

	[Server]
	int MaxScore()
	{
		return players.Max(p => p.totalPoints);
	}

	[Server]
	ExamplePlayerScript PlayerWithHighestScore()
	{
		int highestScore = players.Max(p => p.totalPoints);
		return players.Where(p => p.totalPoints == highestScore).First();
	}

	[Server]
	public void PlayAgain()
	{
		StartCoroutine(RunGame());
	}

	void Update()
	{
		if (isServer)
		{
			if (gameState == GameState.Countdown)
			{
				message = "Game Starting in " + Mathf.Ceil(networkListener.mess.CountdownTimer()) + "...";
			}
			else if (specialMessage != "")
			{
				message = specialMessage;
			}
			else
			{
				message = gameState.ToString();
			}
		}

		gameStateField.text = message;
	}

	// Client RPCs

	[ClientRpc]
	public void RpcOnStartedGame()
	{
		gameRulesField.gameObject.SetActive(true);
	}

	[ClientRpc]
	public void RpcOnAbortedGame()
	{
		gameRulesField.gameObject.SetActive(false);
	}
}
