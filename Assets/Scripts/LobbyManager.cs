using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using ExitGames.Client.Photon;

public class LobbyManager : MonoBehaviourPunCallbacks
{
    [SerializeField] private TMP_Text roomNameText;
    [SerializeField] private Button readyButton;
    [SerializeField] private Transform playerListContent;
    [SerializeField] private GameObject playerListItemPrefab;

    private bool isPlayerReady = false;
    private TMP_Text readyButtonLabel;

    private Dictionary<int, GameObject> playerListItems = new Dictionary<int, GameObject>();

    // Paleta (hasta 4 jugadores distintos; luego cicla)
    private readonly Color[] palette = new Color[] {
        new Color(0.90f,0.20f,0.20f),
        new Color(0.20f,0.50f,0.95f),
        new Color(0.20f,0.80f,0.35f),
        new Color(0.95f,0.80f,0.20f),
        new Color(0.70f,0.30f,0.85f),
        new Color(1.00f,0.55f,0.10f),
        new Color(0.15f,0.85f,0.85f),
        new Color(0.95f,0.40f,0.65f)
    };
    // Clave de la Player Custom Property donde guardamos el "índice de color"
    public const string COLOR_KEY = "playerColorIdx";

    private void Start()
    {
        if (roomNameText != null && PhotonNetwork.CurrentRoom != null)
            roomNameText.text = PhotonNetwork.CurrentRoom.Name;

        if (readyButton != null)
        {
            readyButton.onClick.AddListener(OnReadyClicked);
            readyButtonLabel = readyButton.GetComponentInChildren<TMP_Text>();
            if (readyButtonLabel == null)
                Debug.LogError("[LobbyManager] No TMP_Text found inside Ready Button.");
        }
        else
        {
            Debug.LogError("[LobbyManager] Ready Button not assigned in inspector.");
        }

        UpdatePlayerList();

        // Master asigna color a sí mismo si aún no lo tiene
        if (PhotonNetwork.IsMasterClient)
            EnsurePlayerHasColor(PhotonNetwork.LocalPlayer);
    }

    private void OnReadyClicked()
    {
        isPlayerReady = !isPlayerReady;
        if (readyButtonLabel != null)
            readyButtonLabel.text = isPlayerReady ? "Unready" : "Ready";

        SetPlayerReadyState(isPlayerReady);
        CheckAndStartGame();
    }

    private void SetPlayerReadyState(bool ready)
    {
        Hashtable playerProps = new Hashtable { ["isReady"] = ready };
        PhotonNetwork.LocalPlayer.SetCustomProperties(playerProps);
    }

    private void CheckAndStartGame()
    {
        int playerCount = PhotonNetwork.CurrentRoom.PlayerCount;
        int readyCount = 0;

        foreach (Player p in PhotonNetwork.PlayerList)
        {
            if (p.CustomProperties.ContainsKey("isReady") && (bool)p.CustomProperties["isReady"])
                readyCount++;
        }

        if (playerCount >= 2 && readyCount == playerCount)
            StartGame();
    }

    private void StartGame()
    {
        PhotonNetwork.CurrentRoom.IsOpen = false;
        PhotonNetwork.CurrentRoom.IsVisible = false;

        Debug.Log("[LobbyManager] Todos listos. Iniciando partida...");
        PhotonNetwork.LoadLevel("GameScene");
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        // Master asigna color al que entra
        if (PhotonNetwork.IsMasterClient)
            EnsurePlayerHasColor(newPlayer);

        UpdatePlayerList();
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        UpdatePlayerList();
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        if (playerListItems.ContainsKey(targetPlayer.ActorNumber))
            playerListItems[targetPlayer.ActorNumber].GetComponent<PlayerListItem>().UpdateInfo();

        // Si entró un player sin color, reintentar asignación
        if (PhotonNetwork.IsMasterClient && !targetPlayer.CustomProperties.ContainsKey(COLOR_KEY))
            EnsurePlayerHasColor(targetPlayer);

        if (changedProps.ContainsKey("isReady"))
            CheckAndStartGame();
    }

    private void UpdatePlayerList()
    {
        foreach (var item in playerListItems.Values) Destroy(item);
        playerListItems.Clear();

        foreach (Player player in PhotonNetwork.PlayerList)
        {
            GameObject item = Instantiate(playerListItemPrefab, playerListContent);
            PlayerListItem listItem = item.GetComponent<PlayerListItem>();
            if (listItem != null)
            {
                listItem.SetPlayerInfo(player);
                playerListItems[player.ActorNumber] = item;
            }
        }
    }

    // Buscamos qué índices ya están usados por otros jugadores (leyendo sus Custom Properties).
    // Elegimos el primer índice libre y lo "modulamos" por la cantidad de colores disponibles.
    // Guardamos ese índice en las Player Custom Properties del jugador 'p' (clave COLOR_KEY).
    
    // - Todos los clientes ven el mismo índice para ese jugador.
    // - En la escena de juego, cada avatar leerá ese índice y pintará sus materiales.
    private void EnsurePlayerHasColor(Player p)
    {
        if (p == null) return;
        if (p.CustomProperties != null && p.CustomProperties.ContainsKey(COLOR_KEY)) return;

        // Buscar índices usados
        HashSet<int> used = new HashSet<int>();
        foreach (var pl in PhotonNetwork.PlayerList)
            if (pl.CustomProperties != null && pl.CustomProperties.ContainsKey(COLOR_KEY))
                used.Add((int)pl.CustomProperties[COLOR_KEY]);

        // Elegir primer libre
        int idx = 0;
        while (used.Contains(idx)) idx++;
        idx = idx % palette.Length // cicla si supera la paleta

        // Guardar el índice en las propiedades del jugador 'p'
        Hashtable props = new Hashtable { { COLOR_KEY, idx } };
        p.SetCustomProperties(props); // Photon replica esto a todos
    }

    // Helper para el Player (si está en la misma escena)
    public bool TryGetPlayerColor(Player p, out Color color)
    {
        color = Color.white;
        if (p != null && p.CustomProperties != null && p.CustomProperties.ContainsKey(COLOR_KEY))
        {
            int idx = (int)p.CustomProperties[COLOR_KEY];
            color = palette[idx % palette.Length];
            return true;
        }
        return false;
    }
}
