using System.Collections;
using System.Collections.Generic;
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

        Debug.Log($"🎮 Player {OwnerClientId} spawned as {(IsWhite.Value ? "White" : "Black")}.");
    }

    [ServerRpc(RequireOwnership = false)]
    void SetPlayerColorServerRpc(ServerRpcParams rpcParams = default)
    {
        IsWhite.Value = NetworkManager.Singleton.ConnectedClientsIds.Count <= 1; // First connected player is white
    }

    public bool IsMyTurn()
    {
        return TurnManager.Instance.CanMove(IsWhite.Value);
    }
}

