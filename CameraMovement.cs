using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;

public class CameraMovement : MonoBehaviour
{
    GameObject camera;
    Vector3 posBeforeMove;
    Vector3 targetPos;
    Quaternion rotBeforeMove;
    Quaternion targetRot;
    float timeStartedMove;

    // Start is called before the first frame update
    void Start()
    {
        camera = this.gameObject;
        posBeforeMove = camera.transform.position;
        rotBeforeMove = camera.transform.rotation;
        targetPos = posBeforeMove;
        targetRot = rotBeforeMove;

    }

    void Update()
    {
        SmoothMove();
        SmoothRotate();
    }

    public void MoveTo(GameObject place)
    {
        posBeforeMove = camera.transform.position;
        targetPos = place.transform.position;
        rotBeforeMove = camera.transform.rotation;
        targetRot = place.transform.rotation;
        timeStartedMove = Time.time;

    }

    void SmoothMove()
    {
        camera.transform.position = Vector3.Lerp(posBeforeMove, targetPos, Time.time - timeStartedMove);
    }

    void SmoothRotate()
    {
        camera.transform.rotation = Quaternion.Lerp(rotBeforeMove, targetRot, Time.time - timeStartedMove);
    }
}
