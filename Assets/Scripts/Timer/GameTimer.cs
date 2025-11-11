using UnityEngine;
using Photon.Pun;
using ExitGames.Client.Photon;
using System.Linq; // para loguear keys cambiadas
using Hashtable = ExitGames.Client.Photon.Hashtable;

// Interfaz: el GameManager implementa esto para enterarse cuando el tiempo llega a 0
public interface IGameEndHandler
{
    void OnMatchTimeEnded();
}

/// <summary>
/// Timer de partida sincronizado por Photon.
/// - Master: establece Room Props "matchStartTime" (double) y "matchDuration" (double) si no existen.
/// - Todos: leen esas props y cuentan con PhotonNetwork.Time.
/// - Al llegar a 0, invoca OnMatchTimeEnded() en GameManager (solo el Master cambia de escena).
/// </summary>
public class GameTimer : MonoBehaviourPunCallbacks
{
    public const string ROOM_KEY_START    = "matchStartTime";
    public const string ROOM_KEY_DURATION = "matchDuration";

    private IMatchClock clock;         // Fuente de tiempo global
    private ITimeDisplay display;      // UI (TimerTextPresenter)
    private IGameEndHandler endHandler;

    private double startTime;
    private double durationSec;
    private bool initialized = false;
    private bool finished = false;

    // Llamado normalmente por GameManager al entrar en InGame (ideal).
    public void Initialize(IMatchClock clock, ITimeDisplay display, IGameEndHandler endHandler, double defaultDurationSec)
    {
        this.clock = clock;
        this.display = display;
        this.endHandler = endHandler;

        // PHOTON (Master): crear props si faltan
        if (PhotonNetwork.IsMasterClient)
        {
            var roomProps = PhotonNetwork.CurrentRoom?.CustomProperties;
            bool hasStart = roomProps != null && roomProps.ContainsKey(ROOM_KEY_START);
            bool hasDur   = roomProps != null && roomProps.ContainsKey(ROOM_KEY_DURATION);

            var set = new Hashtable();
            if (!hasStart) set[ROOM_KEY_START] = PhotonNetwork.Time;
            if (!hasDur)   set[ROOM_KEY_DURATION] = defaultDurationSec;

            if (set.Count > 0)
            {
                Debug.Log($"[GameTimer] Master set props. startNow={set.ContainsKey(ROOM_KEY_START)} durSet={set.ContainsKey(ROOM_KEY_DURATION)}");
                PhotonNetwork.CurrentRoom.SetCustomProperties(set);
            }
        }

        // Intento de lectura inmediata; si no es posible, quedamos a la espera de OnRoomPropertiesUpdate
        TryReadRoomProps(out initialized);
        Debug.Log($"[GameTimer] Initialize on {(PhotonNetwork.IsMasterClient ? "Master" : "Client")}. ok={initialized} start={startTime:F3} dur={durationSec:F1}");
        if (initialized)
        {
            TryAutoBootstrap(); // por si falta algo
        }
    }

    private void Update()
    {
        // Si aún no nos “marcaron” como inicializados, volvemos a intentar leer props
        if (!initialized && TryReadRoomProps(out initialized) && initialized)
        {
            Debug.Log("[GameTimer] Late init by polling props.");
            TryAutoBootstrap();
        }

        // Auto-bootstrap suave: si el GameManager no llamó Initialize, nos aseguramos de tener reloj/handler
        if (clock == null || endHandler == null)
        {
            TryAutoBootstrap();
        }

        if (!initialized || finished || clock == null) return;

        double now = clock.Now;
        double endTime = startTime + durationSec;
        double remaining = endTime - now;

        // UI best-effort
        try { display?.SetTime(remaining); } catch { /* ignorar errores de UI */ }

        if (remaining <= 0.0 && !finished)
        {
            finished = true;
            endHandler?.OnMatchTimeEnded();
        }

        // Log cada ~1s para diagnóstico
        /*if (Time.frameCount % 60 == 0)
            Debug.Log($"[GameTimer] remaining={remaining:F2} now={now:F2} end={endTime:F2}");*/
    }

    public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
    {
        if (propertiesThatChanged == null) return;

        // Debug: ver qué cambió
        var keys = string.Join(",", propertiesThatChanged.Keys.Cast<object>());
        Debug.Log($"[GameTimer] OnRoomPropertiesUpdate keys=[{keys}]");

        if (TryReadRoomProps(out bool ok) && ok)
        {
            if (!initialized)
            {
                initialized = true;
                Debug.Log($"[GameTimer] Inicializado desde Room Properties. start={startTime:F2} dur={durationSec:F2}");
            }
            TryAutoBootstrap(); // asegurar reloj/handler aunque el GM no nos haya inicializado
        }
    }

    private bool TryReadRoomProps(out bool ok)
    {
        ok = false;
        var roomProps = PhotonNetwork.CurrentRoom?.CustomProperties;
        if (roomProps == null) return false;

        if (roomProps.ContainsKey(ROOM_KEY_START) && roomProps.ContainsKey(ROOM_KEY_DURATION))
        {
            startTime   = (double)roomProps[ROOM_KEY_START];
            durationSec = (double)roomProps[ROOM_KEY_DURATION];
            ok = true;
            return true;
        }
        return false;
    }

    // Se llama cuando detectamos props válidas, para autonfigurarnos si el GM no lo hizo aún.
    private void TryAutoBootstrap()
    {
        if (clock == null)
        {
            clock = new PhotonMatchClock(); // reloj global de Photon
            Debug.Log("[GameTimer] Auto-bootstrap: clock = PhotonMatchClock");
        }

        if (endHandler == null)
        {
            // Normalmente el GameTimer está en el mismo GameObject que el GameManager
            endHandler = GetComponentInParent<IGameEndHandler>();
            if (endHandler == null)
            {
                var gm = FindObjectOfType<GameManager>();
                if (gm != null) endHandler = gm;
            }
            if (endHandler != null)
                Debug.Log("[GameTimer] Auto-bootstrap: endHandler asignado.");
        }

        if (display == null)
        {
            // La UI es opcional: si no está, el conteo igual corre y el Master terminará el match
            var presenter = FindObjectOfType<TimerTextPresenter>(true);
            if (presenter != null)
            {
                display = presenter;
                Debug.Log("[GameTimer] Auto-bootstrap: display (TimerTextPresenter) asignado.");
            }
        }
    }
}