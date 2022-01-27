using HoloToolkit.Unity.InputModule;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AgentController : MonoBehaviour, IFocusable, IInputClickHandler {
    
    public int Id;
    public Transform trans;
    public Renderer render;
    public Color onNormalColor;
    public Color onFocusColor;

    void Start() {
        AgentDataManager.Instance.Agents.Add(Id, this);
    }

    public void OnFocusEnter() {
        render.material.color = onFocusColor;
    }

    public void OnFocusExit() {
        render.material.color = onNormalColor;
    }

    public void OnInputClicked(InputClickedEventData eventData) {
        AgentDataManager.Instance.SelectAgent(Id);
    }

    #region Data to set

    public void SetPosition(Vector3 position) {
        trans.position = position;
    }

    #endregion
}
