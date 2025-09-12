using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Photon.Pun;
using Photon.Realtime;
using TMPro;

public class LobbyManager : MonoBehaviourPunCallbacks
{
    [SerializeField] private TMP_Text roomNameText;
    [SerializeField] private Button readyButton;
    
    // Aquí están las variables que faltaban:
    [SerializeField] private Transform playerListContent;
    [SerializeField] private GameObject playerListItemPrefab;

    private bool isPlayerReady = false;
    private TMP_Text readyButtonLabel;
    
    private Dictionary<int, GameObject> playerListItems = new Dictionary<int, GameObject>();

    private void Start()
    {
        if (roomNameText != null && PhotonNetwork.CurrentRoom != null)
        {
            roomNameText.text = PhotonNetwork.CurrentRoom.Name;
        }

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
        
        // Llamamos a la función para inicializar la lista de jugadores al inicio
        UpdatePlayerList();
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
        ExitGames.Client.Photon.Hashtable playerProps = new ExitGames.Client.Photon.Hashtable
        {
            ["isReady"] = ready
        };
        PhotonNetwork.LocalPlayer.SetCustomProperties(playerProps);
    }
    
    private void CheckAndStartGame()
    {
        int readyCount = 0;
        foreach (Player p in PhotonNetwork.PlayerList)
        {
            if (p.CustomProperties.ContainsKey("isReady") && (bool)p.CustomProperties["isReady"])
            {
                readyCount++;
            }
        }
        
        if (readyCount > 0)
        {
            PhotonNetwork.LoadLevel("GameScene");
        }
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        UpdatePlayerList();
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        UpdatePlayerList();
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps)
    {
        if (playerListItems.ContainsKey(targetPlayer.ActorNumber))
        {
            playerListItems[targetPlayer.ActorNumber].GetComponent<PlayerListItem>().UpdateInfo();
        }
        
        if (changedProps.ContainsKey("isReady"))
            CheckAndStartGame();
    }

    private void UpdatePlayerList()
    {
        // Limpiamos la lista actual antes de recrearla
        foreach(var item in playerListItems.Values)
        {
            Destroy(item);
        }
        playerListItems.Clear();

        // Creamos un nuevo item para cada jugador en la sala
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
}