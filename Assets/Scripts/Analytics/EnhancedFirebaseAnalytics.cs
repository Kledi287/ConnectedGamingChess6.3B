using System;
using System.Collections.Generic;
using Game.DLC;
using UnityEngine;
using UnityChess;
using Unity.Netcode;

namespace Game.Analytics
{
    public class EnhancedFirebaseAnalytics : MonoBehaviour
    {
        private void Start()
        {
            GameManager.MoveExecutedEvent += OnMoveExecuted;
            
            PurchaseConfirmationUI.OnPurchaseComplete += OnSkinPurchased;
            DLCStoreUI.OnSkinApplied += OnSkinApplied;
        }
        
        private void OnDestroy()
        {
            GameManager.MoveExecutedEvent -= OnMoveExecuted;
            PurchaseConfirmationUI.OnPurchaseComplete -= OnSkinPurchased;
            DLCStoreUI.OnSkinApplied -= OnSkinApplied;
        }
        
        private void OnMoveExecuted()
        {
            if (GameManager.Instance.LatestHalfMoveIndex <= 10)
            {
                if (GameManager.Instance.HalfMoveTimeline.TryGetCurrent(out HalfMove lastMove))
                {
                    string moveNotation = lastMove.ToAlgebraicNotation();
                    int moveNumber = GameManager.Instance.LatestHalfMoveIndex;
                    Side movingSide = lastMove.Piece.Owner;
                    
                    string openingMoveName = $"{moveNumber}_{movingSide}_{moveNotation}";
                    
                    LogOpeningMove(moveNumber, movingSide.ToString(), moveNotation);
                }
            }
        }
        
        private void OnSkinPurchased(string skinId, int cost)
        {
            if (FirebaseAnalyticsManager.Instance != null)
            {
                string simpleSkinName = GetSimpleSkinName(skinId);
                
                Firebase.Analytics.FirebaseAnalytics.LogEvent(
                    Firebase.Analytics.FirebaseAnalytics.EventPurchase,
                    new Firebase.Analytics.Parameter[]
                    {
                        new Firebase.Analytics.Parameter("item_id", skinId),
                        new Firebase.Analytics.Parameter("item_name", simpleSkinName),
                        new Firebase.Analytics.Parameter("price", cost),
                        new Firebase.Analytics.Parameter("currency", "coins"),
                        new Firebase.Analytics.Parameter("item_category", "chess_skin")
                    }
                );
                
                Debug.Log($"[EnhancedAnalytics] Logged purchase of skin: {simpleSkinName}");
            }
        }
        
        private void OnSkinApplied(string skinId, bool isWhitePieces)
        {
            if (FirebaseAnalyticsManager.Instance != null)
            {
                string simpleSkinName = GetSimpleSkinName(skinId);
                
                Firebase.Analytics.FirebaseAnalytics.LogEvent(
                    Firebase.Analytics.FirebaseAnalytics.EventSelectContent,
                    new Firebase.Analytics.Parameter[]
                    {
                        new Firebase.Analytics.Parameter("item_id", skinId),
                        new Firebase.Analytics.Parameter("item_name", simpleSkinName),
                        new Firebase.Analytics.Parameter("item_category", "chess_skin"),
                        new Firebase.Analytics.Parameter("piece_color", isWhitePieces ? "white" : "black")
                    }
                );
                
                Debug.Log($"[EnhancedAnalytics] Logged application of skin: {simpleSkinName}");
            }
        }
        
        private void LogOpeningMove(int moveNumber, string side, string moveNotation)
        {
            if (FirebaseAnalyticsManager.Instance != null)
            {
                string openingPhase = moveNumber <= 2 ? "early" : (moveNumber <= 6 ? "mid" : "late");
                
                Firebase.Analytics.FirebaseAnalytics.LogEvent(
                    "chess_opening_move",
                    new Firebase.Analytics.Parameter[]
                    {
                        new Firebase.Analytics.Parameter("move_number", moveNumber),
                        new Firebase.Analytics.Parameter("side", side),
                        new Firebase.Analytics.Parameter("notation", moveNotation),
                        new Firebase.Analytics.Parameter("opening_phase", openingPhase),
                        new Firebase.Analytics.Parameter("is_multiplayer", NetworkManager.Singleton != null && NetworkManager.Singleton.IsClient ? 1 : 0)
                    }
                );
                
                Debug.Log($"[EnhancedAnalytics] Logged opening move: {side} {moveNotation} (Move {moveNumber})");
            }
        }
        
        private string GetSimpleSkinName(string skinPath)
        {
            if (string.IsNullOrEmpty(skinPath)) return "default";
            
            if (skinPath.Contains("/"))
            {
                string fileName = skinPath.Substring(skinPath.LastIndexOf('/') + 1);
                if (fileName.Contains("."))
                {
                    return fileName.Substring(0, fileName.LastIndexOf('.'));
                }
                return fileName;
            }
            
            return skinPath;
        }
    }
}