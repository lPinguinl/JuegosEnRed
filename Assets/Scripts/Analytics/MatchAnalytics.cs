using UnityEngine;
using UnityEngine.Analytics;
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

        Analytics.CustomEvent("match_start", new Dictionary<string, object>
        {
            { "player_count", playerCount },
            { "lobby_time", lobbyTime }
        });
    }

    public void MatchEnd()
    {
        Analytics.CustomEvent("match_end", new Dictionary<string, object>
        {
            { "match_duration", Time.time - matchStartTime }
        });
    }

    public void CrownPickup(int actorNumber)
    {
        Analytics.CustomEvent("crown_pickup", new Dictionary<string, object>
        {
            { "actor", actorNumber }
        });
    }

    public void CrownLost(int actorNumber)
    {
        Analytics.CustomEvent("crown_lost", new Dictionary<string, object>
        {
            { "actor", actorNumber }
        });
    }

    public void PlayerJump(int actor)
    {
        Analytics.CustomEvent("player_jump", new Dictionary<string, object>
        {
            { "actor", actor }
        });
    }

    public void PlayerStunned(int attacker, int victim)
    {
        Analytics.CustomEvent("player_stunned", new Dictionary<string, object>
        {
            { "attacker", attacker },
            { "victim", victim }
        });
    }

    public void PlayerBlocked(int attacker, int victim)
    {
        Analytics.CustomEvent("player_blocked_hit", new Dictionary<string, object>
        {
            { "attacker", attacker },
            { "victim", victim }
        });
    }
    
// ================= ZONES =================

    public void ZoneEnter(int actorNumber, string zoneName)
    {
        Analytics.CustomEvent("zone_enter", new Dictionary<string, object>
        {
            { "actor", actorNumber },
            { "zone", zoneName }
        });
    }

    public void ZoneExit(int actorNumber, string zoneName, float duration)
    {
        Analytics.CustomEvent("zone_exit", new Dictionary<string, object>
        {
            { "actor", actorNumber },
            { "zone", zoneName },
            { "duration", duration }
        });
    }

    public void ZoneCombat(string zoneName, int attackerActor, int targetActor)
    {
        Analytics.CustomEvent("combat_zone_event", new Dictionary<string, object>
        {
            { "zone", zoneName },
            { "attacker", attackerActor },
            { "target", targetActor }
        });
    }


}