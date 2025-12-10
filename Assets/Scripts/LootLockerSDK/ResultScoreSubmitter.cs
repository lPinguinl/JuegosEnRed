using UnityEngine;
using System.Collections;

public class ResultScoreSubmitter : MonoBehaviour
{
    [Header("Configuración")]
    // Asegúrate de que esta Key sea la misma que en tu LeaderboardUI
    [SerializeField] string leaderboardKey = "e580355f5c684ef4908f55eaf8d9fd43";

    void Start()
    {
        // 1. RECUPERAR EL PUNTAJE
        // Cambiamos el valor por defecto a 0. 
        // Si aparece 0, significa que el guardado falló o el jugador no hizo puntos.
        // Si aparece 100, era el error anterior.
        int finalScore = PlayerPrefs.GetInt("ScoreDeLaPartida", 0);

        Debug.Log($"[ResultScoreSubmitter] Puntaje recuperado de memoria: {finalScore}");

        // 2. ENVIAR A LOOTLOCKER
        // Llamamos al servicio que arreglamos previamente.
        LeaderboardService.SubmitScore(finalScore, leaderboardKey, (success) =>
        {
            if (success)
            {
                Debug.Log("¡Puntaje enviado y procesado exitosamente!");

                // 3. ACTUALIZAR LA TABLA VISUAL
                // Buscamos el script de la tabla en la escena y le decimos que se refresque
                var leaderboardDisplay = FindObjectOfType<ResultSceneLeaderboard>();
                if (leaderboardDisplay != null)
                {
                    leaderboardDisplay.Refresh();
                }
            }
            else
            {
                Debug.LogError("Hubo un error al enviar el puntaje.");
            }
        });
    }
}