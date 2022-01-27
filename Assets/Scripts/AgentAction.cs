using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Academy;

public class AgentAction : InteractibleAction
{
    public int Id = 1;
    public Transform trans;

    [Header("Graphics")]
    public Renderer render;
    public Color normal;
    public Color over;

    public override void PerformAction() {
        AgentDataManager.Instance.SelectAgent(Id);
    }

}
