using System.Collections.Generic;
using System.Threading.Tasks;
using Game.DLC;
using UnityEngine;
using UnityEngine.UI;

namespace Game.DLC
{
    public class PurchaseConfirmationUI : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        private int cost;
        private string skinPath;
        private List<GameObject> myPieces;

        public void Show(int cost, string skinPath, List<GameObject> myPieces)
        {
            this.cost = cost;
            this.skinPath = skinPath;
            this.myPieces = myPieces;
            panel.SetActive(true);
        }

        public void OnConfirmButton()
        {
            // Log the purchase attempt in Analytics
            if (Game.Analytics.FirebaseAnalyticsManager.Instance != null)
            {
                Game.Analytics.FirebaseAnalyticsManager.Instance.LogPurchaseAttempt(skinPath, cost);
            }

            bool canPurchase = CurrencyManager.Instance.TrySpendCoins(cost);
            if (!canPurchase)
            {
                Debug.LogWarning("Not enough coins!");
            }
            else
            {
                DLCStoreUI.Instance.SetSkinOwned(skinPath, true);
            
                DLCStoreUI.Instance.OnBuySkin(skinPath, myPieces);
                
                // Log the successful purchase in Analytics
                if (Game.Analytics.FirebaseAnalyticsManager.Instance != null)
                {
                    Game.Analytics.FirebaseAnalyticsManager.Instance.LogPurchaseComplete(skinPath, cost);
                    
                    // Update user properties after purchase
                    Game.Analytics.FirebaseAnalyticsManager.Instance.SetUserProperties();
                }
            }
            panel.SetActive(false);
        }
    
        public void OnCancelButton()
        {
            panel.SetActive(false);
        }
    }
}