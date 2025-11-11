using UnityEngine;

[System.Serializable]
public class SpawnPointState
{
    public Transform point;
    public bool occupied;
    public float availableAt; // tiempo en el que el punto vuelve a estar disponible

    public SpawnPointState(Transform t)
    {
        point = t;
        occupied = false;
        availableAt = 0f;
    }
}