using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Firebase.Extensions;
using Firebase.Firestore;
using Unity.Netcode;
using UnityEngine;

namespace Game.State
{
    public class GameStateManager : MonoBehaviour
    {
        public static GameStateManager Instance;

        private FirebaseFirestore firestoreDb;
        private string currentMatchId;
        private DateTime matchStartTime;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            InitializeFirestore();
        }

        private void Start()
        {
            // Subscribe to game events
            GameManager.NewGameStartedEvent += OnNewGameStarted;
            GameManager.GameEndedEvent += OnGameEnded;
            GameManager.MoveExecutedEvent += OnMoveExecuted;
        }

        private void OnDestroy()
        {
            // Unsubscribe from game events
            GameManager.NewGameStartedEvent -= OnNewGameStarted;
            GameManager.GameEndedEvent -= OnGameEnded;
            GameManager.MoveExecutedEvent -= OnMoveExecuted;
        }

        private void InitializeFirestore()
        {
            firestoreDb = FirebaseFirestore.DefaultInstance;
            if (firestoreDb != null)
            {
                Debug.Log("[GameStateManager] Firestore initialized successfully");
            }
            else
            {
                Debug.LogError("[GameStateManager] Failed to initialize Firestore");
            }
        }

        #region Event Handlers

        private void OnNewGameStarted()
        {
            // Generate a unique match ID
            currentMatchId = GenerateMatchId();
            matchStartTime = DateTime.UtcNow;

            // Track match start in Analytics (this can happen on client or server)
            bool isMultiplayer = NetworkManager.Singleton != null && 
                                (NetworkManager.Singleton.IsServer || NetworkManager.Singleton.IsClient);
            
            if (Game.Analytics.FirebaseAnalyticsManager.Instance != null)
            {
                Game.Analytics.FirebaseAnalyticsManager.Instance.LogMatchStart(isMultiplayer, currentMatchId);
            }

            // Only save game state if we're on the server
            if (NetworkManager.Singleton == null || NetworkManager.Singleton.IsServer)
            {
                SaveGameState("initialState");
            }
            
            Debug.Log($"[GameStateManager] New game started with ID: {currentMatchId}");
        }

        private void OnGameEnded()
        {
            if (string.IsNullOrEmpty(currentMatchId)) return;

            // Get match details
            int totalMoves = GameManager.Instance.LatestHalfMoveIndex;
            TimeSpan duration = DateTime.UtcNow - matchStartTime;
            int durationSeconds = (int)duration.TotalSeconds;

            // Determine outcome
            string outcome = "unknown";
            
            // Check if TryGetCurrent returns true, which means there is a current half-move
            if (GameManager.Instance.HalfMoveTimeline.TryGetCurrent(out UnityChess.HalfMove lastMove))
            {
                // Now we can check properties on lastMove
                if (lastMove.CausedCheckmate)
                {
                    outcome = $"{lastMove.Piece.Owner}_win";
                }
                else if (lastMove.CausedStalemate)
                {
                    outcome = "draw_stalemate";
                }
            }

            // Only save game state if we're on the server
            if (NetworkManager.Singleton == null || NetworkManager.Singleton.IsServer)
            {
                SaveGameState("endState");
            }

            // Track match end in Analytics (this can happen on client or server)
            bool isMultiplayer = NetworkManager.Singleton != null && 
                                (NetworkManager.Singleton.IsServer || NetworkManager.Singleton.IsClient);
            
            if (Game.Analytics.FirebaseAnalyticsManager.Instance != null)
            {
                Game.Analytics.FirebaseAnalyticsManager.Instance.LogMatchEnd(
                    isMultiplayer, 
                    currentMatchId, 
                    outcome, 
                    totalMoves, 
                    durationSeconds
                );
            }
            
            Debug.Log($"[GameStateManager] Game ended - ID: {currentMatchId}, Outcome: {outcome}");
        }

        private void OnMoveExecuted()
        {
            // Only save game state if we're on the server
            if (NetworkManager.Singleton == null || NetworkManager.Singleton.IsServer)
            {
                // Save game state after every few moves to avoid excessive writes
                if (GameManager.Instance.LatestHalfMoveIndex % 5 == 0)
                {
                    SaveGameState("midgameState");
                }
            }
        }

        #endregion

        #region Game State Management

        // Generate a unique match ID
        private string GenerateMatchId()
        {
            return $"match_{DateTime.UtcNow.Ticks}_{UnityEngine.Random.Range(1000, 9999)}";
        }

