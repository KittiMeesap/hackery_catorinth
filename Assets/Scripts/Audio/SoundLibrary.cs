using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Audio/Sound Library")]
public class SoundLibrary : ScriptableObject
{
    [System.Serializable]
    public class SoundEntry
    {
        public string key;
        public AudioClip clip;
    }

    [SerializeField] private List<SoundEntry> sounds = new();

    public AudioClip GetClip(string key)
    {
        var entry = sounds.Find(s => s.key == key);
        return entry != null ? entry.clip : null;
    }
}
