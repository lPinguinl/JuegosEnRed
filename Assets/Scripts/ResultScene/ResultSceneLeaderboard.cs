using System.Text;
using UnityEngine;
using TMPro;
using LootLocker.Requests;

public class ResultSceneLeaderboard : MonoBehaviour
{
    [Header("LootLocker")]
    [SerializeField] string leaderboardKey = "e580355f5c684ef4908f55eaf8d9fd43"; // remplaza por tu key o usa la sobrecarga con ID
    [SerializeField] int count = 10;

    [Header("UI")]
    [SerializeField] TMP_Text tableText;

    void OnEnable()
    {
        LootLockerBootstrap.OnSessionStarted += HandleSessionStarted;

        if (LootLockerBootstrap.SessionStarted)
            Refresh();
        else
            SetText("Conectando...");
    }

    void OnDisable()
    {
        LootLockerBootstrap.OnSessionStarted -= HandleSessionStarted;
    }

    void HandleSessionStarted()
    {
        Refresh();
    }

    public void Refresh()
    {
        if (!LootLockerBootstrap.SessionStarted)
        {
            SetText("Conectando...");
            return;
        }

        LootLockerSDKManager.GetScoreList(leaderboardKey, count, 0, response =>
        {
            if (!response.success)
            {
                SetText($"Error al cargar leaderboard (status {response.statusCode})");
                Debug.LogError($"GetScoreList fallo. success={response.success} status={response.statusCode}");
                return;
            }

            var sb = new StringBuilder();
            sb.AppendLine("Rank   Name                 Score");
            sb.AppendLine("---------------------------------");

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

                    sb.AppendLine($"{item.rank,4}   {name,-20}   {item.score,6}");
                }
            }

            SetText(sb.ToString());
        });
    }

    void SetText(string msg)
    {
        if (tableText != null) tableText.text = msg;
    }
}