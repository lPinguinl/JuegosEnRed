using Photon.Pun;
using UnityEngine;

public class ShieldPowerUp : PowerUpBase
{
    public override void ApplyEffect(PlayerControllerNewInput player)
    {
        var pv = player.GetComponent<PhotonView>();
        if (pv != null)
        {
            pv.RPC("RPC_SetShield", RpcTarget.All, true);
        }
    }
}