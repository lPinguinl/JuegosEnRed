using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class PowerUpSpawnerManager : MonoBehaviourPunCallbacks
{
    [Header("Spawn Points")]
    [SerializeField] private List<Transform> spawnPoints = new List<Transform>();

    [Header("Spawn Config")]
    [Tooltip("Intervalo entre intentos de spawn (segundos).")]
    [SerializeField] private float spawnInterval = 8f;

    [Tooltip("Cooldown por punto después de spawn (segundos).")]
    [SerializeField] private float pointCooldown = 5f;

    [Tooltip("Cantidad máxima de power-ups simultáneamente en escena (0 = sin límite).")]
    [SerializeField] private int globalMaxSimultaneous = 6;

    [Header("Tipos de PowerUps (con pesos)")]
    [SerializeField] private List<PowerUpSpawnEntry> powerUpTypes = new List<PowerUpSpawnEntry>();

    [Header("Debug")]
    [SerializeField] private bool logSpawns = false;

    // Estado interno
    private readonly List<SpawnPointState> pointStates = new List<SpawnPointState>();
    private readonly Dictionary<string, int> aliveByType = new Dictionary<string, int>();
    private int totalAlive = 0;
    private Coroutine loopRoutine;

    private void Awake()
    {
        // Normalizar nombres de prefabs para usar con InstantiateRoomObject si fuera necesario
        foreach (var entry in powerUpTypes)
        {
            entry.OnValidatePrefabName();
            if (!aliveByType.ContainsKey(entry.prefabName))
                aliveByType.Add(entry.prefabName, 0);
        }

        // Inicializar puntos
        pointStates.Clear();
        foreach (var t in spawnPoints)
        {
            if (t != null)
                pointStates.Add(new SpawnPointState(t));
        }
    }

    private void OnEnable()
    {
        TryStartLoop();
    }

    private void OnDisable()
    {
        if (loopRoutine != null) StopCoroutine(loopRoutine);
        loopRoutine = null;
    }

    public override void OnJoinedRoom()
    {
        TryStartLoop();
    }

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        // Si cambia el master, el nuevo debería continuar el loop
        TryStartLoop(forceRestart: true);
    }

    private void TryStartLoop(bool forceRestart = false)
    {
        if (!PhotonNetwork.InRoom) return;

        if (PhotonNetwork.IsMasterClient)
        {
            if (forceRestart && loopRoutine != null)
            {
                StopCoroutine(loopRoutine);
                loopRoutine = null;
            }

            if (loopRoutine == null)
                loopRoutine = StartCoroutine(SpawnLoop());
        }
        else
        {
            if (loopRoutine != null)
            {
                StopCoroutine(loopRoutine);
                loopRoutine = null;
            }
        }
    }

    private IEnumerator SpawnLoop()
    {
        var wait = new WaitForSeconds(spawnInterval);
        while (true)
        {
            yield return wait;

            if (!PhotonNetwork.IsMasterClient) continue;

            // Chequear límite global
            if (globalMaxSimultaneous > 0 && totalAlive >= globalMaxSimultaneous)
                continue;

            // Elegir un punto disponible
            int idx = FindAvailablePointIndex();
            if (idx < 0) continue; // no hay puntos listos

            // Elegir tipo por pesos y por límites por tipo
            var entry = ChooseTypeWeighted();
            if (entry == null) continue;

            // Validar límite por tipo
            if (entry.maxSimultaneousOfThisType > 0)
            {
                if (aliveByType.TryGetValue(entry.prefabName, out int count) && count >= entry.maxSimultaneousOfThisType)
                    continue;
            }

            // Instanciar como RoomObject
            Vector3 pos = pointStates[idx].point.position;
            Quaternion rot = pointStates[idx].point.rotation;

            // Importante: para InstantiateRoomObject por NOMBRE, el prefab debe estar en Resources y registrado en PhotonRoom
            GameObject go = PhotonNetwork.InstantiateRoomObject(entry.prefabName, pos, rot);
            if (go == null) continue;

            // Marcar estado
            pointStates[idx].occupied = true;
            pointStates[idx].availableAt = Time.time + pointCooldown;

            totalAlive++;
            aliveByType[entry.prefabName] = aliveByType[entry.prefabName] + 1;

            if (logSpawns)
                Debug.Log($"[PowerUpSpawner] Spawned {entry.prefabName} at {pointStates[idx].point.name}. TotalAlive={totalAlive}");

            // Hook de destrucción: cuando el pickup se destruya, liberar punto y contadores
            var tracker = go.AddComponent<SpawnedPickupTracker>();
            tracker.Init(this, entry.prefabName, idx);
        }
    }

    private int FindAvailablePointIndex()
    {
        float now = Time.time;
        // Preferir puntos no ocupados y cuyo cooldown venció
        List<int> candidates = new List<int>();
        for (int i = 0; i < pointStates.Count; i++)
        {
            var ps = pointStates[i];
            if (ps.point == null) continue;

            if (!ps.occupied && ps.availableAt <= now)
                candidates.Add(i);
        }

        if (candidates.Count == 0) return -1;

        return candidates[Random.Range(0, candidates.Count)];
    }

    private PowerUpSpawnEntry ChooseTypeWeighted()
    {
        int totalWeight = 0;
        foreach (var e in powerUpTypes)
        {
            if (e.prefab == null || e.weight <= 0) continue;

            // Respetar límite por tipo si está lleno, no lo consideres
            if (e.maxSimultaneousOfThisType > 0)
            {
                if (aliveByType.TryGetValue(e.prefabName, out int count) && count >= e.maxSimultaneousOfThisType)
                    continue;
            }

            totalWeight += e.weight;
        }

        if (totalWeight <= 0) return null;

        int r = Random.Range(1, totalWeight + 1);
        int acc = 0;
        foreach (var e in powerUpTypes)
        {
            if (e.prefab == null || e.weight <= 0) continue;

            if (e.maxSimultaneousOfThisType > 0)
            {
                if (aliveByType.TryGetValue(e.prefabName, out int count) && count >= e.maxSimultaneousOfThisType)
                    continue;
            }

            acc += e.weight;
            if (r <= acc)
                return e;
        }

        return null;
    }

    // Llamado por SpawnedPickupTracker cuando el pickup se destruye
    private void OnPickupDestroyed(string prefabName, int pointIndex)
    {
        if (aliveByType.ContainsKey(prefabName))
            aliveByType[prefabName] = Mathf.Max(0, aliveByType[prefabName] - 1);

        totalAlive = Mathf.Max(0, totalAlive - 1);

        if (pointIndex >= 0 && pointIndex < pointStates.Count)
        {
            pointStates[pointIndex].occupied = false;
            // El cooldown ya fue seteado en el momento del spawn; aquí no lo tocamos
        }

        if (logSpawns)
            Debug.Log($"[PowerUpSpawner] Pickup destroyed: {prefabName}. TotalAlive={totalAlive}");
    }

    // Tracker auxiliar para detectar destrucción de instancias spawneadas
    private class SpawnedPickupTracker : MonoBehaviour
    {
        private PowerUpSpawnerManager owner;
        private string prefabName;
        private int pointIndex;

        public void Init(PowerUpSpawnerManager owner, string prefabName, int pointIndex)
        {
            this.owner = owner;
            this.prefabName = prefabName;
            this.pointIndex = pointIndex;
        }

        private void OnDestroy()
        {
            if (owner != null && PhotonNetwork.IsMasterClient)
            {
                owner.OnPickupDestroyed(prefabName, pointIndex);
            }
        }
    }

    // Inspector helpers
#if UNITY_EDITOR
    private void OnValidate()
    {
        foreach (var e in powerUpTypes)
            e?.OnValidatePrefabName();
    }
#endif
}