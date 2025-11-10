using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;

public class ChatManager : MonoBehaviourPunCallbacks, IOnEventCallback
{
    [Header("UI References")]
    [SerializeField] private TMP_Text chatContentText;
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private ScrollRect scrollRect;

    private const byte CHAT_EVENT_CODE = 1;
    private const int MAX_MESSAGES = 500;

    private List<string> messageHistory = new List<string>();

    private void Start()
    {
        if (inputField != null)
            inputField.onSubmit.AddListener(OnMessageSubmitted);

        LoadHistoryFromRoom();
    }

    private void Update()
    {
        if (inputField != null && inputField.isFocused && Input.GetKeyDown(KeyCode.Return))
        {
            SendMessageToChat();
        }
    }

    private void OnMessageSubmitted(string _)
    {
        SendMessageToChat();
    }

    private void SendMessageToChat()
    {
        string msg = inputField.text;
        if (string.IsNullOrWhiteSpace(msg)) return;

        string fullMessage = $"{PhotonNetwork.NickName}: {msg}";

        RaiseEventOptions options = new RaiseEventOptions { Receivers = ReceiverGroup.All };
        PhotonNetwork.RaiseEvent(CHAT_EVENT_CODE, fullMessage, options, SendOptions.SendReliable);

        inputField.text = "";
        inputField.ActivateInputField();
    }

    private void AppendMessage(string message)
    {
        messageHistory.Add(message);
        if (messageHistory.Count > MAX_MESSAGES)
            messageHistory.RemoveAt(0);

        chatContentText.text = string.Join("\n", messageHistory);

        StartCoroutine(ForceScrollToBottom());
    }

    private System.Collections.IEnumerator ForceScrollToBottom()
    {
        // Espera al final del frame para que Unity actualice el layout
        yield return new WaitForEndOfFrame();

        Canvas.ForceUpdateCanvases();
        scrollRect.verticalNormalizedPosition = 0; // 0 = abajo, 1 = arriba
    }


    #region Photon Callbacks

    public override void OnJoinedRoom()
    {
        LoadHistoryFromRoom();
    }

    public void OnEvent(EventData photonEvent)
    {
        if (photonEvent.Code != CHAT_EVENT_CODE) return;

        string msg = (string)photonEvent.CustomData.ToString();
        SaveMessageToRoom(msg);
        AppendMessage(msg);
    }

    #endregion

    #region Room Custom Properties

    private void SaveMessageToRoom(string message)
    {
        string[] history = PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey("chatHistory")
            ? (string[])PhotonNetwork.CurrentRoom.CustomProperties["chatHistory"]
            : new string[0];

        List<string> temp = new List<string>(history);
        temp.Add(message);
        if (temp.Count > MAX_MESSAGES) temp.RemoveAt(0);

        ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable
        {
            { "chatHistory", temp.ToArray() }
        };
        PhotonNetwork.CurrentRoom.SetCustomProperties(props);
    }

    private void LoadHistoryFromRoom()
    {
        if (PhotonNetwork.CurrentRoom == null) return;

        if (PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey("chatHistory"))
        {
            string[] history = (string[])PhotonNetwork.CurrentRoom.CustomProperties["chatHistory"];
            messageHistory = new List<string>(history);
            chatContentText.text = string.Join("\n", messageHistory);
            Canvas.ForceUpdateCanvases();
            scrollRect.verticalNormalizedPosition = 0;
        }
    }

    #endregion
}
