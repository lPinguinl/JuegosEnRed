using UnityEngine;
using TMPro; 

public class ResultManager : MonoBehaviour
{
    
    [SerializeField] string leaderboardKey = "e580355f5c684ef4908f55eaf8d9fd43";

    // Variable para guardar el puntaje de la partida
    private int finalScore = 0;

    void Start()
    {
        // Se recupera el puntaje del juego 
        
        finalScore = PlayerPrefs.GetInt("ScoreDeLaPartida", 0);

        Debug.Log($"Puntaje final recuperado: {finalScore}");
    }

    
    public void OnPressSubmitScore()
    {
        Debug.Log("Enviando puntaje...");

        // Llamamos a tu servicio
        LeaderboardService.SubmitScore(finalScore, leaderboardKey, (success) =>
        {
            if (success)
            {
                Debug.Log("¡Puntaje enviado y sumado exitosamente!");
                
            }
            else
            {
                Debug.Log("Error al enviar.");
            }
        });
    }
}