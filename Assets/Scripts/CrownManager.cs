using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class CrownManager : MonoBehaviourPunCallbacks
{
    public static CrownManager Instance;

    [Header("Crown Settings")]
    public GameObject crownPrefab; // Prefab visual de la corona
    public float pointsPerSecond = 1f;

    private int crownHolderId = -1; // ActorNumber del jugador con la corona
    private GameObject crownInstance;
    private float scoreTimer = 0f;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    void Start()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            AssignCrownToRandomPlayer();
        }
    }

    void Update()
    {
        if (!GameManager.Instance.IsPlaying())
            return;

        // Solo el MasterClient suma puntos y sincroniza
        if (PhotonNetwork.IsMasterClient && crownHolderId != -1)
        {
            scoreTimer += Time.deltaTime;
            if (scoreTimer >= 1f)
            {
                GameManager.Instance.AddScore(crownHolderId, pointsPerSecond * scoreTimer);
                scoreTimer = 0f;
            }
        }

        // Seguir al jugador que tiene la corona
        if (crownInstance != null && crownHolderId != -1)
        {
            var holder = GetPlayerObjectById(crownHolderId);
            if (holder != null)
            {
                crownInstance.transform.position = holder.transform.position + Vector3.up * 2f;
            }
        }
    }

    public void AssignCrownAtGameStart()
    {
        AssignCrownToRandomPlayer();
    }

    // Asigna la corona a un jugador aleatorio al inicio
    void AssignCrownToRandomPlayer()
    {
        var players = PhotonNetwork.PlayerList;
        if (players.Length == 0) return;
        int randomIndex = Random.Range(0, players.Length);
        int actorId = players[randomIndex].ActorNumber;
        photonView.RPC(nameof(RPC_SetCrownHolder), RpcTarget.All, actorId);
    }

    // Llama esto cuando un jugador golpea al portador de la corona
    public void TryStealCrown(int attackerId)
    {
        if (!GameManager.Instance.IsPlaying()) return;
        if (!PhotonNetwork.IsMasterClient) return;
        if (attackerId == crownHolderId) return; // No puede robarse a sí mismo

        photonView.RPC(nameof(RPC_SetCrownHolder), RpcTarget.All, attackerId);
    }

    [PunRPC]
    void RPC_SetCrownHolder(int newHolderId)
    {
        crownHolderId = newHolderId;
        AttachCrownToHolder();
    }

    void AttachCrownToHolder()
    {
        if (crownInstance == null)
        {
            crownInstance = Instantiate(crownPrefab);
        }

        var holder = GetPlayerObjectById(crownHolderId);
        if (holder != null)
        {
            crownInstance.transform.SetParent(null);
            crownInstance.transform.position = holder.transform.position + Vector3.up * 2f;
        }
    }

    // Utilidad: busca el objeto del jugador por su ActorNumber
    GameObject GetPlayerObjectById(int actorId)
    {
        foreach (var playerObj in GameObject.FindGameObjectsWithTag("Player"))
        {
            var photonView = playerObj.GetComponent<PhotonView>();
            if (photonView != null && photonView.Owner != null && photonView.Owner.ActorNumber == actorId)
                return playerObj;
        }
        return null;
    }

    public int GetCrownHolderId() => crownHolderId;
}