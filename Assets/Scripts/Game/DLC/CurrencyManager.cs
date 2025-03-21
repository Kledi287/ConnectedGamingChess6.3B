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
    
    private void Start()
    {
        playerCoins = PlayerPrefs.GetInt("PlayerCoins", 1000);
    }
    
    void Update()
    {
        currencyText.text = playerCoins.ToString();
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

            // Save after spending
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
}

