using UnityEngine;
using Photon.Pun;
using UnityEngine.InputSystem;

public class StunHandler : MonoBehaviourPun
{
    [SerializeField] private float attackRange = 1f;

    private PlayerControls controls;

    private void Awake()
    {
        controls = new PlayerControls();
    }
    
    private void OnEnable()
    {
        if (photonView.IsMine)
        {
            controls.Player.Attack.performed += ctx => TryStun();
            controls.Player.Attack.Enable();
        }
    }

    private void OnDisable()
    {
        if (photonView.IsMine)
        {
            controls.Player.Attack.Disable();
        }
    }

    private void TryStun()
    {
        if (!photonView.IsMine) return;

        Ray ray = new Ray(transform.position, transform.forward);
        RaycastHit hit;
        
        if (Physics.Raycast(ray, out hit, attackRange))
        {
            // En lugar de buscar un Tag, buscamos la interfaz IStunable.
            IStunable stunable = hit.collider.GetComponent<IStunable>();
            if (stunable != null && stunable.IsStunned() == false) // <-- Agregamos una verificación para evitar stun repetidos
            {
                PhotonView targetPV = hit.collider.GetComponent<PhotonView>();
                if (targetPV != null)
                {
                    // Llamamos al RPC de stun en el otro jugador, pasando nuestra posición.
                    targetPV.RPC("RPC_OnStunned", RpcTarget.All, transform.position);

                    // --- Lógica de transferencia de corona ---
                    // Solo si el target es el portador de la corona, pedir transferencia
                    if (PhotonNetwork.CurrentRoom != null && PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey("CrownOwner"))
                    {
                        int crownOwner = (int)PhotonNetwork.CurrentRoom.CustomProperties["CrownOwner"];
                        if (targetPV.Owner.ActorNumber == crownOwner)
                        {
                            // Pedir al MasterClient que transfiera la corona a este jugador (el atacante)
                            photonView.RPC("RequestCrownTransfer", RpcTarget.MasterClient, PhotonNetwork.LocalPlayer.ActorNumber);
                        }
                    }
                }
            }
        }
    }
    
    [PunRPC]
    public void RequestCrownTransfer(int newOwnerActorNumber)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable
        {
            { "CrownOwner", newOwnerActorNumber }
        };
        PhotonNetwork.CurrentRoom.SetCustomProperties(props);
    }
}