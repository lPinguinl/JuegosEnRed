using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class MainMenuUI : MonoBehaviour
{
    [SerializeField] private TMP_InputField nameInput;   // ← Usamos TMP
    [SerializeField] private Button connectButton;

    private PhotonConnector connector;

    private void Awake()
    {
        connector = PhotonConnector.Instance;
        if (connector == null)
            Debug.LogError("[MainMenuUI] PhotonConnector is missing. Add PhotonConnector prefab to the MainMenu scene.");
    }

    private void OnEnable()
    {
        if (connectButton != null)
            connectButton.onClick.AddListener(OnConnectClicked);

        if (connector != null)
            connector.JoinedLobby += OnJoinedLobby;
    }

    private void OnDisable()
    {
        if (connectButton != null)
            connectButton.onClick.RemoveListener(OnConnectClicked);

        if (connector != null)
            connector.JoinedLobby -= OnJoinedLobby;
    }

    private void OnConnectClicked()
    {
        if (nameInput == null || string.IsNullOrWhiteSpace(nameInput.text))
        {
            Debug.LogWarning("[MainMenuUI] Player name is empty!");
            return;
        }

        connector.Connect(nameInput.text.Trim());

        // Feedback opcional: desactivar botón mientras conecta
        connectButton.interactable = false;
        Debug.Log($"[MainMenuUI] Connecting as {nameInput.text.Trim()}...");
    }

    private void OnJoinedLobby()
    {
        Debug.Log("[MainMenuUI] Successfully joined lobby. Loading Lobby scene...");
        SceneManager.LoadScene("Lobby");
    }
}