using UnityEngine;
using Photon.Pun;
using UnityEngine.InputSystem;
using System.Collections;

public class StunHandler : MonoBehaviourPun
{
    [SerializeField] private float attackRange = 1f;

    private PlayerControls controls;

    [Header("Animator")]
    [SerializeField] private Animator pAnimator; // opcional, ya no se usa directamente para sync

    // Referencia al controlador principal del player
    private PlayerControllerNewInput playerController;

    private void Awake()
    {
        controls = new PlayerControls();
        playerController = GetComponent<PlayerControllerNewInput>();
    }
    
    private void OnEnable()
    {
        if (photonView.IsMine)
        {
            if (controls == null)
                controls = new PlayerControls();

            controls.Player.Attack.performed += OnAttackInput;
            controls.Player.Attack.Enable();
        }
    }

    private void OnDisable()
    {
        if (photonView.IsMine)
        {
            controls.Player.Attack.performed -= OnAttackInput;
            controls.Player.Attack.Disable();
        }
    }

    private void OnAttackInput(InputAction.CallbackContext ctx)
    {
        if (!photonView.IsMine) return;

        // 1) Disparar animación de punch a través del PlayerController
        if (playerController != null)
        {
            playerController.StartPunch();
        }

        // 2) Lógica de stun (raycast + RPCs de gameplay)
        TryStun();
    }

    private void TryStun()
    {
        if (!photonView.IsMine) return;

        Ray ray = new Ray(transform.position, transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, attackRange))
        {
            IStunable stunable = hit.collider.GetComponent<IStunable>();
            if (stunable == null) return;

            PhotonView targetPV = hit.collider.GetComponent<PhotonView>();
            if (targetPV == null) return;

            // Enviar stun al target incluyendo mi actorNumber
            targetPV.RPC("RPC_OnStunned", RpcTarget.All, transform.position, PhotonNetwork.LocalPlayer.ActorNumber);

            // Esperar un frame para recibir la notificación del target
            StartCoroutine(AfterHitCheckAndMaybeTransferCrown(targetPV));
        }
    }

    private IEnumerator AfterHitCheckAndMaybeTransferCrown(PhotonView targetPV)
    {
        // Esperar el siguiente frame para permitir que llegue RPC_NotifyHitResultToAttacker
        yield return null;

        bool? stunApplied = HitResultNotifier.Consume();

        if (stunApplied == true)
        {
            // Solo si el stun se aplicó realmente
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
        // Si stunApplied es false o null, no transferimos la corona.
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