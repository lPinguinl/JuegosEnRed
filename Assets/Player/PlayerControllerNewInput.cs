using UnityEngine;
using Photon.Pun;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class PlayerControllerNewInput : MonoBehaviourPun, IPunObservable
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpForce = 5f;
    [SerializeField] private float groundCheckDistance = 0.2f; // Distancia para raycast al suelo

    private Rigidbody rb;
    private PlayerControls controls;
    private Vector2 moveInput;

    private Vector3 networkPosition;
    private Quaternion networkRotation;

    private bool isGrounded = false; // Se inicializa en false para evitar saltos indeseados al inicio.

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        controls = new PlayerControls();
    }

    private void OnEnable()
    {
        controls.Player.Enable();

        // Movimiento
        controls.Player.Move.performed += ctx => { if (photonView.IsMine) moveInput = ctx.ReadValue<Vector2>(); };
        controls.Player.Move.canceled += ctx => { if (photonView.IsMine) moveInput = Vector2.zero; };

        // Salto
        controls.Player.Jump.performed += ctx => { if (photonView.IsMine) TryJump(); };
    }

    private void OnDisable()
    {
        controls.Player.Disable();
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
    }

    private void FixedUpdate()
    {
        if (photonView.IsMine)
        {
            GroundCheck();

            Vector3 move = new Vector3(moveInput.x, 0f, moveInput.y);
            if (move.magnitude > 1f) move.Normalize();

            Vector3 targetPos = rb.position + move * moveSpeed * Time.fixedDeltaTime;
            rb.MovePosition(targetPos);

            if (move.sqrMagnitude > 0.001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(move);
                rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRot, 10f * Time.fixedDeltaTime));
            }
        }
        else
        {
            transform.position = Vector3.Lerp(transform.position, networkPosition, 10f * Time.fixedDeltaTime);
            transform.rotation = Quaternion.Slerp(transform.rotation, networkRotation, 10f * Time.fixedDeltaTime);
        }
    }

    private void TryJump()
    {
        if (isGrounded)
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }
    }

    private void GroundCheck()
    {
        // Se lanza el rayo desde los pies del personaje.
        Vector3 rayOrigin = transform.position + Vector3.down * 0.45f;
        Ray ray = new Ray(rayOrigin, Vector3.down);

        // Se ajusta la distancia para que solo detecte el suelo cuando está muy cerca.
        if (Physics.Raycast(ray, out RaycastHit hit, groundCheckDistance))
        {
            var walkable = hit.collider.GetComponent<IWalkableSurface>();
            if (walkable != null && walkable.IsWalkable())
            {
                isGrounded = true;
            }
            else
            {
                isGrounded = false;
            }
        }
        else
        {
            isGrounded = false;
        }
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(rb.position);
            stream.SendNext(rb.rotation);
        }
        else
        {
            networkPosition = (Vector3)stream.ReceiveNext();
            networkRotation = (Quaternion)stream.ReceiveNext();
        }
    }

    /*
    // Opcional: para visualizar el rayo en el Editor.
    private void OnDrawGizmos()
    {
        // Dibuja el rayo en el editor para depurar la detección del suelo.
        Gizmos.color = isGrounded ? Color.green : Color.red;
        Vector3 rayOrigin = transform.position + Vector3.down * 0.45f;
        Gizmos.DrawRay(rayOrigin, Vector3.down * groundCheckDistance);
    }
    */
}