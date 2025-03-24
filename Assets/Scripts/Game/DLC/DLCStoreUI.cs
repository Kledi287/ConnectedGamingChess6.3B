using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using Unity.Netcode;
using UnityChess;
using UnityEngine;
using UnityEngine.UI;

namespace Game.DLC
{ 
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
        
        public void ToggleAutoApplySkins(bool enable)
        {
            PlayerPrefs.SetInt("AutoApplySkins", enable ? 1 : 0);
            PlayerPrefs.Save();
            Debug.Log($"[DLCStoreUI] Auto-apply skins set to: {enable}");
        }
        
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
            try
            {
                Debug.Log($"[DLCStoreUI] Buying/downloading skin: {skinPath}");
                
                if (allMyPieces == null || allMyPieces.Count == 0)
                {
                    Debug.LogError("[DLCStoreUI] No pieces provided to apply skin to!");
                    return;
                }
                
                // Get the side of pieces we're applying to
                VisualPiece samplePiece = allMyPieces[0].GetComponent<VisualPiece>();
                Side pieceSide = samplePiece != null ? samplePiece.PieceColor : Side.None;
                Debug.Log($"[DLCStoreUI] Applying skin to {allMyPieces.Count} pieces of side {pieceSide}");
                
                Texture2D downloadedTex = await DLCManager.Instance.DownloadSkinAsync(skinPath);
                if (downloadedTex != null)
                {
                    DLCManager.Instance.ApplySkinToAllPieces(allMyPieces, downloadedTex);
                    
                    // Always sync in multiplayer mode - use the piece side (not player side)
                    if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsClient)
                    {
                        NetworkPlayer localPlayer = NetworkPlayer.LocalInstance;
                        if (localPlayer != null)
                        {
                            // Critical fix: use the piece color, not the player's IsWhite value
                            bool syncIsWhite = (pieceSide == Side.White);
                            
                            // Mark that we just changed our skin (to avoid downloading it again)
                            localPlayer.MarkSkinChanged(skinPath);
                            
                            // Tell the server about our skin change
                            NetworkChessManager.Instance.SyncSkinServerRpc(syncIsWhite, skinPath);
                            Debug.Log($"[DLCStoreUI] Notified server about skin change to: {skinPath} for {(syncIsWhite ? "white" : "black")} pieces");
                        }
                        else
                        {
                            Debug.LogWarning("[DLCStoreUI] Cannot sync skin - LocalInstance is null");
                        }
                    }
                    
                    Debug.Log("[DLCStoreUI] Skin applied to all pieces successfully!");
                }
                else
                {
                    Debug.LogError("[DLCStoreUI] Failed to download or apply skin.");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DLCStoreUI] Error in OnBuySkin: {ex.Message}\n{ex.StackTrace}");
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
            List<GameObject> myPieces;
            
            // Get the correct pieces based on player's side
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsClient && NetworkPlayer.LocalInstance != null)
            {
                bool isWhite = NetworkPlayer.LocalInstance.IsWhite.Value;
                myPieces = isWhite ? 
                    BoardManager.Instance.GetAllWhitePieces() : 
                    BoardManager.Instance.GetAllBlackPieces();
                
                Debug.Log($"[DLCStoreUI] Getting {(isWhite ? "WHITE" : "BLACK")} pieces for skin application");
            }
            else
            {
                // Default to white pieces in single player
                myPieces = BoardManager.Instance.GetAllWhitePieces();
            }
            
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
            List<GameObject> myPieces;
            
            // Get the correct pieces based on player's side
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsClient && NetworkPlayer.LocalInstance != null)
            {
                bool isWhite = NetworkPlayer.LocalInstance.IsWhite.Value;
                myPieces = isWhite ? 
                    BoardManager.Instance.GetAllWhitePieces() : 
                    BoardManager.Instance.GetAllBlackPieces();
                
                Debug.Log($"[DLCStoreUI] Getting {(isWhite ? "WHITE" : "BLACK")} pieces for skin application");
            }
            else
            {
                // Default to white pieces in single player
                myPieces = BoardManager.Instance.GetAllWhitePieces();
            }
            
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
            List<GameObject> myPieces;
            
            // Get the correct pieces based on player's side
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsClient && NetworkPlayer.LocalInstance != null)
            {
                bool isWhite = NetworkPlayer.LocalInstance.IsWhite.Value;
                myPieces = isWhite ? 
                    BoardManager.Instance.GetAllWhitePieces() : 
                    BoardManager.Instance.GetAllBlackPieces();
                
                Debug.Log($"[DLCStoreUI] Getting {(isWhite ? "WHITE" : "BLACK")} pieces for skin application");
            }
            else
            {
                // Default to white pieces in single player
                myPieces = BoardManager.Instance.GetAllWhitePieces();
            }
            
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
            List<GameObject> myPieces;
            
            // Get the correct pieces based on player's side
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsClient && NetworkPlayer.LocalInstance != null)
            {
                bool isWhite = NetworkPlayer.LocalInstance.IsWhite.Value;
                myPieces = isWhite ? 
                    BoardManager.Instance.GetAllWhitePieces() : 
                    BoardManager.Instance.GetAllBlackPieces();
                
                Debug.Log($"[DLCStoreUI] Getting {(isWhite ? "WHITE" : "BLACK")} pieces for skin application");
            }
            else
            {
                // Default to white pieces in single player
                myPieces = BoardManager.Instance.GetAllWhitePieces();
            }
            
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
            List<GameObject> myPieces;
            
            // Get the correct pieces based on player's side
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsClient && NetworkPlayer.LocalInstance != null)
            {
                bool isWhite = NetworkPlayer.LocalInstance.IsWhite.Value;
                myPieces = isWhite ? 
                    BoardManager.Instance.GetAllWhitePieces() : 
                    BoardManager.Instance.GetAllBlackPieces();
                
                Debug.Log($"[DLCStoreUI] Getting {(isWhite ? "WHITE" : "BLACK")} pieces for skin application");
            }
            else
            {
                // Default to white pieces in single player
                myPieces = BoardManager.Instance.GetAllWhitePieces();
            }
            
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
}

