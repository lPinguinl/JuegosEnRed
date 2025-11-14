using UnityEngine;
using Photon.Pun;
using UnityEngine.InputSystem;
using System.Collections;

public class StunHandler : MonoBehaviourPun
{
    [SerializeField] private float attackRange = 1f;

    private PlayerControls controls;

    [Header("Animator")]
    [SerializeField] private Animator pAnimator;

    private void Awake()
    {
        controls = new PlayerControls();
    }

    private void OnEnable()
    {
        if (!photonView.IsMine) return;

        if (controls == null)
            controls = new PlayerControls();

        controls.Player.Attack.performed += OnAttackInput;
        controls.Enable();
    }

    private void OnDisable()
    {
        if (!photonView.IsMine) return;

        controls.Player.Attack.performed -= OnAttackInput;
        controls.Disable();
    }

    private void OnAttackInput(InputAction.CallbackContext ctx)
    {
        // Sincronizar animación con todos los clientes
        photonView.RPC("RPC_PlayPunchAnimation", RpcTarget.All);

        // Lógica de stun
        TryStun();
    }

    [PunRPC]
    private void RPC_PlayPunchAnimation()
    {
        if (pAnimator == null) return;
        StartCoroutine(PunchAnimation(pAnimator));
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

    private IEnumerator PunchAnimation(Animator animator)
    {
        animator.SetBool("isPunching", true);
        yield return new WaitForSeconds(1f);
        animator.SetBool("isPunching", false);
    }
}