using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Firebase.Extensions;
using Unity.Netcode;
using UnityEngine;

namespace Game.State
{
    /// <summary>
    /// Interface between the GameManager and GameStateManager
    /// No UI required - all operations can be triggered programmatically
    /// </summary>
    public class GameStateInterface : MonoBehaviour
    {
        private List<GameStateManager.SavedGameInfo> savedGames = new List<GameStateManager.SavedGameInfo>();
        private bool isFetchingGames = false;

        public void SaveCurrentGame()
        {
            if (GameStateManager.Instance == null)
            {
                Debug.LogError("[GameStateInterface] GameStateManager instance not found");
                return;
            }

            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsClient)
            {
                // If in networked mode, call the server RPC
                NetworkChessManager.Instance.SaveGameStateServerRpc();
            }
            else
            {
                // If in local mode, save directly
                GameStateManager.Instance.SaveGameState("manualSave");
            }
            
            Debug.Log("[GameStateInterface] Save game requested");
        }

        public async Task<GameStateManager.SavedGameInfo[]> GetSavedGames(int limit = 10)
        {
            if (GameStateManager.Instance == null)
            {
                Debug.LogError("[GameStateInterface] GameStateManager instance not found");
                return Array.Empty<GameStateManager.SavedGameInfo>();
            }

            if (isFetchingGames)
            {
                Debug.Log("[GameStateInterface] Already fetching games, returning cached data");
                return savedGames.ToArray();
            }

            isFetchingGames = true;
            
            try
            {
                savedGames = await GameStateManager.Instance.GetSavedGameStates(limit);
                return savedGames.ToArray();
            }
            finally
            {
                isFetchingGames = false;
            }
        }

        public async Task<bool> LoadGame(string matchId, int moveIndex = -1)
        {
            if (GameStateManager.Instance == null)
            {
                Debug.LogError("[GameStateInterface] GameStateManager instance not found");
                return false;
            }

            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsHost)
            {
                // If in networked mode as a client, request load via server RPC
                NetworkChessManager.Instance.LoadSavedGameServerRpc(matchId, moveIndex);
                
                // We don't know the result immediately, but return true to indicate request was sent
                return true;
            }
            else
            {
                // If in local mode or as host, load directly
                return await GameStateManager.Instance.LoadGameState(matchId, moveIndex);
            }
        }

        // Example of how to use these methods programmatically
        public async void DemonstrateGameStateManagement()
        {
            // 1. Save the current game
            SaveCurrentGame();
            Debug.Log("[GameStateInterface] Current game saved");
            
            // 2. Wait a moment
            await Task.Delay(1000);
            
            // 3. Get list of saved games
            var games = await GetSavedGames(5);
            Debug.Log($"[GameStateInterface] Found {games.Length} saved games");
            
            // 4. If we have any saved games, load the most recent one
            if (games.Length > 0)
            {
                var mostRecent = games[0];
                Debug.Log($"[GameStateInterface] Loading game: {mostRecent.DisplayName}");
                
                bool success = await LoadGame(mostRecent.MatchId);
                Debug.Log($"[GameStateInterface] Load result: {(success ? "Success" : "Failed")}");
            }
            else
            {
                Debug.Log("[GameStateInterface] No saved games found to load");
            }
        }
    }
}