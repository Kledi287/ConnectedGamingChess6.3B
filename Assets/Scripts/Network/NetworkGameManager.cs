using System.Collections;
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

    private Dictionary<string, ulong> persistentPlayers = new Dictionary<string, ulong>(); // Store players by Unique ID

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
    }
    
    public void StartHost()
    {
        Debug.Log("Starting Host...");
        NetworkManager.Singleton.StartHost();
    }

    public void StartClient()
    {
        Debug.Log("Starting Client...");
        NetworkManager.Singleton.StartClient();
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
        Debug.Log($"✅ Player {clientId} has connected.");

        // Find the player's unique ID
        NetworkPlayer player = FindPlayerByClientId(clientId);
        if (player != null)
        {
            string uniqueId = player.PlayerUniqueID;
            if (persistentPlayers.ContainsKey(uniqueId))
            {
                Debug.Log($"Player {clientId} (Unique ID: {uniqueId}) has reconnected.");
            }
            else
            {
                persistentPlayers[uniqueId] = clientId;
                Debug.Log($"Player {clientId} (Unique ID: {uniqueId}) connected for the first time.");
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
            {
                return obj;
            }
        }
        return null;
    }
}




