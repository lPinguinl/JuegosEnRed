using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;
using TMPro;

public class LobbyManager : MonoBehaviourPunCallbacks
{
    [SerializeField] private Button readyButton;

    private bool isPlayerReady = false;
    private TMP_Text readyButtonLabel;

    private void Start()
    {
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
    }

    private void OnReadyClicked()
    {
        isPlayerReady = !isPlayerReady;

        // Actualiza el texto usando TMP_Text
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
        foreach (Player p in PhotonNetwork.PlayerList)
        {
            if (!p.CustomProperties.ContainsKey("isReady") || !(bool)p.CustomProperties["isReady"])
                return;
        }

        //Todos están listos → inicia la partida
        //No cerramos la sala, por lo que nuevos jugadores aún pueden entrar si llegan tarde.
        PhotonNetwork.LoadLevel("GameScene");
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps)
    {
        if (changedProps.ContainsKey("isReady"))
            CheckAndStartGame();
    }
}
