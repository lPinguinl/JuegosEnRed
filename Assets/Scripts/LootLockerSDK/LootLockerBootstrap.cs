using LootLocker.Requests;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LootLockerBootstrap : MonoBehaviour
{
    public static bool SessionStarted {  get; private set; }

    [SerializeField] string playerIdentifier = "1";

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        StartGuest();
    }

    void StartGuest()
    {
        LootLockerSDKManager.StartGuestSession(playerIdentifier, response =>
        {
            if (!response.success)
            {
                Debug.LogError("Fallo");
                return;
            }
            SessionStarted = true;
            Debug.Log("Conectado");
        });
    }
}
