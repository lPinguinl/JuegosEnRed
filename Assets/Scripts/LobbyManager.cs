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

    // Paleta de colores 
    private readonly Color[] palette = new Color[] {
        new Color(0.90f,0.20f,0.20f),  // Rojo
        new Color(0.20f,0.50f,0.95f),  // Azul
        new Color(0.20f,0.80f,0.35f),  // Verde
        new Color(0.95f,0.80f,0.20f),  // Amarillo
        new Color(0.70f,0.30f,0.85f),  // Violeta
        new Color(1.00f,0.55f,0.10f),  // Naranja
        new Color(0.15f,0.85f,0.85f),  // Cian
        new Color(0.95f,0.40f,0.65f)   // Rosa
    };

    public const string COLOR_KEY = "playerColorIdx";
    private const string READY_KEY = "isReady";

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

        // Actualizamos la lista inicial de jugadores
        UpdatePlayerList();

        // Asignar color al jugador local si no tiene
        EnsurePlayerHasColor(PhotonNetwork.LocalPlayer);
    }

    private void OnReadyClicked()
    {
        isPlayerReady = !isPlayerReady;
        if (readyButtonLabel != null)
            readyButtonLabel.text = isPlayerReady ? "Unready" : "Ready";

        SetPlayerReadyState(isPlayerReady);

        if (PhotonNetwork.IsMasterClient)
            CheckAndStartGame();
    }

    private void SetPlayerReadyState(bool ready)
    {
        ExitGames.Client.Photon.Hashtable playerProps = new ExitGames.Client.Photon.Hashtable
        {
            [READY_KEY] = ready
        };
        PhotonNetwork.LocalPlayer.SetCustomProperties(playerProps);
    }

    private void CheckAndStartGame()
    {
        int playerCount = PhotonNetwork.CurrentRoom.PlayerCount;
        int readyCount = 0;

        foreach (Player p in PhotonNetwork.PlayerList)
        {
            if (p.CustomProperties.TryGetValue(READY_KEY, out object readyObj) &&
                readyObj is bool isReady && isReady)
            {
                readyCount++;
            }
        }

        // ✅ Solo arranca si hay mínimo 2 jugadores y todos están listos
        if (playerCount >= 2 && readyCount == playerCount)
        {
            StartGame();
        }
    }

    private void StartGame()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        PhotonNetwork.CurrentRoom.IsOpen = false;
        PhotonNetwork.CurrentRoom.IsVisible = false;

        PhotonNetwork.LoadLevel("GameScene");
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        EnsurePlayerHasColor(newPlayer);
        UpdatePlayerList();

        if (PhotonNetwork.IsMasterClient)
            CheckAndStartGame();
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        UpdatePlayerList();

        if (PhotonNetwork.IsMasterClient)
            CheckAndStartGame();
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps)
    {
        if (playerListItems.TryGetValue(targetPlayer.ActorNumber, out GameObject item))
        {
            item.GetComponent<PlayerListItem>().UpdateInfo();
        }

        if (changedProps.ContainsKey(READY_KEY) && PhotonNetwork.IsMasterClient)
            CheckAndStartGame();
    }

    private void UpdatePlayerList()
    {
        foreach (var item in playerListItems.Values)
            Destroy(item);
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

    private void EnsurePlayerHasColor(Player p)
    {
        if (p == null) return;

        if (p.CustomProperties.ContainsKey(COLOR_KEY))
            return; // Ya tiene un color asignado

        HashSet<int> usedIndices = new HashSet<int>();
        foreach (var pl in PhotonNetwork.PlayerList)
        {
            if (pl.CustomProperties.TryGetValue(COLOR_KEY, out object idxObj))
                usedIndices.Add((int)idxObj);
        }

        // Elegimos el primer índice de color disponible
        int newIdx = 0;
        while (usedIndices.Contains(newIdx))
            newIdx = (newIdx + 1) % palette.Length;

        ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable
        {
            [COLOR_KEY] = newIdx
        };
        p.SetCustomProperties(props);
    }
}
