using LootLocker.Requests;
using UnityEngine;

public class LeaderboardService : MonoBehaviour
{
    // Guardamos el total acumulado localmente
    private const string LocalTotalKey = "e580355f5c684ef4908f55eaf8d9fd43";

    // Envía un score absoluto al leaderboard
    public static void SubmitScore(int score, string leaderboardKey, System.Action<bool> onDone = null)
    {
        LootLockerSDKManager.SubmitScore("", score, leaderboardKey, response =>
        {
            if (!response.success)
            {
                Debug.LogError("[Leaderboard] Falló SubmitScore");
                onDone?.Invoke(false);
                return;
            }
            Debug.Log("[Leaderboard] Score enviado (absoluto)");
            onDone?.Invoke(true);
        });
    }

    // Acumulado local: suma deltaScore al total guardado en PlayerPrefs y sube ese total
    public static void SubmitCumulativeScoreForCurrentPlayer(int deltaScore, string leaderboardKey, System.Action<bool> onDone = null)
    {
        if (!LootLockerBootstrap.SessionStarted)
        {
            Debug.LogWarning("[Leaderboard] La sesión de LootLocker no está iniciada.");
            onDone?.Invoke(false);
            return;
        }

        // Nunca restar
        deltaScore = Mathf.Max(0, deltaScore);

        // Leer total actual local
        int currentTotal = PlayerPrefs.GetInt(LocalTotalKey, 0);
        int newTotal = currentTotal + deltaScore;

        // Guardar localmente
        PlayerPrefs.SetInt(LocalTotalKey, newTotal);
        PlayerPrefs.Save();

        // Enviar total al leaderboard
        LootLockerSDKManager.SubmitScore("", newTotal, leaderboardKey, submitResp =>
        {
            if (!submitResp.success)
            {
                Debug.LogError("[Leaderboard] Falló SubmitScore (acumulado local)");
                onDone?.Invoke(false);
                return;
            }

            Debug.Log($"[Leaderboard] Total acumulado local actualizado y enviado: {currentTotal} + {deltaScore} = {newTotal}");
            onDone?.Invoke(true);
        });
    }
}