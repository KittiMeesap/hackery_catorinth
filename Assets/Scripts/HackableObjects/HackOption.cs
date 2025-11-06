using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class HackOption
{
    public string optionName;
    public Sprite icon;
    public List<ArrowUI.Direction> sequence;
    public UnityEngine.Events.UnityEvent onSuccess;
}
