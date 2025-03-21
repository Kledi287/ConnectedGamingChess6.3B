using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DLCStoreUI : MonoBehaviour
{
    [SerializeField] private GameObject storePanel;  
    [SerializeField] private Button closeButton;      
    
    public Button greenButton;
    public TMP_Text greenButtonText;
    
    public Button blueButton;
    public TMP_Text blueButtonText;
    
    public Button yellowButton;
    public TMP_Text yellowButtonText;
    
    public Button brownButton;
    public TMP_Text brownButtonText;
    
    public Button redButton;
    public TMP_Text redButtonText;
    
    public static DLCStoreUI Instance;
    public PurchaseConfirmationUI purchaseConfirmationUI;
    
    private Dictionary<string, bool> ownedSkins = new Dictionary<string, bool>();
    
    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

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
    
    public void SetSkinOwned(string skinPath, bool owned)
    {
        ownedSkins[skinPath] = owned;
        UpdateButtonLabels();
    }
    
    private bool IsSkinOwned(string skinPath)
    {
        return ownedSkins.ContainsKey(skinPath) && ownedSkins[skinPath];
    }
    
    private void UpdateButtonLabels()
    {
        if (IsSkinOwned("chessSkins/green.png"))
            greenButtonText.text = "USE";
        else
            greenButtonText.text = "BUY";

        if (IsSkinOwned("chessSkins/brown.png"))
            brownButtonText.text = "USE";
        else
            brownButtonText.text = "BUY";
        
        if (IsSkinOwned("chessSkins/red.png"))
            redButtonText.text = "USE";
        else
            redButtonText.text = "BUY";
        
        if (IsSkinOwned("chessSkins/blue.png"))
            blueButtonText.text = "USE";
        else
            blueButtonText.text = "BUY";
        
        if (IsSkinOwned("chessSkins/yellow.png"))
            yellowButtonText.text = "USE";
        else
            yellowButtonText.text = "BUY";
    }
    
    public void OnGreenSkinButtonClicked()
    {
        var myPieces = BoardManager.Instance.GetAllWhitePieces();
        int cost = 250;
        
        if (IsSkinOwned("chessSkins/green.png"))
        {
            OnBuySkin("chessSkins/green.png", myPieces);
        }
        else
        {
            purchaseConfirmationUI.Show(cost, "chessSkins/green.png", myPieces);
        }
    }
    
    public void OnBlueSkinButtonClicked()
    {
        var myPieces = BoardManager.Instance.GetAllWhitePieces();
        int cost = 250;
        
        if (IsSkinOwned("chessSkins/blue.png"))
        {
            OnBuySkin("chessSkins/blue.png", myPieces);
        }
        else
        {
            purchaseConfirmationUI.Show(cost, "chessSkins/blue.png", myPieces);
        }
    }

    public void OnBrownSkinButtonClicked()
    {
        var myPieces = BoardManager.Instance.GetAllWhitePieces();
        int cost = 250;
        
        if (IsSkinOwned("chessSkins/brown.png"))
        {
            OnBuySkin("chessSkins/brown.png", myPieces);
        }
        else
        {
            purchaseConfirmationUI.Show(cost, "chessSkins/brown.png", myPieces);
        }
    }

    public void OnRedSkinButtonClicked()
    {
        var myPieces = BoardManager.Instance.GetAllWhitePieces();
        int cost = 250;
        
        if (IsSkinOwned("chessSkins/red.png"))
        {
            OnBuySkin("chessSkins/red.png", myPieces);
        }
        else
        {
            purchaseConfirmationUI.Show(cost, "chessSkins/red.png", myPieces);
        }
    }

    public void OnYellowSkinButtonClicked()
    {
        var myPieces = BoardManager.Instance.GetAllWhitePieces();
        int cost = 250;
        
        if (IsSkinOwned("chessSkins/yellow.png"))
        {
            OnBuySkin("chessSkins/yellow.png", myPieces);
        }
        else
        {
            purchaseConfirmationUI.Show(cost, "chessSkins/yellow.png", myPieces);
        }
    }
}