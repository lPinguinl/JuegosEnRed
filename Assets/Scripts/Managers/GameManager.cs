using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;
using Hashtable = System.Collections.Hashtable;

/// <summary>
/// Coordina el flujo general del gameplay: spawns, estado de juego y transición a la escena de resultados.
/// La lógica de estados previos al inicio se delega a GameStateManager.
/// </summary>
public class GameManager : MonoBehaviour, IGameEndHandler
{
    private const string CrownOwnerPropertyKey = "CrownOwner";
    private const string MatchWinnerKey = "MatchWinner";
    private const string MatchScoresActorNumbersKey = "MatchScoresActorNumbers";
    private const string MatchScoresValuesKey = "MatchScoresValues";

    [Header("Spawn")]
    [SerializeField] private string playerPrefabName = "PlayerPrefab";
    [SerializeField] private Transform[] spawnPoints;

    [Header("Match Timer")]
    [SerializeField] private GameTimer gameTimer;
    [SerializeField] private TimerTextPresenter timerPresenter;
    [SerializeField] private double matchDurationSeconds = 60.0;
    [SerializeField] private string resultScene = "ResultScene";

    [Header("State Flow")]
    [SerializeField] private GameStateManager stateManager;
    [SerializeField] private TMP_Text preGameCountdownText;
    [SerializeField] private TMP_Text crownAnnouncementText;
    [SerializeField] private float crownAnnouncementDuration = 2.5f;

    [Header("Scoring")]
    [SerializeField] private ScoreManager scoreManager;

    private IMatchClock matchClock; // Fuente de tiempo común (PhotonNetwork.Time)
    private bool hasSpawnedLocalPlayer;
    private GameObject localPlayerInstance;
    private bool matchTimerInitialized;
    private bool stateEventsSubscribed;
    private Coroutine crownAnnouncementRoutine;

    private void Awake()
    {
        // Se usa el reloj global de Photon para cualquier cálculo sincronizado.
        matchClock = new PhotonMatchClock();
    }

    private void Start()
    {
        if (!ValidateDependencies())
        {
            enabled = false;
            return;
        }

        TrySpawnLocalPlayer();
        PrepareUiForPreGame();

        if (PhotonNetwork.IsMasterClient)
        {
            ResetCrownOwnerProperty();
        }

        SubscribeStateEvents();
        stateManager.Initialize(matchClock);
    }

    private void PrepareUiForPreGame()
    {
        if (preGameCountdownText != null)
        {
            preGameCountdownText.gameObject.SetActive(false);
        }

        if (crownAnnouncementText != null)
        {
            crownAnnouncementText.gameObject.SetActive(false);
        }
    }

    private void SubscribeStateEvents()
    {
        if (stateManager == null || stateEventsSubscribed)
        {
            return;
        }

        stateManager.StateChanged += HandleStateChanged;
        stateManager.PreGameCountdownTick += HandlePreGameCountdownTick;
        stateManager.CrownWinnerDecided += HandleCrownWinnerDecided;
        stateEventsSubscribed = true;
    }

    private void UnsubscribeStateEvents()
    {
        if (stateManager == null || !stateEventsSubscribed)
        {
            return;
        }

        stateManager.StateChanged -= HandleStateChanged;
        stateManager.PreGameCountdownTick -= HandlePreGameCountdownTick;
        stateManager.CrownWinnerDecided -= HandleCrownWinnerDecided;
        stateEventsSubscribed = false;
    }

