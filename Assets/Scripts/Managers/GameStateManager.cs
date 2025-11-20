using System;
using System.Collections.Generic;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

/// <summary>
/// Coordina el flujo de estados previos a la partida y decide quién obtiene la corona.
/// Toda la sincronización ocurre a través de Room Custom Properties y RaiseEvent/RPC de Photon.
///</summary>
public class GameStateManager : MonoBehaviourPunCallbacks
{
    public enum State
    {
        PreGameCountdown = 0,
        CrownAssignment = 1,
        InGame = 2,
        GameEnded = 3
    }

    private const string RoomStateKey = "MatchState";
    private const string RoomStateStartKey = "MatchStateStart";
    private const string RoomStateDurationKey = "MatchPreCountdownDuration";
    private const string CrownOwnerPropertyKey = "CrownOwner";

    private const byte CrownAttemptEventCode = 38; // Código de RaiseEvent para presiones de tecla E
    private const double CrownAttemptToleranceSeconds = 0.05; // Tolerancia por latencia (último en el dead-line)

    [SerializeField] private double preGameCountdownSeconds = 5.0;
    [Header("UI Crown Hint")]
    [SerializeField] private TMPro.TMP_Text crownHintText;

    private readonly Dictionary<int, double> lastPressByActor = new(); // ActorNumber ➜ último timestamp (PhotonNetwork.Time) de la tecla E

    private IMatchClock clock;
    private bool initialized;
    private bool hasStateSnapshot;

    private State currentState = State.PreGameCountdown;
    private double stateStartTime;
    private double currentStateDuration;
    private double preGameDeadline;
    private int lastCountdownBroadcast = -1;

    // Eventos para que otros componentes (GameManager, UI, etc.) reaccionen al estado/tiempo.
    public event Action<State> StateChanged;
    public event Action<int> PreGameCountdownTick;
    public event Action<int> CrownWinnerDecided;

    public State CurrentState => currentState;
    public double PreGameCountdownSeconds => preGameCountdownSeconds;

    private void OnEnable()
    {
        // Escuchamos los RaiseEvent que llegan desde clientes (código CrownAttemptEventCode)
        if (PhotonNetwork.NetworkingClient != null)
            PhotonNetwork.NetworkingClient.EventReceived += OnPhotonEvent;
    }

    private void OnDisable()
    {
        if (PhotonNetwork.NetworkingClient != null)
            PhotonNetwork.NetworkingClient.EventReceived -= OnPhotonEvent;
    }

    /// <summary>
    /// Debe llamarse una sola vez desde GameManager, compartiendo el reloj común (PhotonMatchClock).
    /// </summary>
    public void Initialize(IMatchClock sharedClock)
    {
        if (initialized) return;

        clock = sharedClock ?? throw new ArgumentNullException(nameof(sharedClock));

        if (PhotonNetwork.CurrentRoom == null)
        {
            Debug.LogError("[GSM] No hay sala activa; no se puede sincronizar estados.");
            return;
        }

        initialized = true;

        if (PhotonNetwork.IsMasterClient)
        {
            EnsureInitialState(); // Esto hace ApplyState()
            hasStateSnapshot = true;
        }
        else
        {
            hasStateSnapshot = TryReadStateFromRoom();
            Debug.Log($"[GSM] Inicializado (Cliente). hasStateSnapshot={hasStateSnapshot}. Si es false, haré polling hasta que aparezcan las props.");
        }
    }

    private void Update()
    {
        if (!initialized || clock == null) return;

        // Fallback: si aún no tenemos snapshot, intentar leerlo cada frame
        if (!hasStateSnapshot)
        {
            hasStateSnapshot = TryReadStateFromRoom();
            if (!hasStateSnapshot) return; // todavía no puedo avanzar
        }

        if (currentState == State.PreGameCountdown)
        {
            UpdatePreGameCountdown();
        }
    }

    /// <summary>
    /// Llamado por cada jugador local cuando presiona la tecla E durante la cuenta regresiva.
    /// </summary>
    public void ReportCrownAttempt(int actorNumber)
    {
        if (!initialized || currentState != State.PreGameCountdown) return;

        double pressTime = clock.Now;

        if (PhotonNetwork.IsMasterClient)
        {
            // El Master registra directamente sin necesitar RaiseEvent.
            RegisterCrownAttempt(actorNumber, pressTime);
        }
        else
        {
            SendCrownAttemptEvent(actorNumber, pressTime);
        }
    }

