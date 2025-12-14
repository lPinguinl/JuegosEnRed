using Photon.Pun;
using UnityEngine;

public class PlayerAnalyticsTracker : MonoBehaviourPun
{
    public void ReportJump()
    {
        if (!photonView.IsMine) return;
        MatchAnalytics.Instance.PlayerJump(photonView.Owner.ActorNumber);
    }

    public void ReportStun(int attacker)
    {
        MatchAnalytics.Instance.PlayerStunned(attacker, photonView.Owner.ActorNumber);
    }

    public void ReportBlocked(int attacker)
    {
        MatchAnalytics.Instance.PlayerBlocked(attacker, photonView.Owner.ActorNumber);
    }
}