using Photon.Pun;
using UnityEngine;

public class GrenadePickup : PowerUpBase
{
    public override void ApplyEffect(PlayerControllerNewInput player)
    {
        var pv = player.GetComponent<PhotonView>();
        if (pv == null) return;

        // No permitir más de una granada por jugador
        var controller = player;
        if (controller.HasGrenade)
        {
            // Ya tiene granada: no hacemos nada (y o bien no destruimos el pickup, o podrías decidir destruir igual)
            // Aquí elegimos NO destruir para que otro jugador la pueda tomar.
            collected = false; // revertir bandera por si la base lo usó
            return;
        }

        pv.RPC("RPC_SetHasGrenade", RpcTarget.All, true);
    }
}