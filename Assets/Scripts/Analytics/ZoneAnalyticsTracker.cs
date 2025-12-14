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
        if (!other.CompareTag("Player")) return;
        if (isInside) return;

        isInside = true;
        enterTime = Time.time;

        int playerId = PhotonNetwork.LocalPlayer.ActorNumber;
        MatchAnalytics.Instance.ZoneEnter(playerId, zoneId);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (!isInside) return;

        isInside = false;
        float duration = Time.time - enterTime;

        int playerId = PhotonNetwork.LocalPlayer.ActorNumber;
        MatchAnalytics.Instance.ZoneExit(playerId, zoneId, duration);
    }
}