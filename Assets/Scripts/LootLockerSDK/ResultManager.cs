using UnityEngine;
using TMPro; // Si necesitas mostrar el puntaje en pantalla

public class ResultManager : MonoBehaviour
{
    // Asigna aquí la KEY de tu leaderboard (la misma que usaste en el otro script)
    [SerializeField] string leaderboardKey = "e580355f5c684ef4908f55eaf8d9fd43";

    // Variable para guardar el puntaje de la partida
    private int finalScore = 0;

    void Start()
    {
        // 1. AQUÍ RECUPERAS EL PUNTAJE DE TU JUEGO
        // Ejemplo: Si guardas el puntaje en PlayerPrefs o en un GameManager estático.
        // Cambia esta línea por tu lógica real:
        finalScore = PlayerPrefs.GetInt("ScoreDeLaPartida", 0);

        Debug.Log($"Puntaje final recuperado: {finalScore}");
    }

    // ESTA ES LA FUNCIÓN QUE CONECTARÁS AL BOTÓN
    public void OnPressSubmitScore()
    {
        Debug.Log("Enviando puntaje...");

        // Llamamos a tu servicio (que ya arreglamos)
        LeaderboardService.SubmitScore(finalScore, leaderboardKey, (success) =>
        {
            if (success)
            {
                Debug.Log("¡Puntaje enviado y sumado exitosamente!");
                // Opcional: Aquí podrías activar el panel del Leaderboard para que se vea
                // FindObjectOfType<LeaderboardUI>().Refresh();
            }
            else
            {
                Debug.Log("Error al enviar.");
            }
        });
    }
}