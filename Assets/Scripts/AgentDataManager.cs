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
    public string instanceAgentAddress = "/agent/instance";    
    public string sensorPositionInAddress = "/sensor/position";
    public string agentsInfoAddress = "/agents";

    [Header("Agent Parameters")]
    public AgentController agentPrefab;
    public Transform agentParent;

    public Dictionary<int, AgentController> Agents { get; set; }

    public static AgentDataManager Instance;
    private void Awake() {
        Instance = this;
        Agents = new Dictionary<int, AgentController>();
        agentInfos_ = new AgentInfo[128];
        for (int i = 0; i < agentInfos_.Length; i++) {
            agentInfos_[i] = new AgentInfo();
        }
    }

    void Start() {
        //Sender message
        selectOutMessage_ = osc.DefineMessageToClient(selectOutAddress, 1);
        selectOutStateMessage_ = osc.DefineMessageToClient(selectOutState, 2);

        //Receivers        
        osc.OnReceiveMessage += OnReceive;        
    }

    private void OnDestroy() {
        if (osc != null) {            
            osc.OnReceiveMessage -= OnReceive;
        }
    }

    //It receives in another thread, that is why it needs to fill non-unity objects to do modifications in main thread
    private void OnReceive(string address, List<object> values) {
        if (address == instanceAgentAddress && !instantiateAgentsFlag_) {//Instance new agents by removing the old ones first
            instanceAgentInfo_ = new List<object>(values);
            instantiateAgentsFlag_ = true;
        }

        if (address == sensorPositionInAddress && !sensorPositionFlag_) {//Position from mocap for locked agent
            agentIdSensorPosition_ = (int)values[0];
            sensorPosition_ = new Vector3((int)values[1], (int)values[3], (int)values[2]) / 1000.0f;
            sensorPositionFlag_ = true;
        }

        if (address == agentsInfoAddress && !agentInfosFlag_) {//Agents info, position and musical data
            agentInfosSize_ = (int)values[0];
            for (int i = 0; i < agentInfosSize_; i++) {
                agentInfos_[i].Id = (int)values[i * 4 + 1];
                agentInfos_[i].position = new Vector3((int)values[i * 4 + 2], (int)values[i * 4 + 4], (int)values[i * 4 + 3]) / 1000.0f;
            }
            agentInfosFlag_ = true;
        }
    }

    List<object> instanceAgentInfo_;
    bool instantiateAgentsFlag_ = false;
    void InstantiateAgents() {
        if (!instantiateAgentsFlag_) return;
        RemoveAllAgents();
        var agentsSize = (int)instanceAgentInfo_[0];
        for (int i = 0; i < agentsSize; i++) {
            var state = (int)instanceAgentInfo_[i * 2 + 1];
            var colorHex = (string)instanceAgentInfo_[i * 2 + 2];
            ColorUtility.TryParseHtmlString("#" + colorHex, out Color color);

            var newAgent = Instantiate(agentPrefab);
            newAgent.transform.SetParent(agentParent, true);
            newAgent.Id = i;
            newAgent.SetStateFromInt(state);
            newAgent.SetColor(color);
            newAgent.gameObject.SetActive(false);
            Agents.Add(i, newAgent);
        }
        instantiateAgentsFlag_ = false;
    }

    int agentIdSensorPosition_ = -1;
    Vector3 sensorPosition_;
    bool sensorPositionFlag_ = false;
    void SetSensorPosition() {
        if (!sensorPositionFlag_) return;
        var agentId = agentIdSensorPosition_;
        var position = sensorPosition_;
        if (Agents.TryGetValue(agentId, out var agent)) {
            if (!agent.gameObject.activeSelf)
                agent.gameObject.SetActive(true);
            agent.SetState(AgentController.State.Locked);
            agent.SetPosition(position);
        }
        sensorPositionFlag_ = false;
    }

    AgentInfo[] agentInfos_;
    int agentInfosSize_ = 0;
    bool agentInfosFlag_ = false;
    void SetAgentsInfo() {
        if (!agentInfosFlag_) return;
        for (int i = 0; i < agentInfosSize_; i++) {
            if (Agents.TryGetValue(agentInfos_[i].Id, out var agent)) {
                if (!agent.gameObject.activeSelf)
                    agent.gameObject.SetActive(true);
                agent.SetState(AgentController.State.Released);
                agent.SetPosition(agentInfos_[i].position);
                //Debug.Log("ID:" + (int)values[0] + positionInAddress + ": " + agent.trans.position.ToString("F4"));
            }
        }        
        agentInfosFlag_ = false;
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

    private void RemoveAllAgents() {
        foreach (var agent in Agents) {
            Destroy(agent.Value.gameObject);
        }
        Agents.Clear();
    }

    private void Update() {

        InstantiateAgents();
        SetSensorPosition();
        SetAgentsInfo();

        if (Input.GetKeyDown(KeyCode.S)) {
            SelectAgent(Agents[1]);
        }
    }

    List<object> selectOutMessage_;
    List<object> selectOutStateMessage_;

    public class AgentInfo {
        public int Id;
        public Vector3 position;
    }
}