    /// <summary>
    /// Envia un RaiseEvent confiable al MasterClient con ActorNumber y timestamp del intento.
    /// </summary>
    private void SendCrownAttemptEvent(int actorNumber, double pressTime)
    {
        var payload = new object[] { actorNumber, pressTime };
        var options = new RaiseEventOptions { Receivers = ReceiverGroup.MasterClient }; // Solo el Master necesita este dato
        var sendOptions = new SendOptions { Reliability = true }; // Confiable para no perder intentos

        PhotonNetwork.RaiseEvent(CrownAttemptEventCode, payload, options, sendOptions);
    }

    /// <summary>
    /// Procesa los RaiseEvent recibidos por el Master y guarda el último timestamp válido.
    /// </summary>
    private void OnPhotonEvent(EventData photonEvent)
    {
        if (!PhotonNetwork.IsMasterClient || photonEvent.Code != CrownAttemptEventCode) return;

        if (photonEvent.CustomData is not object[] payload || payload.Length != 2) return;

        int actorNumber = (int)payload[0];
        double pressTime = (double)payload[1];

        RegisterCrownAttempt(actorNumber, pressTime);
    }

    private void RegisterCrownAttempt(int actorNumber, double pressTime)
    {
        if (pressTime > preGameDeadline + CrownAttemptToleranceSeconds)
        {
            // Intento fuera de tiempo (latencia o spam tras finalizar la cuenta).
            return;
        }

        lastPressByActor[actorNumber] = pressTime;
    }

    private void EnsureInitialState()
    {
        var room = PhotonNetwork.CurrentRoom;
        if (room == null) return;

        var props = room.CustomProperties;

        if (!props.ContainsKey(RoomStateKey) || !props.ContainsKey(RoomStateStartKey))
        {
            double now = PhotonNetwork.Time;

            var set = new Hashtable
            {
                { RoomStateKey, (int)State.PreGameCountdown },
                { RoomStateStartKey, now },
                { RoomStateDurationKey, preGameCountdownSeconds }
            };

            room.SetCustomProperties(set);
            ApplyState(State.PreGameCountdown, now, preGameCountdownSeconds);
        }
        else
        {
            TryReadStateFromRoom();
        }
    }

    private void ChangeState(State nextState)
    {
        var room = PhotonNetwork.CurrentRoom;
        if (room == null) return;

        double now = PhotonNetwork.Time;

        var props = new Hashtable
        {
            { RoomStateKey, (int)nextState },
            { RoomStateStartKey, now }
        };

        if (nextState == State.PreGameCountdown)
        {
            props[RoomStateDurationKey] = preGameCountdownSeconds;
        }

        // Escribimos en la sala para que todos los clientes reciban el cambio vía OnRoomPropertiesUpdate.
        room.SetCustomProperties(props);

        double duration = nextState == State.PreGameCountdown ? preGameCountdownSeconds : 0.0;
        ApplyState(nextState, now, duration);
    }

    private void ApplyState(State newState, double startTime, double duration)
    {
        currentState = newState;
        stateStartTime = startTime;
        currentStateDuration = duration;
        lastCountdownBroadcast = -1;
        
        // Mostrar/ocultar el texto de \"spammear E\" según el estado
        if (crownHintText != null)
        {
            // Lo mostramos SOLO durante la pre-cuenta
            crownHintText.gameObject.SetActive(newState == State.PreGameCountdown);
        }

        if (newState == State.PreGameCountdown)
        {
            lastPressByActor.Clear();
            preGameDeadline = stateStartTime + (duration > 0.0 ? duration : preGameCountdownSeconds);
        }

        hasStateSnapshot = true;

        StateChanged?.Invoke(currentState);

        if (PhotonNetwork.IsMasterClient && currentState == State.CrownAssignment)
        {
            ResolveCrownAssignment();
        }
    }

    private void UpdatePreGameCountdown()
    {
        double duration = currentStateDuration > 0.0 ? currentStateDuration : preGameCountdownSeconds;
        double endTime = stateStartTime + duration;
        preGameDeadline = endTime;

        double remaining = endTime - clock.Now;
        int seconds = Mathf.Max(0, Mathf.CeilToInt((float)remaining));

        if (seconds != lastCountdownBroadcast)
        {
            lastCountdownBroadcast = seconds;
            PreGameCountdownTick?.Invoke(seconds);
        }

        if (PhotonNetwork.IsMasterClient && remaining <= 0.0)
        {
            ChangeState(State.CrownAssignment);
        }
    }

    private void ResolveCrownAssignment()
    {
        int winningActor = DetermineCrownWinner();

        if (winningActor != -1)
        {
            // Guardamos la corona en Room Custom Properties.
            var props = new Hashtable { { CrownOwnerPropertyKey, winningActor } };
            PhotonNetwork.CurrentRoom?.SetCustomProperties(props);
        }

        // Notificamos a todos quién ganó (incluyendo al propio Master) mediante un RPC.
        photonView.RPC(nameof(RPC_AnnounceCrownWinner), RpcTarget.All, winningActor);

        ChangeState(State.InGame);
    }

