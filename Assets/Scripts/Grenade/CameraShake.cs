using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraShake : MonoBehaviour
{
    private Vector3 originalPos;
    private float duration;
    private float magnitude = 0.25f;

    private void Start()
    {
        originalPos = transform.localPosition;
    }

    private void Update()
    {
        if (duration > 0f)
        {
            transform.localPosition = originalPos + Random.insideUnitSphere * magnitude;
            duration -= Time.deltaTime;
        }
        else
        {
            transform.localPosition = originalPos;
        }
    }

    public void Shake(float time = 0.25f)
    {
        duration = time;
    }
}
