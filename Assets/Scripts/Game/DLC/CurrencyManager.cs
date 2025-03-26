using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Game.DLC
{
    public class CurrencyManager : MonoBehaviour
    {
        public static CurrencyManager Instance;
    
        [SerializeField] private TMP_Text currencyText;

        public int playerCoins = 1000;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }
    
        private void Start()
        {
            playerCoins = PlayerPrefs.GetInt("PlayerCoins", 1000);
            
            //Uncomment for testing purposes
            //ResetPlayerProgress();
        }
    
        void Update()
        {
            currencyText.text = playerCoins.ToString();
        }
        
        public bool TrySpendCoins(int amount)
        {
            if (playerCoins >= amount)
            {
                playerCoins -= amount;
                Debug.Log($"[CurrencyManager] Spent {amount} coins. Remaining: {playerCoins}");
                
                PlayerPrefs.SetInt("PlayerCoins", playerCoins);
                PlayerPrefs.Save();
                return true;
            }
            else
            {
                Debug.LogWarning("[CurrencyManager] Not enough coins!");
                return false;
            }
        }

        public void ResetPlayerProgress()
        {
            playerCoins = 1000;
            PlayerPrefs.SetInt("PlayerCoins", playerCoins);
            
            PlayerPrefs.DeleteKey("OwnedSkins");
            
            PlayerPrefs.DeleteKey("CurrentWhiteSkin");
            PlayerPrefs.DeleteKey("CurrentBlackSkin");
            
            PlayerPrefs.Save();
            
            Debug.Log("[CurrencyManager] Player progress reset: 1000 coins, no skins");
            
            if (DLCStoreUI.Instance != null)
            {
                DLCStoreUI.Instance.LoadOwnedSkins();
                Debug.Log("[CurrencyManager] DLCStoreUI refreshed");
            }
        }
    }
}