using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using System;

public class NetworkPlayer : NetworkBehaviour
{
    public string PlayerUniqueID { get; private set; } // Unique ID for the player

    public override void OnNetworkSpawn()
    {
        if (IsOwner) // Only assign ID if this is the local player
        {
            PlayerUniqueID = Guid.NewGuid().ToString(); // Generate a persistent unique ID
            SetUniqueIDServerRpc(PlayerUniqueID);
        }
        Debug.Log($"Player {PlayerUniqueID} (Client {OwnerClientId}) spawned.");
    }

    [ServerRpc]
    private void SetUniqueIDServerRpc(string uniqueId)
    {
        PlayerUniqueID = uniqueId;
    }
}
