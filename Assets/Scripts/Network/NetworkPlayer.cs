using Unity.Netcode;
using UnityEngine;
using Unity.Collections;
using System.Collections;

public class NetworkPlayer : NetworkBehaviour
{
    public static NetworkPlayer LocalInstance;
    
    public bool HasRecentlyChangedSkin { get; set; } = false;
    public string LastChangedSkinPath { get; set; } = "";
    
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
        if (IsServer)
        {
            PlayerUniqueID.OnValueChanged += OnPlayerUniqueIDChanged;
        }
        
        if (IsOwner)
        {
            LocalInstance = this;
            StartCoroutine(DelayedSetID());
        }
    }
    
    private IEnumerator DelayedSetID()
    {
        yield return null; 
        
        if (string.IsNullOrEmpty(localUserId))
        {
            localUserId = PlayerPrefs.GetString("LocalUserId", "");
            if (string.IsNullOrEmpty(localUserId))
            {
                localUserId = "User_" + Random.Range(1000,9999);
                PlayerPrefs.SetString("LocalUserId", localUserId);
                PlayerPrefs.Save();
            }
        }
        
        SetPlayerColorServerRpc();
        SetPlayerUniqueIdServerRpc(localUserId);
    }
    
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
