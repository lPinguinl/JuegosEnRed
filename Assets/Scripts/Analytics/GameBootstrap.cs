using UnityEngine;
using Unity.Services.Core;

public class GameBootstrap : MonoBehaviour
{
    private async void Awake()
    {
        DontDestroyOnLoad(gameObject);
        await UnityServices.InitializeAsync();
    }
}