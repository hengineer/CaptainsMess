#if UNITY_EDITOR

using UnityEngine;
using System.Collections;
using UnityEditor;

[CustomEditor(typeof(CaptainsMess))]
public class CaptainsMessEditor : Editor 
{
    SerializedProperty broadcastIdentifierProperty;
    SerializedProperty minPlayersProperty;
    SerializedProperty maxPlayersProperty;
    SerializedProperty playerPrefabProperty;
    SerializedProperty countdownDurationProperty;
    SerializedProperty listenerProperty;
    SerializedProperty verboseLoggingProperty;
    SerializedProperty useDebugGUIProperty;
    SerializedProperty forceServerProperty;

	public void OnEnable()
	{
        broadcastIdentifierProperty = serializedObject.FindProperty("broadcastIdentifier");
        minPlayersProperty = serializedObject.FindProperty("minPlayers");
        maxPlayersProperty = serializedObject.FindProperty("maxPlayers");
        playerPrefabProperty = serializedObject.FindProperty("playerPrefab");
        countdownDurationProperty = serializedObject.FindProperty("countdownDuration");
        listenerProperty = serializedObject.FindProperty("listener");
        verboseLoggingProperty = serializedObject.FindProperty("verboseLogging");
        useDebugGUIProperty = serializedObject.FindProperty("useDebugGUI");
        forceServerProperty = serializedObject.FindProperty("forceServer");
	}

    public override void OnInspectorGUI()
    {
    	serializedObject.Update();

        CaptainsMess cm = (CaptainsMess)target;

        EditorGUILayout.Space();

        EditorGUILayout.PropertyField(broadcastIdentifierProperty);
        EditorGUILayout.HelpBox("Your game will only connect to games matching the same Broadcast Identifier (eg. \'Spaceteam\' for Spaceteam games). Usually you should use the name of your own game here.", MessageType.None);

        // Check for errors
        if (cm.broadcastIdentifier == "Spaceteam" || cm.broadcastIdentifier == "")
        {
            GUI.color = Color.yellow;
            EditorGUILayout.HelpBox("Please choose a different identifier", MessageType.Warning);
            GUI.color = Color.white;
        }

        EditorGUILayout.PropertyField(minPlayersProperty);
        EditorGUILayout.HelpBox("The minimum number of players needed to start a game (eg. 2 for Spaceteam). You can change this to 1 for testing purposes.", MessageType.None);

        EditorGUILayout.PropertyField(maxPlayersProperty);
        EditorGUILayout.HelpBox("The maximum number of players supported (eg. 8 for Spaceteam).", MessageType.None);

        EditorGUILayout.PropertyField(playerPrefabProperty);
        EditorGUILayout.HelpBox("An object of this type will get created for each player that joins a game.", MessageType.None);

        // Check for errors
        if (playerPrefabProperty.objectReferenceValue == null) {
            GUI.color = Color.red;
            EditorGUILayout.HelpBox("Please choose a player prefab", MessageType.Error);
            GUI.color = Color.white;
        }

        EditorGUILayout.PropertyField(countdownDurationProperty);
        EditorGUILayout.HelpBox("The delay (in seconds) before starting a game once all players are ready.", MessageType.None);

        EditorGUILayout.PropertyField(listenerProperty);
        EditorGUILayout.HelpBox("The object that will receive messages from CaptainsMess like: JoinedLobby, StartGame, etc.", MessageType.None);

        // Check for errors
        if (listenerProperty.objectReferenceValue == null) {
            GUI.color = Color.red;
            EditorGUILayout.HelpBox("Please choose a listener", MessageType.Error);
            GUI.color = Color.white;
        }

        EditorGUILayout.PropertyField(verboseLoggingProperty);
        EditorGUILayout.HelpBox("Show lots of log messages for debugging.", MessageType.None);

        EditorGUILayout.PropertyField(useDebugGUIProperty);
        EditorGUILayout.HelpBox("Show a Debug GUI for testing connections.", MessageType.None);

        EditorGUILayout.PropertyField(forceServerProperty);
        EditorGUILayout.HelpBox("Force this device to be the server", MessageType.None);

		serializedObject.ApplyModifiedProperties();
    }
}

#endif