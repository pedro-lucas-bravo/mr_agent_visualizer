using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TransFollower : MonoBehaviour
{
    public Transform target;

    private void Awake() {
        trans_ = transform;
    }

    // Update is called once per frame
    void Update()
    {
        trans_.position = target.position;
        trans_.rotation = target.rotation;
    }

    Transform trans_;
}
