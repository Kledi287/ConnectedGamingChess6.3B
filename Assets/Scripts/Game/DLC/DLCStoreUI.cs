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

    public async void OnBuySkin(string skinPath, List<GameObject> allMyPieces)
    {
        Debug.Log($"[DLCStoreUI] Buying/downloading skin: {skinPath}");
    
        Texture2D downloadedTex = await DLCManager.Instance.DownloadSkinAsync(skinPath);
        if (downloadedTex != null)
        {
            // Apply to all the user's pieces
            DLCManager.Instance.ApplySkinToAllPieces(allMyPieces, downloadedTex);

            Debug.Log("[DLCStoreUI] Skin applied to all pieces successfully!");
        }
        else
        {
            Debug.LogError("[DLCStoreUI] Failed to download or apply skin.");
        }
    }
    
    public void OnGreenSkinButtonClicked()
    {
        var myPieces = BoardManager.Instance.GetAllWhitePieces(); 
        OnBuySkin("chessSkins/green.png", myPieces);
    }
    
    public void OnBlueSkinButtonClicked()
    {
        var myPieces = BoardManager.Instance.GetAllWhitePieces();
        OnBuySkin("chessSkins/blue.png", myPieces);
    }

    public void OnBrownSkinButtonClicked()
    {
        var myPieces = BoardManager.Instance.GetAllWhitePieces();
        OnBuySkin("chessSkins/brown.png", myPieces);
    }
    
    public void OnRedSkinButtonClicked()
    {
        var myPieces = BoardManager.Instance.GetAllWhitePieces();
        OnBuySkin("chessSkins/red.png", myPieces);
    }

    public void OnYellowSkinButtonClicked()
    {
        var myPieces = BoardManager.Instance.GetAllWhitePieces();
        OnBuySkin("chessSkins/yellow.png", myPieces);
    }
    
}