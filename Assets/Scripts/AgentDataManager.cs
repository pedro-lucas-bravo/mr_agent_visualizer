using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using TMPro;
using UnityOSC;

public class AgentDataManager : MonoBehaviour
{    
    public OscManager osc;

    [Header("Addresses to send")]
    public string selectOutAddress = "/agent/select";
    public string gazeDataAddress = "/gaze";
    public string gazeDirectionAddress = "/gazedir";
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
    public float gazePeriodSending = 1f;
    public Transform worldReference;

    [Header("Testing")]
    public TextMeshPro counterPkgText;

    public Dictionary<int, AgentController> Agents { get; set; }
    public class DataTime { 
        public int startTime;
        public int length;
        public int frameLength;

        public DataTime(int sT) {
            startTime = sT;
            length = 0;
            frameLength = 0;
        }
    }
    public Dictionary<string, DataTime> processTimeDic = new Dictionary<string, DataTime>();

    public static AgentDataManager Instance;
    private void Awake() {
        Instance = this;
        Agents = new Dictionary<int, AgentController>();
        agentInfos_ = new AgentInfo[128];
        for (int i = 0; i < agentInfos_.Length; i++) {
            agentInfos_[i] = new AgentInfo();
        }
        transCam_ = Camera.main.transform;
        startFrameTime = DateTime.Now.Millisecond;
    }

