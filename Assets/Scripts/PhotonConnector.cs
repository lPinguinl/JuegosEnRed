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
                    var go = new GameObject(nameof(PhotonConnector));
                    _instance = go.AddComponent<PhotonConnector>();
                }
            }
            return _instance;
        }
    }

    [Header("Room Defaults")]
    [SerializeField] private byte maxPlayers = 4;

    private string desiredRoomName;  // lo que pide el usuario
    private bool joinIssued = false; // evita duplicados

    private void Awake()
    {
        PhotonNetwork.AutomaticallySyncScene = true;

        if (_instance != null && _instance != this) { Destroy(gameObject); return; }
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // Punto único de entrada desde MainMenuUI
    public void ConnectAndJoinRoom(string nickName, string roomName)
    {
        joinIssued = false; // reset
        desiredRoomName = string.IsNullOrWhiteSpace(roomName) ? null : roomName.Trim();

        PhotonNetwork.NickName = string.IsNullOrWhiteSpace(nickName)
            ? $"Player{Random.Range(1000, 9999)}"
            : nickName.Trim();

        if (PhotonNetwork.IsConnectedAndReady)
        {
            DecideJoinStrategy();
        }
        else
        {
            PhotonNetwork.ConnectUsingSettings();
            Debug.Log($"[Connector] Connecting as {PhotonNetwork.NickName}...");
        }
    }

    // Decide en un solo lugar cómo entrar a una sala
    private void DecideJoinStrategy()
    {
        if (joinIssued) return;
        joinIssued = true;

        // 1) Rejoin a la última sala si corresponde
        if (RejoinMemory.WantRejoin && !string.IsNullOrEmpty(RejoinMemory.LastRoom))
        {
            Debug.Log($"[Connector] Trying RejoinRoom({RejoinMemory.LastRoom})");
            bool started = PhotonNetwork.RejoinRoom(RejoinMemory.LastRoom);
            if (started) return; // esperamos callback
            Debug.Log("[Connector] RejoinRoom could not start; clearing intent");
            RejoinMemory.ClearIntent();
        }

        // 2) Join por nombre solicitado por el usuario (si lo hay)
        RoomOptions opts = new RoomOptions { MaxPlayers = maxPlayers };

        if (!string.IsNullOrEmpty(desiredRoomName))
        {
            Debug.Log($"[Connector] JoinOrCreateRoom({desiredRoomName})");
            PhotonNetwork.JoinOrCreateRoom(desiredRoomName, opts, TypedLobby.Default);
            return;
        }

        // 3) Flujo default: JoinRandom -> CreateRoom en OnJoinRandomFailed
        Debug.Log("[Connector] JoinRandomRoom()");
        PhotonNetwork.JoinRandomRoom();
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("[Connector] OnConnectedToMaster");
        DecideJoinStrategy();
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        Debug.Log($"[Connector] JoinRandom failed ({returnCode}): {message} -> CreateRoom");
        var opts = new RoomOptions { MaxPlayers = maxPlayers };
        PhotonNetwork.CreateRoom(null, opts, TypedLobby.Default);
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        Debug.LogWarning($"[Connector] OnJoinRoomFailed code={returnCode} msg={message}");

        // Casos donde la sala ya no existe o fue cerrada
        const int GameClosed = 32764;
        const int GameDoesNotExist = 32758;
        if (returnCode == GameClosed || returnCode == GameDoesNotExist ||
            (!string.IsNullOrEmpty(message) && message.ToLower().Contains("game closed")))
        {
            // Cancela rejoin, no insistas a esa sala
            RejoinMemory.ClearIntent();
            RejoinMemory.LastRoom = string.Empty;
            joinIssued = false; // permite decidir otra estrategia
            DecideJoinStrategy();
            return;
        }

        // Si falló rejoin por otra razón y teníamos intent, limpiamos y hacemos estrategia normal
        if (RejoinMemory.WantRejoin)
        {
            RejoinMemory.ClearIntent();
            joinIssued = false;
            DecideJoinStrategy();
            return;
        }

        // Último recurso: JoinRandom -> CreateRoom
        joinIssued = false;
        DecideJoinStrategy();
    }

    public override void OnJoinedRoom()
    {
        Debug.Log($"[Connector] OnJoinedRoom: {PhotonNetwork.CurrentRoom?.Name}");
        // Guarda intención de rejoin SOLO si estamos en una sala (para caídas)
        if (PhotonNetwork.CurrentRoom != null)
            RejoinMemory.CaptureIfInRoom(PhotonNetwork.CurrentRoom.Name);

        // Tu flujo actual:
        SceneManager.LoadScene("Lobby");
    }

    public override void OnLeftRoom()
    {
        Debug.Log("[Connector] OnLeftRoom");
        // Salida voluntaria: no queremos rejoin automático
        RejoinMemory.ClearIntent();
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.LogWarning($"[Connector] Disconnected: {cause}");
        // No reconectes aquí. Solo deja la intención si había LastRoom.
        if (!string.IsNullOrEmpty(RejoinMemory.LastRoom))
            RejoinMemory.WantRejoin = true;

        // La reconexión la gestiona ConnectAndJoinRoom + OnConnectedToMaster cuando el usuario lo pida.
    }
}