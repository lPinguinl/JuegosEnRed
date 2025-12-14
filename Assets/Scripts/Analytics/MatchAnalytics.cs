using UnityEngine;
using Unity.Services.Analytics;
using System.Collections.Generic;

public class MatchAnalytics : MonoBehaviour
{
    public static MatchAnalytics Instance { get; private set; }

    private float matchStartTime;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void MatchStart(int playerCount, float lobbyTime)
    {
        matchStartTime = Time.time;

        AnalyticsService.Instance.CustomData("match_start", new Dictionary<string, object>
        {
            { "player_count", playerCount },
            { "lobby_time", lobbyTime }
        });
    }

    public void MatchEnd()
    {
        AnalyticsService.Instance.CustomData("match_end", new Dictionary<string, object>
        {
            { "match_duration", Time.time - matchStartTime }
        });
    }

    public void CrownPickup(int actorNumber)
    {
        AnalyticsService.Instance.CustomData("crown_pickup", new Dictionary<string, object>
        {
            { "actor", actorNumber }
        });
    }

    public void CrownLost(int actorNumber)
    {
        AnalyticsService.Instance.CustomData("crown_lost", new Dictionary<string, object>
        {
            { "actor", actorNumber }
        });
    }

    public void PlayerJump(int actor)
    {
        AnalyticsService.Instance.CustomData("player_jump", new Dictionary<string, object>
        {
            { "actor", actor }
        });
    }

    public void PlayerStunned(int attacker, int victim)
    {
        AnalyticsService.Instance.CustomData("player_stunned", new Dictionary<string, object>
        {
            { "attacker", attacker },
            { "victim", victim }
        });
    }

    public void PlayerBlocked(int attacker, int victim)
    {
        AnalyticsService.Instance.CustomData("player_blocked_hit", new Dictionary<string, object>
        {
            { "attacker", attacker },
            { "victim", victim }
        });
    }

    // ================= ZONES =================

    public void ZoneEnter(int actorNumber, string zoneName)
    {
        AnalyticsService.Instance.CustomData("zone_enter", new Dictionary<string, object>
        {
            { "actor", actorNumber },
            { "zone", zoneName }
        });
    }

    public void ZoneExit(int actorNumber, string zoneName, float duration)
    {
        AnalyticsService.Instance.CustomData("zone_exit", new Dictionary<string, object>
        {
            { "actor", actorNumber },
            { "zone", zoneName },
            { "duration", duration }
        });
    }

    public void ZoneCombat(string zoneName, int attackerActor, int targetActor)
    {
        AnalyticsService.Instance.CustomData("combat_zone_event", new Dictionary<string, object>
        {
            { "zone", zoneName },
            { "attacker", attackerActor },
            { "target", targetActor }
        });
    }
}
