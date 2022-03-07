using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttachedObject : MonoBehaviour
{
    public float distanceToHead = 2f;
    public float smoothTime = 0.15f;

    private void Awake() {
        trans_ = transform;
        cameraTrans_ = Camera.main.transform;
    }

    private void Update() {
        var targetPosition = cameraTrans_.position + cameraTrans_.forward * distanceToHead;
        var vel = Vector3.zero;
        trans_.position = Vector3.SmoothDamp(trans_.position, targetPosition, ref vel, smoothTime);
        trans_.forward = cameraTrans_.forward;
        //trans_.up = cameraTrans_.up;
    }

    Transform trans_;
    Transform cameraTrans_;
}
