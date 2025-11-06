using UnityEngine;
using UnityEngine.Localization;

[CreateAssetMenu(menuName = "Dialogue/Character")]
public class CharacterData : ScriptableObject
{
    [Tooltip("Stable ID to compare characters (optional but recommended).")]
    public string characterId;

    [Tooltip("Localized display name (Table e.g. 'Names')")]
    public LocalizedString displayName;

    [Tooltip("Default portrait")]
    public Sprite defaultPortrait;
}
