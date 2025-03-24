using Unity.Netcode;
using UnityEngine;
using Unity.Collections;
using System.Collections;

public class NetworkPlayer : NetworkBehaviour
{
    public static NetworkPlayer LocalInstance;
    
    public bool HasRecentlyChangedSkin { get; set; } = false;
    public string LastChangedSkinPath { get; set; } = "";

    // We'll store the local user ID in PlayerPrefs so it persists across Play sessions.
    private static string localUserId;

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
        // SERVER: subscribe to ID changes
        if (IsServer)
        {
            PlayerUniqueID.OnValueChanged += OnPlayerUniqueIDChanged;
        }

        // If I'm the owner, set my local user ID if needed
        if (IsOwner)
        {
            LocalInstance = this;
            StartCoroutine(DelayedSetID());
        }
    }

    // Wait 1 frame so the object is fully spawned, then set the ID
    private IEnumerator DelayedSetID()
    {
        yield return null; // wait one frame

        // 1) Load from PlayerPrefs if we haven't already
        if (string.IsNullOrEmpty(localUserId))
        {
            localUserId = PlayerPrefs.GetString("LocalUserId", "");
            if (string.IsNullOrEmpty(localUserId))
            {
                // If none stored, generate once
                localUserId = "User_" + Random.Range(1000,9999);
                PlayerPrefs.SetString("LocalUserId", localUserId);
                PlayerPrefs.Save();
            }
        }

        // 2) Now call the server RPC to set the ID
        SetPlayerColorServerRpc();
        SetPlayerUniqueIdServerRpc(localUserId);
    }

    // Called on the server side when PlayerUniqueID changes
    private void OnPlayerUniqueIDChanged(FixedString64Bytes oldVal, FixedString64Bytes newVal)
    {
        string newID = newVal.ToString();
        if (!string.IsNullOrEmpty(newID))
        {
            Debug.Log($"[Server] OnPlayerUniqueIDChanged => {OwnerClientId} => {newID}");
            NetworkChessManager.Instance.HandlePlayerIDSet(OwnerClientId, newID);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void SetPlayerUniqueIdServerRpc(string newId)
    {
        PlayerUniqueID.Value = newId;
        Debug.Log($"[Server] Assigned ID '{newId}' to player {OwnerClientId}");
    }

    [ServerRpc(RequireOwnership = false)]
    private void SetPlayerColorServerRpc()
    {
        // Basic logic: first connection => White, second => Black
        IsWhite.Value = NetworkManager.Singleton.ConnectedClients.Count <= 1;
    }

    public bool IsMyTurn()
    {
        return TurnManager.Instance.CanMove(IsWhite.Value);
    }
    
    public void MarkSkinChanged(string skinPath)
    {
        HasRecentlyChangedSkin = true;
        LastChangedSkinPath = skinPath;
    }
}
