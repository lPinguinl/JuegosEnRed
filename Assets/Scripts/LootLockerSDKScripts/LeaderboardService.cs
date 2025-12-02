using LootLocker.Requests;
using UnityEngine;

public static class LeaderboardService
{
    /// <summary>
    /// Envia el puntaje del jugador actual al leaderboard especificado, 
    /// utilizando el MemberID para acumular puntos.
    /// </summary>
    public static void SubmitScore(int score, string leaderboardKey, System.Action<LootLockerSubmitScoreResponse> onDone = null)
    {
        if (!LootLockerBootstrap.SessionStarted)
        {
            Debug.LogError("No se puede enviar puntaje: la sesión de LootLocker no ha iniciado.");
            onDone?.Invoke(null);
            return;
        }

        // Obtener el MemberID del jugador logueado (CLAVE para la acumulación)
        string memberId = LootLocker.Requests.LootLocker.GetPlayerID();

        Debug.Log($"Enviando puntaje {score} para memberId: {memberId} al leaderboard: {leaderboardKey}");

        // Envía el puntaje.
        LootLockerSDKManager.SubmitScore(memberId, score, leaderboardKey, response =>
        {
            if (!response.success)
            {
                Debug.LogError($"Fallo al enviar puntaje. success={response.success} status={response.statusCode}");
            }
            else
            {
                Debug.Log($"Puntaje enviado exitosamente. Rank: {response.rank}, Nuevo Score: {response.score}");
            }

            onDone?.Invoke(response);
        });
    }
}