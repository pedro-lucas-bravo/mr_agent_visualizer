using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AgentDataManager : MonoBehaviour
{

    //public OSC osc;
    public string addressToSend;
    public string addressToReceive;
    public static AgentDataManager Instance;
    private void Awake() {
        Instance = this;
    }

    void Start() {
        //Sender message
        //message_ = new OscMessage();
        //message_.address = addressToSend;
        //message_.values.Add(0f);

        //Receivers
        //osc.SetAddressHandler(addressToReceive, OnReceive);
    }

    public void SelectAgent() {
        //message_.values[0] = 1.0f;
        //osc.Send(message_);
        Debug.LogWarning("OSC SEND");
    }

    //private void OnReceive(OscMessage oscM) {
    //    Debug.Log("OSC: " + oscM.GetInt(0));
    //}

    //OscMessage message_;
}