    void Start() {
        //Sender message
        selectOutMessage_ = osc.DefineMessageToClient(selectOutAddress, 1);
        gazeDataMessage_ = osc.DefineMessageToClient(gazeDataAddress, 5);
        gazeDirectionMessage_ = osc.DefineMessageToClient(gazeDirectionAddress, 6);        

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
    private void OnReceive(string address, List<object> values, OSCPacket packet) {
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

        //if (address == agentsInfoAddress && !agentInfosFlag_) {//Agents info, position and musical data
        if (address.Contains(agentsInfoAddress) && !agentInfosFlag_) {//Agents info, position and musical data
                                                                      //try {
            if (processTime) {
                if(!processTimeDic.ContainsKey(address))
                    processTimeDic.Add(address, new DataTime(DateTime.Now.Millisecond));
                else
                    processTimeDic[address] = new DataTime(DateTime.Now.Millisecond);
                lastAgentInfosAddress_ = address;
            }
                var incomingId = (int)values[0];
                if (incomingId != agentSensorPos_.Id) {
                    previousAgentSensorPos_.Set(agentSensorPos_);
                }
                agentSensorPos_.Id = incomingId;
                agentSensorPos_.state = (AgentController.State)values[1];
                agentSensorPos_.position = new Vector3(Convert.ToInt32(values[2]), Convert.ToInt32(values[4]), Convert.ToInt32(values[3])) / 1000.0f;
                agentInfosSize_ = (int)values[5];
                for (int i = 0; i < agentInfosSize_; i++) {
                    agentInfos_[i].Id = (int)values[i * 4 + 6];
                    agentInfos_[i].state = AgentController.State.Released;                
                    agentInfos_[i].position = new Vector3(Convert.ToInt32(values[i * 4 + 7]), Convert.ToInt32(values[i * 4 + 9]), Convert.ToInt32(values[i * 4 + 8])) / 1000.0f;
                }
                agentInfosFlag_ = true;
            //TEST - FOR ROUND TRIP
                if (roundTrip) {
                    roundTripAgentsMessage_ = osc.DefineMessageToClient(address, values.Count);
                    for (int i = 0; i < values.Count; i++) {
                        roundTripAgentsMessage_[i] = values[i];
                    }
                    osc.SendMessageToClient(address);
                }
            if (countPkg) {
                counterPkg++;
                sizePkgAcc += packet.BinaryData.Length;
            }            
            //} catch (Exception) {//In case any conversion goes wrong because of malformed data from network or something
            //    agentInfosFlag_ = false;
            //}
        }

        if (address == "/pkg0") {
            countPkg = true;            
        }

        if (address == "/pkg1") {
            countPkg = false;
        }

        if (address == "roundtrip0") {
            roundTrip = true;
        }

        if (address == "roundtrip1") {
            roundTrip = false;
        }

        if (address == "processtime0") {
            processTime = true;
            processTimeDic.Clear();
        }

        if (address == "processtime1") {
            processTime = false;
            sendProcessResult = true;
        }

        if (address == packetsAddress) {
            packetsRequest = true;
        }
    }

    bool roundTrip = false;
    bool processTime = false;
    bool sendProcessResult = false;
    bool packetsRequest = false;

    bool countPkg = false;
    int counterPkg = 0;
    int lastCounterPkg = 0;
    int sizePkgAcc = 0;
    float lastSizePkg = 0;

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
            newAgent.CreateIndicator();
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
    string lastAgentInfosAddress_ = "";

    string packetsAddress = "/packets";

    int startFrameTime = 0;
    int frameLength = 0;
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
        
        if (processTime) {
            if (processTimeDic.ContainsKey(lastAgentInfosAddress_)) {
                processTimeDic[lastAgentInfosAddress_].length = DateTime.Now.Millisecond - processTimeDic[lastAgentInfosAddress_].startTime;
                processTimeDic[lastAgentInfosAddress_].frameLength = frameLength;
            }
        }        
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

    public void SendGazeData() {
        if (OSCHandler.Instance.Clients.Any()) {
            //if (Agents.ContainsKey(agentSensorPos_.Id)) {
            //    var distance = Vector3.Distance(agentSensorPos_.position, transCam_.position);
            //    var angle = Vector3.Angle(agentSensorPos_.position - transCam_.position, transCam_.forward/*, Vector3.up*/);
            //    gazeDataMessage_[0] = (int)(distance * 1000);
            //    gazeDataMessage_[1] = (int)angle;
            //    gazeDataMessage_[2] = (int)(agentSensorPos_.position.x * 1000);
            //    gazeDataMessage_[3] = (int)(agentSensorPos_.position.z * 1000);//Change z by y because of max/msp convention
            //    gazeDataMessage_[4] = (int)(agentSensorPos_.position.y * 1000);
            //    osc.SendMessageToClient(gazeDataAddress);
            //}
            var camPos = worldReference.InverseTransformPoint(transCam_.position) * worldReference.localScale.x;
            var camDir = worldReference.InverseTransformVector(transCam_.forward).normalized;
            gazeDirectionMessage_[0] = Mathf.RoundToInt(camPos.x * 1000);
            gazeDirectionMessage_[1] = Mathf.RoundToInt(camPos.z * 1000);//Change z by y because of max/msp convention
            gazeDirectionMessage_[2] = Mathf.RoundToInt(camPos.y * 1000);
            gazeDirectionMessage_[3] = Mathf.RoundToInt(camDir.x * 1000);
            gazeDirectionMessage_[4] = Mathf.RoundToInt(camDir.z * 1000);//Change z by y because of max/msp convention
            gazeDirectionMessage_[5] = Mathf.RoundToInt(camDir.y * 1000);
            osc.SendMessageToClient(gazeDirectionAddress);
        }
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

        timerGaze_ += Time.deltaTime;
        if (timerGaze_ > gazePeriodSending) {
            SendGazeData();
            timerGaze_ = 0;
        }

        //if (Input.GetKeyDown(KeyCode.Alpha1))
        //    SelectAgent(Agents[0]);
        //if (Input.GetKeyDown(KeyCode.Alpha2))
        //    SelectAgent(Agents[1]);
        //if (Input.GetKeyDown(KeyCode.Alpha3))
        //    SelectAgent(Agents[2]);
        //if (Input.GetKeyDown(KeyCode.Alpha4))
        //    SelectAgent(Agents[3]);

        //TEST
        if (countPkg) {
            counterPkgText.color = Color.green;
            counterPkgText.text = counterPkg + "";
        }
        if (!countPkg && counterPkg > 0) {
            counterPkgText.color = Color.red;
            counterPkgText.text = counterPkg + "";
            lastCounterPkg = counterPkg;
            lastSizePkg = (float)sizePkgAcc / (float)counterPkg;
            counterPkg = 0;
            sizePkgAcc = 0;
        }

        var currentTime = DateTime.Now.Millisecond;
        frameLength = currentTime - startFrameTime;
        startFrameTime = currentTime;

        if (sendProcessResult) {
            StartCoroutine(SendProcessResult());
            sendProcessResult = false;
        }

        if (!countPkg && packetsRequest) {
            var pkgMsg = osc.DefineMessageToClient(packetsAddress, 2);
            pkgMsg[0] = lastCounterPkg;
            pkgMsg[1] = lastSizePkg;
            osc.SendMessageToClient(packetsAddress);
            packetsRequest = false;
        }
    }

    IEnumerator SendProcessResult() {
        foreach(var item in processTimeDic) {
            var processMsgs = osc.DefineMessageToClient(item.Key, 2);
            for (int i = 0; i < 3; i++) {
                processMsgs[0] = item.Value.length;
                processMsgs[1] = item.Value.frameLength;
            }
            osc.SendMessageToClient(item.Key);
            yield return new WaitForSeconds(0.05f);
        }
    }

    List<object> selectOutMessage_;
    List<object> gazeDataMessage_;
    List<object> gazeDirectionMessage_;
    List<object> roundTripAgentsMessage_;
    Transform transCam_;
    float timerGaze_ = 0;

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
