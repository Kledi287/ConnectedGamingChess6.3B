using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class NetworkPlayer : NetworkBehaviour
{
    public ulong ClientId { get; private set; }

    public void SetClientId(ulong id)
    {
        ClientId = id;
    }

    public override void OnNetworkSpawn()
    {
        Debug.Log($"NetworkPlayer {ClientId} spawned.");
    }
}
