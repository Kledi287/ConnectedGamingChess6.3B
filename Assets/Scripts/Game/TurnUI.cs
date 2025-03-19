using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class TurnUI : MonoBehaviour
{
    public TMP_Text turnText;

    void Update()
    {
        if (TurnManager.Instance != null && NetworkPlayer.LocalInstance != null)
        {
            bool myTurn = NetworkPlayer.LocalInstance.IsMyTurn();
            turnText.text = myTurn ? "Your turn!" : "Waiting for opponent...";
        }
    }
}