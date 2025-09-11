using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine.UI;
using ExitGames.Client.Photon;
using System.Linq;

public class LobbyManager : MonoBehaviourPunCallbacks
{
    [Header("UI References")]
    [SerializeField] private Transform playerListContent;
    [SerializeField] private GameObject playerListItemPrefab;
    [SerializeField] private Button readyButton;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private Button startButton; // Nuevo botón para el Master Client

    private void Start()
    {
        UpdatePlayerList();

        if (readyButton != null)
            readyButton.onClick.AddListener(OnReadyClicked);
        
        // Asignar el listener y configurar la visibilidad del botón de inicio
        if (startButton != null)
        {
            startButton.onClick.AddListener(OnStartClicked);
            // Solo el Master Client puede ver y usar este botón
            startButton.gameObject.SetActive(PhotonNetwork.IsMasterClient);
        }
    }

    private void OnDestroy()
    {
        if (readyButton != null)
            readyButton.onClick.RemoveListener(OnReadyClicked);
        if (startButton != null)
            startButton.onClick.RemoveListener(OnStartClicked);
    }

    // Nuevo método para manejar el clic en el botón de inicio
    private void OnStartClicked()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            Debug.Log("[Lobby] Master Client manually starting game.");
            PhotonNetwork.LoadLevel("GameScene");
        }
    }

    private void OnReadyClicked()
    {
        bool currentReady = PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey("isReady")
            && (bool)PhotonNetwork.LocalPlayer.CustomProperties["isReady"];
        bool newReady = !currentReady;
        Hashtable props = new Hashtable { { "isReady", newReady } };
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);

        if (readyButton != null)
        {
            TMP_Text btnText = readyButton.GetComponentInChildren<TMP_Text>();
            if (btnText != null)
                btnText.text = newReady ? "Unready" : "Ready";
        }
    }

    private void UpdatePlayerList()
    {
        foreach (Transform child in playerListContent)
            Destroy(child.gameObject);

        foreach (Player player in PhotonNetwork.PlayerList)
        {
            GameObject itemGO = Instantiate(playerListItemPrefab, playerListContent);
            PlayerListItem item = itemGO.GetComponent<PlayerListItem>();
            item.SetPlayerInfo(player);
        }
    }

    private void CheckAllPlayersReady()
    {
        bool allReady = PhotonNetwork.PlayerList.All(p =>
            p.CustomProperties.ContainsKey("isReady") &&
            (bool)p.CustomProperties["isReady"]);

        // La lógica de inicio automático se mantiene, pero el Master Client también puede iniciar manualmente
        if (allReady && PhotonNetwork.PlayerList.Length >= 2)
        {
            Debug.Log("[Lobby] All players ready. Starting game...");
            if (statusText != null)
                statusText.text = "Starting game...";
            PhotonNetwork.LoadLevel("GameScene");
        }
    }

    // --- Photon Callbacks ---
    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        UpdatePlayerList();
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        UpdatePlayerList();
    }

    // Cuando el Master Client cambia, la visibilidad del botón se actualiza
    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        if (startButton != null)
        {
            startButton.gameObject.SetActive(PhotonNetwork.IsMasterClient);
        }
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        foreach (Transform child in playerListContent)
        {
            PlayerListItem item = child.GetComponent<PlayerListItem>();
            if (item.GetPlayer() == targetPlayer)
            {
                item.UpdateInfo();
                break;
            }
        }

        if (PhotonNetwork.IsMasterClient)
            CheckAllPlayersReady();
    }
}