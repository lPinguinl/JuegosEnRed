using LootLocker.Requests;
using UnityEngine;

public class LootLockerBootstrap : MonoBehaviour
{
    public static bool SessionStarted { get; private set; }
    public static event System.Action OnSessionStarted;

    [Header("Identificador de jugador")]
    [Tooltip("Si está activo, usa el deviceUniqueIdentifier como memberId. Si no, usa playerIdentifier.")]
    [SerializeField] bool useDeviceId = true;

    [Tooltip("Solo se usa si useDeviceId = false")]
    [SerializeField] string playerIdentifier = "player-1";

    void Awake()
    {
        DontDestroyOnLoad(gameObject);
        StartGuest();
    }

    void StartGuest()
    {
        // Elige el identificador
        var id = useDeviceId ? SystemInfo.deviceUniqueIdentifier : playerIdentifier;

        if (string.IsNullOrEmpty(id))
        {
            // Fallback por si algún dispositivo devuelve vacío
            id = System.Guid.NewGuid().ToString("N");
            PlayerPrefs.SetString("LL_FallbackId", id);
            PlayerPrefs.Save();
        }

        Debug.Log($"[LootLockerBootstrap] Iniciando guest session con id='{id}'");

        // Lanza la sesión invitado
        LootLockerSDKManager.StartGuestSession(id, response =>
        {
            if (!response.success)
            {
                // En algunas versiones existe .text; si no, solo se imprimen success y statusCode
                var maybeText = (response as object).GetType().GetProperty("text")?.GetValue(response, null);
                Debug.LogError($"Fallo al iniciar sesión. success={response.success} status={response.statusCode}" +
                               (maybeText != null ? $" text={maybeText}" : ""));
                return;
            }

            SessionStarted = true;
            Debug.Log("[LootLockerBootstrap] Conectado a LootLocker");
            OnSessionStarted?.Invoke();
        });
    }
}