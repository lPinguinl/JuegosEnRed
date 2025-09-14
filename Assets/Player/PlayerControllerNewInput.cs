using UnityEngine;
using Photon.Pun;
using System.Collections;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class PlayerControllerNewInput : MonoBehaviourPun, IStunable, IPunObservable
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpForce = 5f;
    [SerializeField] private float groundCheckDistance = 0.2f;

    private Rigidbody rb;
    private PlayerControls controls;
    private Vector2 moveInput;

    private Vector3 networkPosition;
    private Quaternion networkRotation;

    private bool isGrounded = false;
    private bool canMove = true;
    private bool isStunned = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        controls = new PlayerControls();
    }

    private void OnEnable()
    {
        controls.Player.Enable();
        controls.Player.Move.performed += ctx => { if (photonView.IsMine) moveInput = ctx.ReadValue<Vector2>(); };
        controls.Player.Move.canceled += ctx => { if (photonView.IsMine) moveInput = Vector2.zero; };
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
            if (canMove)
            {
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
        }
        else
        {
            transform.position = Vector3.Lerp(transform.position, networkPosition, 10f * Time.fixedDeltaTime);
            transform.rotation = Quaternion.Slerp(transform.rotation, networkRotation, 10f * Time.fixedDeltaTime);
        }
    }

    private void TryJump()
    {
        if (isGrounded && canMove)
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }
    }

    private void GroundCheck()
    {
        Vector3 rayOrigin = transform.position + Vector3.down * 0.45f;
        Ray ray = new Ray(rayOrigin, Vector3.down);

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
    
    // Método de la interfaz IStunable (Ahora llamado por el RPC)
    public void Stun(Vector3 attackerPosition)
    {
        // No necesitamos código aquí porque la lógica está en el RPC
    }

    [PunRPC]
    private void RPC_OnStunned(Vector3 attackerPosition)
    {
        if (isStunned) return; // Evita stuns repetidos en la misma máquina

        isStunned = true;
        StartCoroutine(StunCoroutine(attackerPosition));
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

    // Nuevo método de la interfaz para verificar el estado de stun
    public bool IsStunned()
    {
        return isStunned;
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(rb.position);
            stream.SendNext(rb.rotation);
            stream.SendNext(isStunned);
        }
        else
        {
            networkPosition = (Vector3)stream.ReceiveNext();
            networkRotation = (Quaternion)stream.ReceiveNext();
            isStunned = (bool)stream.ReceiveNext();
        }
    }
}