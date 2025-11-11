using UnityEngine;

[System.Serializable]
public class PowerUpSpawnEntry
{
    [Tooltip("Prefab de PowerUp (RoomObject). Debe tener PhotonView y un script que herede de PowerUpBase.")]
    public GameObject prefab;

    [Tooltip("Peso relativo para la selección aleatoria (>=1).")]
    public int weight = 1;

    [Tooltip("Máximo simultáneo de este tipo (0 = sin límite específico).")]
    public int maxSimultaneousOfThisType = 0;

    [HideInInspector] public string prefabName; // útil si usás InstantiateRoomObject por nombre

    public void OnValidatePrefabName()
    {
        if (prefab != null)
        {
            prefabName = prefab.name;
        }
    }
}