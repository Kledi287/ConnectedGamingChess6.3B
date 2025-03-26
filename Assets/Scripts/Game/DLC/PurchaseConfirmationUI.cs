using System.Collections.Generic;
using System.Threading.Tasks;
using Game.Analytics;
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
        
        public static event System.Action<string, int> OnPurchaseComplete;

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
            if (FirebaseAnalyticsManager.Instance != null)
            {
                FirebaseAnalyticsManager.Instance.LogPurchaseAttempt(skinPath, cost);
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
                
                OnPurchaseComplete?.Invoke(skinPath, cost);
                
                if (FirebaseAnalyticsManager.Instance != null)
                {
                    FirebaseAnalyticsManager.Instance.LogPurchaseComplete(skinPath, cost);
                    
                    FirebaseAnalyticsManager.Instance.SetUserProperties();
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