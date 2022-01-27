using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AgentDataManager : MonoBehaviour
{    
    public OscManager osc;

    [Header("Addresses to send")]
    public string selectOutAddress;

    [Header("Addresses to receive")]
    public string positionInAddress;

    public Dictionary<int, AgentController> Agents { get; set; }

    public static AgentDataManager Instance;
    private void Awake() {
        Instance = this;
        Agents = new Dictionary<int, AgentController>();
    }

    void Start() {
        //Sender message
        selectOutMessage_ = osc.DefineMessageToClient(selectOutAddress, 4);

        //Receivers
        osc.OnReceiveMessage += OnReceive;
    }

    private void OnDestroy() {
        if(osc != null)
            osc.OnReceiveMessage -= OnReceive;
    }

    private void OnReceive(string address, List<object> values) {
        if (address == positionInAddress) {
            var agentId = (int)values[0];
            var position = new Vector3((int)values[1], (int)values[2], (int)values[3]) / 1000.0f;
            Debug.Log("ID1:" + (int)values[0] + positionInAddress + ": " + position.ToString("F4"));
            if (Agents.TryGetValue(agentId, out var agent)) {                
                agent.SetPosition(position);
                Debug.Log("ID3:" + (int)values[0] + positionInAddress + ": " + agent.trans.position.ToString("F4"));
            }            
        }
    }

    public void SelectAgent(int id) {
        selectOutMessage_[0] = id;
        selectOutMessage_[1] = 0.254f;
        selectOutMessage_[2] = 1.236f;
        selectOutMessage_[3] = 2.857f;
        
        osc.SendMessageToClient(selectOutAddress);
        Debug.LogWarning("OSC SEND");
    }

    List<object> selectOutMessage_;
}
