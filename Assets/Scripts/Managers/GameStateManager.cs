using System;
using System.Collections.Generic;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
// Si tu GameManager está en un namespace (como Photon.Pun.Demo.PunBasics), descomenta la siguiente línea:
// using Photon.Pun.Demo.PunBasics;

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

    private const byte CrownAttemptEventCode = 38;
    private const double CrownAttemptToleranceSeconds = 0.05;

    [Header("Settings")]
    [SerializeField] private double preGameCountdownSeconds = 5.0;
    // NUEVO: Duración de la partida (ej: 120 segundos)
    [SerializeField] private double matchDurationSeconds = 120.0;

    [Header("UI Crown Hint")]
    [SerializeField] private TMPro.TMP_Text crownHintText;

    private readonly Dictionary<int, double> lastPressByActor = new();

    private IMatchClock clock;
    private bool initialized;
    private bool hasStateSnapshot;

    private State currentState = State.PreGameCountdown;
    private double stateStartTime;
    private double currentStateDuration;
    private double preGameDeadline;
    private int lastCountdownBroadcast = -1;

    public event Action<State> StateChanged;
    public event Action<int> PreGameCountdownTick;
    public event Action<int> CrownWinnerDecided;

    public State CurrentState => currentState;
    public double PreGameCountdownSeconds => preGameCountdownSeconds;

    private void OnEnable()
    {
        if (PhotonNetwork.NetworkingClient != null)
            PhotonNetwork.NetworkingClient.EventReceived += OnPhotonEvent;
    }

    private void OnDisable()
    {
        if (PhotonNetwork.NetworkingClient != null)
            PhotonNetwork.NetworkingClient.EventReceived -= OnPhotonEvent;
    }

    public void Initialize(IMatchClock sharedClock)
    {
        if (initialized) return;

        clock = sharedClock ?? throw new ArgumentNullException(nameof(sharedClock));

        if (PhotonNetwork.CurrentRoom == null)
        {
            Debug.LogError("[GSM] No hay sala activa.");
            return;
        }

        initialized = true;

        if (PhotonNetwork.IsMasterClient)
        {
            EnsureInitialState();
            hasStateSnapshot = true;
        }
        else
        {
            hasStateSnapshot = TryReadStateFromRoom();
        }
    }

    private void Update()
    {
        if (!initialized || clock == null) return;

        if (!hasStateSnapshot)
        {
            hasStateSnapshot = TryReadStateFromRoom();
            if (!hasStateSnapshot) return;
        }

        // LÓGICA DE ACTUALIZACIÓN SEGÚN EL ESTADO
        if (currentState == State.PreGameCountdown)
        {
            UpdatePreGameCountdown();
        }
        else if (currentState == State.InGame) // <--- NUEVO: Controlamos el tiempo de juego
        {
            UpdateInGameTimer();
        }
    }

    // --- LÓGICA DE TIEMPO DE JUEGO ---
    private void UpdateInGameTimer()
    {
        // Calculamos cuándo termina la partida
        double endTime = stateStartTime + matchDurationSeconds;
        double remaining = endTime - clock.Now;

        // Si el tiempo se acabó y soy el Master, termino el juego
        if (PhotonNetwork.IsMasterClient && remaining <= 0.0)
        {
            Debug.Log("[GSM] Tiempo agotado. Finalizando partida.");
            ChangeState(State.GameEnded);
        }
    }

    // --- CAMBIO DE ESTADO ---
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

        // Guardamos la duración correcta según el estado
        if (nextState == State.PreGameCountdown)
            props[RoomStateDurationKey] = preGameCountdownSeconds;
        else if (nextState == State.InGame)
            props[RoomStateDurationKey] = matchDurationSeconds; // Guardamos duración del partido

        room.SetCustomProperties(props);

        double duration = 0.0;
        if (nextState == State.PreGameCountdown) duration = preGameCountdownSeconds;
        if (nextState == State.InGame) duration = matchDurationSeconds;

        ApplyState(nextState, now, duration);
    }

    private void ApplyState(State newState, double startTime, double duration)
    {
        currentState = newState;
        stateStartTime = startTime;
        currentStateDuration = duration;
        lastCountdownBroadcast = -1;

        if (crownHintText != null)
            crownHintText.gameObject.SetActive(newState == State.PreGameCountdown);

        if (newState == State.PreGameCountdown)
        {
            lastPressByActor.Clear();
            preGameDeadline = stateStartTime + duration;
        }

        hasStateSnapshot = true;
        StateChanged?.Invoke(currentState);

        if (PhotonNetwork.IsMasterClient && currentState == State.CrownAssignment)
        {
            ResolveCrownAssignment();
        }

        // --- AQUÍ SOLUCIONAMOS EL PROBLEMA DE LOS PUNTOS ---
        if (currentState == State.GameEnded)
        {
            Debug.Log("[GSM] Fin del juego detectado. Guardando puntaje...");

            // 1. Buscamos el ScoreManager y guardamos el puntaje REAL en PlayerPrefs
            var scoreManager = FindObjectOfType<ScoreManager>();
            if (scoreManager != null)
            {
                // ESTA LLAMADA EVITA QUE APAREZCA EL "100"
                scoreManager.GuardarPuntajeEnMemoria();
            }

            // 2. Cambiamos de escena
            // Como no tocamos el GameManager, usamos LoadLevel para mover a todos a resultados
            if (PhotonNetwork.IsMasterClient)
            {
                // Asegúrate que tu escena se llame EXACTAMENTE "ResultScene"
                PhotonNetwork.LoadLevel("ResultScene");
            }
        }
    }

    // --- RESTO DE TU CÓDIGO ORIGINAL (SIN CAMBIOS) ---

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

    public void ReportCrownAttempt(int actorNumber)
    {
        if (!initialized || currentState != State.PreGameCountdown) return;
        double pressTime = clock.Now;
        if (PhotonNetwork.IsMasterClient) RegisterCrownAttempt(actorNumber, pressTime);
        else SendCrownAttemptEvent(actorNumber, pressTime);
    }

    private void SendCrownAttemptEvent(int actorNumber, double pressTime)
    {
        var payload = new object[] { actorNumber, pressTime };
        var options = new RaiseEventOptions { Receivers = ReceiverGroup.MasterClient };
        var sendOptions = new SendOptions { Reliability = true };
        PhotonNetwork.RaiseEvent(CrownAttemptEventCode, payload, options, sendOptions);
    }

    private void OnPhotonEvent(EventData photonEvent)
    {
        if (!PhotonNetwork.IsMasterClient || photonEvent.Code != CrownAttemptEventCode) return;
        if (photonEvent.CustomData is not object[] payload || payload.Length != 2) return;
        RegisterCrownAttempt((int)payload[0], (double)payload[1]);
    }

    private void RegisterCrownAttempt(int actorNumber, double pressTime)
    {
        if (pressTime > preGameDeadline + CrownAttemptToleranceSeconds) return;
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

    private void ResolveCrownAssignment()
    {
        int winningActor = DetermineCrownWinner();
        if (winningActor != -1)
        {
            var props = new Hashtable { { CrownOwnerPropertyKey, winningActor } };
            PhotonNetwork.CurrentRoom?.SetCustomProperties(props);
        }
        photonView.RPC(nameof(RPC_AnnounceCrownWinner), RpcTarget.All, winningActor);
        ChangeState(State.InGame);
    }

    private int DetermineCrownWinner()
    {
        if (lastPressByActor.Count == 0) return GetLowestActorNumberInRoom();
        int winner = -1;
        double latestTime = double.MinValue;
        foreach (var kvp in lastPressByActor)
        {
            if (kvp.Value > preGameDeadline + CrownAttemptToleranceSeconds) continue;
            if (kvp.Value > latestTime || (kvp.Value == latestTime && (winner == -1 || kvp.Key < winner)))
            {
                latestTime = kvp.Value;
                winner = kvp.Key;
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
            if (player != null && player.ActorNumber < lowest) lowest = player.ActorNumber;
        return lowest == int.MaxValue ? -1 : lowest;
    }

    public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
    {
        if (propertiesThatChanged == null) return;
        if (propertiesThatChanged.ContainsKey(RoomStateKey) || propertiesThatChanged.ContainsKey(RoomStateStartKey))
            hasStateSnapshot = TryReadStateFromRoom() || hasStateSnapshot;
    }

    private bool TryReadStateFromRoom()
    {
        var room = PhotonNetwork.CurrentRoom;
        if (room == null) return false;
        var props = room.CustomProperties;
        if (!props.TryGetValue(RoomStateKey, out object stateValue) || !props.TryGetValue(RoomStateStartKey, out object startValue)) return false;

        State state = (State)(int)stateValue;
        double start = (double)startValue;
        double duration = 0.0;

        if (state == State.PreGameCountdown || state == State.InGame)
        {
            if (props.TryGetValue(RoomStateDurationKey, out object val)) duration = (double)val;
        }

        ApplyState(state, start, duration);
        return true;
    }

    public override void OnPlayerLeftRoom(Photon.Realtime.Player otherPlayer)
    {
        if (!PhotonNetwork.IsMasterClient || otherPlayer == null) return;
        var room = PhotonNetwork.CurrentRoom;
        if (room == null) return;

        int currentCrownOwner = -1;
        if (room.CustomProperties != null && room.CustomProperties.TryGetValue("CrownOwner", out object v) && v is int owner)
            currentCrownOwner = owner;

        if (currentCrownOwner == otherPlayer.ActorNumber)
        {
            int newOwner = PickNewCrownOwnerExcluding(otherPlayer.ActorNumber);
            var props = new Hashtable { { "CrownOwner", newOwner } };
            room.SetCustomProperties(props);
            photonView.RPC(nameof(RPC_AnnounceCrownWinner), RpcTarget.All, newOwner);
        }
    }

    private int PickNewCrownOwnerExcluding(int excludedActorNumber)
    {
        var players = PhotonNetwork.PlayerList;
        if (players == null || players.Length == 0) return -1;
        int candidate = -1;
        foreach (var p in players)
        {
            if (p == null || p.ActorNumber == excludedActorNumber) continue;
            if (candidate == -1 || p.ActorNumber < candidate) candidate = p.ActorNumber;
        }
        return candidate;
    }

    [PunRPC]
    private void RPC_AnnounceCrownWinner(int winnerActorNumber)
    {
        CrownWinnerDecided?.Invoke(winnerActorNumber);
    }
}