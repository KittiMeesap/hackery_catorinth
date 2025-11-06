using UnityEngine;
using UnityEditor;

public class RevealHiddenShadows
{
    [MenuItem("Tools/Reveal Hidden Sprite Shadows")]
    static void Reveal()
    {
        int count = 0;
        var all = Resources.FindObjectsOfTypeAll<SpriteRenderer>();
        foreach (var sr in all)
        {
            if (sr.gameObject.name == "SpriteShadow")
            {
                sr.gameObject.hideFlags = HideFlags.None;
                EditorUtility.SetDirty(sr.gameObject);
                count++;
            }
        }
        Debug.Log($" Revealed {count} hidden SpriteShadow objects.");
    }
}
