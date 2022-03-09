using HoloToolkit.Unity.InputModule;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class AgentController : MonoBehaviour, IFocusable, IInputClickHandler {

    public enum State { Empty, Locked, Released}

    public int Id;

    [Header("Components")]
    public Transform trans;
    public Renderer render;    
    public TextMeshPro textNumber;
    public TrailRenderer trail;
    public Renderer cage;
    public Renderer shell;
    public CrossBeatingBehaviour beatingBehaviour;
    
    [Header("Balance")]
    public Color color;
    public float smoothTime = 0.15f;

    private State state_;
    public State state {
        get => state_;
        private set {
            state_ = value;
            switch (state_) {
                case State.Empty:
                    ApplyFeedback(true, 0.25f);
                    break;
                case State.Locked:
                    ApplyFeedback(true, 1);
                    break;
                case State.Released:
                    ApplyFeedback(false, 1);
                    break;
                default:
                    break;
            }
        } 
    }

    void Awake() {
        transCore_ = render.transform;
        transText_ = textNumber.transform;
        transCage_ = cage.transform;
        cameraTrans_ = Camera.main.transform;

        textSeparation_ = trans.lossyScale.x * 0.5f;
        defaultScale_ = trans.localScale.x;
        defaultCoreScale_ = transCore_.localScale.x;
    }

    void Start() {
        //AgentDataManager.Instance.Agents.Add(Id, this);
        lastPosition_ = trans.position;
        state = State.Empty;
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
        if (newState == state) return;
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
        SetState((State)s);
    }

    public void SetColor(Color c) {
        color = c;
        render.material.color = color;

        var cageColor = color;
        cageColor.a = cage.material.color.a;
        cage.material.color = cageColor;

        var shellColor = color;
        shellColor.a = shell.material.color.a;
        shell.material.color = shellColor;

        var startColor = color;
        startColor.a = 0.5f;
        trail.startColor = startColor;
        trail.endColor = new Color(1,1,1,0);
    }

    public void Beat() {
        beatingBehaviour.Apply();
    }

    #endregion

    #region Feedback

    void ApplyLockedFeedback() {
        //render.material.color = onLockedColor;
    }

    void ApplyReleasedFeedback() {
        //render.material.color = onReleasedColor;
    }

    void ApplyFeedback(bool isLocked, float coreScale) {
        cage.gameObject.SetActive(isLocked);
        shell.gameObject.SetActive(!isLocked);
        transCore_.localScale = coreScale * Vector3.one;
        defaultCoreScale_ = coreScale;
    }

    #endregion

    void Update() {
        var vel = Vector3.zero;        
        trans.localPosition = Vector3.SmoothDamp(trans.localPosition, lastPosition_, ref vel, smoothTime);
        var dir = (cameraTrans_.position - trans.position).normalized;
        var deltaTime = Time.deltaTime;
        UpdateTextDirection(dir);
        UpdateCageDirection(dir);
        UpdateBeating(deltaTime);

        //Test
        //if (Input.GetKeyDown(KeyCode.A)) {
        //    beatingBehaviour.Apply();
        //}
    }

    void UpdateTextDirection(Vector3 dir) {        
        transText_.position = trans.position + dir * textSeparation_;
        transText_.forward = -dir;
    }

    void UpdateCageDirection(Vector3 dir) {
        transCage_.forward = -dir;
    }

    void UpdateBeating(float deltaTime) {
        beatingBehaviour.Update(deltaTime);
        if (beatingBehaviour.IsApplying) {
            transCore_.localScale = (defaultCoreScale_ + defaultCoreScale_ * beatingBehaviour.GetValue()) * Vector3.one;
        }
    }

    private Vector3 lastPosition_;
    private Transform transCore_;
    private Transform transText_;
    private Transform transCage_;
    private Transform cameraTrans_;
    private float textSeparation_;
    private float defaultScale_;
    private float defaultCoreScale_;
}
