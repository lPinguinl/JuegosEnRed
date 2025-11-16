using UnityEngine;
using TMPro;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;

public class PlayerNameDisplay : MonoBehaviourPunCallbacks
{
    // Asigna este campo en el Inspector a tu TextMeshPro
    [SerializeField] private TMP_Text nameText;
    
    // Este PhotonView es para saber a qué jugador pertenece este objeto
    private PhotonView photonView;

    private void Awake()
    {
        photonView = GetComponentInParent<PhotonView>();

        if (nameText == null)
        {
            Debug.LogWarning("[PlayerNameDisplay] nameText no asignado.");
        }
    }

    private void OnEnable()
    {
        Refresh();
    }

    private void Start()
    {
        Refresh();
    }

    private void Refresh()
    {
        if (photonView == null)
        {
            return;
        }

        ApplyName();
        ApplyColor();
    }

    private void ApplyName()
    {
        if (nameText == null)
        {
            return;
        }

        if (photonView.IsMine)
        {
            nameText.text = PhotonNetwork.LocalPlayer?.NickName ?? "";
        }
        else if (photonView.Owner != null)
        {
            nameText.text = photonView.Owner.NickName;
        }
    }

    private void ApplyColor()
    {
        if (nameText == null || photonView.Owner == null)
        {
            return;
        }

        if (photonView.Owner.CustomProperties.TryGetValue(LobbyManager.COLOR_KEY, out object idxObj) &&
            idxObj is int colorIdx)
        {
            nameText.color = LobbyManager.GetPaletteColor(colorIdx);
        }
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        if (photonView == null || targetPlayer != photonView.Owner)
        {
            return;
        }

        if (changedProps.ContainsKey(LobbyManager.COLOR_KEY))
        {
            ApplyColor();
        }
    }
}