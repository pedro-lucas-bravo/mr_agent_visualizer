using Microsoft.MixedReality.Toolkit.Utilities.Solvers;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class AgentDirectionalIndicator : MonoBehaviour
{
    public DirectionalIndicator indicator;
    public Renderer indicatorRender;
    public TextMeshPro textNumber;

    public void InitializeFeedback(Transform agentTarget, Color color, int agentNumber) {
        indicator.DirectionalTarget = agentTarget;
        indicatorRender.material.color = color;
        textNumber.text = agentNumber + "";
        transCam_ = Camera.main.transform;
    }

    private void Update() {
        textNumber.transform.rotation = Quaternion.LookRotation(transCam_.forward, transCam_.up);
    }

    Transform transCam_;
}
