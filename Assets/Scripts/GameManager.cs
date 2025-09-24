using UnityEngine;
using Photon.Pun;
using System.Linq;
using TMPro;

public class GameManager : MonoBehaviour, IGameEndHandler
{
    [Header("Spawn")]
    [SerializeField] private string playerPrefabName = "PlayerPrefab";
    [SerializeField] private Transform[] spawnPoints;

    [Header("Match Timer")]
    [SerializeField] private GameTimer gameTimer;                 
    [SerializeField] private TimerTextPresenter timerPresenter;   
    [SerializeField] private double matchDurationSeconds = 60.0;  
    [SerializeField] private string resultScene = "ResultScene";

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
    }

    // Callback del GameTimer al finalizar
    public void OnMatchTimeEnded()
    {
        Debug.Log($"[GameManager] OnMatchTimeEnded called. Master={PhotonNetwork.IsMasterClient}, scene={resultScene}");
        if (PhotonNetwork.IsMasterClient)
        {
            // se carga la escena con el automaticallysyncscene !!! (importante sino no se me mostraba a todos)
            PhotonNetwork.LoadLevel(resultScene);
        }
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
}