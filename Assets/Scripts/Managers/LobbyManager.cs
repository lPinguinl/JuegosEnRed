using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using ExitGames.Client.Photon;

public class LobbyManager : MonoBehaviourPunCallbacks
{
    [SerializeField] private TMP_Text roomNameText;
    [SerializeField] private Button readyButton;
    [SerializeField] private Transform playerListContent;
    [SerializeField] private GameObject playerListItemPrefab;

    // === UI DE MAPA ===
    [SerializeField] private TMP_Dropdown MapDropdown;

    private bool isPlayerReady = false;
    private TMP_Text primaryReadyLabel;
    private TMP_Text[] allReadyLabels;    // <- capturamos todos los textos del botón
    private Dictionary<int, GameObject> playerListItems = new Dictionary<int, GameObject>();

    // Paleta de colores compartida para un máximo de 4 jugadores.
    private static readonly Color[] palette =
    {
        new Color(0.90f, 0.20f, 0.20f),  // Rojo
        new Color(0.20f, 0.50f, 0.95f),  // Azul
        new Color(0.20f, 0.80f, 0.35f),  // Verde
        new Color(0.95f, 0.80f, 0.20f)   // Amarillo
    };

    public static Color GetPaletteColor(int index)
    {
        if (palette.Length == 0)
        {
            return Color.white;
        }

        int safeIndex = Mathf.Clamp(index, 0, palette.Length - 1);
        return palette[safeIndex];
    }

    public const string COLOR_KEY = "playerColorIdx";
    private const string READY_KEY = "isReady";

    // === ROOM PROPERTY PARA MAPA ===
    private const string MAP_KEY = "selectedMap";

    // Nombres de escenas correspondientes a las opciones del dropdown:
    // Map 1 -> "GameScene"
    // Map 2 -> "GameScene_2"
    private readonly string[] mapSceneNames = { "GameScene", "GameScene_2" };
    
    private LobbyAnalyticsHook lobbyAnalytics;


    private void Start()
    {
        if (roomNameText != null && PhotonNetwork.CurrentRoom != null)
            roomNameText.text = PhotonNetwork.CurrentRoom.Name;

        // --- Ready Button ---
        if (readyButton != null)
        {
            readyButton.onClick.AddListener(OnReadyClicked);

            allReadyLabels = readyButton.GetComponentsInChildren<TMP_Text>(true);
            if (allReadyLabels == null || allReadyLabels.Length == 0)
            {
                Debug.LogError("[LobbyManager] No TMP_Text components found inside Ready Button.");
            }
            else
            {
                primaryReadyLabel = allReadyLabels[0];
                UpdateReadyButtonTexts();
            }
        }
        else
        {
            Debug.LogError("[LobbyManager] Ready Button not assigned in inspector.");
        }

        // --- Map Dropdown ---
        if (MapDropdown != null)
        {
            // Nos aseguramos de que el dropdown tenga las opciones correctas
            MapDropdown.ClearOptions();
            MapDropdown.AddOptions(new List<string> { "Map 1", "Map 2" });

            MapDropdown.onValueChanged.AddListener(OnMapDropdownChanged);

            // Al entrar al lobby, sincronizamos la UI con la Room:
            InitializeMapSelectionFromRoom();

            // Mostrar/ocultar según sea MasterClient o no
            if (PhotonNetwork.IsMasterClient)
            {
                MapDropdown.interactable = true;
                MapDropdown.gameObject.SetActive(true);
            }
            else
            {
                MapDropdown.interactable = false;
                MapDropdown.gameObject.SetActive(true); 
            }
        }
        else
        {
            Debug.LogWarning("[LobbyManager] MapDropdown not assigned in inspector.");
        }

        // Actualizamos la lista inicial de jugadores
        UpdatePlayerList();

        // Asignar color al jugador local si no tiene
        EnsurePlayerHasColor(PhotonNetwork.LocalPlayer);
        
        lobbyAnalytics = GetComponent<LobbyAnalyticsHook>();
        if (lobbyAnalytics == null)
        {
            Debug.LogError("[LobbyManager] LobbyAnalyticsHook no encontrado en el mismo GameObject.");
        }
        else
        {
            lobbyAnalytics.Init();
        }


    }

    // ============================
    //   LÓGICA DE READY / START
    // ============================

