using UnityEngine;
using Photon.Pun;
using System.Collections;
using UnityEngine.InputSystem;
using Photon.Realtime;
using ExitGames.Client.Photon;

[RequireComponent(typeof(Rigidbody))]
public class PlayerControllerNewInput : MonoBehaviourPun, IStunable
{
    [SerializeField] private GameObject crownVisual;

    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpForce = 5f;
    [SerializeField] private float groundCheckDistance = 0.2f;

    [SerializeField] private Renderer[] renderersToTint;

    private Rigidbody rb;
    private PlayerControls controls;
    private Vector2 moveInput;

    // ESTADO DE RED RECIBIDO
    private Vector3 networkPosition;
    private Quaternion networkRotation;

    private bool isGrounded = false;
    private bool canMove = true;
    private bool isStunned = false;

    private const string COLOR_KEY = "playerColorIdx";

    private InputAction crownClaimAction;    // Acción manual para la tecla E (Input System)
    private GameStateManager cachedStateManager;    // Referencia perezosa al gestor de estados
    
    [Header("PowerUp States")]
    [SerializeField] private bool hasShield = false;

    [SerializeField] private bool isSpeedBoosted = false;
    [SerializeField] private float speedBoostMultiplier = 2.0f;
    private Coroutine speedBoostRoutine;
    
    [Header("Grenade")]
    [SerializeField] private bool hasGrenade = false;

    [Header("Animator")]
    [SerializeField] private Animator pAnimator;

    [Header("PowerUp UI")]
    [SerializeField] private GameObject shieldIconUI;
    [SerializeField] private GameObject speedIconUI;
    [SerializeField] private GameObject grenadeIconUI;

    // === ESTADOS DE ANIMACIÓN LOCALES Y DE RED ===
    // Estos se envían/reciben en nuestros paquetes hechos a mano.
    private bool isRunning;
    private bool isPunching;
    [SerializeField] private float punchDuration = 0.5f;
    private float punchTimer;

    public bool HasGrenade => hasGrenade;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        controls = new PlayerControls();

