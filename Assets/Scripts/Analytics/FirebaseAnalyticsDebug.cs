using UnityEngine;
using System.Collections;

namespace Game.Analytics
{
    public class FirebaseAnalyticsDebug : MonoBehaviour
    {
        public static FirebaseAnalyticsDebug Instance;

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
            }
        }

        private void Start()
        {
            StartCoroutine(SendTestEvent());
        }

        private IEnumerator SendTestEvent()
        {
            yield return new WaitForSeconds(3);
            
            // Send a test event
            if (FirebaseAnalyticsManager.Instance != null)
            {
                SendTestAnalyticsEvent();
                Debug.Log("[AnalyticsDebug] Test event sent. Check DebugView in Firebase Console.");
            }
            else
            {
                Debug.LogError("[AnalyticsDebug] FirebaseAnalyticsManager not found");
            }
        }
        
        public void SendTestAnalyticsEvent()
        {
            if (FirebaseAnalyticsManager.Instance != null)
            {
                Firebase.Analytics.FirebaseAnalytics.LogEvent(
                    "test_event",
                    new Firebase.Analytics.Parameter[]
                    {
                        new Firebase.Analytics.Parameter("test_time", System.DateTime.UtcNow.ToLongTimeString())
                    }
                );
                
                Debug.Log("[AnalyticsDebug] Test event sent with timestamp");
            }
        }
    }
}