using System;
using System.Collections.Generic;
using Firebase;
using Firebase.Analytics;
using Firebase.Extensions;
using Game.DLC;
using Unity.Netcode;
using UnityEngine;

namespace Game.Analytics
{
    public class FirebaseAnalyticsManager : MonoBehaviour
    {
        public static FirebaseAnalyticsManager Instance;

        private bool _isInitialized = false;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            InitializeAnalytics();
        }

        private void InitializeAnalytics()
        {
            FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
            {
                var dependencyStatus = task.Result;
                if (dependencyStatus == DependencyStatus.Available)
                {
                    if (NetworkPlayer.LocalInstance != null)
                    {
                        string userId = NetworkPlayer.LocalInstance.PlayerUniqueID.Value.ToString();
                        if (!string.IsNullOrEmpty(userId))
                        {
                            FirebaseAnalytics.SetUserId(userId);
                        }
                    }
                    else
                    {
                        string localUserId = PlayerPrefs.GetString("LocalUserId", "");
                        if (!string.IsNullOrEmpty(localUserId))
                        {
                            FirebaseAnalytics.SetUserId(localUserId);
                        }
                    }
                    
                    FirebaseAnalytics.SetAnalyticsCollectionEnabled(true);

                    _isInitialized = true;
                }
                else
                {
                    Debug.LogError($"[FirebaseAnalyticsManager] Could not resolve Firebase dependencies: {dependencyStatus}");
                }
            });
        }

        #region Match Events

        public void LogMatchStart(bool isMultiplayer, string matchId)
        {
            if (!_isInitialized) return;

            try
            {
                // Using Parameter objects as required by your Firebase SDK version
                Firebase.Analytics.FirebaseAnalytics.LogEvent(
                    "level_start",
                    new Firebase.Analytics.Parameter[]
                    {
                        new Firebase.Analytics.Parameter("level_name", "chess_match"),
                        new Firebase.Analytics.Parameter("match_id", matchId),
                        new Firebase.Analytics.Parameter("is_multiplayer", isMultiplayer ? 1 : 0),
                        new Firebase.Analytics.Parameter("timestamp", DateTimeOffset.UtcNow.ToUnixTimeSeconds())
                    }
                );
                
                Debug.Log($"[FirebaseAnalyticsManager] Match Start logged - ID: {matchId}, Multiplayer: {isMultiplayer}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[FirebaseAnalyticsManager] Error logging match start: {e.Message}");
            }
        }

        public void LogMatchEnd(bool isMultiplayer, string matchId, string outcome, int totalMoves, int matchDurationSeconds)
        {
            if (!_isInitialized) return;

            try
            {
                // Using Parameter objects as required by your Firebase SDK version
                Firebase.Analytics.FirebaseAnalytics.LogEvent(
                    "level_end",
                    new Firebase.Analytics.Parameter[]
                    {
                        new Firebase.Analytics.Parameter("level_name", "chess_match"),
                        new Firebase.Analytics.Parameter("match_id", matchId),
                        new Firebase.Analytics.Parameter("is_multiplayer", isMultiplayer ? 1 : 0),
                        new Firebase.Analytics.Parameter("outcome", outcome),
                        new Firebase.Analytics.Parameter("total_moves", totalMoves),
                        new Firebase.Analytics.Parameter("match_duration_seconds", matchDurationSeconds),
                        new Firebase.Analytics.Parameter("timestamp", DateTimeOffset.UtcNow.ToUnixTimeSeconds())
                    }
                );
                
                Debug.Log($"[FirebaseAnalyticsManager] Match End logged - ID: {matchId}, Outcome: {outcome}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[FirebaseAnalyticsManager] Error logging match end: {e.Message}");
            }
        }

        #endregion

        #region DLC Events

        public void LogPurchaseAttempt(string skinId, int cost)
        {
            if (!_isInitialized) return;

            try
            {
                Firebase.Analytics.FirebaseAnalytics.LogEvent(
                    "purchase_attempt",
                    new Firebase.Analytics.Parameter[]
                    {
                        new Firebase.Analytics.Parameter("item_id", skinId),
                        new Firebase.Analytics.Parameter("price", cost),
                        new Firebase.Analytics.Parameter("currency", "coins"),
                        new Firebase.Analytics.Parameter("timestamp", DateTimeOffset.UtcNow.ToUnixTimeSeconds())
                    }
                );
                
                Debug.Log($"[FirebaseAnalyticsManager] Purchase Attempt logged - Skin: {skinId}, Cost: {cost}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[FirebaseAnalyticsManager] Error logging purchase attempt: {e.Message}");
            }
        }

        public void LogPurchaseComplete(string skinId, int cost)
        {
            if (!_isInitialized) return;

            try
            {
                Firebase.Analytics.FirebaseAnalytics.LogEvent(
                    "purchase",
                    new Firebase.Analytics.Parameter[]
                    {
                        new Firebase.Analytics.Parameter("item_id", skinId),
                        new Firebase.Analytics.Parameter("price", cost),
                        new Firebase.Analytics.Parameter("currency", "coins"),
                        new Firebase.Analytics.Parameter("value", cost),
                        new Firebase.Analytics.Parameter("timestamp", DateTimeOffset.UtcNow.ToUnixTimeSeconds())
                    }
                );
                
                Debug.Log($"[FirebaseAnalyticsManager] Purchase Complete logged - Skin: {skinId}, Cost: {cost}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[FirebaseAnalyticsManager] Error logging purchase complete: {e.Message}");
            }
        }

        public void LogSkinApplied(string skinId, bool isWhitePieces)
        {
            if (!_isInitialized) return;

            try
            {
                Firebase.Analytics.FirebaseAnalytics.LogEvent(
                    "skin_applied",
                    new Firebase.Analytics.Parameter[]
                    {
                        new Firebase.Analytics.Parameter("item_id", skinId),
                        new Firebase.Analytics.Parameter("piece_color", isWhitePieces ? "white" : "black"),
                        new Firebase.Analytics.Parameter("timestamp", DateTimeOffset.UtcNow.ToUnixTimeSeconds())
                    }
                );
                
                Debug.Log($"[FirebaseAnalyticsManager] Skin Applied logged - Skin: {skinId}, White Pieces: {isWhitePieces}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[FirebaseAnalyticsManager] Error logging skin applied: {e.Message}");
            }
        }

        #endregion

        #region User Properties

        public void SetUserProperties()
        {
            if (!_isInitialized) return;

            try
            {
                // Set user properties for segmentation
                string ownedSkinsJson = PlayerPrefs.GetString("OwnedSkins", "");
                string ownedSkinsCount = "0";
                
                if (!string.IsNullOrEmpty(ownedSkinsJson))
                {
                    try
                    {
                        DLCStoreUI.OwnedSkinsData data = JsonUtility.FromJson<DLCStoreUI.OwnedSkinsData>(ownedSkinsJson);
                        ownedSkinsCount = data.entries.Count.ToString();
                    }
                    catch
                    {
                        ownedSkinsCount = "0";
                    }
                }
                
                Firebase.Analytics.FirebaseAnalytics.SetUserProperty("owned_skins_count", ownedSkinsCount);
                Firebase.Analytics.FirebaseAnalytics.SetUserProperty("player_coins", PlayerPrefs.GetInt("PlayerCoins", 1000).ToString());
                Firebase.Analytics.FirebaseAnalytics.SetUserProperty("auto_apply_skins", PlayerPrefs.GetInt("AutoApplySkins", 0).ToString());
                
                Debug.Log("[FirebaseAnalyticsManager] User properties updated");
            }
            catch (Exception e)
            {
                Debug.LogError($"[FirebaseAnalyticsManager] Error setting user properties: {e.Message}");
            }
        }

        #endregion
    }
}