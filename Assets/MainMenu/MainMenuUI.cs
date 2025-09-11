using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class MainMenuUI : MonoBehaviour
{
    [SerializeField] private TMP_InputField nameInput;
    [SerializeField] private Button connectButton;

    private PhotonConnector connector;

    private void Start()
    {
        // La inicialización se realiza aquí, después de todos los Awakes.
        connector = PhotonConnector.Instance;
        if (connector == null)
            Debug.LogError("[MainMenuUI] PhotonConnector is missing. Ensure the prefab is in the scene.");
    }

    private void OnEnable()
    {
        // Nos aseguramos de tener el conector antes de suscribir el evento.
        if (connector == null)
        {
            connector = PhotonConnector.Instance;
        }

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

        // Desactivamos el botón para evitar clics múltiples.
        connectButton.interactable = false;
        Debug.Log($"[MainMenuUI] Connecting as {nameInput.text.Trim()}...");
    }

    private void OnJoinedLobby()
    {
        Debug.Log("[MainMenuUI] Successfully joined lobby. Loading Lobby scene...");
        SceneManager.LoadScene("Lobby");
    }
}