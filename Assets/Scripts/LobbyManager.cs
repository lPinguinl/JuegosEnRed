using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;
using TMPro;

public class LobbyManager : MonoBehaviourPunCallbacks
{
    [Header("UI")]
    [SerializeField] private Transform playerListContent; // parent for player items
    [SerializeField] private GameObject playerListItemPrefab; // prefab with PlayerListItem
    [SerializeField] private Button readyButton;
    [SerializeField] private Button startGameButton;
    [SerializeField] private TMP_Text statusText;

    private PhotonConnector connector;
    private Dictionary<int, GameObject> listItems = new Dictionary<int, GameObject>();

    private void Awake()
    {
        connector = PhotonConnector.Instance;
    }

    private void Start()
    {
        readyButton.onClick.AddListener(OnReadyClicked);
        startGameButton.onClick.AddListener(OnStartGameClicked);
        startGameButton.interactable = PhotonNetwork.IsMasterClient;

        // If not in a room yet, try to join/create one
        if (!PhotonNetwork.InRoom)
        {
            connector.JoinRandomOrCreateRoom();
            statusText.text = "Joining room...";
        }
        else
        {
            RefreshPlayerList();
        }
    }

    public override void OnJoinedRoom()
    {
        statusText.text = $"Room: {PhotonNetwork.CurrentRoom.Name} ({PhotonNetwork.CurrentRoom.PlayerCount}/{PhotonNetwork.CurrentRoom.MaxPlayers})";
        RefreshPlayerList();
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        RefreshPlayerList();
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        RefreshPlayerList();
    }

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        startGameButton.interactable = PhotonNetwork.IsMasterClient;
    }

    private void RefreshPlayerList()
    {
        // clear existing
        foreach (var kv in listItems) Destroy(kv.Value);
        listItems.Clear();

        foreach (var p in PhotonNetwork.PlayerList)
        {
            var go = Instantiate(playerListItemPrefab, playerListContent);
            var item = go.GetComponent<PlayerListItem>();
            item.Setup(p);
            listItems[p.ActorNumber] = go;
        }
    }

    private void OnReadyClicked()
    {
        bool currentReady = PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey("isReady")
            ? (bool)PhotonNetwork.LocalPlayer.CustomProperties["isReady"]
            : false;

        Hashtable props = new Hashtable { { "isReady", !currentReady } };
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);

        RefreshPlayerList();
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        RefreshPlayerList();
    }

    private void OnStartGameClicked()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        // Optional: check all ready or other conditions here
        PhotonNetwork.AutomaticallySyncScene = true; // important so all clients load same scene
        PhotonNetwork.LoadLevel("GameScene");
    }

    private void OnDestroy()
    {
        readyButton.onClick.RemoveListener(OnReadyClicked);
        startGameButton.onClick.RemoveListener(OnStartGameClicked);
    }
}
