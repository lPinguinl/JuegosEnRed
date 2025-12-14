using UnityEngine;

public class LobbyAnalyticsHook : MonoBehaviour
{
    private float lobbyStartTime;

    public void Init()
    {
        lobbyStartTime = Time.time;
    }

    public float GetLobbyDuration()
    {
        return Time.time - lobbyStartTime;
    }
}