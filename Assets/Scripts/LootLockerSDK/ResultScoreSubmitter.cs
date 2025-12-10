using UnityEngine;
using System.Collections;

public class ResultScoreSubmitter : MonoBehaviour
{
    [Header("Configuración")]
    [SerializeField] string leaderboardKey = "e580355f5c684ef4908f55eaf8d9fd43";

    void Start()
    {
        
        int finalScore = PlayerPrefs.GetInt("ScoreDeLaPartida", 0);

        Debug.Log($"[ResultScoreSubmitter] Puntaje recuperado de memoria: {finalScore}");

        // Envia a LootLocker 

        LeaderboardService.SubmitScore(finalScore, leaderboardKey, (success) =>
        {
            if (success)
            {
                Debug.Log("¡Puntaje enviado y procesado exitosamente!");

                // Actualiza la tabla de leaderboard visual
                
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