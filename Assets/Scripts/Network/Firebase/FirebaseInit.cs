using Firebase;
using Firebase.Extensions;
using Firebase.Firestore;
using UnityEngine;

namespace Network.Firebase
{
    public class FirebaseInit : MonoBehaviour
    {
        private void Awake()
        {
            FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
            {
                var dependencyStatus = task.Result;
                if (dependencyStatus == DependencyStatus.Available)
                {
                    Debug.Log("[FirebaseInit] Firebase dependencies resolved successfully.");
                    FirebaseFirestore.DefaultInstance.Settings.PersistenceEnabled = false;
                }
                else
                {
                    Debug.LogError($"[FirebaseInit] Could not resolve all Firebase dependencies: {dependencyStatus}");
                }
            });
        }
    }
}
