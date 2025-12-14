using UnityEngine;
using Photon.Pun;

public class ZoneAnalyticsTracker : MonoBehaviour
{
    [SerializeField] private string zoneName;
    private float enterTime;

    void OnTriggerEnter(Collider other)
    {
        PhotonView pv = other.GetComponentInParent<PhotonView>();
        if (pv == null || !pv.IsMine) return;

        enterTime = Time.time;
        MatchAnalytics.Instance.ZoneEnter(
            PhotonNetwork.LocalPlayer.ActorNumber,
            zoneName
        );
    }

    void OnTriggerExit(Collider other)
    {
        PhotonView pv = other.GetComponentInParent<PhotonView>();
        if (pv == null || !pv.IsMine) return;

        MatchAnalytics.Instance.ZoneExit(
            PhotonNetwork.LocalPlayer.ActorNumber,
            zoneName,
            Time.time - enterTime
        );
    }
}