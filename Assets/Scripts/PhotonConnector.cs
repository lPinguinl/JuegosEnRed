using System;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class PhotonConnector : MonoBehaviourPunCallbacks, IConnectionService
{
    private static PhotonConnector _instance;
    public static PhotonConnector Instance 
    {
        get
        {
            if (_instance == null)
            {
                // Busca la instancia en la escena
                _instance = FindObjectOfType<PhotonConnector>();

                // Si no se encuentra, crea un nuevo objeto
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

    public event Action ConnectedToMaster;
    public event Action JoinedLobby;
    public event Action JoinedRoom;
    public event Action Disconnected;

    [SerializeField] private bool autoJoinLobby = true;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            // Si ya existe otra instancia, destrúyela para evitar duplicados.
            Destroy(gameObject);
        }
        else
        {
            // Asigna la instancia actual y asegúrate de que persista.
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
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