    private int DetermineCrownWinner()
    {
        if (lastPressByActor.Count == 0)
        {
            return GetLowestActorNumberInRoom();
        }

        int winner = -1;
        double latestTime = double.MinValue;

        foreach (var kvp in lastPressByActor)
        {
            int actor = kvp.Key;
            double pressTime = kvp.Value;

            if (pressTime > preGameDeadline + CrownAttemptToleranceSeconds)
                continue;

            bool isBetter = pressTime > latestTime || (pressTime == latestTime && (winner == -1 || actor < winner));
            if (isBetter)
            {
                latestTime = pressTime;
                winner = actor;
            }
        }

        return winner == -1 ? GetLowestActorNumberInRoom() : winner;
    }

    private int GetLowestActorNumberInRoom()
    {
        var players = PhotonNetwork.PlayerList;
        if (players == null || players.Length == 0) return -1;

        int lowest = int.MaxValue;
        foreach (var player in players)
        {
            if (player != null && player.ActorNumber < lowest)
                lowest = player.ActorNumber;
        }

        return lowest == int.MaxValue ? -1 : lowest;
    }

    public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
    {
        if (propertiesThatChanged == null) return;

        var keys = string.Join(",", propertiesThatChanged.Keys);
        Debug.Log($"[GSM] OnRoomPropertiesUpdate keys=[{keys}] Master={PhotonNetwork.IsMasterClient}");

        if (propertiesThatChanged.ContainsKey(RoomStateKey) || propertiesThatChanged.ContainsKey(RoomStateStartKey))
        {
            hasStateSnapshot = TryReadStateFromRoom() || hasStateSnapshot;
        }
    }

    private bool TryReadStateFromRoom()
    {
        var room = PhotonNetwork.CurrentRoom;
        if (room == null) return false;

        var props = room.CustomProperties;
        if (!props.TryGetValue(RoomStateKey, out object stateValue) ||
            !props.TryGetValue(RoomStateStartKey, out object startValue))
        {
            return false;
        }

        State state = (State)(int)stateValue;
        double start = (double)startValue;

        double duration = state == State.PreGameCountdown &&
                          props.TryGetValue(RoomStateDurationKey, out object durationValue)
            ? (double)durationValue
            : 0.0;

        Debug.Log($"[GSM] ApplyState state={state} start={start:F3} duration={duration:F2} now={clock?.Now:F3}");
        ApplyState(state, start, duration);
        return true;
    }
    
    //Disconetion Handler
    public override void OnPlayerLeftRoom(Photon.Realtime.Player otherPlayer)
    {
        // Solo el Master escribe la verdad en Room Properties
        if (!PhotonNetwork.IsMasterClient) return;
        if (otherPlayer == null) return;

        // 1) Leer quién es el dueño actual de la corona
        var room = PhotonNetwork.CurrentRoom;
        if (room == null) return;

        int currentCrownOwner = -1;
        if (room.CustomProperties != null &&
            room.CustomProperties.TryGetValue("CrownOwner", out object v) &&
            v is int owner)
        {
            currentCrownOwner = owner;
        }

        // 2) Si el que se fue era el dueño, decidir nuevo dueño
        if (currentCrownOwner == otherPlayer.ActorNumber)
        {
            int newOwner = PickNewCrownOwnerExcluding(otherPlayer.ActorNumber);

            // 3) Escribir Room Prop y anunciar
            var props = new ExitGames.Client.Photon.Hashtable { { "CrownOwner", newOwner } };
            room.SetCustomProperties(props);

            // Reusar RPC para feedback y lógica local
            photonView.RPC(nameof(RPC_AnnounceCrownWinner), RpcTarget.All, newOwner);
        }
    }

// Regla simple: menor ActorNumber entre los que siguen en la sala; si no hay, -1
    private int PickNewCrownOwnerExcluding(int excludedActorNumber)
    {
        var players = PhotonNetwork.PlayerList;
        if (players == null || players.Length == 0) return -1;

        int candidate = -1;
        foreach (var p in players)
        {
            if (p == null) continue;
            if (p.ActorNumber == excludedActorNumber) continue;
            if (candidate == -1 || p.ActorNumber < candidate)
                candidate = p.ActorNumber;
        }
        return candidate; // si queda solo, retorna -1
    }

    [PunRPC]
    private void RPC_AnnounceCrownWinner(int winnerActorNumber)
    {
        // Este RPC se dispara en todos los clientes para mostrar feedback de UI y sincronizar lógica local.
        CrownWinnerDecided?.Invoke(winnerActorNumber);
    }
}