using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HackOptionUI : MonoBehaviour
{
    public List<ArrowUI.Direction> sequence;
    public List<ArrowUI> arrowUIs = new();
    public System.Action onComplete;
    public HackOptionSO optionData;

    public void Setup(List<ArrowUI.Direction> seq, System.Action onCompleteCallback)
    {
        sequence = new List<ArrowUI.Direction>(seq);
        onComplete = onCompleteCallback;
    }

    public void Highlight(int index)
    {
        for (int i = 0; i < arrowUIs.Count; i++)
        {
            if (i <= index)
                arrowUIs[i].SetCorrect();
            else
                arrowUIs[i].ResetArrow();
        }
    }

    public void SetIncorrect()
    {
        StartCoroutine(FlashIncorrectRoutine());
    }

    private IEnumerator FlashIncorrectRoutine()
    {
        foreach (var arrow in arrowUIs)
            arrow.SetIncorrect();

        yield return new WaitForSeconds(0.3f);

        ResetAll();
    }

    public void ResetAll()
    {
        foreach (var arrow in arrowUIs)
            arrow.ResetArrow();
    }
}
