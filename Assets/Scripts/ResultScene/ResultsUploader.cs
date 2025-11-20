using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;

public class ResultsUploader : MonoBehaviour
{
    [Header("Leaderboard")]
    [SerializeField] string leaderboardKey = "e580355f5c684ef4908f55eaf8d9fd43";
    [SerializeField] ScoreManager scoreManager;

    [Header("UI")]
    [SerializeField] private Button backToMenuButton;   // <- botón opcional

    bool uploaded;

    void Awake()
    {
        // Registrar callback del botón si está asignado
        if (backToMenuButton != null)
        {
            backToMenuButton.onClick.AddListener(OnBackToMenuClicked);
        }
    }

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

        LeaderboardService.SubmitCumulativeScoreForCurrentPlayer(myMatchScore, leaderboardKey, success =>
        {
            uploaded = success;
            if (!success)
            {
                Invoke(nameof(TryUpload), 2.0f);
            }
        });
    }

    // ---- Botón Volver al Menú ----
    private void OnBackToMenuClicked()
    {
        
        PhotonNetwork.LeaveRoom();
        PhotonNetwork.LoadLevel("MainMenu");
    }
}