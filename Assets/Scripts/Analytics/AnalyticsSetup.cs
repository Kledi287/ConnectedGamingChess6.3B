using Game;
using UnityEngine;
using Game.Analytics;
using Game.State;

public class AnalyticsSetup : MonoBehaviour
{
    [SerializeField] private GameObject analyticsManagerPrefab;
    [SerializeField] private GameObject gameStateManagerPrefab;
    [SerializeField] private GameObject gameStateInterfacePrefab;

    private void Awake()
    {
        // Create the necessary instances if they don't already exist
        if (FirebaseAnalyticsManager.Instance == null && analyticsManagerPrefab != null)
        {
            Instantiate(analyticsManagerPrefab);
        }
        
        if (GameStateManager.Instance == null && gameStateManagerPrefab != null)
        {
            Instantiate(gameStateManagerPrefab);
        }
        
        if (FindObjectOfType<GameStateInterface>() == null && gameStateInterfacePrefab != null)
        {
            Instantiate(gameStateInterfacePrefab);
        }
    }

    private void Start()
    {
        Debug.Log("[AnalyticsSetup] Analytics and game state management initialized");
        
        // You could add automatic demonstration here for testing
        // GetComponent<GameStateInterface>()?.Invoke("DemonstrateGameStateManagement", 5f);
    }
}