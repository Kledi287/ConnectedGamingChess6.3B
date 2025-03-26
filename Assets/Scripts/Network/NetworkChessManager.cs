using System;
using System.Collections.Generic;
using Game.DLC;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using ParrelSync;
using UnityChess;

public class NetworkChessManager : NetworkBehaviour
{
    public static NetworkChessManager Instance;

    [SerializeField] private Button hostButton; 
    [SerializeField] private Button joinButton; 
    
    private string currentWhiteSkin = "";
    private string currentBlackSkin = "";

    private Dictionary<string, ulong> persistentPlayers = new Dictionary<string, ulong>();

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (hostButton != null) hostButton.onClick.AddListener(StartHost);
        if (joinButton != null) joinButton.onClick.AddListener(StartClient);

        NetworkManager.Singleton.OnClientDisconnectCallback += (clientId) =>
        {
            Debug.LogError($"Player {clientId} disconnected unexpectedly!");
        };

        NetworkManager.Singleton.OnTransportFailure += () =>
        {
            Debug.LogError("Transport Failure! Check UnityTransport configuration.");
        };
        
        if (!PlayerPrefs.HasKey("AutoApplySkins"))
        {
            PlayerPrefs.SetInt("AutoApplySkins", 0);
            PlayerPrefs.Save();
        }
        
