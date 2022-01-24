using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Academy;

public class AgentAction : InteractibleAction
{
    public override void PerformAction() {
        AgentDataManager.Instance.SelectAgent();
    }

}
