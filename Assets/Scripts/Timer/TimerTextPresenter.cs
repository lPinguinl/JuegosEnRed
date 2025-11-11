using TMPro;
using UnityEngine;

public class TimerTextPresenter : MonoBehaviour, ITimeDisplay
{
    [SerializeField] private TMP_Text text;
    [SerializeField] private bool hideWhenZero = false;

    public void SetTime(double secondsRemaining)
    {
        int clamped = Mathf.Max(0, Mathf.CeilToInt((float)secondsRemaining));
        int m = clamped / 60;
        int s = clamped % 60;

        if (text != null)
        {
            text.text = $"{m:00}:{s:00}";
            if (hideWhenZero) text.gameObject.SetActive(clamped > 0);
        }
    }
}