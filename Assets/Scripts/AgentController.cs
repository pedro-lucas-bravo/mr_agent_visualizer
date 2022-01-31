using HoloToolkit.Unity.InputModule;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AgentController : MonoBehaviour, IFocusable, IInputClickHandler {

    public enum State { Locked, Released}

    public int Id;
    public Transform trans;
    public Renderer render;
    public Color onLockedColor;
    public Color onReleasedColor;
    public Color onFocusColor;
    public float smoothTime = 0.15f;

    public State state { get; private set; }

    void Start() {
        AgentDataManager.Instance.Agents.Add(Id, this);
        lastPosition_ = trans.position;
        state = State.Locked;
    }

    public void OnFocusEnter() {
        render.material.color = onFocusColor;
    }

    public void OnFocusExit() {
        switch (state) {
            case State.Locked:
                ApplyLockedFeedback();
                break;
            case State.Released:
                ApplyReleasedFeedback();
                break;
            default:
                break;
        }
    }

    public void OnInputClicked(InputClickedEventData eventData) {
        AgentDataManager.Instance.SelectAgent(this);
    }

    #region Data to set

    public void SetPosition(Vector3 position) {
        lastPosition_ = position;
    }

    public void SetState(State newState) {
        state = newState;
        switch (newState) {
            case State.Locked:
                ApplyLockedFeedback();
                break;
            case State.Released:
                ApplyReleasedFeedback();
                break;
            default:
                break;
        }
    }

    #endregion

    #region Feedback

    void ApplyLockedFeedback() {
        render.material.color = onLockedColor;
    }

    void ApplyReleasedFeedback() {
        render.material.color = onReleasedColor;
    }

    #endregion

    void Update() {
        var vel = Vector3.zero;
        trans.position = Vector3.SmoothDamp(trans.position, lastPosition_, ref vel, smoothTime);
    }

    private Vector3 lastPosition_;    
}
