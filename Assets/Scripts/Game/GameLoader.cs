using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Game.State;

public class GameLoader : MonoBehaviour
{
    [SerializeField] private Button loadGameButton;
    [SerializeField] private InputField gameStringInputField;

    private GameStateManager gameStateManager;

    private void Start()
    {
        gameStateManager = FindObjectOfType<GameStateManager>();
        
        if (loadGameButton != null)
        {
            loadGameButton.onClick.AddListener(OnLoadGameClicked);
        }
        else
        {
            Debug.LogWarning("[GameLoader] Load Game button not assigned");
        }
    }

    public void OnLoadGameClicked()
    {
        if (gameStringInputField != null && !string.IsNullOrEmpty(gameStringInputField.text))
        {
            LoadGameFromInputField();
        }
        else
        {
            Debug.LogWarning("[GameLoader] Input field is empty or not assigned");
            StartCoroutine(LoadMostRecentGameCoroutine());
        }
    }

    private void LoadGameFromInputField()
    {
        string input = gameStringInputField.text;
        
        if (input.StartsWith("match_"))
        {
            Debug.Log($"[GameLoader] Loading game with ID: {input}");
            StartCoroutine(LoadGame(input));
        }
        else if (input.Length > 10)
        {
            Debug.Log("[GameLoader] Loading game from serialized state");
            GameManager.Instance.LoadGame(input, true);
        }
        else
        {
            Debug.LogWarning("[GameLoader] Invalid input format");
        }
    }

    private IEnumerator LoadGame(string matchId, int moveIndex = -1)
    {
        Debug.Log($"[GameLoader] Loading game: {matchId}, move: {moveIndex}");
        
        if (Unity.Netcode.NetworkManager.Singleton != null && 
            Unity.Netcode.NetworkManager.Singleton.IsHost)
        {
            NetworkChessManager.Instance.LoadSavedGameServerRpc(matchId, moveIndex);
            yield break;
        }
        
        var loadTask = gameStateManager.LoadGameState(matchId, moveIndex);
        
        while (!loadTask.IsCompleted)
        {
            yield return null;
        }
        
        if (loadTask.Result)
        {
            Debug.Log("[GameLoader] Game loaded successfully");
        }
        else
        {
            Debug.LogError("[GameLoader] Failed to load game");
        }
    }

    // Load most recent game
    private IEnumerator LoadMostRecentGameCoroutine()
    {
        Debug.Log("[GameLoader] Loading most recent game...");
        
        var getSavedGamesTask = gameStateManager.GetSavedGameStates(1);
        
        while (!getSavedGamesTask.IsCompleted)
        {
            yield return null;
        }
        
        var savedGames = getSavedGamesTask.Result;
        if (savedGames.Count > 0)
        {
            yield return StartCoroutine(LoadGame(savedGames[0].MatchId));
        }
        else
        {
            Debug.LogWarning("[GameLoader] No saved games found");
        }
    }
}