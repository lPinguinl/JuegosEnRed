using TMPro;
using UnityEngine;

// Presentador de UI del timer.
// Recibe el tiempo restante (en segundos) y actualiza un TMP_Text en formato mm:ss.
public class TimerTextPresenter : MonoBehaviour, ITimeDisplay
{
    // Asigná desde el Inspector el TextMeshPro donde querés mostrar el tiempo
    [SerializeField] private TMP_Text timeText;

    // Llamado por el sistema del timer cada frame para refrescar el texto
    public void SetTime(double secondsRemaining)
    {
        // Evita números negativos cuando el tiempo llega a 0
        secondsRemaining = Mathf.Max(0f, (float)secondsRemaining);

        // Redondea hacia arriba para no saltar de 00:01 a 00:00 demasiado pronto
        int s = Mathf.CeilToInt((float)secondsRemaining);

        // Convierte a minutos y segundos
        int m = s / 60;
        int r = s % 60;

        // Escribe en formato mm:ss (ej: 01:05)
        if (timeText != null)
            timeText.text = $"{m:00}:{r:00}";
    }
}