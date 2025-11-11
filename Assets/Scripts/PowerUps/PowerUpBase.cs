using Photon.Pun;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public abstract class PowerUpBase : MonoBehaviourPun, IPowerUp
{
    protected bool collected;

    protected virtual void Awake()
    {
        var col = GetComponent<Collider>();
        if (col) col.isTrigger = true;
    }

    protected virtual void OnTriggerEnter(Collider other)
    {
        if (collected) return;
        if (!PhotonNetwork.IsMasterClient) return;

        var controller = other.GetComponentInParent<PlayerControllerNewInput>();
        if (controller == null) return;

        // Evito condiciones de carrera
        collected = true;

        ApplyEffect(controller);
        OnCollected();
    }

    public abstract void ApplyEffect(PlayerControllerNewInput player);

    public virtual void OnCollected()
    {
        // Destruir sincronizado
        if (photonView != null && photonView.IsMine)
        {
            PhotonNetwork.Destroy(gameObject);
        }
        else
        {
            // Fallback local (por si el PV no es mío)
            Destroy(gameObject);
        }
    }
}