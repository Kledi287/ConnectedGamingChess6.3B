using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Firebase.Storage;

namespace Game.DLC
{
    public class DLCManager : MonoBehaviour
    {
        public static DLCManager Instance;

        private Dictionary<string, Texture2D> textureCache = new Dictionary<string, Texture2D>();

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        public async Task<Texture2D> DownloadSkinAsync(string skinPath)
        {
            if (textureCache.ContainsKey(skinPath))
                return textureCache[skinPath];
            
            var storage = FirebaseStorage.DefaultInstance;
            StorageReference skinRef = storage.GetReference(skinPath);
            
            const long maxAllowedSize = 5 * 1024 * 1024; // 5MB limit
            byte[] fileBytes;
            try
            {
                fileBytes = await skinRef.GetBytesAsync(maxAllowedSize);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[DLCManager] Failed to download '{skinPath}': {e}");
                return null;
            }
        
            Texture2D tex = new Texture2D(2, 2, TextureFormat.ARGB32, false);
            if (!tex.LoadImage(fileBytes))
            {
                Debug.LogError("[DLCManager] Failed to load image data into Texture2D.");
                return null;
            }
        
            textureCache[skinPath] = tex;
            Debug.Log($"[DLCManager] Downloaded & cached skin: {skinPath}");
            return tex;
        }

        public void ApplySkinToPiece(GameObject pieceGO, Texture2D texture)
        {
            Renderer pieceRenderer = pieceGO.GetComponentInChildren<Renderer>();
            if (pieceRenderer != null)
            {
                pieceRenderer.material.mainTexture = texture;
            }
            else
            {
                Debug.LogWarning($"[DLCManager] No Renderer found on '{pieceGO.name}'!");
            }
        }
        
        public void ApplySkinToAllPieces(List<GameObject> pieces, Texture2D texture)
        {
            if (pieces == null || pieces.Count == 0 || texture == null)
            {
                Debug.LogWarning("[DLCManager] Cannot apply skin - no valid pieces or texture provided");
                return;
            }

            string debugInfo = "";
            int count = 0;
            int failedCount = 0;

            foreach (GameObject pieceGO in pieces)
            {
                // Skip null pieces
                if (pieceGO == null)
                {
                    failedCount++;
                    continue;
                }
        
                try
                {
                    Renderer pieceRenderer = pieceGO.GetComponentInChildren<Renderer>();
                    if (pieceRenderer != null && pieceRenderer.material != null)
                    {
                        pieceRenderer.material.mainTexture = texture;
                        count++;
            
                        // Include piece info in debug log
                        VisualPiece vp = pieceGO.GetComponent<VisualPiece>();
                        if (vp != null && debugInfo.Length < 100)
                        {
                            debugInfo += $"{vp.PieceColor} {pieceGO.name}, ";
                        }
                    }
                    else
                    {
                        failedCount++;
                        Debug.LogWarning($"[DLCManager] No valid Renderer found on '{pieceGO.name}'!");
                    }
                }
                catch (MissingReferenceException)
                {
                    failedCount++;
                    Debug.LogWarning("[DLCManager] Skipping destroyed piece GameObject");
                }
                catch (Exception ex)
                {
                    failedCount++;
                    Debug.LogError($"[DLCManager] Error applying skin to piece: {ex.Message}");
                }
            }

            Debug.Log($"[DLCManager] Applied skin to {count}/{pieces.Count} pieces. Failed: {failedCount}. First few: {debugInfo}");
        }
    }
}

