using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using ParrelSync;

public class NetworkGameManager : NetworkBehaviour
{
    public static NetworkGameManager Instance { get; private set; }

    [SerializeField] private Button hostButton; // Assign in Inspector
    [SerializeField] private Button joinButton; // Assign in Inspector

    private Dictionary<string, ulong> persistentPlayers = new Dictionary<string, ulong>();

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
        }
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
            Debug.LogError($"🚨 Cannot start host: Already running. Server:{NetworkManager.Singleton.IsServer} Client:{NetworkManager.Singleton.IsClient}");
            return;
        }

        bool started = NetworkManager.Singleton.StartHost();
        Debug.Log($"🎯 Host Started: {started}");
    }

    public void StartClient()
    {
        if (NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsServer)
        {
            Debug.LogError($"🚨 Cannot start client: Already connected (Server:{NetworkManager.Singleton.IsServer}, Client:{NetworkManager.Singleton.IsClient})");
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

            if (persistentPlayers.ContainsKey(uniqueId))
            {
                Debug.Log($"🔄 Player {clientId} (Unique ID: {uniqueId}) has reconnected.");
            }
            else
            {
                persistentPlayers[uniqueId] = clientId;
                Debug.Log($"✅ Player {clientId} (Unique ID: {uniqueId}) connected for the first time.");
            }
        }
    }

    private void OnClientDisconnected(ulong clientId)
    {
        Debug.Log($"Player {clientId} disconnected.");
    }

    private NetworkPlayer FindPlayerByClientId(ulong clientId)
    {
        foreach (var obj in FindObjectsOfType<NetworkPlayer>())
        {
            if (obj.OwnerClientId == clientId)
                return obj;
        }
        return null;
    }

    [ServerRpc(RequireOwnership = false)]
    public void RequestMoveServerRpc(Vector2Int from, Vector2Int to, ServerRpcParams rpcParams = default)
    {
        ulong playerId = rpcParams.Receive.SenderClientId;
        NetworkPlayer player = FindPlayerByClientId(playerId);

        if (player == null)
        {
            Debug.LogError($"❌ No player found for client {playerId}");
            return;
        }

        if (!player.IsMyTurn())
        {
            Debug.LogError($"❌ Player {playerId} tried to move out of turn!");
            return;
        }

        bool moveSuccessful = TryMovePiece(from, to);

        if (moveSuccessful)
        {
            TurnManager.Instance.EndTurnServerRpc();
            UpdateBoardClientRpc(from, to);
        }
    }

    private bool TryMovePiece(Vector2Int from, Vector2Int to)
    {
        Debug.Log($"♟ Moving piece from {from} to {to}.");
        return true;
    }

    [ClientRpc]
    public void UpdateBoardClientRpc(Vector2Int from, Vector2Int to)
    {
        Debug.Log($"📡 Updating board on client: Move from {from} to {to}");
        MovePieceOnBoard(from, to);
    }

    private void MovePieceOnBoard(Vector2Int from, Vector2Int to)
    {
        Debug.Log($"♟ Piece visually moved on board from {from} to {to}");
        // TODO: Implement visual board movement logic.
    }
}