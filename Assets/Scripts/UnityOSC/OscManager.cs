using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityOSC;
using System.Linq;
using System.Net;

public class OscManager : MonoBehaviour {

	public string Id = "D0";
    public string TargetAddr;
    public int OutGoingPort;
    public int InComingPort;

	private Dictionary<string, List<object>> messagesToSend;

	public Action<string, List<object>> OnReceiveMessage;//oscaddress, values
    public Action<string, List<object>> OnReceiveMessageOnMainThread;//oscaddress, values

    private bool wasReceived_ = false;
	private string lastAddress_;
	private List<object> lastData_;

    public static readonly string CONNECT_ADDRESS = "/connect";

    // Script initialization
    void Awake() {
        //OSCHandler.Instance.Init(Id, TargetAddr, OutGoingPort,InComingPort);
        //servers = new Dictionary<string, ServerLog>();

        //Create receiver
        OSCHandler.Instance.CreateServer(Id, InComingPort);

        messagesToSend = new Dictionary<string, List<object>>();
		OSCHandler.Instance.OnReceiveMessage += OnReceive;
	}

    private void OnReceive(string address, List<object> data) {
        wasReceived_ = true;//The flag strategy avoids to execute unity code in a different thread
        lastAddress_ = address;
        lastData_ = data;
        if (lastAddress_ == CONNECT_ADDRESS)
            ConnectClient();
        if (OnReceiveMessage != null) {
            OnReceiveMessage(lastAddress_, lastData_);
        }
    }

    private void OnDestroy() {
		if (OSCHandler.Instance != null)
			OSCHandler.Instance.OnReceiveMessage -= OnReceive;
    }


    /// <summary>
    /// Create the object for values, it has to be filled with the data before send
    /// </summary>
    /// <param name="oscAddress"></param>
    /// <param name="numberOfValues"></param>
    /// <returns></returns>
    public List<object> DefineMessageToClient(string oscAddress, int numberOfValues) {
		return DefineMessage(messagesToSend, oscAddress, numberOfValues);
	}

	private List<object> DefineMessage(Dictionary<string, List<object>> messages, string oscAddress, int numberOfValues) {
		if (!messages.ContainsKey(oscAddress)) {
			var values = new List<object>();
            for (int i = 0; i < numberOfValues; i++) {
				values.Add(null);
            }
			messages.Add(oscAddress, values);
		}
		return messages[oscAddress];
	}

	/// <summary>
	/// It assumes that the corresponding object was filled before
	/// </summary>
	/// <param name="oscAddress"></param>
	public void SendMessageToClient(string oscAddress) {
		OSCHandler.Instance.SendMessageToClient(Id, oscAddress, messagesToSend[oscAddress]);
	}

    private void ConnectClient() {
        //if request a connection
        IPAddress ipAddressClient;
        if (lastAddress_ == CONNECT_ADDRESS &&
            lastData_.Count == 2 &&
            IPAddress.TryParse(lastData_[0] as string, out ipAddressClient) &&
            lastData_[1] is int port
            ) {
            //Close and remove all clients
            foreach (var pair in OSCHandler.Instance.Clients) {
                pair.Value.client.Close();
            }
            OSCHandler.Instance.Clients.Clear();
            //Create a new client
            OSCHandler.Instance.CreateClient(Id, ipAddressClient, port);
            //Debug.Log("Connected: " + lastData_[0] + ", " + lastData_[1]);
        }
    }

    private void Update() {
        if (wasReceived_) {            
            //Debug.Log(OSCPacket.Test);
            //Debug.Log(OSCServer.Test);
            if (OnReceiveMessageOnMainThread != null) {              
                OnReceiveMessageOnMainThread(lastAddress_, lastData_);
            }
            wasReceived_ = false;
        }
    }

    // NOTE: The received messages at each server are updated here
    // Hence, this update depends on your application architecture
    // How many frames per second or Update() calls per frame?
    //void Update() {

    //	OSCHandler.Instance.UpdateLogs();

    //	servers = OSCHandler.Instance.Servers;

    //	for (int i = 0; i < servers.Count; i++) {
    //		var item = servers.ElementAt(i);
    //		//foreach (KeyValuePair<string, ServerLog> item in servers) {
    //		// If we have received at least one packet,
    //		// show the last received from the log in the Debug console
    //		if (item.Value.log.Count > 0) {
    //			int lastPacketIndex = item.Value.packets.Count - 1;
    //               //UnityEngine.Debug.LogWarning(String.Format("RECIVE: {0} ADDRESS: {1} VALUE : {2}",
    //               //                                        item.Key, // Server name
    //               //                                        item.Value.packets[lastPacketIndex].Address, // OSC address
    //               //                                        (item.Value.packets[lastPacketIndex].Data.Count > 0  ? item.Value.packets[lastPacketIndex].Data[0].ToString() : "null") 
    //               //                                        )                                                     
    //               //                                        ); //First data value                                
    //               if (item.Value.packets[lastPacketIndex].Data.Count > 0){
    //				if (OnReceiveMessage != null)
    //					OnReceiveMessage(item.Value.packets[lastPacketIndex].Address, item.Value.packets[lastPacketIndex].Data);
    //               }

    //           }
    //	}			

    //	//foreach( KeyValuePair<string, ClientLog> item in clients )
    //	//{
    //	//	// If we have sent at least one message,
    //	//	// show the last sent message from the log in the Debug console
    //	//	if(item.Value.log.Count > 0) 
    //	//	{
    //	//		int lastMessageIndex = item.Value.messages.Count- 1;				
    //	//		UnityEngine.Debug.Log(String.Format("SEND: {0} ADDRESS: {1} VALUE 0: {2}", 
    //	//		                                    item.Key, // Server name
    //	//		                                    item.Value.messages[lastMessageIndex].Address, // OSC address
    //	//		                                    item.Value.messages[lastMessageIndex].Data[0].ToString())); //First data value				                                    
    //	//	}
    //	//}
    //}
}