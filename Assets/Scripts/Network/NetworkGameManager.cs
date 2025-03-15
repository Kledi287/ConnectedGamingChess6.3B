using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using ParrelSync;

public class NetworkGameManager : NetworkBehaviour
{
    public static NetworkGameManager Instance { get; private set; }
    private Dictionary<ulong, NetworkPlayer> connectedPlayers = new Dictionary<ulong, NetworkPlayer>();

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.H))
        {
            Debug.Log("Starting Host...");
            NetworkManager.Singleton.StartHost();
        }
        if (Input.GetKeyDown(KeyCode.C))
        {
            Debug.Log("Starting Client...");
            NetworkManager.Singleton.StartClient();
        }
    }
    
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
        NetworkManager.Singleton.OnClientConnectedCallback += (clientId) =>
        {
            Debug.Log($"Player {clientId} connected.");
        };

        NetworkManager.Singleton.OnClientDisconnectCallback += (clientId) =>
        {
            Debug.Log($"Player {clientId} disconnected.");
        };
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
        Debug.Log($"Player {clientId} connected.");

        if (connectedPlayers.ContainsKey(clientId))
        {
            Debug.Log($"Player {clientId} rejoined.");
            return;
        }

        NetworkPlayer player = InstantiatePlayer(clientId);
        connectedPlayers.Add(clientId, player);
    }


    private void OnClientDisconnected(ulong clientId)
    {
        Debug.Log($"Player {clientId} disconnected.");
        
        if (connectedPlayers.ContainsKey(clientId))
        {
            Destroy(connectedPlayers[clientId].gameObject);
            connectedPlayers.Remove(clientId);
        }
    }

    private NetworkPlayer InstantiatePlayer(ulong clientId)
    {
        GameObject playerObject = new GameObject($"Player_{clientId}");
        NetworkPlayer player = playerObject.AddComponent<NetworkPlayer>();
        player.SetClientId(clientId);
        return player;
    }
}



