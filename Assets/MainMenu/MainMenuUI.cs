using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MainMenuUI : MonoBehaviour
{
    [SerializeField] private InputField nameInput;
    [SerializeField] private Button connectButton;

    private PhotonConnector connector;

    private void Awake()
    {
        connector = PhotonConnector.Instance;
        if (connector == null)
            Debug.LogError("PhotonConnector is missing. Add PhotonConnector prefab to the MainMenu scene.");
    }

    private void OnEnable()
    {
        connectButton.onClick.AddListener(OnConnectClicked);
        if (connector != null) connector.JoinedLobby += OnJoinedLobby;
    }

    private void OnDisable()
    {
        connectButton.onClick.RemoveListener(OnConnectClicked);
        if (connector != null) connector.JoinedLobby -= OnJoinedLobby;
    }

    private void OnConnectClicked()
    {
        connector.Connect(nameInput.text.Trim());
        // visual feedback: you can disable the button and show "Connecting..."
    }

    private void OnJoinedLobby()
    {
        // When we join the Photon lobby, go to the Lobby scene
        SceneManager.LoadScene("Lobby");
    }
}