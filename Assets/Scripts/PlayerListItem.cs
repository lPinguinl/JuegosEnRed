using UnityEngine;
using TMPro;
using Photon.Realtime;
using ExitGames.Client.Photon;

public class PlayerListItem : MonoBehaviour
{
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text readyText;

    public void Setup(Player p)
    {
        nameText.text = p.NickName;
        bool ready = p.CustomProperties.ContainsKey("isReady") && (bool)p.CustomProperties["isReady"];
        readyText.text = ready ? "Ready" : "Not Ready";
    }
}