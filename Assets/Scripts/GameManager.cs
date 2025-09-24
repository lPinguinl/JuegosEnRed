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
    [SerializeField] private GameTimer gameTimer;                 // Asignar en Inspector (GameObject con GameTimer)
    [SerializeField] private TimerTextPresenter timerPresenter;   // Asignar en Inspector (GameObject con TimerTextPresenter)
    [SerializeField] private double matchDurationSeconds = 60.0;  // Configurable
    [SerializeField] private string resultSceneName = "Result Scene";

    private IMatchClock matchClock;

    private void Awake()
    {
        // Inyectamos el reloj sincronizado con Photon
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

        // Inicializar timer sincronizado (sin FindObject)
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
        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.LoadLevel(resultSceneName);
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