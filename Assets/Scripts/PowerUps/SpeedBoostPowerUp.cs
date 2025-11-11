using Photon.Pun;
using UnityEngine;

public class SpeedBoostPowerUp : PowerUpBase
{
    [SerializeField] private float multiplier = 2.0f;
    [SerializeField] private float duration = 2.0f;

    public override void ApplyEffect(PlayerControllerNewInput player)
    {
        var pv = player.GetComponent<PhotonView>();
        if (pv != null)
        {
            pv.RPC("RPC_ActivateSpeedBoost", RpcTarget.All, multiplier, duration);
        }
    }
}