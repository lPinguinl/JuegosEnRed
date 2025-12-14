using UnityEngine;
using Unity.Services.Analytics;
using Unity.Services.Core;


public class GameBootstrap : MonoBehaviour
{
    private void Awake()
    {
        UnityServices.InitializeAsync();
        DontDestroyOnLoad(gameObject);
    }
}