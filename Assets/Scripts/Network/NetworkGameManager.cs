using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using ParrelSync; // ParrelSync detection

public class NetworkGameManager : MonoBehaviour
{
    public static NetworkGameManager Instance { get; private set; }

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
        if (ClonesManager.IsClone())
        {
            Debug.Log("Clone instance detected: Joining as client...");
            StartClient();
        }
        else
        {
            Debug.Log("Main instance detected: Starting as host...");
            StartHost();
        }
    }

    public void StartHost()
    {
        if (!NetworkManager.Singleton.IsHost)
        {
            NetworkManager.Singleton.StartHost();
            Debug.Log("Hosting Game...");
        }
    }

    public void StartClient()
    {
        if (!NetworkManager.Singleton.IsClient)
        {
            NetworkManager.Singleton.StartClient();
            Debug.Log("Joining Game as Client...");
        }
    }
}



