using LootLocker.Requests;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerNameHelper : MonoBehaviour
{
    public static void SetPlayerName(string name)
    {
        LootLockerSDKManager.SetPlayerName(name, resp =>
        {
            if (!resp.success) Debug.LogError("Fallo nombre");
            else Debug.Log("Se puso el nombre");
        });
    }
}
