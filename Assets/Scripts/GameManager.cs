using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using System.Collections.Generic;
using System.Linq;

public enum GameState
{
    Waiting,
    Starting,
    Playing,
    Finished
}

public class GameManager : MonoBehaviourPunCallbacks
{
    public static GameManager Instance;

    [Header("Prefabs y Spawn")]
    [SerializeField] private string playerPrefabName = "PlayerPrefab";
    [SerializeField] private Transform[] spawnPoints;

    [Header("Game Settings")]
    public float matchDuration = 120f; // Duración de la partida en segundos

    [Header("UI")]
    public TMPro.TextMeshProUGUI timerText;
    public GameObject resultsPanel;
    public TMPro.TextMeshProUGUI resultsText;
    public UnityEngine.UI.Button returnButton;

    private double matchStartTime;
    private GameState currentState = GameState.Waiting;
    private Dictionary<int, float> playerScores = new Dictionary<int, float>(); // playerID -> score

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    private void Start()
    {
        // --- SPAWN ---
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            // Busca puntos de spawn automáticamente si no están asignados
            spawnPoints = GameObject.FindGameObjectsWithTag("SpawnPoint")
                .Select(go => go.transform)
                .ToArray();
        }

        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogError("Error: Los puntos de spawn no están asignados en el GameManager.");
            return;
        }

        int randomSpawnIndex = Random.Range(0, spawnPoints.Length);
        Transform spawnPoint = spawnPoints[randomSpawnIndex];

        if (PhotonNetwork.IsConnectedAndReady)
        {
            PhotonNetwork.Instantiate(playerPrefabName, spawnPoint.position, spawnPoint.rotation);
        }

        // --- GAMELOOP ---
        if (PhotonNetwork.IsMasterClient)
        {
            Invoke(nameof(StartMatch), 3f); // Demo: inicia tras 3 segundos
        }
    }

    void Update()
    {
        if (currentState == GameState.Playing)
        {
            float timeLeft = GetTimeLeft();
            UpdateTimerUI(timeLeft);

            if (timeLeft <= 0f)
            {
                EndMatch();
            }
        }
    }

    void UpdateTimerUI(float timeLeft)
    {
        if (timerText != null)
        {
            int minutes = Mathf.FloorToInt(timeLeft / 60f);
            int seconds = Mathf.FloorToInt(timeLeft % 60f);
            timerText.text = $"{minutes:00}:{seconds:00}";
        }
    }

    float GetTimeLeft()
    {
        double elapsed = PhotonNetwork.Time - matchStartTime;
        return Mathf.Max(0f, matchDuration - (float)elapsed);
    }

    [PunRPC]
    void RPC_StartMatch(double startTime)
    {
        matchStartTime = startTime;
        currentState = GameState.Playing;

        // Inicializar scores
        playerScores.Clear();
        foreach (Player p in PhotonNetwork.PlayerList)
        {
            playerScores[p.ActorNumber] = 0f;
        }

        if (GetGameState() == GameState.Playing)
        {
            CrownManager.Instance.AssignCrownAtGameStart();
        } else 
        {
            Debug.Log("crown not assigned");
        }
    }

    void StartMatch()
    {
        double startTime = PhotonNetwork.Time;
        photonView.RPC(nameof(RPC_StartMatch), RpcTarget.All, startTime);
    }

    void EndMatch()
    {
        if (currentState != GameState.Playing)
            return;

        currentState = GameState.Finished;

        // Mostrar resultados
        ShowResults();
    }

    void ShowResults()
    {
        if (resultsPanel != null && resultsText != null)
        {
            resultsPanel.SetActive(true);

            // Ordenar scores
            List<KeyValuePair<int, float>> sortedScores = new List<KeyValuePair<int, float>>(playerScores);
            sortedScores.Sort((a, b) => b.Value.CompareTo(a.Value));

            string podium = "<b>Podio:</b>\n";
            int place = 1;
            foreach (var entry in sortedScores)
            {
                Player p = PhotonNetwork.CurrentRoom.GetPlayer(entry.Key);
                string playerName = p != null ? p.NickName : $"Player {entry.Key}";
                string medal = place == 1 ? "🥇" : place == 2 ? "🥈" : place == 3 ? "🥉" : "";
                podium += $"{medal} {place}. {playerName} - {entry.Value:0.0} pts\n";
                place++;
            }
            resultsText.text = podium;

            // Botón para volver al lobby
            if (returnButton != null)
            {
                returnButton.onClick.RemoveAllListeners();
                returnButton.onClick.AddListener(ReturnToLobby);
                returnButton.gameObject.SetActive(true);
            }
        }
    }

    // Llamar esto cuando un jugador gane puntos (ej: por tener la corona)
    public void AddScore(int playerID, float amount)
    {
        if (!playerScores.ContainsKey(playerID))
            playerScores[playerID] = 0f;
        playerScores[playerID] += amount;
    }

    public void ReturnToLobby()
    {
        PhotonNetwork.LeaveRoom();
        // Cambia esto por el nombre real de tu escena de lobby
        UnityEngine.SceneManagement.SceneManager.LoadScene("NombreDeTuEscenaLobby");
    }

    // Acceso a estado de la partida
    public bool IsPlaying() => currentState == GameState.Playing;
    public float GetMatchTimeLeft() => GetTimeLeft();
    public GameState GetGameState() => currentState;
    public Dictionary<int, float> GetScores() => playerScores;
}