using System.Collections;
using Photon.Pun;
using UnityEngine;

[RequireComponent(typeof(PhotonView))]
[RequireComponent(typeof(Rigidbody))]
public class Grenade : MonoBehaviourPun, IPunObservable
{
    [Header("Explosion")]
    [SerializeField] private float fuseTime = 2.0f;
    [SerializeField] private float explosionRadius = 3.5f;
    [SerializeField] private float explosionForce = 6f; // opcional: empuje en explosión
    [SerializeField] private LayerMask playerLayerMask;

    [Header("Collision")]
    [SerializeField] private bool explodeOnImpact = true;

    private Rigidbody rb;
    private int attackerActorNumber;
    private bool exploded = false;

    // Sincronización
    private Vector3 netPosition;
    private Quaternion netRotation;
    private Vector3 netVelocity;
    private Vector3 netAngularVelocity;
    private bool firstSync = true;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        // Recomendado para suavizado visual
        rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    public void Init(int attackerActorNumber)
    {
        this.attackerActorNumber = attackerActorNumber;
    }

    public void Launch(float forwardForce, float upwardModifier)
    {
        if (rb == null) rb = GetComponent<Rigidbody>();

        // Datos iniciales de lanzamiento desde el owner
        Vector3 pos = transform.position;
        Vector3 fwd = transform.forward.normalized;

        // RPC a todos para aplicar exactamente el mismo estado inicial
        photonView.RPC(nameof(RPC_ApplyLaunch), RpcTarget.All, pos, fwd, forwardForce, upwardModifier);

        if (photonView.IsMine)
        {
            // Solo el owner inicia el fuse
            StartCoroutine(FuseCoroutine());
        }
    }

    [PunRPC]
    private void RPC_ApplyLaunch(Vector3 startPosition, Vector3 forwardDir, float forwardForce, float upwardModifier, PhotonMessageInfo info)
    {
        if (rb == null) rb = GetComponent<Rigidbody>();

        // Fijar posición inicial exacta en todos
        rb.position = startPosition;
        transform.position = startPosition;

        // Fijar rotación para alinear el forward con la dir enviada
        if (forwardDir.sqrMagnitude > 0.0001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(forwardDir, Vector3.up);
            rb.rotation = targetRot;
            transform.rotation = targetRot;
        }

        // Borrar cualquier velocidad previa y aplicar exactamente la misma
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        Vector3 force = forwardDir.normalized * forwardForce + Vector3.up * upwardModifier;
        rb.AddForce(force, ForceMode.VelocityChange);

        // Reset del filtro de sync para evitar “salto” en el primer snapshot
        firstSync = true;
        netPosition = rb.position;
        netRotation = rb.rotation;
        netVelocity = rb.velocity;
        netAngularVelocity = rb.angularVelocity;
    }

    private IEnumerator FuseCoroutine()
    {
        yield return new WaitForSeconds(fuseTime);
        Explode();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!explodeOnImpact) return;
        if (exploded) return;
        Explode();
    }

    private void Explode()
    {
        if (exploded) return;
        exploded = true;

        // AOE: encontrar jugadores alrededor
        Collider[] hits = Physics.OverlapSphere(transform.position, explosionRadius, playerLayerMask, QueryTriggerInteraction.Ignore);

        foreach (var hit in hits)
        {
            PhotonView targetPV = hit.GetComponent<PhotonView>();
            if (targetPV == null) targetPV = hit.GetComponentInParent<PhotonView>();
            if (targetPV == null) continue;

            // Opcional: no afectar al atacante
            if (targetPV.Owner != null && targetPV.Owner.ActorNumber == attackerActorNumber)
                continue;

            // Notificar intento de stun con attackerActorNumber
            targetPV.RPC("RPC_OnStunned", RpcTarget.All, transform.position, attackerActorNumber);
        }

        // Destruir sincronizado
        if (photonView.IsMine || PhotonNetwork.IsMasterClient)
            PhotonNetwork.Destroy(gameObject);
        else
            Destroy(gameObject);
    }

    private void FixedUpdate()
    {
        if (photonView.IsMine) return;

        // Suavizado para no-owners
        // Ajustá estos factores si querés más/menos suavizado
        float posLerp = 16f;
        float rotLerp = 16f;

        // Mover hacia la posición/rotación recibida
        rb.MovePosition(Vector3.Lerp(rb.position, netPosition, Time.fixedDeltaTime * posLerp));
        rb.MoveRotation(Quaternion.Slerp(rb.rotation, netRotation, Time.fixedDeltaTime * rotLerp));

        // Sincronizar velocidades para coherencia (mitiga deriva)
        rb.velocity = Vector3.Lerp(rb.velocity, netVelocity, 0.5f);
        rb.angularVelocity = Vector3.Lerp(rb.angularVelocity, netAngularVelocity, 0.5f);
    }

    // IPunObservable: sincronizar estado de Rigidbody
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            // Owner escribe
            stream.SendNext(rb.position);
            stream.SendNext(rb.rotation);
            stream.SendNext(rb.velocity);
            stream.SendNext(rb.angularVelocity);
        }
        else
        {
            // No-owner lee
            netPosition = (Vector3)stream.ReceiveNext();
            netRotation = (Quaternion)stream.ReceiveNext();
            netVelocity = (Vector3)stream.ReceiveNext();
            netAngularVelocity = (Vector3)stream.ReceiveNext();

            if (firstSync)
            {
                // Ajuste inicial para evitar salto
                rb.position = netPosition;
                rb.rotation = netRotation;
                rb.velocity = netVelocity;
                rb.angularVelocity = netAngularVelocity;
                firstSync = false;
            }
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.25f);
        Gizmos.DrawSphere(transform.position, explosionRadius);
    }
#endif
}