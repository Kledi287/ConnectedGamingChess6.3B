using Unity.Netcode;
using UnityEngine;

public class TurnManager : NetworkBehaviour
{
    public static TurnManager Instance;

    public NetworkVariable<bool> IsWhiteTurn = new NetworkVariable<bool>(
        true,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
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
        IsWhiteTurn.Value = !IsWhiteTurn.Value;
    }
}