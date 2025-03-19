using Unity.Netcode;
using UnityEngine;
using Unity.Collections;

public class NetworkPlayer : NetworkBehaviour
{
    public static NetworkPlayer LocalInstance;

    public NetworkVariable<FixedString64Bytes> PlayerUniqueID = new NetworkVariable<FixedString64Bytes>(
        new FixedString64Bytes(""),
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public NetworkVariable<bool> IsWhite = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            LocalInstance = this;
            SetPlayerColorServerRpc();
        }
        // Minimal log:
        // Debug.Log($"Player {OwnerClientId} spawned. IsWhite={IsWhite.Value}");
    }

    [ServerRpc(RequireOwnership = false)]
    void SetPlayerColorServerRpc(ServerRpcParams rpcParams = default)
    {
        // First connection => White, second => Black
        IsWhite.Value = NetworkManager.Singleton.ConnectedClients.Count <= 1;
        // Remove or keep minimal logs:
        // Debug.Log($"Assigning player {OwnerClientId} => {(IsWhite.Value ? "White" : "Black")}");
    }

    public bool IsMyTurn()
    {
        return TurnManager.Instance.CanMove(IsWhite.Value);
    }
}