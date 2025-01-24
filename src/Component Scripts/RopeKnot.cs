using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RopeKnot : MonoBehaviour
{
    private void OnEnable()
    {
        RotateRandomly();
    }

    public void RotateRandomly()
    {
        transform.localEulerAngles = new Vector3(0f, 0f, Random.Range(0, 3) * 90f);
    }
}