        // Acción rápida para la tecla E (Interactuar con la corona antes de iniciar la partida).
        crownClaimAction = new InputAction("ClaimCrown", binding: "<Keyboard>/e");
    }

    private void OnEnable()
    {
        controls.Player.Enable();
        controls.Player.Move.performed += ctx => { if (photonView.IsMine) moveInput = ctx.ReadValue<Vector2>(); };
        controls.Player.Move.canceled += ctx => { if (photonView.IsMine) moveInput = Vector2.zero; };
        controls.Player.Jump.performed += ctx => { if (photonView.IsMine) TryJump(); };

        crownClaimAction.performed += OnCrownClaimPerformed;
        crownClaimAction.Enable();

        PhotonNetwork.NetworkingClient.EventReceived += OnPhotonEvent;
    }

    private void OnDisable()
    {
        controls.Player.Disable();

        crownClaimAction.performed -= OnCrownClaimPerformed;
        crownClaimAction.Disable();

        PhotonNetwork.NetworkingClient.EventReceived -= OnPhotonEvent;
    }

    private void OnDestroy()
    {
        crownClaimAction?.Dispose();
    }

    private void Start()
    {
        if (photonView.IsMine)
        {
            rb.isKinematic = false;
            Camera cam = GetComponentInChildren<Camera>(true);
            if (cam) cam.enabled = true;
        }
        else
        {
            rb.isKinematic = true;
            Camera cam = GetComponentInChildren<Camera>(true);
            if (cam) cam.enabled = false;
        }

        networkPosition = transform.position;
        networkRotation = transform.rotation;

        ApplyColorFromProperties();
    }

    private void FixedUpdate()
    {
        if (photonView.IsMine)
        {
            // === SIMULACIÓN LOCAL ===
            GroundCheck();
            if (canMove)
            {
                Vector3 move = new Vector3(moveInput.x, 0f, moveInput.y);
                if (move.magnitude > 1f) move.Normalize();
                float currentSpeed = isSpeedBoosted ? moveSpeed * speedBoostMultiplier : moveSpeed;
                Vector3 targetPos = rb.position + move * currentSpeed * Time.fixedDeltaTime;
                rb.MovePosition(targetPos);

                if (move.sqrMagnitude > 0.001f)
                {
                    Quaternion targetRot = Quaternion.LookRotation(move);
                    rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRot, 10f * Time.fixedDeltaTime));
                }

                // LÓGICA DE CORRER (solo dueño)
                isRunning = (move != Vector3.zero);
            }
            else
            {
                isRunning = false;
            }

            // LÓGICA DE PUNCH (solo dueño)
            if (isPunching)
            {
                punchTimer -= Time.fixedDeltaTime;
                if (punchTimer <= 0f)
                {
                    isPunching = false;
                }
            }

            // === ENVÍO DE PAQUETE HECHO A MANO CON MI ESTADO ===
            SendStatePacket();
        }
        else
        {
            // === INTERPOLACIÓN DE RED EN CLIENTES REMOTOS ===
            transform.position = Vector3.Lerp(transform.position, networkPosition, 5f * Time.fixedDeltaTime);
            transform.rotation = Quaternion.Slerp(transform.rotation, networkRotation, 5f * Time.fixedDeltaTime);
        }

        // === APLICAR ANIMACIONES EN TODOS LOS CLIENTES ===
        if (pAnimator != null)
        {
            pAnimator.SetBool("isRunning", isRunning);
            pAnimator.SetBool("isPunching", isPunching);
        }

        UpdateCrownVisual();
    }

    private void TryJump()
    {
        if (isGrounded && canMove)
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }
    }
    
    // === MÉTODO PARA INICIAR EL PUNCH (LO LLAMA StunHandler SOLO EN EL DUEÑO) ===
    public void StartPunch()
    {
        if (!photonView.IsMine) return;

        isPunching = true;
        punchTimer = punchDuration;
    }

    private void GroundCheck()
    {
        Vector3 rayOrigin = transform.position + Vector3.down * 0.45f;
        Ray ray = new Ray(rayOrigin, Vector3.down);

        if (Physics.Raycast(ray, out RaycastHit hit, groundCheckDistance))
        {
            var walkable = hit.collider.GetComponent<IWalkableSurface>();
            isGrounded = walkable != null && walkable.IsWalkable();
        }
        else
        {
            isGrounded = false;
        }
    }

    // === Interacción con la corona durante la cuenta regresiva ===
    private void OnCrownClaimPerformed(InputAction.CallbackContext context)
    {
        if (!photonView.IsMine)
        {
            return;
        }

        GameStateManager manager = ResolveStateManager();
        if (manager == null)
        {
            return;
        }

        manager.ReportCrownAttempt(photonView.OwnerActorNr);
    }

    private GameStateManager ResolveStateManager()
    {
        if (cachedStateManager == null)
        {
            cachedStateManager = FindObjectOfType<GameStateManager>();
            if (cachedStateManager == null)
            {
                Debug.LogWarning("[PlayerController] GameStateManager no encontrado en escena.");
            }
        }

        return cachedStateManager;
    }

    // Método requerido por IStunable (llamado mediante RPC)
    public void Stun(Vector3 attackerPosition)
    {
        // La lógica concreta del stun se encuentra en RPC_OnStunned.
    }

    [PunRPC]
    private void RPC_OnStunned(Vector3 attackerPosition, int attackerActorNumber)
    {
        // Si hay escudo, lo consumimos y evitamos el stun
        if (hasShield)
        {
            photonView.RPC(nameof(RPC_SetShield), RpcTarget.All, false);

            // Notificar al atacante que el golpe fue bloqueado
            photonView.RPC(nameof(RPC_NotifyHitResultToAttacker), RpcTarget.All, attackerActorNumber, false);
            return;
        }

        if (isStunned)
        {
            // Ya estaba stuneado: lo consideramos golpe no-aplicado para evitar dobles transferencias
            photonView.RPC(nameof(RPC_NotifyHitResultToAttacker), RpcTarget.All, attackerActorNumber, false);
            return;
        }

        isStunned = true;
        StartCoroutine(StunCoroutine(attackerPosition));

        // Notificar golpe aplicado
        photonView.RPC(nameof(RPC_NotifyHitResultToAttacker), RpcTarget.All, attackerActorNumber, true);
    }
    
    [PunRPC]
    private void RPC_NotifyHitResultToAttacker(int attackerActorNumber, bool stunApplied)
    {
        // Solo el atacante procesa esta notificación (por actorNumber)
        if (PhotonNetwork.LocalPlayer == null || PhotonNetwork.LocalPlayer.ActorNumber != attackerActorNumber)
            return;

        HitResultNotifier.Report(stunApplied);
    }

    private IEnumerator StunCoroutine(Vector3 attackerPosition)
    {
        canMove = false;
        rb.constraints = RigidbodyConstraints.None;

        Vector3 knockbackDirection = (transform.position - attackerPosition).normalized;
        rb.AddForce(knockbackDirection * 5f, ForceMode.Impulse);

        yield return new WaitForSeconds(2f);

        transform.rotation = Quaternion.identity;
        rb.constraints = RigidbodyConstraints.FreezeRotation;

        canMove = true;
        isStunned = false;
    }

    public bool IsStunned()
    {
        return isStunned;
    }

    // === ENVÍO DE PAQUETE MANUAL CON RaiseEvent ===
    private void SendStatePacket()
    {
        // Armamos el paquete de estado del jugador
        object[] content = new object[]
        {
            photonView.OwnerActorNr, // int: quién soy
            rb.position,             // Vector3
            rb.rotation,             // Quaternion
            isStunned,               // bool
            isRunning,               // bool
            isPunching               // bool
        };

        var raiseEventOptions = new RaiseEventOptions
        {
            Receivers = ReceiverGroup.Others,   // Solo los demás
            CachingOption = EventCaching.DoNotCache
        };

        var sendOptions = new ExitGames.Client.Photon.SendOptions
        {
            Reliability = false                 // movimiento/animación suele ir no fiable
        };

        PhotonNetwork.RaiseEvent(MyEventCodes.PlayerStateUpdate, content, raiseEventOptions, sendOptions);
    }

    // === RECEPCIÓN DE PAQUETES Y APLICACIÓN DE ESTADO REMOTO ===
    private void OnPhotonEvent(EventData photonEvent)
    {
        // 252 = EventCode interno de Photon para actualizar CustomProperties de Room/Player.
        if (photonEvent.Code == 252)
        {
            UpdateCrownVisual();
            return;
        }

        if (photonEvent.Code == MyEventCodes.PlayerStateUpdate)
        {
            object[] data = (object[])photonEvent.CustomData;

            int actorNumber = (int)data[0];

            // Ignoramos paquetes que no son para este jugador
            if (actorNumber != photonView.OwnerActorNr)
                return;

            // Si soy el dueño, no necesito aplicar mi propio paquete
            if (photonView.IsMine)
                return;

            Vector3 pos = (Vector3)data[1];
            Quaternion rot = (Quaternion)data[2];
            bool stunned = (bool)data[3];
            bool running = (bool)data[4];
            bool punching = (bool)data[5];

            networkPosition = pos;
            networkRotation = rot;
            isStunned = stunned;
            isRunning = running;
            isPunching = punching;
        }
    }
    
    [PunRPC]
    private void RPC_SetShield(bool active)
    {
        hasShield = active;

        if (shieldIconUI != null)
            shieldIconUI.SetActive(active);
    }

    [PunRPC]
    private void RPC_ActivateSpeedBoost(float multiplier, float duration)
    {
        if (isSpeedBoosted) return; // No acumulable
        isSpeedBoosted = true;
        speedBoostMultiplier = multiplier;

        if (speedIconUI != null)
            speedIconUI.SetActive(true);

        if (speedBoostRoutine != null) StopCoroutine(speedBoostRoutine);
        speedBoostRoutine = StartCoroutine(SpeedBoostCoroutine(duration));
    }

    [PunRPC]
    private void RPC_DeactivateSpeedBoost()
    {
        if (!isSpeedBoosted) return;

        isSpeedBoosted = false;

        if (speedIconUI != null)
            speedIconUI.SetActive(false);

        if (speedBoostRoutine != null)
        {
            StopCoroutine(speedBoostRoutine);
            speedBoostRoutine = null;
        }
    }

    private System.Collections.IEnumerator SpeedBoostCoroutine(float duration)
    {
        yield return new WaitForSeconds(duration);
        // Al expirar, apagar en todos
        photonView.RPC(nameof(RPC_DeactivateSpeedBoost), RpcTarget.All);
    }
    
    [PunRPC]
    private void RPC_SetHasGrenade(bool value)
    {
        hasGrenade = value;

        if (grenadeIconUI != null)
            grenadeIconUI.SetActive(value);
    }

    private void ApplyColorFromProperties()
    {
        if (renderersToTint == null || renderersToTint.Length == 0) return;

        var owner = photonView.Owner;
        if (owner == null || owner.CustomProperties == null || !owner.CustomProperties.ContainsKey(COLOR_KEY))
            return;

        int idx = (int)owner.CustomProperties[COLOR_KEY];

        Color[] palette = {
            new Color(0.90f,0.20f,0.20f),
            new Color(0.20f,0.50f,0.95f),
            new Color(0.20f,0.80f,0.35f),
            new Color(0.95f,0.80f,0.20f)
        };
        Color color = palette[idx % palette.Length];

        foreach (var r in renderersToTint)
        {
            if (r == null) continue;
            var mats = r.materials;
            for (int i = 0; i < mats.Length; i++)
            {
                var m = mats[i];
                if (m == null)
                {
                    mats[i] = CreateColoredMaterial(color);
                    continue;
                }

                if (m.HasProperty("_BaseColor"))
                {
                    m.SetColor("_BaseColor", color);
                }
                else if (m.HasProperty("_Color"))
                {
                    m.color = color;
                }
                else
                {
                    mats[i] = CreateColoredMaterial(color);
                }
            }
            r.materials = mats;
        }
    }

    private Material CreateColoredMaterial(Color c)
    {
        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLit != null)
        {
            var mat = new Material(urpLit);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c);
            else if (mat.HasProperty("_Color")) mat.color = c;
            return mat;
        }

        Shader standard = Shader.Find("Standard");
        var stdMat = new Material(standard != null ? standard : Shader.Find("Sprites/Default"));
        if (stdMat.HasProperty("_Color")) stdMat.color = c;
        return stdMat;
    }

    private void UpdateCrownVisual()
    {
        if (crownVisual == null) return;

        int myActorNumber = photonView.Owner.ActorNumber;
        int crownOwner = GameManager.GetCrownOwnerActorNumber();
        crownVisual.SetActive(myActorNumber == crownOwner);
    }
}

//Clase estatica para notificar si el stun fue efectivo o no y decidir sobre la transferencia de la corona
public static class HitResultNotifier
{
    private static bool? lastResult;

    public static void Report(bool stunApplied) => lastResult = stunApplied;
    public static bool? Consume()
    {
        var r = lastResult;
        lastResult = null;
        return r;
    }
}

public static class MyEventCodes
{
    public const byte PlayerStateUpdate = 1;
}