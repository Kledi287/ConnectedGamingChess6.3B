using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

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
    
    void Update()
    {
        currencyText.text = "" + CurrencyManager.Instance.playerCoins;
    }

    /// <summary>
    /// Tries to spend 'amount' coins from the player's balance.
    /// Returns true if successful; false if insufficient funds.
    /// </summary>
    public bool TrySpendCoins(int amount)
    {
        if (playerCoins >= amount)
        {
            playerCoins -= amount;
            Debug.Log($"[CurrencyManager] Spent {amount} coins. Remaining: {playerCoins}");
            return true;
        }
        else
        {
            Debug.LogWarning("[CurrencyManager] Not enough coins!");
            return false;
        }
    }
    
}

