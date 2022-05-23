using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotatingWall : MonoBehaviour
{
    public GameObject Wall;
    public Transform Base;
    private Quaternion initialRotation;

    // Start is called before the first frame update
    void Start()
    {
        initialRotation = Base.rotation;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        transform.rotation = initialRotation;
        //if (Quaternion.Angle(transform.rotation, Base.rotation) > 60f && Quaternion.Angle(transform.rotation, Base.rotation) < 120f)
        //{
        //    Wall.SetActive(false);
        //}
        //else
        //{
        //    Wall.SetActive(true);
        //}
    }
}
