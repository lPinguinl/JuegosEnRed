using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WalkableGround : MonoBehaviour, IWalkableSurface
{
    [SerializeField] private bool walkable = true;

    public bool IsWalkable() => walkable;
}