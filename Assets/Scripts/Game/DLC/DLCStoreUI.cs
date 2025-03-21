using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class DLCStoreUI : MonoBehaviour
{
    [Header("Store UI References")]
    [SerializeField] private GameObject storePanel;  
    [SerializeField] private Button closeButton;      

    private void Start()
    {
        storePanel.SetActive(false);
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(OnCloseButtonClicked);
        }
    }

    public void OpenStore()
    {
        storePanel.SetActive(true);
    }

    private void OnCloseButtonClicked()
    {
        storePanel.SetActive(false);
    }

    // Suppose this is called when user clicks "Buy" or "Use" on a specific skin
    // skinPath => the path in Firebase Storage, e.g. "chessSkins/BlackKnight.png"
    // pieceGO => the 3D piece you want to update
    public async void OnBuySkin(string skinPath, GameObject pieceGO)
    {
        Debug.Log($"[DLCStoreUI] Buying/downloading skin: {skinPath}");
        
        Texture2D downloadedTex = await DLCManager.Instance.DownloadSkinAsync(skinPath);
        if (downloadedTex != null)
        {
            // Apply to the piece's material
            DLCManager.Instance.ApplySkinToPiece(pieceGO, downloadedTex);

            Debug.Log("[DLCStoreUI] Skin applied successfully!");
        }
        else
        {
            Debug.LogError("[DLCStoreUI] Failed to download or apply skin.");
        }
    }
}