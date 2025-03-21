using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

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
        bool canPurchase = CurrencyManager.Instance.TrySpendCoins(cost);
        if (!canPurchase)
        {
            Debug.LogWarning("Not enough coins!");
        }
        else
        {
            DLCStoreUI.Instance.SetSkinOwned(skinPath, true);
            
            DLCStoreUI.Instance.OnBuySkin(skinPath, myPieces);
        }
        panel.SetActive(false);
    }
    
    public void OnCancelButton()
    {
        panel.SetActive(false);
    }
}