    private void OnReadyClicked()
    {
        isPlayerReady = !isPlayerReady;
        UpdateReadyButtonTexts();

        SetPlayerReadyState(isPlayerReady);

        if (PhotonNetwork.IsMasterClient)
            CheckAndStartGame();
    }

    private void UpdateReadyButtonTexts()
    {
        if (allReadyLabels == null || allReadyLabels.Length == 0) return;

        string labelText = isPlayerReady ? "Unready" : "Ready";

        for (int i = 0; i < allReadyLabels.Length; i++)
        {
            TMP_Text label = allReadyLabels[i];
            if (label == null) continue;

            label.text = (i == 0) ? labelText : string.Empty;
        }
    }

    private void SetPlayerReadyState(bool ready)
    {
        ExitGames.Client.Photon.Hashtable playerProps = new ExitGames.Client.Photon.Hashtable
        {
            [READY_KEY] = ready
        };
        PhotonNetwork.LocalPlayer.SetCustomProperties(playerProps);
    }

    private void CheckAndStartGame()
    {
        int playerCount = PhotonNetwork.CurrentRoom.PlayerCount;
        int readyCount = 0;

        foreach (Player p in PhotonNetwork.PlayerList)
        {
            if (p.CustomProperties.TryGetValue(READY_KEY, out object readyObj) &&
                readyObj is bool isReady && isReady)
            {
                readyCount++;
            }
        }

        // ✅ Solo arranca si hay mínimo 2 jugadores y todos están listos
        if (playerCount >= 2 && readyCount == playerCount)
        {
            StartGame();
        }
    }

    private void StartGame()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        PhotonNetwork.CurrentRoom.IsOpen = false;
        PhotonNetwork.CurrentRoom.IsVisible = false;

        // Determinar qué escena cargar según MAP_KEY
        string sceneToLoad = mapSceneNames[0]; // por defecto "GameScene"

        if (PhotonNetwork.CurrentRoom != null &&
            PhotonNetwork.CurrentRoom.CustomProperties != null &&
            PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(MAP_KEY, out object mapNameObj))
        {
            string mapName = mapNameObj as string;
            if (!string.IsNullOrEmpty(mapName))
            {
                sceneToLoad = mapName;
            }
        }

        PhotonNetwork.LoadLevel(sceneToLoad);
        
