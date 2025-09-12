using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class MainMenuUI : MonoBehaviour
{
    [SerializeField] private TMP_InputField nameInput;
    [SerializeField] private TMP_InputField roomInput;
    [SerializeField] private Button connectButton;

    private PhotonConnector connector;

    private void Start()
    {
        connector = PhotonConnector.Instance;
        if (connector == null)
            Debug.LogError("[MainMenuUI] PhotonConnector is missing. Ensure the prefab is in the scene.");
    }

    private void OnEnable()
    {
        if (connector == null)
        {
            connector = PhotonConnector.Instance;
        }

        if (connectButton != null)
            connectButton.onClick.AddListener(OnConnectClicked);
        
        // No es necesario suscribirse a OnJoinedLobby aquí, ya que la carga de escena la hace el conector
    }

    private void OnDisable()
    {
        if (connectButton != null)
            connectButton.onClick.RemoveListener(OnConnectClicked);
    }

    private void OnConnectClicked()
    {
        if (nameInput == null || string.IsNullOrWhiteSpace(nameInput.text))
        {
            Debug.LogWarning("[MainMenuUI] Player name is empty!");
            return;
        }
        
        string roomName = string.IsNullOrWhiteSpace(roomInput.text) ? null : roomInput.text.Trim();

        connector.ConnectAndJoinRoom(nameInput.text.Trim(), roomName);
        
        connectButton.interactable = false;
        Debug.Log($"[MainMenuUI] Connecting as {nameInput.text.Trim()}...");
    }
}