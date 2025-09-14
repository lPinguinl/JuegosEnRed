using UnityEngine;

public interface IStunable
{
    void Stun(Vector3 attackerPosition);
    bool IsStunned();
}