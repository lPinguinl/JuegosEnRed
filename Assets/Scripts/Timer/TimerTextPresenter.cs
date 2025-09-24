using TMPro;
using UnityEngine;

public class TimerTextPresenter : MonoBehaviour, ITimeDisplay
{
    [SerializeField] private TMP_Text timeText;

    // Formatea mm:ss
    public void SetTime(double secondsRemaining)
    {
        secondsRemaining = Mathf.Max(0f, (float)secondsRemaining);
        int s = Mathf.CeilToInt((float)secondsRemaining);
        int m = s / 60;
        int r = s % 60;

        if (timeText != null)
            timeText.text = $"{m:00}:{r:00}";
    }
}