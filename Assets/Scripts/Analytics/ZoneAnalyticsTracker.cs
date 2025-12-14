using UnityEngine;
using Photon.Pun;

public class ZoneAnalyticsTracker : MonoBehaviour
{
    [Header("Analytics")]
    [SerializeField] private string zoneId;

    private float enterTime;
    private bool isInside = false;

    private void Awake()
    {
        if (string.IsNullOrEmpty(zoneId))
        {
            zoneId = gameObject.name;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        PhotonView pv = other.GetComponentInParent<PhotonView>();
        if (pv == null || !pv.IsMine) return;
        if (isInside) return;

        isInside = true;
        enterTime = Time.time;

        MatchAnalytics.Instance.ZoneEnter(
            pv.Owner.ActorNumber,
            zoneId
        );
    }

    private void OnTriggerExit(Collider other)
    {
        PhotonView pv = other.GetComponentInParent<PhotonView>();
        if (pv == null || !pv.IsMine) return;
        if (!isInside) return;

        isInside = false;
        float duration = Time.time - enterTime;

        MatchAnalytics.Instance.ZoneExit(
            pv.Owner.ActorNumber,
            zoneId,
            duration
        );
    }
}