using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class Launcher : MonoBehaviourPunCallbacks
{
    [SerializeField] private GameObject playerPrefab; 
    [SerializeField] private Transform[] spawnPoints; 
    [SerializeField] private byte maxPlayers = 4;

    void Start()
    {
        PhotonNetwork.AutomaticallySyncScene = false;
        if (!PhotonNetwork.IsConnected)
        {
            PhotonNetwork.ConnectUsingSettings();
            Debug.Log("[Launcher] Connecting to Photon...");
        }
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("[Launcher] Connected to Master. Joining random room...");
        PhotonNetwork.NickName = "P" + Random.Range(1000, 9999);
        PhotonNetwork.JoinRandomRoom();
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        Debug.Log("[Launcher] JoinRandom failed. Creating a room...");
        RoomOptions options = new RoomOptions { MaxPlayers = maxPlayers };
        PhotonNetwork.CreateRoom(null, options);
    }

    public override void OnJoinedRoom()
    {
        Debug.Log("[Launcher] Joined room. Spawning player...");
        Vector3 spawnPos = Vector3.zero;
        if (spawnPoints != null && spawnPoints.Length > 0)
            spawnPos = spawnPoints[Random.Range(0, spawnPoints.Length)].position;

        PhotonNetwork.Instantiate(playerPrefab.name, spawnPos, Quaternion.identity);
    }
}