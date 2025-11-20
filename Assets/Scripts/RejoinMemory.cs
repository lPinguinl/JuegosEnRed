using UnityEngine;

public static class RejoinMemory
{
    private const string KeyRoom = "LastRoom";
    private const string KeyWantRejoin = "WantRejoin";

    public static string LastRoom
    {
        get => PlayerPrefs.GetString(KeyRoom, "");
        set => PlayerPrefs.SetString(KeyRoom, value ?? "");
    }

    // Marca intención de reingresar a la última sala al reconectar al Master
    public static bool WantRejoin
    {
        get => PlayerPrefs.GetInt(KeyWantRejoin, 0) == 1;
        set => PlayerPrefs.SetInt(KeyWantRejoin, value ? 1 : 0);
    }

    public static void CaptureIfInRoom(string roomName)
    {
        if (!string.IsNullOrEmpty(roomName))
        {
            LastRoom = roomName;
            WantRejoin = true;
        }
    }

    // Llamar cuando el usuario sale voluntariamente al menú o se detecta que la sala no existe
    public static void ClearIntent()
    {
        WantRejoin = false;
    }
}