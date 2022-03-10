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
    //public string selectOutState = "/agent/state";

    [Header("Addresses to receive")]
    public string instanceAgentAddress = "/agent/instance";    
    //public string sensorPositionInAddress = "/sensor/position";
    public string agentsInfoAddress = "/agents";
    public string noteInfoAddress = "/note";
    public string volumeInfoAddress = "/volume";

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
        //selectOutStateMessage_ = osc.DefineMessageToClient(selectOutState, 2);

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

        if (address == noteInfoAddress && !noteFlag_) {
            agentNoteId_ = (int)values[0];
            noteFlag_ = true;
        }

        if (address == volumeInfoAddress && !volumeFlag_) {
            agentVolumeId_ = (int)values[0];
            try {
                volumeValue_ = (float)values[1];
            } catch {
                volumeValue_ = (int)values[1];
            }
            volumeFlag_ = true;
        }

        if (address == agentsInfoAddress && !agentInfosFlag_) {//Agents info, position and musical data
            try {
                var incomingId = (int)values[0];
                if (incomingId != agentSensorPos_.Id) {
                    previousAgentSensorPos_.Set(agentSensorPos_);
                }
                agentSensorPos_.Id = incomingId;
                agentSensorPos_.state = (AgentController.State)values[1];
                agentSensorPos_.position = new Vector3((int)values[2], (int)values[4], (int)values[3]) / 1000.0f;
                agentInfosSize_ = (int)values[5];
                for (int i = 0; i < agentInfosSize_; i++) {
                    agentInfos_[i].Id = (int)values[i * 4 + 6];
                    agentInfos_[i].state = AgentController.State.Released;
                    agentInfos_[i].position = new Vector3((int)values[i * 4 + 7], (int)values[i * 4 + 9], (int)values[i * 4 + 8]) / 1000.0f;
                }
                agentInfosFlag_ = true;
            } catch (Exception) {//In case any conversion goes wrong because of malformed data from network or something
                agentInfosFlag_ = false;
            }
        }
    }

    List<object> instanceAgentInfo_;
    bool instantiateAgentsFlag_ = false;
    void InstantiateAgents() {
        if (!instantiateAgentsFlag_) return;
        RemoveAllAgents();
        var agentsSize = (int)instanceAgentInfo_[0];
        for (int i = 0; i < agentsSize; i++) {
            var state = (int)instanceAgentInfo_[i * 3 + 1];
            var colorHex = (string)instanceAgentInfo_[i * 3 + 2];
            ColorUtility.TryParseHtmlString("#" + colorHex, out Color color);
            float volume = 0.5f;
            try {
                volume = (float)instanceAgentInfo_[i * 3 + 3];
            } catch {
                volume = (int)instanceAgentInfo_[i * 3 + 3];
            }
            var newAgent = Instantiate(agentPrefab);
            newAgent.transform.SetParent(agentParent, true);
            newAgent.SetId(i);
            newAgent.SetStateFromInt(state);
            newAgent.SetColor(color);
            newAgent.SetShellSize(volume);
            newAgent.gameObject.SetActive(false);
            Agents.Add(i, newAgent);
        }
        instantiateAgentsFlag_ = false;
    }

    AgentInfo agentSensorPos_ = new AgentInfo();
    AgentInfo previousAgentSensorPos_ = new AgentInfo();
    AgentInfo[] agentInfos_;
    int agentInfosSize_ = 0;
    bool agentInfosFlag_ = false;
    void SetAgentsInfo() {
        if (!agentInfosFlag_) return;
        //Set sensor position
        if (Agents.TryGetValue(agentSensorPos_.Id, out var agentPosition)) {
            if (previousAgentSensorPos_.Id != agentSensorPos_.Id && 
                previousAgentSensorPos_.state == AgentController.State.Empty &&
                Agents.TryGetValue(previousAgentSensorPos_.Id, out var previousAgentPosition) &&
                previousAgentPosition.gameObject.activeSelf) {
                previousAgentPosition.gameObject.SetActive(false);
            }
            agentPosition.SetState(agentSensorPos_.state);

            if (!agentPosition.gameObject.activeSelf)
                agentPosition.gameObject.SetActive(true);            
            agentPosition.SetPosition(agentSensorPos_.position);
        }

        //Set agent infos
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

    int agentNoteId_ = -1;
    bool noteFlag_;
    void SetAgentNote() {
        if (!noteFlag_) return;
        if (Agents.TryGetValue(agentNoteId_, out var agent)) {
            agent.Beat();
        }
        noteFlag_ = false;
    }

    int agentVolumeId_ = -1;
    float volumeValue_ = 0;
    bool volumeFlag_;
    void SetAgentVolume() {
        if (!volumeFlag_) return;
        if (Agents.TryGetValue(agentVolumeId_, out var agent)) {
            agent.SetShellSize(volumeValue_);
        }
        volumeFlag_ = false;
    }

    public void SelectAgent(AgentController agent) {

        //Change state accordingly ->UPDATE: State is not changed because the max patch
        //                          is the one that change and the new state is sent through 
        //                          continuous data.
        //switch (agent.state) {
        //    case AgentController.State.Locked:
        //        agent.SetState(AgentController.State.Released);
        //        break;
        //    case AgentController.State.Released:
        //        agent.SetState(AgentController.State.Locked);
        //        break;
        //    default:
        //        break;
        //}
        
        //Send Message
        selectOutMessage_[0] = agent.Id;
        //selectOutStateMessage_[1] = (int)agent.state;
        osc.SendMessageToClient(selectOutAddress);
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
        //SetSensorPosition();
        SetAgentsInfo();
        SetAgentNote();
        SetAgentVolume();

        //if (Input.GetKeyDown(KeyCode.Alpha1))
        //    SelectAgent(Agents[0]);
        //if (Input.GetKeyDown(KeyCode.Alpha2))
        //    SelectAgent(Agents[1]);
        //if (Input.GetKeyDown(KeyCode.Alpha3))
        //    SelectAgent(Agents[2]);
        //if (Input.GetKeyDown(KeyCode.Alpha4))
        //    SelectAgent(Agents[3]);

    }

    List<object> selectOutMessage_;

    public class AgentInfo {
        public int Id;
        public AgentController.State state;
        public Vector3 position;

        public void Set(AgentInfo other) {
            Id = other.Id;
            state = other.state;
            position = other.position;
        }
    }
}
