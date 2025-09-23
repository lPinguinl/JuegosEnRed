using UnityEngine;
using Photon.Pun;
using System.Collections.Generic;
using System.Linq;

public class GameManager : MonoBehaviour
{
    [SerializeField] private string playerPrefabName = "PlayerPrefab";
    [SerializeField] private Transform[] spawnPoints;

    private static List<int> usedSpawnIndexes = new List<int>();

    private void Start()
    {
        GetSpawnPoints();

        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogError("Error: Los puntos de spawn no están asignados en el GameManager.");
            return;
        }

        if (PhotonNetwork.IsConnectedAndReady)
        {
            int spawnIndex = GetAvailableSpawnIndex();

            if (spawnIndex != -1)
            {
                Transform spawnPoint = spawnPoints[spawnIndex];
                usedSpawnIndexes.Add(spawnIndex);

                PhotonNetwork.Instantiate(playerPrefabName, spawnPoint.position, spawnPoint.rotation);
            }
            else
            {
                Debug.LogWarning("No hay puntos de spawn disponibles para este jugador.");
            }
        }
    }

    private void GetSpawnPoints()
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            spawnPoints = GameObject.FindGameObjectsWithTag("SpawnPoint")
                .Select(go => go.transform)
                .ToArray();
        }
    }

    private int GetAvailableSpawnIndex()
    {
        List<int> availableIndexes = Enumerable.Range(0, spawnPoints.Length)
            .Where(i => !usedSpawnIndexes.Contains(i)).ToList();

        if (availableIndexes.Count == 0)
        {
            return -1; // No hay spawn libre
        }

        // Elegir un spawn aleatorio de los disponibles
        int randomIndex = Random.Range(0, availableIndexes.Count);
        return availableIndexes[randomIndex];
    }

    //Reset cuando todos salen de la sala
    public static void ResetSpawnPoints()
    {
        usedSpawnIndexes.Clear();
    }
}
