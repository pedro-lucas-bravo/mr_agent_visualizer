using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class AgentDataManager : MonoBehaviour
{    
    public OscManager osc;

    [Header("Addresses to send")]
    public string selectOutAddress = "/agent/select";
    public string selectOutState = "/agent/state";

    [Header("Addresses to receive")]
    public string sensorPositionInAddress = "/sensor/position";
    public string positionInAddress = "/agent/position";
    public string positionInDawSelect= "/agent/daw/select";

    public Dictionary<int, AgentController> Agents { get; set; }

    public static AgentDataManager Instance;
    private void Awake() {
        Instance = this;
        Agents = new Dictionary<int, AgentController>();
    }

    void Start() {
        //Sender message
        selectOutMessage_ = osc.DefineMessageToClient(selectOutAddress, 1);
        selectOutStateMessage_ = osc.DefineMessageToClient(selectOutState, 2);

        //Receivers
        osc.OnReceiveMessage += OnReceive;
    }

    private void OnDestroy() {
        if(osc != null)
            osc.OnReceiveMessage -= OnReceive;
    }

    private void OnReceive(string address, List<object> values) {
        if (address == sensorPositionInAddress) {//Position from mocap for locked agent
            var position = new Vector3((int)values[0], (int)values[1], (int)values[2]) / 1000.0f;
            var lockedAgent = Agents.FirstOrDefault(a => a.Value.state == AgentController.State.Locked);
            if (lockedAgent.Value != null) {
                var agent = lockedAgent.Value;
                agent.SetPosition(position);
            }
        }

        if (address == positionInAddress) {//Position for released agent from controller
            var agentId = (int)values[0];
            var position = new Vector3((int)values[1], (int)values[2], (int)values[3]) / 1000.0f;
            if (Agents.TryGetValue(agentId, out var agent) && agent.state == AgentController.State.Released) {                
                agent.SetPosition(position);
                //Debug.Log("ID:" + (int)values[0] + positionInAddress + ": " + agent.trans.position.ToString("F4"));
            }        
        }       
    }

    public void SelectAgent(AgentController agent) {

        //Change state accordingly
        switch (agent.state) {
            case AgentController.State.Locked:
                agent.SetState(AgentController.State.Released);
                break;
            case AgentController.State.Released:
                agent.SetState(AgentController.State.Locked);
                break;
            default:
                break;
        }
        
        //Send Message
        selectOutStateMessage_[0] = agent.Id;
        selectOutStateMessage_[1] = (int)agent.state;
        osc.SendMessageToClient(selectOutState);
        //Debug.LogWarning("OSC SEND");

        //TO DO: Implement confirmation mesages startegy
    }

    private void Update() {
        if (Input.GetKeyDown(KeyCode.S)) {
            SelectAgent(Agents[1]);
        }
    }

    List<object> selectOutMessage_;
    List<object> selectOutStateMessage_;
}
