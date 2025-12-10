using LootLocker.Requests;
using System.Collections; 
using System.Text;
using UnityEngine;


public class LeaderboardUI : MonoBehaviour
{
    [SerializeField] string leaderboardKey = "e580355f5c684ef4908f55eaf8d9fd43";
    [SerializeField] int count = 10;
    [SerializeField] TMPro.TextMeshProUGUI tableText;

    [SerializeField] float autoRefreshInterval = 0f;
    Coroutine autoRoutine;

    void OnEnable()
    {
        LootLockerBootstrap.OnSessionStarted += HandleSessionStarted;

        if (LootLockerBootstrap.SessionStarted)
            Refresh();
    }

    void Start()
    {
        if (autoRefreshInterval > 0f)
            autoRoutine = StartCoroutine(AutoRefreshLoop());
    }

    void OnDisable()
    {
        LootLockerBootstrap.OnSessionStarted -= HandleSessionStarted;

        if (autoRoutine != null)
        {
            StopCoroutine(autoRoutine);
            autoRoutine = null;
        }
    }

    void HandleSessionStarted()
    {
        Refresh();
    }

    IEnumerator AutoRefreshLoop()
    {
        while (!LootLockerBootstrap.SessionStarted)
            yield return null;

        var wait = new WaitForSeconds(autoRefreshInterval);
        while (true)
        {
            Refresh();
            yield return wait;
        }
    }

    // FUNCIÓN CLAVE (A) - Conectada al botón de la UI que usa un Input Field.
    public void OnSubmitScoreTMP(TMPro.TMP_InputField scoreInput)
    {
        if (int.TryParse(scoreInput.text, out var score))
        {
            LeaderboardService.SubmitScore(score, leaderboardKey, _ => Refresh());
        }
    }

    // FUNCIÓN CLAVE (B) - Usada para enviar un score que ya fue calculado en el código (Result Scene).
    // Conecta tu Result Manager a esta función si no tienes Input Field.
    public void SubmitCalculatedScore(int scoreToSend)
    {
        if (!LootLockerBootstrap.SessionStarted)
        {
            Debug.LogWarning("No se puede enviar el puntaje, la sesión de LootLocker no ha iniciado.");
            return;
        }

        LeaderboardService.SubmitScore(scoreToSend, leaderboardKey, _ => Refresh());
    }

    // Función conectada al botón para establecer el nombre (en el menú).
    public void OnSetNameTMP(TMPro.TMP_InputField nameInput)
    {
        var newName = nameInput.text;

        PlayerNameHelper.SetPlayerName(newName, success =>
        {
            if (!success)
            {
                Refresh();
                return;
            }
            Refresh();
        });
    }

    public void Refresh()
    {
        if (!LootLockerBootstrap.SessionStarted)
        {
            if (tableText) tableText.text = "Logueando...";
            return;
        }

        LootLockerSDKManager.GetScoreList(leaderboardKey, count, 0, response =>
        {
            if (!response.success)
            {
                if (tableText) tableText.text = "Error...";
                Debug.LogError($"GetScoreList fallo. success={response.success} status={response.statusCode}");
                return;
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Global Leaderboard");
            sb.AppendLine("------------------");

            var items = response.items;
            if (items == null || items.Length == 0)
            {
                sb.AppendLine("No hay entradas aún.");
            }
            else
            {
                foreach (var item in items)
                {
                    string name = string.IsNullOrEmpty(item.player.name)
                        ? $"Player {item.player.id}"
                        : item.player.name;

                    sb.AppendLine($"{item.rank}. {name} - {item.score}");
                }
            }

            if (tableText) tableText.text = sb.ToString();
        });
    }
}