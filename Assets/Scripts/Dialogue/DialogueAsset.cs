using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;

[CreateAssetMenu(menuName = "Dialogue/Sequence")]
public class DialogueAsset : ScriptableObject
{
    [System.Serializable]
    public class Line
    {
        [Tooltip("Who speaks this line")]
        public CharacterData characterData;

        [Tooltip("Optional hint; not used for deciding the speaker anymore")]
        public bool isPlayer;

        [Tooltip("Localized line text (Table e.g. 'Dialogue')")]
        public LocalizedString dialogueText;

        [Tooltip("Optional portrait override for this line")]
        public Sprite overridePortrait;
    }

    public List<Line> lines = new List<Line>();
}
