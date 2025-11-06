using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "HackOption", menuName = "Hacking/Hack Option")]
public class HackOptionSO : ScriptableObject
{
    public enum HackType
    {
        ExplodeNow,
        Trap,
        Disable,
        Alarm,
        Enable
    }

    [Header("Option Settings")]
    public string optionName;
    public Sprite icon;
    public HackType optionType;

    [Header("Input Sequence")]
    public bool isRandom = false;
    public int randomLength = 4;
    public List<ArrowUI.Direction> sequence;

    [TextArea]
    public string description;
}