        // Save the current game state to Firestore
        public void SaveGameState(string stateType)
        {
            // Only the server should save game states to Firestore
            if (NetworkManager.Singleton != null && !NetworkManager.Singleton.IsServer)
            {
                Debug.Log("[GameStateManager] Skipping game state save - not running on server");
                return;
            }

            if (firestoreDb == null || string.IsNullOrEmpty(currentMatchId))
            {
                Debug.LogWarning("[GameStateManager] Cannot save game state: Firestore not initialized or no match ID");
                return;
            }

            try
            {
                string serializedGame = GameManager.Instance.SerializeGame();
                
                // Create a dictionary with game state data
                Dictionary<string, object> gameStateData = new Dictionary<string, object>
                {
                    { "matchId", currentMatchId },
                    { "gameState", serializedGame },
                    { "stateType", stateType },
                    { "moveIndex", GameManager.Instance.LatestHalfMoveIndex },
                    { "timestamp", Timestamp.FromDateTime(DateTime.UtcNow) },
                    // Only access ConnectedClientsList on server
                    { "playerCount", (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer) 
                        ? NetworkManager.Singleton.ConnectedClientsList.Count 
                        : 1 }
                };

                // Add player-specific info if this is a multiplayer game on the server
                if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
                {
                    Dictionary<string, object> players = new Dictionary<string, object>();
                    
                    foreach (var player in FindObjectsOfType<NetworkPlayer>())
                    {
                        string playerId = player.PlayerUniqueID.Value.ToString();
                        if (!string.IsNullOrEmpty(playerId))
                        {
                            players[playerId] = new Dictionary<string, object>
                            {
                                { "clientId", player.OwnerClientId },
                                { "isWhite", player.IsWhite.Value }
                            };
                        }
                    }
                    
                    gameStateData["players"] = players;
                }

                // Add current skins info
                if (NetworkChessManager.Instance != null)
                {
                    Dictionary<string, string> skinInfo = new Dictionary<string, string>
                    {
                        { "whiteSkin", PlayerPrefs.GetString("CurrentWhiteSkin", "") },
                        { "blackSkin", PlayerPrefs.GetString("CurrentBlackSkin", "") }
                    };
                    
                    gameStateData["skins"] = skinInfo;
                }

                // Save to Firestore
                firestoreDb.Collection("game_states").Document($"{currentMatchId}_{GameManager.Instance.LatestHalfMoveIndex}")
                    .SetAsync(gameStateData)
                    .ContinueWithOnMainThread(task =>
                    {
                        if (task.IsFaulted)
                        {
                            if (task.Exception is AggregateException aggEx)
                            {
                                foreach (var ex in aggEx.InnerExceptions)
                                {
                                    Debug.LogError($"[GameStateManager] Error detail: {ex.GetType().Name}: {ex.Message}");
                                }
                            }
                            Debug.LogError($"[GameStateManager] Error saving game state: {task.Exception}");
                        }
                        else
                        {
                            Debug.Log($"[GameStateManager] Game state saved successfully: {stateType} at move {GameManager.Instance.LatestHalfMoveIndex}");
                        }
                    });
            }
            catch (Exception e)
            {
                Debug.LogError($"[GameStateManager] Error saving game state: {e.Message}");
            }
        }

