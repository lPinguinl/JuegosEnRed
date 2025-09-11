using UnityEngine;
using TMPro;
using Photon.Realtime;

public class PlayerListItem : MonoBehaviour
{
    [SerializeField] private TMP_Text playerNameText;
    [SerializeField] private TMP_Text readyStatusText;

    private Player player;

    public void SetPlayerInfo(Player player)
    {
        this.player = player;
        UpdateInfo();
    }

    public void UpdateInfo()
    {
        if (playerNameText != null)
            playerNameText.text = player.NickName;

        bool isReady = player.CustomProperties.ContainsKey("isReady") && (bool)player.CustomProperties["isReady"];
        if (readyStatusText != null)
            readyStatusText.text = isReady ? "✅ Ready" : "⏳ Not Ready";
    }

    public Player GetPlayer() => player;
}