using LootLocker.Requests;
using UnityEngine;

public class LeaderboardService : MonoBehaviour
{
    // Envía el score al leaderboard
    public static void SubmitScore(int score, string leaderboardKey, System.Action<bool> onDone = null)
    {
        var memberId = SystemInfo.deviceUniqueIdentifier;

        LootLockerSDKManager.SubmitScore(memberId, score, leaderboardKey, response =>
        {
            Debug.Log($"SubmitScore success={response.success} status={response.statusCode}");
            if (!response.success)
            {
                Debug.LogError("Fallo el envío de score");
                onDone?.Invoke(false);
                return;
            }

            onDone?.Invoke(true);
        });
    }
}