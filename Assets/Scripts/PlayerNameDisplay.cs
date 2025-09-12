using UnityEngine;
using TMPro;
using Photon.Pun;

public class PlayerNameDisplay : MonoBehaviour
{
    // Asigna este campo en el Inspector a tu TextMeshPro
    [SerializeField] private TMP_Text nameText;
    
    // Este PhotonView es para saber a qué jugador pertenece este objeto
    private PhotonView photonView;

    void Awake()
    {
        photonView = GetComponent<PhotonView>();
        
        if (photonView.IsMine)
        {
            // Si este es mi jugador, obtengo mi nombre de la red
            nameText.text = PhotonNetwork.LocalPlayer.NickName;
        }
        else
        {
            // Si es el jugador de otro, obtengo su nombre de la red
            nameText.text = photonView.Owner.NickName;
        }
    }
}