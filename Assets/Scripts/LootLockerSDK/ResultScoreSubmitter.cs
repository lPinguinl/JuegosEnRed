using UnityEngine;
using LootLocker.Requests;

public class ResultScoreSubmitter : MonoBehaviour
{
    // Asegúrate de que esta key coincida con la de tu LeaderboardUI
    [SerializeField] string leaderboardKey = "e580355f5c684ef4908f55eaf8d9fd43";

    // Este script se activa automáticamente al cargar la ResultScene
    void Start()
    {
        // 1. LEE EL PUNTAJE REAL guardado por el ScoreManager (o 0 si no encuentra nada)
        // Usamos 0 como valor por defecto, no 100.
        int finalScore = PlayerPrefs.GetInt("ScoreDeLaPartida", 0);

        // Si el score es 0, puedes decidir si enviarlo o no. Lo enviamos por si quieres registrar 0.
        if (finalScore <= 0)
        {
            Debug.Log($"[ResultScoreSubmitter] No se detectó puntaje (o es 0), no se enviará a LootLocker. Score: {finalScore}");
            // return; // Descomenta esta línea si NO quieres registrar partidas con 0 puntos.
        }

        Debug.Log($"[ResultScoreSubmitter] Enviando puntaje a LootLocker: {finalScore}");

        // 2. Llama al servicio estático para enviar
        LeaderboardService.SubmitScore(finalScore, leaderboardKey, (success) =>
        {
            if (success)
            {
                Debug.Log("¡Puntaje enviado exitosamente! Actualizando Leaderboard...");

                // 3. Opcional: Refrescar la tabla de posiciones inmediatamente
                var leaderboardUI = FindObjectOfType<ResultSceneLeaderboard>();
                leaderboardUI?.Refresh();
            }
            else
            {
                Debug.LogError("Error al enviar el puntaje a LootLocker.");
            }
        });
    }
}