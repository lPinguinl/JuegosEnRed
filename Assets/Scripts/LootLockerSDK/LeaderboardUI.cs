using LootLocker.Requests;
using System.Text;
using UnityEngine;

public class LeaderboardUI : MonoBehaviour
{
    [SerializeField] string leaderboardKey = "e580355f5c684ef4908f55eaf8d9fd43";
    [SerializeField] int count = 10;
    [SerializeField] TMPro.TextMeshProUGUI tableText;

    void OnEnable()
    {
        Refresh();
    }

    public void Refresh()
    {
        if (!LootLockerBootstrap.SessionStarted)
        {
            tableText.text = "Logueando...";
            return;
        }

        LootLockerSDKManager.GetScoreList(leaderboardKey, count, 0, response =>
        {
            if (!response.success)
            {
                tableText.text = "Error...";
                return;
            }

            var sb = new StringBuilder();
            sb.AppendLine("Rank  Name              Score");
            sb.AppendLine("----  ----------------  -----");

            var items = response.items;
            if (items == null || items.Length == 0)
            {
                sb.AppendLine("No se registró nada todavía");
            }
            else
            {
                foreach (var item in items)
                {
                    string name = string.IsNullOrEmpty(item.player.name)
                        ? "Player " + item.player.id
                        : item.player.name;
                    sb.AppendLine($"{item.rank,4}  {name,-16}  {item.score,5}");
                }
            }

            tableText.text = sb.ToString();
        });
    }

    // Botones opcionales:
    public void OnSubmitScoreTMP(TMPro.TMP_InputField scoreInput)
    {
        if (int.TryParse(scoreInput.text, out var score))
        {
            LeaderboardService.SubmitScore(score, leaderboardKey, _ => Refresh());
        }
    }

    public void OnSetNameTMP(TMPro.TMP_InputField nameInput)
    {
        PlayerNameHelper.SetPlayerName(nameInput.text);
    }
}