    private bool ValidateDependencies()
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogError("[GameManager] Debes asignar puntos de spawn.");
            return false;
        }
        if (gameTimer == null)
        {
            Debug.LogError("[GameManager] GameTimer no asignado.");
            return false;
        }
        if (timerPresenter == null)
        {
            Debug.LogError("[GameManager] TimerTextPresenter no asignado.");
            return false;
        }
        if (stateManager == null)
        {
            Debug.LogError("[GameManager] GameStateManager no asignado.");
            return false;
        }
        if (preGameCountdownText == null)
        {
            Debug.LogError("[GameManager] preGameCountdownText no asignado.");
            return false;
        }
        if (crownAnnouncementText == null)
        {
            Debug.LogError("[GameManager] crownAnnouncementText no asignado.");
            return false;
        }
        if (scoreManager == null)
        {
            Debug.LogError("[GameManager] ScoreManager no asignado.");
            return false;
        }
        if (string.IsNullOrWhiteSpace(playerPrefabName))
        {
            Debug.LogError("[GameManager] playerPrefabName no configurado.");
            return false;
        }
        if (!PhotonNetwork.IsConnectedAndReady || !PhotonNetwork.InRoom)
        {
            Debug.LogError("[GameManager] Photon no está conectado o aún no entró a la sala.");
            return false;
        }
        return true;
    }

    private void TrySpawnLocalPlayer()
    {
        if (hasSpawnedLocalPlayer)
        {
            Debug.LogWarning("[GameManager] El jugador local ya fue instanciado previamente.");
            return;
        }

        var localPlayer = PhotonNetwork.LocalPlayer;
        if (localPlayer == null)
        {
            Debug.LogError("[GameManager] PhotonNetwork.LocalPlayer es null.");
            return;
        }

        if (localPlayer.TagObject is GameObject existingInstance && existingInstance != null)
        {
            localPlayerInstance = existingInstance;
            hasSpawnedLocalPlayer = true;
            Debug.LogWarning("[GameManager] Reutilizando instancia de jugador existente.");
            return;
        }

        var spawnPoint = SelectSpawnPointFor(localPlayer);
        if (spawnPoint == null)
        {
            Debug.LogError("[GameManager] No se encontró un punto de spawn válido.");
            return;
        }

        localPlayerInstance = PhotonNetwork.Instantiate(playerPrefabName, spawnPoint.position, spawnPoint.rotation);
        localPlayer.TagObject = localPlayerInstance;
        hasSpawnedLocalPlayer = true;

        Debug.Log($"[GameManager] Jugador local instanciado en \"{spawnPoint.name}\".");
    }

    private Transform SelectSpawnPointFor(Player player)
    {
        if (player == null || spawnPoints == null || spawnPoints.Length == 0)
        {
            return null;
        }

        var orderedPlayers = PhotonNetwork.PlayerList.OrderBy(p => p.ActorNumber).ToArray();
        int playerIndex = System.Array.FindIndex(orderedPlayers, p => p.ActorNumber == player.ActorNumber);
        if (playerIndex < 0)
        {
            playerIndex = 0;
            Debug.LogWarning("[GameManager] ActorNumber no encontrado; usando índice 0.");
        }

        int spawnIndex = playerIndex % spawnPoints.Length;
        Transform selectedSpawn = spawnPoints[spawnIndex];

        if (selectedSpawn == null)
        {
            Debug.LogWarning($"[GameManager] Spawn en índice {spawnIndex} es null, buscando fallback.");
            selectedSpawn = spawnPoints.FirstOrDefault(sp => sp != null);
        }

        return selectedSpawn;
    }

    private void ResetCrownOwnerProperty()
    {
        var room = PhotonNetwork.CurrentRoom;
        if (room == null)
        {
            return;
        }

        // Dejamos la propiedad con valor -1 para indicar que nadie posee la corona todavía.
        var props = new ExitGames.Client.Photon.Hashtable { { CrownOwnerPropertyKey, -1 } };
        room.SetCustomProperties(props);
    }

    private void HandleStateChanged(GameStateManager.State newState)
    {
        switch (newState)
        {
            case GameStateManager.State.PreGameCountdown:
                ShowCountdownText(Mathf.CeilToInt((float)stateManager.PreGameCountdownSeconds).ToString());
                HideCrownAnnouncement();
                break;

            case GameStateManager.State.CrownAssignment:
                ShowCountdownText("Resolviendo...");
                break;

            case GameStateManager.State.InGame:
                HideCountdownText();
                StartMatchTimer();
                break;

            case GameStateManager.State.GameEnded:
                HideCountdownText();
                break;
        }
    }

    private void HandlePreGameCountdownTick(int secondsRemaining)
    {
        ShowCountdownText(Mathf.Max(0, secondsRemaining).ToString());
    }

    private void HandleCrownWinnerDecided(int actorNumber)
    {
        string playerName = "<nadie>";
        if (actorNumber >= 0)
        {
            Player player = PhotonNetwork.CurrentRoom?.GetPlayer(actorNumber);
            playerName = player != null && !string.IsNullOrWhiteSpace(player.NickName)
                ? player.NickName
                : $"Jugador {actorNumber}";
        }

        ShowCrownAnnouncement($"¡{playerName} se quedó con la corona!");
    }

    private void ShowCountdownText(string message)
    {
        if (preGameCountdownText == null)
        {
            return;
        }

        preGameCountdownText.gameObject.SetActive(true);
        preGameCountdownText.text = message;
    }

    private void HideCountdownText()
    {
        if (preGameCountdownText != null)
        {
            preGameCountdownText.gameObject.SetActive(false);
        }
    }

    private void ShowCrownAnnouncement(string message)
    {
        if (crownAnnouncementText == null)
        {
            return;
        }

        if (crownAnnouncementRoutine != null)
        {
            StopCoroutine(crownAnnouncementRoutine);
        }

        crownAnnouncementRoutine = StartCoroutine(CrownAnnouncementRoutine(message));
    }

    private void HideCrownAnnouncement()
    {
        if (crownAnnouncementRoutine != null)
        {
            StopCoroutine(crownAnnouncementRoutine);
            crownAnnouncementRoutine = null;
        }

        if (crownAnnouncementText != null)
        {
            crownAnnouncementText.gameObject.SetActive(false);
        }
    }

    private IEnumerator CrownAnnouncementRoutine(string message)
    {
        crownAnnouncementText.gameObject.SetActive(true);
        crownAnnouncementText.text = message;

        yield return new WaitForSeconds(crownAnnouncementDuration);

        crownAnnouncementText.gameObject.SetActive(false);
        crownAnnouncementRoutine = null;
    }

    private void StartMatchTimer()
    {
        if (matchTimerInitialized)
        {
            return;
        }

        // El Master fijará ROOM_KEY_START/ROOM_KEY_DURATION si aún no existen.
        gameTimer.Initialize(matchClock, timerPresenter, this, matchDurationSeconds);
        matchTimerInitialized = true;
    }

    private void OnDisable()
    {
        UnsubscribeStateEvents();
    }

    private void OnDestroy()
    {
        UnsubscribeStateEvents();
    }

    /// <summary>
    /// Llamado desde GameTimer cuando el tiempo oficial llega a cero.
    /// Solo el Master realiza el LoadLevel; los demás esperan la sincronización automática.
    /// </summary>
    public void OnMatchTimeEnded()
    {
        Debug.Log($"[GameManager] OnMatchTimeEnded. Master={PhotonNetwork.IsMasterClient}, resultScene={resultScene}");

        if (!PhotonNetwork.IsMasterClient)
        {
            return;
        }

        Player winner = null;
        Dictionary<int, int> scoreSnapshot = null;

        if (scoreManager != null)
        {
            bool hasWinner = scoreManager.TryDetermineWinner(out winner, out scoreSnapshot);
            if (hasWinner && winner != null)
            {
                var roomProps = new ExitGames.Client.Photon.Hashtable { { MatchWinnerKey, winner.ActorNumber } };

                if (scoreSnapshot != null && scoreSnapshot.Count > 0)
                {
                    int[] actorNumbers = new int[scoreSnapshot.Count];
                    int[] scores = new int[scoreSnapshot.Count];
                    int index = 0;
                    foreach (var kvp in scoreSnapshot)
                    {
                        actorNumbers[index] = kvp.Key;
                        scores[index] = kvp.Value;
                        index++;
                    }

                    roomProps[MatchScoresActorNumbersKey] = actorNumbers;
                    roomProps[MatchScoresValuesKey] = scores;
                }

                PhotonNetwork.CurrentRoom.SetCustomProperties(roomProps);
            }
            else
            {
                Debug.LogWarning("[GameManager] No se pudo determinar un ganador antes de cambiar de escena.");
            }
        }

        PhotonNetwork.LoadLevel(resultScene);
    }

    public static int GetCrownOwnerActorNumber()
    {
        if (PhotonNetwork.CurrentRoom == null)
        {
            return -1;
        }

        if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(CrownOwnerPropertyKey, out object value) &&
            value is int actorNumber)
        {
            return actorNumber;
        }

        return -1;
    }
}