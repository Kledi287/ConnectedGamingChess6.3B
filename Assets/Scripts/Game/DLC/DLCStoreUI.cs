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
    public TMP_Text greenCostText;
    
    public Button blueButton;
    public TMP_Text blueButtonText;
    public TMP_Text blueCostText;
    
    public Button yellowButton;
    public TMP_Text yellowButtonText;
    public TMP_Text yellowCostText;
    
    public Button brownButton;
    public TMP_Text brownButtonText;
    public TMP_Text brownCostText;
    
    public Button redButton;
    public TMP_Text redButtonText;
    public TMP_Text redCostText;
    
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
        LoadOwnedSkins();
        UpdateButtonLabels();
    }

    public void OpenStore()
    {
        storePanel.SetActive(true);
    }

    private void OnCloseButtonClicked()
    {
        storePanel.SetActive(false);
    }
    
    private void LoadOwnedSkins()
    {
        string json = PlayerPrefs.GetString("OwnedSkins", "");
        if (!string.IsNullOrEmpty(json))
        {
            OwnedSkinsData data = JsonUtility.FromJson<OwnedSkinsData>(json);
            // Clear or create the dictionary
            ownedSkins = new Dictionary<string, bool>();
            foreach (var entry in data.entries)
            {
                ownedSkins[entry.skinPath] = entry.owned;
            }
        }
        else
        {
            // If no data found, do nothing (ownedSkins remains empty).
        }
        UpdateButtonLabels();
    }

    public async void OnBuySkin(string skinPath, List<GameObject> allMyPieces)
    {
        Debug.Log($"[DLCStoreUI] Buying/downloading skin: {skinPath}");
    
        Texture2D downloadedTex = await DLCManager.Instance.DownloadSkinAsync(skinPath);
        if (downloadedTex != null)
        {
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
        SaveOwnedSkins();
    }
    
    private void SaveOwnedSkins()
    {
        OwnedSkinsData data = new OwnedSkinsData();
        data.entries = new List<OwnedSkinEntry>();
        foreach (var kvp in ownedSkins)
        {
            var entry = new OwnedSkinEntry();
            entry.skinPath = kvp.Key;
            entry.owned    = kvp.Value;
            data.entries.Add(entry);
        }

        string json = JsonUtility.ToJson(data);
        PlayerPrefs.SetString("OwnedSkins", json);
        PlayerPrefs.Save();
        Debug.Log("[DLCStoreUI] Owned skins saved: " + json);
    }
    
    private bool IsSkinOwned(string skinPath)
    {
        return ownedSkins.ContainsKey(skinPath) && ownedSkins[skinPath];
    }
    
    private void UpdateButtonLabels()
    {
        if (IsSkinOwned("chessSkins/green.png"))
        {
            greenButtonText.text = "USE";
            greenCostText.text = "Purchased!";
        }
        else
        {
            greenButtonText.text = "BUY";
        }

        if (IsSkinOwned("chessSkins/brown.png"))
        {
            brownButtonText.text = "USE";
            brownCostText.text = "Purchased!";
        }
        else
        {
            brownButtonText.text = "BUY";
        }

        if (IsSkinOwned("chessSkins/red.png"))
        {
            redButtonText.text = "USE";
            redCostText.text = "Purchased!";
        }
        else
        {
            redButtonText.text = "BUY";
        }

        if (IsSkinOwned("chessSkins/blue.png"))
        {
            blueButtonText.text = "USE";
            blueCostText.text = "Purchased!";
        }
        else
        {
            blueButtonText.text = "BUY";
        }

        if (IsSkinOwned("chessSkins/yellow.png"))
        {
            yellowButtonText.text = "USE";
            yellowCostText.text = "Purchased!";
        }
        else
        {
            yellowButtonText.text = "BUY";
        }
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
    
    [System.Serializable]
    public class OwnedSkinsData
    {
        public List<OwnedSkinEntry> entries;
    }

    [System.Serializable]
    public class OwnedSkinEntry
    {
        public string skinPath;
        public bool owned;
    }
}