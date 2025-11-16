using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;

public class PlayerListItem : MonoBehaviourPunCallbacks
{
    [SerializeField] private TMP_Text playerNameText;
    [SerializeField] private TMP_Text readyStatusText;
    [SerializeField] private Image background;      // Opcional, para tintar el fondo

    private Player player;

    public void SetPlayerInfo(Player player)
    {
        this.player = player;
        UpdateInfo();
    }

    public void UpdateInfo()
    {
        if (player == null)
        {
            return;
        }

        // Nombre
        if (playerNameText != null)
        {
            playerNameText.text = player.NickName;
        }

        // Estado Ready/Not Ready
        bool isReady = player.CustomProperties.TryGetValue("isReady", out object readyObj) &&
                       readyObj is bool ready && ready;

        if (readyStatusText != null)
        {
            readyStatusText.text = isReady ? "Ready" : "Not Ready";
        }

        ApplyColor();
    }

    private void ApplyColor()
    {
        if (playerNameText == null || player == null)
        {
            return;
        }

        if (player.CustomProperties.TryGetValue(LobbyManager.COLOR_KEY, out object colorIdxObj) &&
            colorIdxObj is int colorIdx)
        {
            Color playerColor = LobbyManager.GetPaletteColor(colorIdx);
            playerNameText.color = playerColor;

            if (background != null)
            {
                background.color = playerColor * 0.6f; // opcional, ajusta intensidad
            }
        }
        else
        {
            playerNameText.color = Color.white;
            if (background != null)
            {
                background.color = Color.white;
            }
        }
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        if (targetPlayer == player &&
            (changedProps.ContainsKey(LobbyManager.COLOR_KEY) || changedProps.ContainsKey("isReady")))
        {
            UpdateInfo();
        }
    }

    public Player GetPlayer() => player;
}