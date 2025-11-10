using UnityEngine;
using TMPro;
using Photon.Realtime;
using ExitGames.Client.Photon;

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
        if (player == null) return;

        // 1️⃣ Nombre del jugador
        if (playerNameText != null)
            playerNameText.text = player.NickName;

        // 2️⃣ Estado "Ready"
        bool isReady = player.CustomProperties.ContainsKey("isReady") &&
                       (bool)player.CustomProperties["isReady"];
        if (readyStatusText != null)
            readyStatusText.text = isReady ? "Ready" : "Not Ready";

        // 3️⃣ Color (nuevo)
        if (player.CustomProperties.TryGetValue(LobbyManager.COLOR_KEY, out object colorIdxObj))
        {
            int colorIdx = (int)colorIdxObj;
            Color playerColor = GetColorFromPalette(colorIdx);
            playerNameText.color = playerColor;
        }
        else
        {
            // Si por alguna razón no hay color asignado, usa blanco por defecto
            playerNameText.color = Color.white;
        }
    }

    private Color GetColorFromPalette(int idx)
    {
        // Usa la paleta definida en LobbyManager
        Color[] palette = new Color[]
        {
            new Color(0.90f,0.20f,0.20f),
            new Color(0.20f,0.50f,0.95f),
            new Color(0.20f,0.80f,0.35f),
            new Color(0.95f,0.80f,0.20f),
            new Color(0.70f,0.30f,0.85f),
            new Color(1.00f,0.55f,0.10f),
            new Color(0.15f,0.85f,0.85f),
            new Color(0.95f,0.40f,0.65f)
        };

        if (palette.Length == 0) return Color.white;
        return palette[idx % palette.Length];
    }

    public Player GetPlayer() => player;
}