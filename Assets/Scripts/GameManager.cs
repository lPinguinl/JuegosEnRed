using System.Collections.Generic;
using System.Linq;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;

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

    [Header("Scoring")]
    [SerializeField] private ScoreManager scoreManager;

    private IMatchClock matchClock;  // (usa PhotonNetwork.Time)

    // Guardas anti-duplicados de spawn
    private bool hasSpawnedLocalPlayer;
    private GameObject localPlayerInstance;

    private void Awake()
    {
        // Reloj global de Photon para que todos cuenten el mismo tiempo
        matchClock = new PhotonMatchClock();
    }

    private void Start()
    {
        if (!ValidateDependencies())
        {
            enabled = false;
            return;
        }

        // Inicia timer sincronizado por Photon
        gameTimer.Initialize(matchClock, timerPresenter, this, matchDurationSeconds);

        // Spawn del jugador local (robusto)
        TrySpawnLocalPlayer();

        // Solo el Master inicializa la corona si aún no existe (determinista)
        if (PhotonNetwork.IsMasterClient)
            EnsureCrownOwnerExists();
    }

    private bool ValidateDependencies()
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogError("Error: Los puntos de spawn no están asignados en el GameManager.");
            return false;
        }
        if (gameTimer == null)
        {
            Debug.LogError("Error: GameTimer no asignado en GameManager.");
            return false;
        }
        if (timerPresenter == null)
        {
            Debug.LogError("Error: TimerTextPresenter no asignado en GameManager.");
            return false;
        }
        if (scoreManager == null)
        {
            Debug.LogError("Error: ScoreManager no asignado en GameManager.");
            return false;
        }
        if (string.IsNullOrWhiteSpace(playerPrefabName))
        {
            Debug.LogError("Error: playerPrefabName no está configurado en el GameManager.");
            return false;
        }
        if (!PhotonNetwork.IsConnectedAndReady || !PhotonNetwork.InRoom)
        {
            Debug.LogError("Error: PhotonNetwork no está listo o no se ha unido a una sala.");
            return false;
        }
        return true;
    }

    private void TrySpawnLocalPlayer()
    {
        if (hasSpawnedLocalPlayer)
        {
            Debug.LogWarning("[GameManager] El jugador local ya fue spawneado previamente.");
            return;
        }

        var localPlayer = PhotonNetwork.LocalPlayer;
        if (localPlayer == null)
        {
            Debug.LogError("[GameManager] PhotonNetwork.LocalPlayer es null. No se puede spawnear.");
            return;
        }

        // Si ya existe una instancia asociada (por recarga/auto-sync), reutilizar
        if (localPlayer.TagObject is GameObject existingInstance && existingInstance != null)
        {
            localPlayerInstance = existingInstance;
            hasSpawnedLocalPlayer = true;
            Debug.LogWarning("[GameManager] Reutilizando instancia existente del jugador local.");
            return;
        }

        var spawnPoint = SelectSpawnPointFor(localPlayer);
        if (spawnPoint == null)
        {
            Debug.LogError("[GameManager] No se encontró un punto de spawn válido para el jugador local.");
            return;
        }

        localPlayerInstance = PhotonNetwork.Instantiate(playerPrefabName, spawnPoint.position, spawnPoint.rotation);
        localPlayer.TagObject = localPlayerInstance;
        hasSpawnedLocalPlayer = true;

        Debug.Log($"[GameManager] Jugador local instanciado en spawn \"{spawnPoint.name}\".");
    }

    private Transform SelectSpawnPointFor(Player player)
    {
        if (player == null || spawnPoints == null || spawnPoints.Length == 0)
            return null;

        // Orden determinista por ActorNumber (mismo orden para todos)
        var orderedPlayers = PhotonNetwork.PlayerList.OrderBy(p => p.ActorNumber).ToArray();

        int playerIndex = System.Array.FindIndex(orderedPlayers, p => p.ActorNumber == player.ActorNumber);
        if (playerIndex < 0)
        {
            playerIndex = 0;
            Debug.LogWarning("[GameManager] No se pudo determinar el índice del jugador; usando fallback 0.");
        }

        int spawnIndex = playerIndex % spawnPoints.Length;
        Transform selectedSpawn = spawnPoints[spawnIndex];

        if (selectedSpawn == null)
        {
            Debug.LogWarning($"[GameManager] Spawn point en índice {spawnIndex} es null. Buscando primer spawn válido.");
            selectedSpawn = spawnPoints.FirstOrDefault(sp => sp != null);
        }

        return selectedSpawn;
    }

    private void EnsureCrownOwnerExists()
    {
        var room = PhotonNetwork.CurrentRoom;
        if (room == null) return;

        if (!room.CustomProperties.ContainsKey(CrownOwnerPropertyKey))
        {
            var players = PhotonNetwork.PlayerList;
            if (players != null && players.Length > 0)
            {
                // Determinista: el de menor ActorNumber arranca con la corona
                int crownOwnerActorNumber = players.OrderBy(p => p.ActorNumber).First().ActorNumber;

                var props = new Hashtable
                {
                    { CrownOwnerPropertyKey, crownOwnerActorNumber }
                };
                room.SetCustomProperties(props);
            }
        }
    }

    // Callback del GameTimer al finalizar
    public void OnMatchTimeEnded()
    {
        Debug.Log($"[GameManager] OnMatchTimeEnded called. Master={PhotonNetwork.IsMasterClient}, scene={resultScene}");

        if (!PhotonNetwork.IsMasterClient)
        {
            // Si no somos el master, solo esperamos a que él haga la transición.
            return;
        }

        Player winner = null;
        Dictionary<int, int> scoreSnapshot = null;

        if (scoreManager != null)
        {
            bool hasWinner = scoreManager.TryDetermineWinner(out winner, out scoreSnapshot);
            if (hasWinner && winner != null)
            {
                var roomProps = new Hashtable
                {
                    { MatchWinnerKey, winner.ActorNumber }
                };

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

    // Si quisieras cargar spawn points por tag, mantené esto privado (no se usa en este flujo)
    private void GetSpawnPoints()
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            spawnPoints = GameObject.FindGameObjectsWithTag("SpawnPoint")
                .Select(go => go.transform)
                .ToArray();
        }
    }

    public static int GetCrownOwnerActorNumber()
    {
        if (PhotonNetwork.CurrentRoom != null && PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(CrownOwnerPropertyKey))
            return (int)PhotonNetwork.CurrentRoom.CustomProperties[CrownOwnerPropertyKey];
        return -1;
    }
}