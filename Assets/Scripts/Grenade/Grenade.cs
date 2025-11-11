using System.Collections;
using Photon.Pun;
using UnityEngine;

[RequireComponent(typeof(PhotonView))]
[RequireComponent(typeof(Rigidbody))]
public class Grenade : MonoBehaviourPun
{
    [Header("Explosion")]
    [SerializeField] private float fuseTime = 2.0f;
    [SerializeField] private float explosionRadius = 3.5f;
    [SerializeField] private float explosionForce = 6f; // opcional, para empujar
    [SerializeField] private LayerMask playerLayerMask;

    [Header("Collision")]
    [SerializeField] private bool explodeOnImpact = true;

    private Rigidbody rb;
    private int attackerActorNumber;
    private bool exploded = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    public void Init(int attackerActorNumber)
    {
        this.attackerActorNumber = attackerActorNumber;
    }

    public void Launch(float forwardForce, float upwardModifier)
    {
        if (rb == null) rb = GetComponent<Rigidbody>();

        Vector3 force = transform.forward * forwardForce + Vector3.up * upwardModifier;
        rb.AddForce(force, ForceMode.VelocityChange);

        // Iniciar conteo para explotar
        StartCoroutine(FuseCoroutine());
    }

    private IEnumerator FuseCoroutine()
    {
        yield return new WaitForSeconds(fuseTime);
        Explode();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!explodeOnImpact) return;
        // Evitar explotar varias veces
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
            if (targetPV == null) continue;

            // No te stunees a vos mismo: opcional, comentá si querés que el atacante también se afecte
            if (targetPV.Owner != null && targetPV.Owner.ActorNumber == attackerActorNumber)
                continue;

            // Notificar stun a todos; el target decide si se aplica o escudo bloquea
            targetPV.RPC("RPC_OnStunned", RpcTarget.All, transform.position, attackerActorNumber);
        }

        // Destroy proyectil en red. Owner lo puede destruir; si querés, Master puede destruir también.
        if (photonView.IsMine || PhotonNetwork.IsMasterClient)
            PhotonNetwork.Destroy(gameObject);
        else
            Destroy(gameObject);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.25f);
        Gizmos.DrawSphere(transform.position, explosionRadius);
    }
#endif
}