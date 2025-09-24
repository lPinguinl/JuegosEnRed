using System.Collections.Generic;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

[DisallowMultipleComponent]
public class ScoreManager : MonoBehaviourPunCallbacks
{
    [Header("Dependencies")]
    [SerializeField] private GameManager gameManager; // Asignar por Inspector

    [Header("Settings")]
    [SerializeField] private float pointsPerSecond = 1f;

    private const string CrownScoreKey = "CrownScore";
    private const string CrownOwnerKey = "CrownOwner";

    private float crownHoldingAccumulator;
    private bool isActiveMaster;

    private sealed class PlayerScoreTracker
    {
        public int HighestScore;
        public double FirstReachTime;
    }

    private readonly Dictionary<int, PlayerScoreTracker> scoreTrackers = new();

    private void Awake()
    {
        if (gameManager == null)
        {
            Debug.LogError("[ScoreManager] GameManager reference is missing.");
        }
    }

    private void Start()
    {
        TryInitialize();
    }

    private void Update()
    {
        if (!isActiveMaster) return;
        if (PhotonNetwork.CurrentRoom == null) return;

        int crownOwnerActor = GetCurrentCrownOwner();
        if (crownOwnerActor == -1) return;

        crownHoldingAccumulator += Time.deltaTime;
        while (crownHoldingAccumulator >= 1f)
        {
            crownHoldingAccumulator -= 1f;
            AwardPoint(crownOwnerActor);
        }
    }

    private void TryInitialize()
    {
        isActiveMaster = PhotonNetwork.IsMasterClient;
        if (!isActiveMaster) return;

        InitializeCrownScores();
    }

    private void InitializeCrownScores()
    {
        foreach (Player player in PhotonNetwork.PlayerList)
        {
            EnsureScoreInitialized(player);
        }
    }

    private void EnsureScoreInitialized(Player player)
    {
        if (player == null) return;

        int currentScore = 0;
        if (player.CustomProperties.TryGetValue(CrownScoreKey, out object existingScore))
        {
            currentScore = (int)existingScore;
        }
        else
        {
            var props = new Hashtable { { CrownScoreKey, 0 } };
            player.SetCustomProperties(props);
        }

        if (!scoreTrackers.ContainsKey(player.ActorNumber))
        {
            scoreTrackers[player.ActorNumber] = new PlayerScoreTracker
            {
                HighestScore = currentScore,
                FirstReachTime = PhotonNetwork.Time
            };
        }
    }

    private int GetCurrentCrownOwner()
    {
        if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(CrownOwnerKey, out object value))
        {
            return (int)value;
        }
        return -1;
    }

    private void AwardPoint(int actorNumber)
    {
        Player player = GetPlayerByActorNumber(actorNumber);
        if (player == null) return;

        int currentScore = 0;
        if (player.CustomProperties.TryGetValue(CrownScoreKey, out object scoreObj))
        {
            currentScore = (int)scoreObj;
        }

        int increment = Mathf.RoundToInt(pointsPerSecond);
        if (increment <= 0) return;

        int newScore = currentScore + increment;
        var props = new Hashtable { { CrownScoreKey, newScore } };
        player.SetCustomProperties(props);

        var tracker = GetOrCreateTracker(actorNumber);
        if (newScore > tracker.HighestScore)
        {
            tracker.HighestScore = newScore;
            tracker.FirstReachTime = PhotonNetwork.Time;
        }
    }

    private PlayerScoreTracker GetOrCreateTracker(int actorNumber)
    {
        if (!scoreTrackers.TryGetValue(actorNumber, out PlayerScoreTracker tracker))
        {
            tracker = new PlayerScoreTracker
            {
                HighestScore = 0,
                FirstReachTime = PhotonNetwork.Time
            };
            scoreTrackers[actorNumber] = tracker;
        }
        return tracker;
    }

    private Player GetPlayerByActorNumber(int actorNumber)
    {
        foreach (Player player in PhotonNetwork.PlayerList)
        {
            if (player.ActorNumber == actorNumber)
            {
                return player;
            }
        }
        return null;
    }

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        isActiveMaster = PhotonNetwork.IsMasterClient;
        if (isActiveMaster)
        {
            InitializeCrownScores();
        }
    }

    public bool TryDetermineWinner(out Player winner, out Dictionary<int, int> scoreSnapshot)
    {
        winner = null;
        scoreSnapshot = new Dictionary<int, int>();
        if (PhotonNetwork.CurrentRoom == null) return false;

        int maxScore = int.MinValue;
        List<Player> candidates = new();

        foreach (Player player in PhotonNetwork.PlayerList)
        {
            int score = 0;
            if (player.CustomProperties.TryGetValue(CrownScoreKey, out object scoreObj))
            {
                score = (int)scoreObj;
            }

            scoreSnapshot[player.ActorNumber] = score;

            if (score > maxScore)
            {
                maxScore = score;
                candidates.Clear();
                candidates.Add(player);
            }
            else if (score == maxScore)
            {
                candidates.Add(player);
            }
        }

        if (candidates.Count == 0) return false;
        if (candidates.Count == 1)
        {
            winner = candidates[0];
            return true;
        }

        double bestTime = double.MaxValue;
        Player bestPlayer = null;

        foreach (Player candidate in candidates)
        {
            var tracker = GetOrCreateTracker(candidate.ActorNumber);
            if (tracker.FirstReachTime < bestTime)
            {
                bestTime = tracker.FirstReachTime;
                bestPlayer = candidate;
            }
        }

        winner = bestPlayer ?? candidates[0];
        return true;
    }
}