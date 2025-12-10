using LootLocker.Requests;
using UnityEngine;

public class PlayerNameHelper : MonoBehaviour
{
    public static void SetPlayerName(string name, System.Action<bool> onDone = null)
    {
        LootLockerSDKManager.SetPlayerName(name, resp =>
        {
            if (!resp.success)
            {
                Debug.LogError($"Fallo al setear nombre. success={resp.success} status={resp.statusCode}");
                onDone?.Invoke(false);
                return;
            }

            Debug.Log("Se puso el nombre");
            onDone?.Invoke(true);
        });
    }
}