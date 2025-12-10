using LootLocker.Requests;
using UnityEngine;
using System;

public class LeaderboardService : MonoBehaviour
{
    public static void SubmitScore(int score, string leaderboardKey, Action<bool> onDone = null)
    {
        string memberId = SystemInfo.deviceUniqueIdentifier;

        //Definimos el callback exacto para evitar confusiones al compilador
        Action<LootLockerSubmitScoreResponse> onResponse = (response) =>
        {
            if (!response.success)
            {
                // Manejo de errores seguro (evita el error de "message" o "Error")
                string errorMsg = "Error desconocido";
                if (response.errorData != null) errorMsg = response.errorData.message;

                Debug.LogError($"Fallo el envío de score: {errorMsg}");
                onDone?.Invoke(false);
                return;
            }

            Debug.Log("Puntaje enviado correctamente.");
            onDone?.Invoke(true);
        };

     
        // La configuración de sumar se hace en la web.
        LootLockerSDKManager.SubmitScore(memberId, score, leaderboardKey, onResponse);
    }
}