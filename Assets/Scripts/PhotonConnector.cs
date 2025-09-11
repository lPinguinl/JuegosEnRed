using System;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class PhotonConnector : MonoBehaviourPunCallbacks, IConnectionService
{
    public static PhotonConnector Instance { get; private set; }

    public event Action ConnectedToMaster;
    public event Action JoinedLobby;
    public event Action JoinedRoom;
    public event Action Disconnected;

    [SerializeField] private bool autoJoinLobby = true;

    private void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else { Destroy(gameObject); return; }
    }

    public void Connect(string nickName)
    {
        if (PhotonNetwork.IsConnected)
        {
            ConnectedToMaster?.Invoke();
            return;
        }

        PhotonNetwork.NickName = string.IsNullOrWhiteSpace(nickName) ? $"Player{UnityEngine.Random.Range(1000,9999)}" : nickName;
        PhotonNetwork.ConnectUsingSettings();
        Debug.Log($"[PhotonConnector] Connecting as {PhotonNetwork.NickName}...");
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("[PhotonConnector] OnConnectedToMaster");
        ConnectedToMaster?.Invoke();

        if (autoJoinLobby)
            PhotonNetwork.JoinLobby();
    }

    public override void OnJoinedLobby()
    {
        Debug.Log("[PhotonConnector] OnJoinedLobby");
        JoinedLobby?.Invoke();
    }

    public override void OnJoinedRoom()
    {
        Debug.Log("[PhotonConnector] OnJoinedRoom");
        JoinedRoom?.Invoke();
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.LogWarning($"[PhotonConnector] Disconnected: {cause}");
        Disconnected?.Invoke();
    }

    // Simple helper used in LobbyManager to join or create a room
    public void JoinRandomOrCreateRoom(byte maxPlayers = 4)
    {
        PhotonNetwork.JoinRandomRoom();
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        Debug.Log("[PhotonConnector] JoinRandom failed — creating room");
        RoomOptions options = new RoomOptions { MaxPlayers = 4 };
        PhotonNetwork.CreateRoom(null, options, TypedLobby.Default);
    }
}
