using UnityEngine;
using Photon.Pun;

public class ResultsUploader : MonoBehaviour
{
    [SerializeField] string leaderboardKey = "e580355f5c684ef4908f55eaf8d9fd43";
    [SerializeField] ScoreManager scoreManager; 

    bool uploaded;

    void OnEnable()
    {
        TryUpload();
    }

    void TryUpload()
    {
        if (uploaded) return;

        if (!LootLockerBootstrap.SessionStarted)
        {
            Debug.LogWarning("[ResultsUploader] LootLocker no listo aún. Reintentando en 1s...");
            Invoke(nameof(TryUpload), 1.0f);
            return;
        }

        if (scoreManager == null)
        {
            scoreManager = FindObjectOfType<ScoreManager>(true);
        }

        int myActorNumber = PhotonNetwork.LocalPlayer?.ActorNumber ?? -1;
        if (myActorNumber < 0)
        {
            Debug.LogWarning("[ResultsUploader] LocalPlayer inválido.");
            return;
        }

        int myMatchScore = 0;
        if (scoreManager != null)
        {
            scoreManager.TryGetScoreForActor(myActorNumber, out myMatchScore);
        }

        // Envia mi score de la partida como acumulado al leaderboard global
        LeaderboardService.SubmitCumulativeScoreForCurrentPlayer(myMatchScore, leaderboardKey, success =>
        {
            uploaded = success;
            if (!success)
            {
                // Reintento simple si falla la red
                Invoke(nameof(TryUpload), 2.0f);
            }
        });
    }
}