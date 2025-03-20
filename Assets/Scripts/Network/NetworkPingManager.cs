using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class NetworkPingManager : NetworkBehaviour
{
    [SerializeField] private TMP_Text pingText;  
    [SerializeField] private float pingInterval = 2f;

    private float pingTimer = 0f;

    void Update()
    {
        if (IsOwner && IsClient)
        {
            pingTimer += Time.deltaTime;
            if (pingTimer >= pingInterval)
            {
                pingTimer = 0f;
                float clientTime = Time.realtimeSinceStartup;
                PingServerRpc(clientTime);
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void PingServerRpc(float clientTime, ServerRpcParams serverRpcParams = default)
    {
        ulong senderClientId = serverRpcParams.Receive.SenderClientId;
        float serverTime = Time.realtimeSinceStartup;
        float halfTrip = serverTime - clientTime;
        
        PongClientRpc(halfTrip, senderClientId);
    }

    [ClientRpc]
    private void PongClientRpc(float halfTrip, ulong targetClientId, ClientRpcParams clientRpcParams = default)
    {
        if (NetworkManager.Singleton.LocalClientId != targetClientId)
            return;
        
        float approxRtt = halfTrip * 2f;
        float ms = approxRtt * 1000f;

        if (pingText != null)
        {
            pingText.text = $"Ping: {ms:0.0} ms";
        }
        else
        {
            Debug.LogWarning("[Client] pingText is null, can't display ping");
        }
    }
}
