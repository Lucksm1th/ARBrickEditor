using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DirectionIndicator : MonoBehaviour
{
    public GameObject player;
    public Vector3 offset = Vector3.zero;

    private void Update()
    {
        transform.position = player.transform.position + offset;
        transform.rotation = Quaternion.Euler(90, Camera.main.transform.eulerAngles.y, 0);
    }
}
