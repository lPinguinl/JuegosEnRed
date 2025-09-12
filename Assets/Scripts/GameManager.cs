using UnityEngine;
using Photon.Pun;
using System.Linq; // Necesario para usar .ToArray()

public class GameManager : MonoBehaviour
{
    // El nombre de tu prefab de jugador dentro de la carpeta Resources/Prefabs
    [SerializeField] private string playerPrefabName = "PlayerPrefab";

    // Array para almacenar los puntos de spawn
    [SerializeField] private Transform[] spawnPoints;

    private void Start()
    {
        // Verifica si los puntos de spawn están asignados en el Inspector
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogError("Error: Los puntos de spawn no están asignados en el GameManager.");
            return;
        }

        // Elige un punto de spawn aleatorio del array
        int randomSpawnIndex = Random.Range(0, spawnPoints.Length);
        Transform spawnPoint = spawnPoints[randomSpawnIndex];
        
        // Instancia un prefab de jugador para cada cliente
        if (PhotonNetwork.IsConnectedAndReady)
        {
            PhotonNetwork.Instantiate(playerPrefabName, spawnPoint.position, spawnPoint.rotation);
        }
    }

    // Método para obtener los puntos de spawn
    private void GetSpawnPoints()
    {
        // Si no has asignado los puntos en el Inspector, los busca automáticamente en la escena
        if (spawnPoints.Length == 0)
        {
            spawnPoints = GameObject.FindGameObjectsWithTag("SpawnPoint")
                .Select(go => go.transform)
                .ToArray();
        }
    }
}