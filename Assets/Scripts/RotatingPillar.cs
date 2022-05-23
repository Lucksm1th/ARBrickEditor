using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotatingPillar : MonoBehaviour
{
    public bool Active = false;
    public int CorrectAngle = 90;
    public bool Incremental = false;


    // Start is called before the first frame update
    void Start()
    {
        Incremental = true;
        Active = true;
    }

    public void SetState(bool state)
    {
        Active = state;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (TrackImagesInfo.Instance.imageTrans == null)
            return;

        if (TrackImagesInfo.Instance.imageTrackingState == UnityEngine.XR.ARSubsystems.TrackingState.None)
            return;

        Vector3 forward = TrackImagesInfo.Instance.imageTrans.forward;
        if (transform.forward == forward)
            return;

        Vector3 relativePos = forward - transform.position;

        // the second argument, upwards, defaults to Vector3.up
        Quaternion rotation = Quaternion.LookRotation(relativePos, Vector3.up);
        rotation.eulerAngles = new Vector3(0, rotation.eulerAngles.y, 0);

        transform.rotation = rotation;
    }
}
