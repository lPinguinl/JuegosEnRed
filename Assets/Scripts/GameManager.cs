using System.Collections.Generic;
using System.Linq;
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

    private void Awake()
    {
        // Reloj global de Photon para que todos cuenten el mismo tiempo
        matchClock = new PhotonMatchClock();
    }

    private void Start()
    {
        // Validaciones
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogError("Error: Los puntos de spawn no están asignados en el GameManager.");
            return;
        }
        if (gameTimer == null)
        {
            Debug.LogError("Error: GameTimer no asignado en GameManager.");
            return;
        }
        if (timerPresenter == null)
        {
            Debug.LogError("Error: TimerTextPresenter no asignado en GameManager.");
            return;
        } 
        if (scoreManager == null)
        {
            Debug.LogError("Error: ScoreManager no asignado en GameManager.");
            return;
        }

        
       
       // calculan el tiempo restante con PhotonNetwork.Time.
        // - Cuando llega a 0, GameTimer llama OnMatchTimeEnded() en este GameManager.
        
        gameTimer.Initialize(matchClock, timerPresenter, this, matchDurationSeconds);

        // Spawn del jugador local
        int randomSpawnIndex = Random.Range(0, spawnPoints.Length);
        Transform spawnPoint = spawnPoints[randomSpawnIndex];

        if (PhotonNetwork.IsConnectedAndReady)
        {
            PhotonNetwork.Instantiate(playerPrefabName, spawnPoint.position, spawnPoint.rotation);
        }
        
        // Solo el MasterClient inicializa la corona
        if (PhotonNetwork.IsMasterClient)
        {
            // Elegir un jugador aleatorio para empezar con la corona
            var players = PhotonNetwork.PlayerList;
            int randomIdx = Random.Range(0, players.Length);
            int crownOwnerActorNumber = players[randomIdx].ActorNumber;

            ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable
            {
                { "CrownOwner", crownOwnerActorNumber }
            };
            PhotonNetwork.CurrentRoom.SetCustomProperties(props);
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
                var roomProps = new ExitGames.Client.Photon.Hashtable
                {
                    { "MatchWinner", winner.ActorNumber }
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

                    roomProps["MatchScoresActorNumbers"] = actorNumbers;
                    roomProps["MatchScoresValues"] = scores;
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
        if (PhotonNetwork.CurrentRoom != null && PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey("CrownOwner"))
            return (int)PhotonNetwork.CurrentRoom.CustomProperties["CrownOwner"];
        return -1;
    }
}