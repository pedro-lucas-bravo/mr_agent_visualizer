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
    public float smoothTime = 0.15f;

    void Start() {
        AgentDataManager.Instance.Agents.Add(Id, this);
        lastPosition_ = trans.position;
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
        lastPosition_ = position;
    }

    #endregion

    void Update() {
        var vel = Vector3.zero;
        trans.position = Vector3.SmoothDamp(trans.position, lastPosition_, ref vel, smoothTime);
    }

    private Vector3 lastPosition_;
}
