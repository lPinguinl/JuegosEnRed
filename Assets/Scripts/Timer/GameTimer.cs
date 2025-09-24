using UnityEngine;
using Photon.Pun;
using ExitGames.Client.Photon;

public interface IGameEndHandler
{
    // Notificación de fin de tiempo
    void OnMatchTimeEnded();
}

public class GameTimer : MonoBehaviourPunCallbacks
{
    public const string ROOM_KEY_START = "matchStartTime";
    public const string ROOM_KEY_DURATION = "matchDuration";

    private IMatchClock clock;
    private ITimeDisplay display;
    private IGameEndHandler endHandler;

    private double startTime;     // PhotonNetwork.Time cuando arranca el match
    private double durationSec;   // Duración en segundos
    private bool initialized = false;
    private bool finished = false;

    // Inyección de dependencias (se llama desde GameManager)
    public void Initialize(IMatchClock clock, ITimeDisplay display, IGameEndHandler endHandler, double defaultDurationSec)
    {
        this.clock = clock;
        this.display = display;
        this.endHandler = endHandler;

        // El Master asegura que existan las props de inicio y duración
        if (PhotonNetwork.IsMasterClient)
        {
            var roomProps = PhotonNetwork.CurrentRoom?.CustomProperties;
            bool hasStart = roomProps != null && roomProps.ContainsKey(ROOM_KEY_START);
            bool hasDur = roomProps != null && roomProps.ContainsKey(ROOM_KEY_DURATION);

            Hashtable set = new Hashtable();
            if (!hasStart) set[ROOM_KEY_START] = PhotonNetwork.Time; // arranca ahora
            if (!hasDur) set[ROOM_KEY_DURATION] = defaultDurationSec;

            if (set.Count > 0)
                PhotonNetwork.CurrentRoom.SetCustomProperties(set);
        }

        // Intento leer inmediatamente (si aún no están, OnRoomPropertiesUpdate lo capturará)
        TryReadRoomProps(out initialized);
    }

    private void Update()
    {
        if (!initialized || finished || clock == null) return;

        double now = clock.Now;
        double endTime = startTime + durationSec;
        double remaining = endTime - now;

        display?.SetTime(remaining);

        if (remaining <= 0.0 && !finished)
        {
            finished = true;
            Debug.Log("[GameTimer] Time ended, notifying GameManager");
            endHandler?.OnMatchTimeEnded();
        }
    }

    public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
    {
        if (propertiesThatChanged == null) return;

        if (TryReadRoomProps(out bool ok) && ok)
            initialized = true;
    }

    private bool TryReadRoomProps(out bool ok)
    {
        ok = false;
        var roomProps = PhotonNetwork.CurrentRoom?.CustomProperties;
        if (roomProps == null) return false;

        if (roomProps.ContainsKey(ROOM_KEY_START) && roomProps.ContainsKey(ROOM_KEY_DURATION))
        {
            startTime = (double)roomProps[ROOM_KEY_START];
            durationSec = (double)roomProps[ROOM_KEY_DURATION];
            ok = true;
            return true;
        }
        return false;
    }
}