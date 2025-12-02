using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class ApiCall : MonoBehaviour
{
    public string apiUrl = "https://api.chucknorris.io/jokes/random";
    public TMPro.TextMeshProUGUI joke;

    void Start()
    {
        StartCoroutine(GetApiData());
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space)) 
        {
            StartCoroutine(GetApiData());
        }
    }

    IEnumerator GetApiData()
    {
        using (UnityWebRequest request = UnityWebRequest.Get(apiUrl))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError($"Error: {request.error}");
            }
            else
            {
                string responseJson = request.downloadHandler.text;
                Debug.Log($"Respuesta de la API: {responseJson}");
                JokeData data = JsonUtility.FromJson<JokeData>(responseJson);
                joke.text = data.value;
            }
        }
    }
}

[System.Serializable]
public class JokeData
{
    public string[] categories;
    public string created_at;
    public string icon_url;
    public string id;
    public string updated_at;
    public string url;
    public string value;
}