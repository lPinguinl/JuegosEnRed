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
            IStunable stunable = hit.collider.GetComponent<IStunable>();
            if (stunable != null && stunable.IsStunned() == false)
            {
                PhotonView targetPV = hit.collider.GetComponent<PhotonView>();
                if (targetPV != null)
                {
                    // Llamamos al RPC de stun en el otro jugador, pasando nuestra posición.
                    targetPV.RPC("RPC_OnStunned", RpcTarget.All, transform.position);

                    // INTENTO DE ROBO DE CORONA
                    var targetPlayer = hit.collider.GetComponent<PlayerControllerNewInput>();
                    if (targetPlayer != null && targetPlayer.HasCrown())
                    {
                        // Solo el MasterClient puede transferir la corona
                        if (PhotonNetwork.IsMasterClient)
                        {
                            CrownManager.Instance.TryStealCrown(photonView.Owner.ActorNumber);
                        }
                    }
                }
            }
        }
    }
}