using System;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.SceneManagement;

public class PhotonConnector : MonoBehaviourPunCallbacks
{
    private static PhotonConnector _instance;
    public static PhotonConnector Instance 
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<PhotonConnector>();
                if (_instance == null)
                {
                    GameObject singletonObject = new GameObject();
                    _instance = singletonObject.AddComponent<PhotonConnector>();
                    singletonObject.name = typeof(PhotonConnector).ToString() + " (Singleton)";
                }
            }
            return _instance;
        }
    }

    private string roomNameToJoin;

    private void Awake()
    {
        PhotonNetwork.AutomaticallySyncScene = true;

        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }
    
    public void ConnectAndJoinRoom(string nickName, string roomName)
    {
        roomNameToJoin = roomName;

        // Aquí está el cambio. Verificamos si el nombre está vacío o es solo espacios.
        // Si lo está, asignamos un nombre por defecto.
        string finalNickName = string.IsNullOrWhiteSpace(nickName) ? $"Player{UnityEngine.Random.Range(1000, 9999)}" : nickName;

        if (PhotonNetwork.IsConnected)
        {
            JoinRoom(finalNickName, roomName);
        }
        else
        {
            PhotonNetwork.NickName = finalNickName;
            PhotonNetwork.ConnectUsingSettings();
            Debug.Log($"[PhotonConnector] Connecting as {PhotonNetwork.NickName}...");
        }
    }

    private void JoinRoom(string nickName, string roomName)
    {
        PhotonNetwork.NickName = nickName;

        RoomOptions roomOptions = new RoomOptions();
        roomOptions.MaxPlayers = 4;

        if (!string.IsNullOrEmpty(roomName))
        {
            PhotonNetwork.JoinOrCreateRoom(roomName, roomOptions, TypedLobby.Default);
        }
        else
        {
            // La corrección está aquí. JoinRandomRoom no necesita opciones,
            // y CreateRoom se llama en OnJoinRandomFailed.
            PhotonNetwork.JoinRandomRoom();
        }
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("[PhotonConnector] OnConnectedToMaster. Joining Room...");
        JoinRoom(PhotonNetwork.NickName, roomNameToJoin);
    }
    
    public override void OnJoinedRoom()
    {
        Debug.Log("[PhotonConnector] OnJoinedRoom. Loading Lobby scene...");
        SceneManager.LoadScene("Lobby");
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        Debug.LogWarning($"[PhotonConnector] OnJoinRoomFailed: {message}");
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.LogWarning($"[PhotonConnector] Disconnected: {cause}");
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        Debug.Log("[PhotonConnector] JoinRandom failed — creating room");
        RoomOptions options = new RoomOptions { MaxPlayers = 4 };
        PhotonNetwork.CreateRoom(null, options, TypedLobby.Default);
    }
}