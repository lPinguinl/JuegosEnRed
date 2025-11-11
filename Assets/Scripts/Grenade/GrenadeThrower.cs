using Photon.Pun;
using UnityEngine;
using UnityEngine.InputSystem; // si estás usando el new Input System

[RequireComponent(typeof(PhotonView))]
public class GrenadeThrower : MonoBehaviourPun
{
    [Header("References")]
    [SerializeField] private Transform throwOrigin; // punto desde donde sale la granada (frente del jugador/cámara)
    [SerializeField] private GameObject grenadePrefab; // debe estar en Resources/ y registrado si usás Instantiate

    [Header("Throw Config")]
    [SerializeField] private float throwForce = 12f;
    [SerializeField] private float upwardModifier = 2f;

    private PlayerControllerNewInput controller;

    private void Awake()
    {
        controller = GetComponent<PlayerControllerNewInput>();
        if (throwOrigin == null)
        {
            // Fallback: usa el transform del jugador
            throwOrigin = this.transform;
        }
    }
    
    private void Update()
    {
        // Input clásico
        if (!photonView.IsMine) return;                  // Solo el dueño procesa input local
        if (Input.GetKeyDown(KeyCode.F))                 // Tecla F
        {
            TryThrowGrenade();                           // Lanza si tiene granada
        }
    }

    // Si usas el nuevo Input System, puedes mapear una acción a este callback
    public void OnThrow(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed) return;
        TryThrowGrenade();
    }

    // Si usas el antiguo Input, puedes llamar TryThrowGrenade() desde Update al presionar KeyCode.F cuando photonView.IsMine sea true.

    public void TryThrowGrenade()
    {
        if (!photonView.IsMine) return;
        if (controller == null || !controller.HasGrenade) return;

        // Instanciar la granada en red; el owner será este jugador
        // Opción A: Instantiate (owned) para proyectil con OnCollision local que RPC-stunea a todos
        GameObject go = PhotonNetwork.Instantiate(grenadePrefab.name, throwOrigin.position, throwOrigin.rotation, 0);
        var grenade = go.GetComponent<Grenade>();
        if (grenade != null)
        {
            grenade.Init(PhotonNetwork.LocalPlayer.ActorNumber);
            grenade.Launch(throwForce, upwardModifier);
        }

        // Consumir granada en todos
        photonView.RPC(nameof(RPC_ConsumeGrenade), RpcTarget.All);
    }

    [PunRPC]
    private void RPC_ConsumeGrenade()
    {
        controller.SendMessage("RPC_SetHasGrenade", false, SendMessageOptions.DontRequireReceiver);
        // Si preferís llamar directamente:
        // controller.photonView.RPC("RPC_SetHasGrenade", RpcTarget.All, false);
        // pero eso duplicaría el RPC; la línea de arriba solo cambia local. Mejor:
        controller.GetComponent<PhotonView>().RPC("RPC_SetHasGrenade", RpcTarget.All, false);
    }
}