        if (PlayerPrefs.HasKey("CurrentWhiteSkin") || PlayerPrefs.HasKey("CurrentBlackSkin"))
        {
            Debug.Log("[NetworkChessManager] Clearing previously saved skins to avoid automatic application");
            PlayerPrefs.DeleteKey("CurrentWhiteSkin");
            PlayerPrefs.DeleteKey("CurrentBlackSkin");
            PlayerPrefs.Save();
        }
    }

    public void StartHost()
    {
        if (NetworkManager.Singleton.IsServer || NetworkManager.Singleton.IsClient)
        {
            Debug.LogError("Already running as server or client.");
            return;
        }
        bool started = NetworkManager.Singleton.StartHost();
        Debug.Log($"Host Started: {started}");
    }

    public void StartClient()
    {
        if (NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsServer)
        {
            Debug.LogError("Already running as client or server.");
            return;
        }
        bool started = NetworkManager.Singleton.StartClient();
        Debug.Log($"Client started successfully: {started}");
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
            
            if (PlayerPrefs.GetInt("AutoApplySkins", 0) == 1)
            {
                LoadSavedSkins();
            }
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        NetworkPlayer player = FindPlayerByClientId(clientId);
        if (player == null) return;

        string uniqueId = player.PlayerUniqueID.Value.ToString();

        if (string.IsNullOrEmpty(uniqueId))
        {
            Debug.Log($"Player {clientId} connected but has no unique ID yet. Waiting for OnValueChanged...");
            // We do NOT do anything else here. We'll handle them in OnPlayerUniqueIDChanged.
            return;
        }

        // If the ID is already set, handle them immediately
        HandlePlayerIDSet(clientId, uniqueId);
    }

    /// <summary>
    /// Called by NetworkPlayer when a player's ID is finally set (non-empty).
    /// </summary>
    public void HandlePlayerIDSet(ulong clientId, string uniqueId)
    {
        if (persistentPlayers.ContainsKey(uniqueId))
        {
            Debug.Log($"Player {clientId} (Unique ID: {uniqueId}) has reconnected.");
            SendBoardToReconnectingClient(clientId);
        }
        else
        {
            persistentPlayers[uniqueId] = clientId;
            Debug.Log($"Player {clientId} (Unique ID: {uniqueId}) connected for the first time.");
            SendBoardToReconnectingClient(clientId);
        }
    }
    
    private void OnClientDisconnected(ulong clientId)
    {
        Debug.Log($"[Server] Player {clientId} disconnected.");

        // Optionally do NOT remove them from dictionary if you want to preserve ID => client mapping
        // This ensures next time they rejoin with the same ID, we see "reconnected."
        /*
        var toRemove = "";
        foreach (var kvp in persistentPlayers)
        {
            if (kvp.Value == clientId)
            {
                toRemove = kvp.Key;
                break;
            }
        }
        if (toRemove != "")
            persistentPlayers.Remove(toRemove);
        */

        // Also do NOT shut down if you want to allow rejoin
        /*
        if (persistentPlayers.Count == 0 && NetworkManager.Singleton.IsServer)
        {
            Debug.Log("[Server] All players disconnected. Shutting down server...");
            NetworkManager.Singleton.Shutdown();
        }
        */
    }


    private NetworkPlayer FindPlayerByClientId(ulong clientId)
    {
        foreach (var np in FindObjectsOfType<NetworkPlayer>())
        {
            if (np.OwnerClientId == clientId)
                return np;
        }
        return null;
    }

    // Send the current board to a specific client
    private void SendBoardToReconnectingClient(ulong clientId)
    {
        string fenOrPgn = GameManager.Instance.SerializeGame();

        var sendParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new[] { clientId }
            }
        };

        // Send the board state
        SyncBoardToOneClientRpc(fenOrPgn, sendParams);
    
        // Send current skin states if they exist
        if (!string.IsNullOrEmpty(currentWhiteSkin))
        {
            SyncSkinClientRpc(true, currentWhiteSkin, sendParams);
        }
    
        if (!string.IsNullOrEmpty(currentBlackSkin))
        {
            SyncSkinClientRpc(false, currentBlackSkin, sendParams);
        }
    }
    
    [ClientRpc]
    public void SyncBoardToOneClientRpc(string serializedBoard, ClientRpcParams clientRpcParams = default)
    {
        Debug.Log($"[ClientRpc] Loading board for reconnecting/new client.");

        // If your serializer only has the start position, you must store all moves or final position
        // Then jump to the final half-move after loading:
        GameManager.Instance.LoadGame(serializedBoard, true);

        BoardManager.Instance.EnsureOnlyPiecesOfSideAreEnabled(GameManager.Instance.SideToMove);
    }
    
    [ServerRpc(RequireOwnership = false)]
    public void LoadSavedGameServerRpc(string matchId, int moveIndex, ServerRpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;
        Debug.Log($"[Server] Client {clientId} requested to load game: {matchId} at move {moveIndex}");
    
        // Only allow the host to load saved games
        if (clientId != NetworkManager.Singleton.LocalClientId)
        {
            Debug.LogWarning($"[Server] Client {clientId} attempted to load a game but is not the host");
            return;
        }
    
        // Use the GameStateManager to load the saved state - with corrected namespace
        var gameStateManager = FindObjectOfType<Game.State.GameStateManager>();
        if (gameStateManager != null)
        {
            gameStateManager.LoadGameState(matchId, moveIndex)
                .ContinueWith(task =>
                {
                    if (!task.Result)
                    {
                        Debug.LogError($"[Server] Failed to load game: {matchId}");
                    }
                });
        }
        else
        {
            Debug.LogError("[Server] GameStateManager not found - cannot load game state");
        }
    }
    
    [ServerRpc(RequireOwnership = false)]
    public void SyncSkinServerRpc(bool isWhite, string skinPath, ServerRpcParams rpcParams = default)
    {
        try
        {
            ulong clientId = rpcParams.Receive.SenderClientId;
        
            // IMPORTANT FIX: Don't check if player.IsWhite.Value matches isWhite
            // The player might be applying skins to either white or black pieces
            // regardless of which side they're playing as
            NetworkPlayer player = FindPlayerByClientId(clientId);
            if (player == null)
            {
                Debug.LogWarning($"[Server] Player {clientId} not found when setting skin");
                return;
            }
        
            // Store the skin path for this side based on the piece color (isWhite)
            if (isWhite)
                currentWhiteSkin = skinPath;
            else
                currentBlackSkin = skinPath;
        
            // Save the skins to PlayerPrefs for persistence
            SaveCurrentSkins();
        
            Debug.Log($"[Server] Player {clientId} applied skin: {skinPath} for {(isWhite ? "white" : "black")} pieces");
        
            // Broadcast to all clients
            SyncSkinClientRpc(isWhite, skinPath);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Server] Error in SyncSkinServerRpc: {ex.Message}\n{ex.StackTrace}");
        }
    }
    
    [ClientRpc]
    public void SyncSkinClientRpc(bool isWhite, string skinPath, ClientRpcParams clientRpcParams = default)
    {
        Debug.Log($"[Client] Received skin update: {skinPath} for {(isWhite ? "white" : "black")} pieces");
    
        // Save skin path to PlayerPrefs for persistence across sessions
        if (isWhite)
        {
            PlayerPrefs.SetString("CurrentWhiteSkin", skinPath);
        }
        else
        {
            PlayerPrefs.SetString("CurrentBlackSkin", skinPath);
        }
        PlayerPrefs.Save();
    
        Side pieceSide = isWhite ? UnityChess.Side.White : UnityChess.Side.Black;
    
        List<GameObject> pieces = new List<GameObject>();
        VisualPiece[] allVisualPieces = BoardManager.Instance.GetComponentsInChildren<VisualPiece>(true);
    
        foreach (VisualPiece vp in allVisualPieces)
        {
            if (vp.PieceColor == pieceSide)
            {
                pieces.Add(vp.gameObject);
            }
        }
    
        bool isLocalPlayerSkin = NetworkPlayer.LocalInstance != null && 
                                 NetworkPlayer.LocalInstance.IsWhite.Value == isWhite && 
                                 NetworkPlayer.LocalInstance.HasRecentlyChangedSkin && 
                                 NetworkPlayer.LocalInstance.LastChangedSkinPath == skinPath;

        if (isLocalPlayerSkin)
        {
            NetworkPlayer.LocalInstance.HasRecentlyChangedSkin = false;
        }
        else
        {
            ApplySkinToSide(skinPath, pieces);
        }
    
        // Log the skin application in Analytics if we're the player who initiated it
        if (Game.Analytics.FirebaseAnalyticsManager.Instance != null && 
            (isLocalPlayerSkin || NetworkPlayer.LocalInstance == null))
        {
            Game.Analytics.FirebaseAnalyticsManager.Instance.LogSkinApplied(skinPath, isWhite);
        }
    }
    
    private async void ApplySkinToSide(string skinPath, List<GameObject> pieces)
    {
        try
        {
            // Create a new, filtered list that only contains valid (non-null) GameObjects
            List<GameObject> validPieces = new List<GameObject>();
            
            if (pieces != null)
            {
                foreach (var piece in pieces)
                {
                    if (piece != null)
                    {
                        validPieces.Add(piece);
                    }
                }
            }
            
            // If we have no valid pieces, find them again
            if (validPieces.Count == 0)
            {
                Debug.Log($"[NetworkChessManager] No valid pieces found, getting fresh references");
                
                // Get all pieces of the right color from the current board state
                bool isWhiteSkin = skinPath.Contains("CurrentWhiteSkin");
                Side pieceSide = isWhiteSkin ? UnityChess.Side.White : UnityChess.Side.Black;
                
                VisualPiece[] allVisualPieces = BoardManager.Instance.GetComponentsInChildren<VisualPiece>(true);
                foreach (VisualPiece vp in allVisualPieces)
                {
                    if (vp != null && vp.PieceColor == pieceSide)
                    {
                        validPieces.Add(vp.gameObject);
                    }
                }
            }
            
            // If we still have no pieces, log and exit
            if (validPieces.Count == 0)
            {
                Debug.LogWarning($"[NetworkChessManager] Couldn't find any valid {(skinPath.Contains("White") ? "white" : "black")} pieces to apply skin to");
                return;
            }
            
            // Download and apply the skin
            Texture2D downloadedTex = await DLCManager.Instance.DownloadSkinAsync(skinPath);
            if (downloadedTex != null)
            {
                Debug.Log($"[NetworkChessManager] Applying skin {skinPath} to {validPieces.Count} pieces");
                DLCManager.Instance.ApplySkinToAllPieces(validPieces, downloadedTex);
            }
            else
            {
                Debug.LogError($"[NetworkChessManager] Failed to download skin: {skinPath}");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[NetworkChessManager] Error in ApplySkinToSide: {ex.Message}\n{ex.StackTrace}");
        }
    }
    
    [ServerRpc(RequireOwnership = false)]
    public void SaveGameStateServerRpc(ServerRpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;
    
        // Only allow the host to save games
        if (clientId != NetworkManager.Singleton.LocalClientId)
        {
            Debug.LogWarning($"[Server] Client {clientId} attempted to save a game but is not the host");
            return;
        }
    
        // Use the GameStateManager to save the current state - with corrected namespace
        var gameStateManager = FindObjectOfType<Game.State.GameStateManager>();
        if (gameStateManager != null)
        {
            gameStateManager.SaveGameState("manualSave");
            Debug.Log("[Server] Game state saved manually");
        }
        else
        {
            Debug.LogError("[Server] GameStateManager not found - cannot save game state");
        }
    }
    
    [ServerRpc(RequireOwnership = false)]
    public void RequestMoveServerRpc(Vector2Int from, Vector2Int to, ServerRpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;
        NetworkPlayer player = FindPlayerByClientId(clientId);
        if (player == null) return;

        if (!player.IsMyTurn())
        {
            ResetPieceClientRpc(from, clientId);
            return;
        }

        Square startSquare = new Square(from.x, from.y);
        Square endSquare   = new Square(to.x, to.y);

        if (!GameManager.Instance.game.TryGetLegalMove(startSquare, endSquare, out Movement move))
        {
            ResetPieceClientRpc(from, clientId);
            return;
        }

        bool executed = GameManager.Instance.TryExecuteMove(move);
        if (!executed)
        {
            ResetPieceClientRpc(from, clientId);
            return;
        }

        TurnManager.Instance.EndTurnServerRpc();

        Side sideToMove = GameManager.Instance.SideToMove;
        UpdateBoardStateClientRpc(sideToMove);

        UpdateBoardClientRpc(from, to);
    }
    
    [ServerRpc(RequireOwnership = false)]
    public void ResignServerRpc(ulong resigningClientId)
    {
        NetworkPlayer player = FindPlayerByClientId(resigningClientId);
        if (player == null) return;

        bool isWhite = player.IsWhite.Value;
        string resigningSide = isWhite ? "White" : "Black";
        string winningSide = isWhite ? "Black" : "White";

        AnnounceOutcomeServerRpc($"{winningSide} wins by resignation! ({resigningSide} resigned)");
    }
    
    [ServerRpc(RequireOwnership = false)]
    public void AnnounceOutcomeServerRpc(string outcomeMessage)
    {
        // This calls a ClientRpc to show the outcome
        AnnounceOutcomeClientRpc(outcomeMessage);
    }
    
    [ClientRpc]
    private void AnnounceOutcomeClientRpc(string outcomeMessage)
    {
        Debug.Log($"[ClientRpc] AnnounceOutcome => {outcomeMessage}");
        UIManager.Instance.ShowOutcomeText(outcomeMessage);
        BoardManager.Instance.SetActiveAllPieces(false);
        UIManager.Instance.DisableResignButton();
    }
  
    [ClientRpc]
    private void UpdateBoardClientRpc(Vector2Int from, Vector2Int to)
    {
        BoardManager.Instance.TryDestroyVisualPiece(new Square(to.x, to.y));
        GameObject pieceGO = BoardManager.Instance.GetPieceGOAtPosition(new Square(from.x, from.y));
        if (pieceGO != null)
        {
            Transform squareTransform = BoardManager.Instance.GetSquareGOByPosition(new Square(to.x, to.y)).transform;
            pieceGO.transform.parent = squareTransform;
            pieceGO.transform.localPosition = Vector3.zero;
            
            VisualPiece visualPiece = pieceGO.GetComponent<VisualPiece>();
            if (visualPiece != null)
            {
                bool isWhite = visualPiece.PieceColor == UnityChess.Side.White;
                string currentSkin = isWhite ? currentWhiteSkin : currentBlackSkin;
                
                if (!string.IsNullOrEmpty(currentSkin))
                {
                    ReapplySkinAfterMove(pieceGO, currentSkin);
                }
            }
        }
    }
    
    private async void ReapplySkinAfterMove(GameObject pieceGO, string skinPath)
    {
        Texture2D downloadedTex = await DLCManager.Instance.DownloadSkinAsync(skinPath);
        if (downloadedTex != null)
        {
            DLCManager.Instance.ApplySkinToPiece(pieceGO, downloadedTex);
        }
    }

    [ClientRpc]
    private void UpdateBoardStateClientRpc(Side sideToMove)
    {
        BoardManager.Instance.EnsureOnlyPiecesOfSideAreEnabled(sideToMove);
    }

    [ClientRpc]
    private void ResetPieceClientRpc(Vector2Int from, ulong targetClientId, ClientRpcParams clientRpcParams = default)
    {
        if (NetworkManager.Singleton.LocalClientId != targetClientId) return;

        GameObject pieceGO = BoardManager.Instance.GetPieceGOAtPosition(new Square(from.x, from.y));
        if (pieceGO != null)
        {
            pieceGO.transform.position = pieceGO.transform.parent.position;
        }
    }
    
    private void SaveCurrentSkins()
    {
        if (!string.IsNullOrEmpty(currentWhiteSkin))
        {
            PlayerPrefs.SetString("CurrentWhiteSkin", currentWhiteSkin);
        }
    
        if (!string.IsNullOrEmpty(currentBlackSkin))
        {
            PlayerPrefs.SetString("CurrentBlackSkin", currentBlackSkin);
        }
    
        PlayerPrefs.Save();
    }
    
    private void LoadSavedSkins()
    {
        if (PlayerPrefs.GetInt("AutoApplySkins", 0) != 1)
        {
            Debug.Log("[NetworkChessManager] Skipping automatic skin loading (disabled in preferences)");
            return;
        }

        currentWhiteSkin = PlayerPrefs.GetString("CurrentWhiteSkin", "");
        currentBlackSkin = PlayerPrefs.GetString("CurrentBlackSkin", "");
    
        Debug.Log($"[NetworkChessManager] Loaded saved skins - White: {currentWhiteSkin}, Black: {currentBlackSkin}");
    
        // Only apply if non-empty and if we're actually in a game
        if (!string.IsNullOrEmpty(currentWhiteSkin) && BoardManager.Instance != null)
        {
            Debug.Log($"[NetworkChessManager] Applying saved white skin: {currentWhiteSkin}");
            ApplyLoadedSkin(true, currentWhiteSkin);
        }
    
        if (!string.IsNullOrEmpty(currentBlackSkin) && BoardManager.Instance != null)
        {
            Debug.Log($"[NetworkChessManager] Applying saved black skin: {currentBlackSkin}");
            ApplyLoadedSkin(false, currentBlackSkin);
        }
    }
    
    private void ApplyLoadedSkin(bool isWhite, string skinPath)
    {
        if (!IsServer && !NetworkManager.Singleton.IsHost) return;
    
        List<GameObject> pieces = isWhite ? 
            BoardManager.Instance.GetAllWhitePieces() : 
            BoardManager.Instance.GetAllBlackPieces();
    
        ApplySkinToSide(skinPath, pieces);
        
        if (NetworkManager.Singleton.IsServer)
        {
            SyncSkinClientRpc(isWhite, skinPath);
        }
    }
    
    [ClientRpc]
    public void SyncTurnStateClientRpc(bool isWhiteTurn, ClientRpcParams clientRpcParams = default)
    {
        Debug.Log($"[NetworkChessManager] Syncing turn state: WhiteTurn = {isWhiteTurn}");
        
        // Check if we're the server - only the server can modify NetworkVariables
        if (NetworkManager.Singleton.IsServer)
        {
            // Update TurnManager's NetworkVariable directly
            if (TurnManager.Instance != null)
            {
                TurnManager.Instance.IsWhiteTurn.Value = isWhiteTurn;
            }
        }
        else
        {
            // For clients, we need to request the server to make this change
            // Instead of modifying the NetworkVariable directly, just use the value for visual updates
            Debug.Log($"[Client] Received turn state update: WhiteTurn = {isWhiteTurn}");
        }
        
        // These operations are safe for both client and server - they don't modify NetworkVariables
        
        // Update BoardManager to enable the correct pieces
        if (BoardManager.Instance != null)
        {
            // This will enable pieces for the side whose turn it is
            Side sideToMove = isWhiteTurn ? UnityChess.Side.White : UnityChess.Side.Black;
            BoardManager.Instance.EnsureOnlyPiecesOfSideAreEnabled(sideToMove);
            
            // Reset the visual state of all pieces
            foreach (var visualPiece in BoardManager.Instance.GetComponentsInChildren<VisualPiece>())
            {
                if (visualPiece != null && visualPiece.transform.parent != null)
                {
                    // Reset position to parent square's position
                    visualPiece.transform.position = visualPiece.transform.parent.position;
                }
            }
        }
        
        // Update UI to show whose turn it is (this doesn't modify NetworkVariables)
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ValidateIndicators();
        }
        
        Debug.Log($"[{(NetworkManager.Singleton.IsServer ? "Server" : "Client")}] Turn sync complete. Active player: {(isWhiteTurn ? "White" : "Black")}");
    }
}
