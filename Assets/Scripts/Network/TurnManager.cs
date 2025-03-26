using Unity.Netcode;
using UnityEngine;

public class TurnManager : NetworkBehaviour
{
    public static TurnManager Instance;

    public NetworkVariable<bool> IsWhiteTurn = new NetworkVariable<bool>(
        true,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server  // Only server can write to this
    );

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

    public bool CanMove(bool isPlayerWhite)
    {
        return IsWhiteTurn.Value == isPlayerWhite;
    }

    [ServerRpc(RequireOwnership = false)]
    public void EndTurnServerRpc()
    {
        // This can only be called on the server
        if (!IsServer)
        {
            Debug.LogWarning("[TurnManager] EndTurnServerRpc must be called on the server!");
            return;
        }
        
        IsWhiteTurn.Value = !IsWhiteTurn.Value;
        Debug.Log($"[Server] Turn ended, now it's {(IsWhiteTurn.Value ? "White" : "Black")}'s turn");
    }
    
    // This method should only be called by the server
    public void SetTurnState(bool isWhiteTurn)
    {
        if (!IsServer)
        {
            Debug.LogWarning("[TurnManager] Only the server can set the turn state!");
            return;
        }
        
        Debug.Log($"[TurnManager] Updating turn state: White's turn = {isWhiteTurn}");
        IsWhiteTurn.Value = isWhiteTurn;
        
        // Make sure to update the UI
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ValidateIndicators();
        }
    }
}