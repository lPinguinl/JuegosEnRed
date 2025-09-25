using System.Text;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using TMPro;

public class ResultSceneController : MonoBehaviour
{
    private const string MatchWinnerKey = "MatchWinner";
    private const string ScoresActorNumbersKey = "MatchScoresActorNumbers";
    private const string ScoresValuesKey = "MatchScoresValues";
    private const string CrownScoreKey = "CrownScore";

    [Header("UI")]
    [SerializeField] private TMP_Text winnerText;      // Asigna en Inspector
    [SerializeField] private TMP_Text scoreboardText;  // Asigna en Inspector (opcional)

    private void Start()
    {
        RenderResults();
    }

    private void RenderResults()
    {
        if (PhotonNetwork.CurrentRoom == null)
        {
            Debug.LogWarning("[ResultSceneController] CurrentRoom is null. No result information available.");
            SetText(winnerText, "No hay resultados disponibles");
            SetText(scoreboardText, string.Empty);
            return;
        }

        // Winner
        int winnerActorNumber = -1;
        if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(MatchWinnerKey, out object winnerObj))
        {
            winnerActorNumber = (int)winnerObj;
        }
        Player winnerPlayer = FindPlayerByActorNumber(winnerActorNumber);

        if (winnerPlayer != null)
        {
            SetText(winnerText, $"Winner: {winnerPlayer.NickName}");
        }
        else
        {
            SetText(winnerText, "Winner: -");
            Debug.LogWarning("[ResultSceneController] No se encontró un ganador válido en las Room Properties.");
        }

        // Scoreboard
        string scoreboard = BuildScoreboardText();
        SetText(scoreboardText, scoreboard);

        // Logs de respaldo
        if (!string.IsNullOrEmpty(scoreboard))
        {
            Debug.Log($"[ResultScene] Puntajes registrados:\n{scoreboard}");
        }
    }

    private void SetText(TMP_Text tmp, string value)
    {
        if (tmp != null) tmp.text = value ?? string.Empty;
    }

    private Player FindPlayerByActorNumber(int actorNumber)
    {
        if (actorNumber == -1) return null;
        foreach (Player player in PhotonNetwork.PlayerList)
        {
            if (player.ActorNumber == actorNumber) return player;
        }
        return null;
    }

    private string BuildScoreboardText()
    {
        var sb = new StringBuilder();
        Player[] players = PhotonNetwork.PlayerList;
        if (players == null || players.Length == 0) return string.Empty;

        // Si hay snapshot publicado, úsalo (orden estable)
        if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(ScoresActorNumbersKey, out object actorsObj) &&
            PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(ScoresValuesKey, out object scoresObj) &&
            actorsObj is int[] actorNumbers &&
            scoresObj is int[] scores &&
            actorNumbers.Length == scores.Length)
        {
            for (int i = 0; i < actorNumbers.Length; i++)
            {
                Player player = FindPlayerByActorNumber(actorNumbers[i]);
                string nickname = player != null ? player.NickName : $"Actor {actorNumbers[i]}";
                sb.AppendLine($"{nickname}: {scores[i]} pts");
            }
            return sb.ToString();
        }

        // Si no hay snapshot, arma desde Custom Properties actuales
        foreach (Player player in players)
        {
            int score = 0;
            if (player.CustomProperties.TryGetValue(CrownScoreKey, out object scoreObj))
            {
                score = (int)scoreObj;
            }
            sb.AppendLine($"{player.NickName}: {score} pts");
        }
        return sb.ToString();
    }
}