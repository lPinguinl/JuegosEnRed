using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;
using System.Linq;

public class LobbyManager : MonoBehaviourPunCallbacks
{
    [SerializeField] private Button readyButton;

    private bool isPlayerReady = false;
    private bool gameStarted = false;

    private void Start()
    {
        if (readyButton != null)
            readyButton.onClick.AddListener(OnReadyClicked);
    }

    private void OnDestroy()
    {
        if (readyButton != null)
            readyButton.onClick.RemoveListener(OnReadyClicked);
    }

    private void OnReadyClicked()
    {
        isPlayerReady = !isPlayerReady;

        if (readyButton != null)
            readyButton.GetComponentInChildren<Text>().text = isPlayerReady ? "Unready" : "Ready";

        SetPlayerReadyState(isPlayerReady);
        CheckAndStartGame();
    }

    private void SetPlayerReadyState(bool ready)
    {
        Hashtable playerProps = new Hashtable { { "isReady", ready } };
        PhotonNetwork.LocalPlayer.SetCustomProperties(playerProps);
    }

    private void CheckAndStartGame()
    {
        if (gameStarted) return; // Previene doble carga de escena

        // ✅ Ahora no depende del MasterClient
        bool allReady = PhotonNetwork.PlayerList.All(p =>
            p.CustomProperties.ContainsKey("isReady") &&
            (bool)p.CustomProperties["isReady"]);

        if (allReady && PhotonNetwork.CurrentRoom.PlayerCount > 0)
        {
            gameStarted = true;
            PhotonNetwork.IsMessageQueueRunning = false; // Evita duplicados
            PhotonNetwork.LoadLevel("GameScene");
        }
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        if (changedProps.ContainsKey("isReady"))
            CheckAndStartGame();
    }
}