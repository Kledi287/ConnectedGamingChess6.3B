using System.Collections.Generic;
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
            Debug.LogError($"❌ Player {clientId} disconnected unexpectedly!");
        };

        NetworkManager.Singleton.OnTransportFailure += () =>
        {
            Debug.LogError("🚨 Transport Failure! Check UnityTransport configuration.");
        };
    }

    public void StartHost()
    {
        if (NetworkManager.Singleton.IsServer || NetworkManager.Singleton.IsClient)
        {
            Debug.LogError("Already running as server or client.");
            return;
        }
        bool started = NetworkManager.Singleton.StartHost();
        Debug.Log($"🎯 Host Started: {started}");
    }

    public void StartClient()
    {
        if (NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsServer)
        {
            Debug.LogError("Already running as client or server.");
            return;
        }
        bool started = NetworkManager.Singleton.StartClient();
        Debug.Log($"🎯 Client started successfully: {started}");
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        NetworkPlayer player = FindPlayerByClientId(clientId);
        if (player == null) return;

        string uniqueId = player.PlayerUniqueID.Value.ToString();

        if (string.IsNullOrEmpty(uniqueId))
        {
            Debug.Log($"⚠ Player {clientId} connected but has no unique ID yet. Waiting for OnValueChanged...");
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
            Debug.Log($"🔄 Player {clientId} (Unique ID: {uniqueId}) has reconnected.");
            SendBoardToReconnectingClient(clientId);
        }
        else
        {
            persistentPlayers[uniqueId] = clientId;
            Debug.Log($"✅ Player {clientId} (Unique ID: {uniqueId}) connected for the first time.");
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

        SyncBoardToOneClientRpc(fenOrPgn, sendParams);
    }
    
    [ClientRpc]
    private void SyncBoardToOneClientRpc(string serializedBoard, ClientRpcParams clientRpcParams = default)
    {
        Debug.Log($"[ClientRpc] loading board for reconnecting/new client.");

        // If your serializer only has the start position, you must store all moves or final position
        // Then jump to the final half-move after loading:
        GameManager.Instance.LoadGame(serializedBoard, true);

        BoardManager.Instance.EnsureOnlyPiecesOfSideAreEnabled(GameManager.Instance.SideToMove);
    }


    // The existing code for handling moves (unchanged)
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
}
