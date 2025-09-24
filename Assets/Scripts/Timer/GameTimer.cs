using UnityEngine;
using Photon.Pun;
using ExitGames.Client.Photon;
using System.Collections;
using System.Diagnostics;

// Interfaz: el GameManager implementa esto para enterarse cuando el tiempo llega a 0
public interface IGameEndHandler
{
    void OnMatchTimeEnded();
}


// Usa Room Custom Properties para guardar el inicio y la duraci�n
// Usa el reloj global de Photon (PhotonNetwork.Time) para que todos cuenten igual
// Cuando llega a 0, avisa al GameManager para que canbie de escena
public class GameTimer : MonoBehaviourPunCallbacks
{
    // Claves en las Room Properties
    public const string ROOM_KEY_START = "matchStartTime";  // instante de arranque (PhotonNetwork.Time)
    public const string ROOM_KEY_DURATION = "matchDuration"; // duraci�n total en segundos

    // Dependencias inyectadas desde GameManager
    private IMatchClock clock;      // fuente de tiempo (PhotonMatchClock)
    private ITimeDisplay display;   // UI que muestra el tiempo (TimerTextPresenter)
    private IGameEndHandler endHandler; // qui�n se entera cuando termina (GameManager)

    // Estado del timer
    private double startTime;    
    private double durationSec; 
    private bool initialized = false; 
    private bool finished = false;    

    // Llamado por GameManager al comenzar la escena de juego
    // Crea/asegura las Room Properties (solo el Master) y luego las lee en todos.
    public void Initialize(IMatchClock clock, ITimeDisplay display, IGameEndHandler endHandler, double defaultDurationSec)
    {
        this.clock = clock;
        this.display = display;
        this.endHandler = endHandler;

        // El Master escribe las propiedades de inicio/duraci�n si a�n no existen
        if (PhotonNetwork.IsMasterClient)
        {
            var roomProps = PhotonNetwork.CurrentRoom?.CustomProperties;
            bool hasStart = roomProps != null && roomProps.ContainsKey(ROOM_KEY_START);
            bool hasDur = roomProps != null && roomProps.ContainsKey(ROOM_KEY_DURATION);

            Hashtable set = new Hashtable();
            if (!hasStart) set[ROOM_KEY_START] = PhotonNetwork.Time;     // arrancar ahora
            if (!hasDur) set[ROOM_KEY_DURATION] = defaultDurationSec;

            if (set.Count > 0)
                PhotonNetwork.CurrentRoom.SetCustomProperties(set);
        }

        // Intentamos leer de inmediato; si todav�a no llegaron, escuchamos OnRoomPropertiesUpdate
        TryReadRoomProps(out initialized);
    }

    private void Update()
    {
        // No hacemos nada hasta tener datos v�lidos o si ya termin�
        if (!initialized || finished || clock == null) return;

        // C�lculo del tiempo restante con reloj global de Photon
        double now = clock.Now;
        double endTime = startTime + durationSec;
        double remaining = endTime - now;

        // Actualizar UI
        display?.SetTime(remaining);

        // Al llegar a 0, notificamos
        if (remaining <= 0.0 && !finished)
        {
            finished = true;
            Debug.Log("[GameTimer] Time ended, notifying GameManager");
            endHandler?.OnMatchTimeEnded(); // El GameManager (si es Master) har� el LoadLevel
        }
    }

    // Se llama cuando cambian propiedades de la sala (Room Custom Properties)
    public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
    {
        if (propertiesThatChanged == null) return;

        if (TryReadRoomProps(out bool ok) && ok)
            initialized = true;
    }

    // Lee start/duration desde las Room Properties
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