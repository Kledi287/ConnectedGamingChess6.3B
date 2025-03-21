using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Firebase.Storage;

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
        // Check cache
        if (textureCache.ContainsKey(skinPath))
            return textureCache[skinPath];

        // Firebase Storage
        var storage = FirebaseStorage.DefaultInstance;
        StorageReference skinRef = storage.GetReference(skinPath);

        const long maxAllowedSize = 5 * 1024 * 1024;
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

        // Convert to Texture2D
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
        foreach (GameObject pieceGO in pieces)
        {
            ApplySkinToPiece(pieceGO, texture);
        }
    }

}