        MatchAnalytics.Instance.MatchStart(
            PhotonNetwork.CurrentRoom.PlayerCount,
            lobbyAnalytics.GetLobbyDuration()
        );

    }

    // ============================
    //   CALLBACKS DE PHOTON
    // ============================

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        base.OnMasterClientSwitched(newMasterClient);

        if (MapDropdown == null) return;

        // Si ahora yo soy el Master, habilito el dropdown.
        if (PhotonNetwork.LocalPlayer == newMasterClient)
        {
            MapDropdown.gameObject.SetActive(true);
            MapDropdown.interactable = true;
        }
        else
        {
            // Si no soy el Master, lo deshabilito (o lo oculto).
            MapDropdown.interactable = false;
            MapDropdown.gameObject.SetActive(true); // si querés ocultarlo
        }
    } 
    
    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        EnsurePlayerHasColor(newPlayer);
        UpdatePlayerList();

        if (PhotonNetwork.IsMasterClient)
            CheckAndStartGame();
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        UpdatePlayerList();

        if (PhotonNetwork.IsMasterClient)
            CheckAndStartGame();
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps)
    {
        if (playerListItems.TryGetValue(targetPlayer.ActorNumber, out GameObject item))
        {
            item.GetComponent<PlayerListItem>().UpdateInfo();
        }

        if (changedProps.ContainsKey(READY_KEY) && PhotonNetwork.IsMasterClient)
            CheckAndStartGame();
    }

    // Se dispara cuando cambian las CustomProperties de la Room (incluido MAP_KEY)
    public override void OnRoomPropertiesUpdate(ExitGames.Client.Photon.Hashtable propertiesThatChanged)
    {
        if (propertiesThatChanged.ContainsKey(MAP_KEY))
        {
            string mapName = propertiesThatChanged[MAP_KEY] as string;
            UpdateMapDropdownFromRoomProperty(mapName);
        }
    }

    // ============================
    //   UI / MAPA
    // ============================

    /// <summary>
    /// Llamado cuando cambia el dropdown de mapa en la UI.
    /// Solo el MasterClient debe aplicar el cambio en la Room.
    /// </summary>
    private void OnMapDropdownChanged(int optionIndex)
    {
        if (!PhotonNetwork.IsMasterClient)
        {
            // Los clientes que no son master solo reflejan el valor que viene de la Room,
            // así que si cambian localmente, idealmente deberías ignorarlo o revertirlo.
            // Para simplificar, dejamos que el cambio se vea pero NO escribimos en la Room.
            return;
        }

        // Seguridad por si el tamaño no coincide
        if (optionIndex < 0 || optionIndex >= mapSceneNames.Length)
        {
            Debug.LogWarning($"[LobbyManager] MapDropdown index {optionIndex} fuera de rango. Usando índice 0.");
            optionIndex = 0;
        }

        string selectedSceneName = mapSceneNames[optionIndex];

        ExitGames.Client.Photon.Hashtable roomProps = new ExitGames.Client.Photon.Hashtable
        {
            [MAP_KEY] = selectedSceneName
        };
        PhotonNetwork.CurrentRoom.SetCustomProperties(roomProps);
    }

    /// <summary>
    /// Al entrar al lobby, sincroniza el dropdown con la Room (o setea default si es Master).
    /// </summary>
    private void InitializeMapSelectionFromRoom()
    {
        if (PhotonNetwork.CurrentRoom == null || MapDropdown == null)
            return;

        // Si ya hay un MAP_KEY, lo usamos para posicionar el dropdown.
        if (PhotonNetwork.CurrentRoom.CustomProperties != null &&
            PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(MAP_KEY, out object mapNameObj))
        {
            string mapName = mapNameObj as string;
            UpdateMapDropdownFromRoomProperty(mapName);
        }
        else
        {
            // Si no hay MAP_KEY y soy Master, seteo un valor por defecto.
            if (PhotonNetwork.IsMasterClient)
            {
                string defaultMap = mapSceneNames[0]; // "GameScene"
                ExitGames.Client.Photon.Hashtable roomProps = new ExitGames.Client.Photon.Hashtable
                {
                    [MAP_KEY] = defaultMap
                };
                PhotonNetwork.CurrentRoom.SetCustomProperties(roomProps);

                // y también ajusto el dropdown localmente
                MapDropdown.value = 0;
            }
            else
            {
                // Si no soy Master y no hay propiedad, simplemente mostramos opción 0
                MapDropdown.value = 0;
            }
        }
    }

    /// <summary>
    /// Dado un nombre de escena (guardado en la Room), coloca el dropdown en el índice correcto.
    /// </summary>
    private void UpdateMapDropdownFromRoomProperty(string mapName)
    {
        if (MapDropdown == null) return;
        if (string.IsNullOrEmpty(mapName))
        {
            MapDropdown.value = 0;
            return;
        }

        // Buscar el índice en mapSceneNames
        int index = 0;
        for (int i = 0; i < mapSceneNames.Length; i++)
        {
            if (mapSceneNames[i] == mapName)
            {
                index = i;
                break;
            }
        }

        MapDropdown.value = index;
    }

    // ============================
    //   LISTA DE JUGADORES / COLORES
    // ============================

    private void UpdatePlayerList()
    {
        foreach (var item in playerListItems.Values)
            GameObject.Destroy(item);
        playerListItems.Clear();

        foreach (Player player in PhotonNetwork.PlayerList)
        {
            GameObject item = Instantiate(playerListItemPrefab, playerListContent);
            PlayerListItem listItem = item.GetComponent<PlayerListItem>();
            if (listItem != null)
            {
                listItem.SetPlayerInfo(player);
                playerListItems[player.ActorNumber] = item;
            }
        }
    }

    private void EnsurePlayerHasColor(Player p)
    {
        if (p == null) return;

        if (p.CustomProperties.ContainsKey(COLOR_KEY))
            return; // Ya tiene un color asignado

        HashSet<int> usedIndices = new HashSet<int>();
        foreach (var pl in PhotonNetwork.PlayerList)
        {
            if (pl.CustomProperties.TryGetValue(COLOR_KEY, out object idxObj))
                usedIndices.Add((int)idxObj);
        }

        // Elegimos el primer índice de color disponible
        int newIdx = 0;
        while (usedIndices.Contains(newIdx))
            newIdx = (newIdx + 1) % palette.Length;

        ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable
        {
            [COLOR_KEY] = newIdx
        };
        p.SetCustomProperties(props);
    }
}