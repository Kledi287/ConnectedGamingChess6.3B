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
            Debug.LogError("Already running.");
            return;
        }
        bool started = NetworkManager.Singleton.StartHost();
        Debug.Log($"🎯 Host Started: {started}");
    }

    public void StartClient()
    {
        if (NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsServer)
        {
            Debug.LogError("Already running.");
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
        if (player != null)
        {
            string uniqueId = player.PlayerUniqueID.Value.ToString();
            if (!persistentPlayers.ContainsKey(uniqueId))
            {
                persistentPlayers[uniqueId] = clientId;
                Debug.Log($"✅ Player {clientId} connected for the first time.");
            }
            else
            {
                Debug.Log($"🔄 Player {clientId} reconnected.");
            }
        }
    }

    private void OnClientDisconnected(ulong clientId)
    {
        Debug.Log($"Player {clientId} disconnected.");
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

    [ServerRpc(RequireOwnership = false)]
    public void RequestMoveServerRpc(Vector2Int from, Vector2Int to, ServerRpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;
        NetworkPlayer player = FindPlayerByClientId(clientId);
        if (player == null) return;

        // Out-of-turn => reset piece, no freeze
        if (!player.IsMyTurn())
        {
            ResetPieceClientRpc(from, clientId);
            return;
        }

        Square startSquare = new Square(from.x, from.y);
        Square endSquare   = new Square(to.x, to.y);

        // Illegal move => reset piece, no freeze
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

        // Toggle turn (white -> black or black -> white)
        TurnManager.Instance.EndTurnServerRpc();

        // Update board so only the next side is enabled
        Side sideToMove = GameManager.Instance.SideToMove;
        UpdateBoardStateClientRpc(sideToMove);

        // Move piece visually
        UpdateBoardClientRpc(from, to);
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
        // Only the client who made the illegal move sees this reset
        if (NetworkManager.Singleton.LocalClientId != targetClientId) return;

        GameObject pieceGO = BoardManager.Instance.GetPieceGOAtPosition(new Square(from.x, from.y));
        if (pieceGO != null)
        {
            // Snap back
            pieceGO.transform.position = pieceGO.transform.parent.position;
        }
    }
}
