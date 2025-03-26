using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Firebase.Extensions;
using Unity.Netcode;
using UnityEngine;

namespace Game.State
{
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
                NetworkChessManager.Instance.SaveGameStateServerRpc();
            }
            else
            {
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
                NetworkChessManager.Instance.LoadSavedGameServerRpc(matchId, moveIndex);
                
                return true;
            }

            return await GameStateManager.Instance.LoadGameState(matchId, moveIndex);
        }
        
        public async void DemonstrateGameStateManagement()
        {
            SaveCurrentGame();
            Debug.Log("[GameStateInterface] Current game saved");
            
            await Task.Delay(1000);
            
            var games = await GetSavedGames(5);
            Debug.Log($"[GameStateInterface] Found {games.Length} saved games");
            
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