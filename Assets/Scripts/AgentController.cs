using HoloToolkit.Unity.InputModule;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class AgentController : MonoBehaviour, IFocusable, IInputClickHandler {

    public enum State { Empty, Locked, Released}

    public int Id;
    public Transform trans;
    public Renderer render;
    public Color color;
    public TextMeshPro textNumber;
    //public Color onLockedColor;
    //public Color onReleasedColor;
    //public Color onFocusColor;
    public float smoothTime = 0.15f;

    public State state { get; private set; }

    void Awake() {
        transText_ = textNumber.transform;
        cameraTrans_ = Camera.main.transform;        
    }

    void Start() {
        //AgentDataManager.Instance.Agents.Add(Id, this);
        lastPosition_ = trans.position;
        //state = State.Locked;
    }

    public void OnFocusEnter() {
        //render.material.color = onFocusColor;
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

    public void SetId(int id) {
        Id = id;
        textNumber.text = "" + (Id + 1);
    }

    public void SetPosition(Vector3 position) {
        position = trans.parent != null ? position / trans.parent.localScale.x : position;
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

    public void SetStateFromInt(int s) {
        state = (State)s;
    }

    public void SetColor(Color c) {
        color = c;
        render.material.color = color;
    }

    #endregion

    #region Feedback

    void ApplyLockedFeedback() {
        //render.material.color = onLockedColor;
    }

    void ApplyReleasedFeedback() {
        //render.material.color = onReleasedColor;
    }

    #endregion

    void Update() {
        var vel = Vector3.zero;        
        trans.localPosition = Vector3.SmoothDamp(trans.localPosition, lastPosition_, ref vel, smoothTime);
        UpdateTextDirection();
    }

    void UpdateTextDirection() {
        var dir = (cameraTrans_.position - trans.position).normalized;
        transText_.position = trans.position + dir * trans.lossyScale.x * 0.5f;
        transText_.forward = -dir;
    }

    private Vector3 lastPosition_;
    private Transform transText_;
    private Transform cameraTrans_;
}