        // Load a game state from a specific match and restore it
        public async Task<bool> LoadGameState(string matchId, int moveIndex = -1)
        {
            if (firestoreDb == null)
            {
                Debug.LogWarning("[GameStateManager] Cannot load game state: Firestore not initialized");
                return false;
            }

            try
            {
                // Use a simpler query that doesn't require a complex index
                Query query = firestoreDb.Collection("game_states")
                    .WhereEqualTo("matchId", matchId);
                    
                QuerySnapshot snapshot = await query.GetSnapshotAsync();

                if (snapshot.Count == 0)
                {
                    Debug.LogWarning($"[GameStateManager] No game state found for match: {matchId}");
                    return false;
                }

                DocumentSnapshot document;
                
                // If a specific move index is provided, find that one
                if (moveIndex >= 0)
                {
                    document = snapshot.Documents.FirstOrDefault(doc => 
                        doc.GetValue<long>("moveIndex") == moveIndex);
                        
                    if (document == null)
                    {
                        Debug.LogWarning($"[GameStateManager] Move {moveIndex} not found for match: {matchId}");
                        return false;
                    }
                }
                // Otherwise, find the latest move manually
                else
                {
                    document = snapshot.Documents
                        .OrderByDescending(doc => doc.GetValue<long>("moveIndex"))
                        .FirstOrDefault();
                }
                
                if (document != null && document.Exists)
                {
                    // Get the serialized game state
                    string serializedGame = document.GetValue<string>("gameState");
                    
                    // Load the game state locally first to determine whose turn it is
                    GameManager.Instance.LoadGame(serializedGame, true);
                    
                    // Determine whose turn it is after loading
                    bool isWhiteTurn = GameManager.Instance.SideToMove == UnityChess.Side.White;
                    
                    // If this is the server in a networked game, sync to all clients
                    if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
                    {
                        try
                        {
                            // Sync the board state first
                            NetworkChessManager.Instance.SyncBoardToOneClientRpc(serializedGame);
                            
                            // Wait a moment for the board to initialize on clients
                            await Task.Delay(300);
                            
                            // First set the turn state directly on the server
                            if (TurnManager.Instance != null)
                            {
                                TurnManager.Instance.IsWhiteTurn.Value = isWhiteTurn;
                                Debug.Log($"[Server] Set turn state directly: WhiteTurn = {isWhiteTurn}");
                            }
                            
                            // Now notify clients about the turn state (for visual updates)
                            NetworkChessManager.Instance.SyncTurnStateClientRpc(isWhiteTurn);
                            
                            // Wait a bit more before applying skins
                            await Task.Delay(200);
                            
                            // Sync skin information if available
                            if (document.TryGetValue<Dictionary<string, object>>("skins", out var skinData))
                            {
                                if (skinData.TryGetValue("whiteSkin", out object whiteSkinObj) && whiteSkinObj is string whiteSkin && !string.IsNullOrEmpty(whiteSkin))
                                {
                                    NetworkChessManager.Instance.SyncSkinClientRpc(true, whiteSkin);
                                }
                                
                                if (skinData.TryGetValue("blackSkin", out object blackSkinObj) && blackSkinObj is string blackSkin && !string.IsNullOrEmpty(blackSkin))
                                {
                                    NetworkChessManager.Instance.SyncSkinClientRpc(false, blackSkin);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.LogError($"[GameStateManager] Error syncing game state to clients: {ex.Message}");
                        }
                    }
                    // Client already loaded the state locally above
                    
                    // Update the current match ID to the loaded match
                    currentMatchId = matchId;
                    
                    Debug.Log($"[GameStateManager] Game state loaded successfully for match: {matchId}");
                    return true;
                }
                else
                {
                    Debug.LogWarning($"[GameStateManager] Game state document not found for match: {matchId}");
                    return false;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[GameStateManager] Error loading game state: {e.Message}");
                return false;
            }
        }

        // Get a list of saved game states for display
        public async Task<List<SavedGameInfo>> GetSavedGameStates(int limit = 10)
        {
            List<SavedGameInfo> savedGames = new List<SavedGameInfo>();
            
            if (firestoreDb == null)
            {
                Debug.LogWarning("[GameStateManager] Cannot get saved games: Firestore not initialized");
                return savedGames;
            }

            try
            {
                // Use a simpler query that doesn't require a complex index
                Query query = firestoreDb.Collection("game_states");
                    
                QuerySnapshot snapshot = await query.GetSnapshotAsync();
                
                // Process results manually on the client side
                Dictionary<string, SavedGameInfo> latestMoveByMatch = new Dictionary<string, SavedGameInfo>();
                
                foreach (DocumentSnapshot document in snapshot.Documents)
                {
                    if (document.Exists)
                    {
                        string matchId = document.GetValue<string>("matchId");
                        long moveIndex = document.GetValue<long>("moveIndex");
                        string stateType = document.GetValue<string>("stateType");
                        
                        // We're looking for endState games or the latest move for each match
                        if (stateType == "endState" || !latestMoveByMatch.ContainsKey(matchId) || 
                            latestMoveByMatch[matchId].MoveCount < moveIndex)
                        {
                            SavedGameInfo gameInfo = new SavedGameInfo
                            {
                                MatchId = matchId,
                                MoveCount = moveIndex,
                                Timestamp = document.GetValue<Timestamp>("timestamp").ToDateTime(),
                                PlayerCount = document.ContainsField("playerCount") ? document.GetValue<long>("playerCount") : 1,
                                StateType = stateType
                            };
                            
                            latestMoveByMatch[matchId] = gameInfo;
                        }
                    }
                }
                
                // Convert dictionary to list and sort by timestamp
                savedGames = latestMoveByMatch.Values.ToList();
                savedGames.Sort((a, b) => b.Timestamp.CompareTo(a.Timestamp));
                
                // Limit the number of results
                if (savedGames.Count > limit)
                {
                    savedGames = savedGames.Take(limit).ToList();
                }

                Debug.Log($"[GameStateManager] Retrieved {savedGames.Count} saved games");
                return savedGames;
            }
            catch (Exception e)
            {
                Debug.LogError($"[GameStateManager] Error getting saved games: {e.Message}");
                return savedGames;
            }
        }

        #endregion

        // Class to represent saved game information
        public class SavedGameInfo
        {
            public string MatchId { get; set; }
            public long MoveCount { get; set; }
            public DateTime Timestamp { get; set; }
            public long PlayerCount { get; set; }
            public string StateType { get; set; }
            
            public string DisplayName => $"Game {MatchId.Substring(Math.Max(0, MatchId.Length - 8))} - {Timestamp.ToString("MM/dd HH:mm")} - {MoveCount} moves";
        }
    }
}