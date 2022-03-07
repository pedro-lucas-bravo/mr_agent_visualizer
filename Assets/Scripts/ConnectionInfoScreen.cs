using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Linq;
using System.Net;

public class ConnectionInfoScreen : MonoBehaviour
{
    public TextMeshProUGUI ipLabel;
    public TextMeshProUGUI portLabel;

    public void Start() {
        ipLabel.text = GetIp();
        portLabel.text = "" + OSCHandler.Instance.Servers.First().Value.server.LocalPort;
    }

#if ENABLE_WINMD_SUPPORT
    public string GetIp() {
        List<string> IpAddress = new List<string>();
        var Hosts = Windows.Networking.Connectivity.NetworkInformation.GetHostNames().ToList();
        foreach (var Host in Hosts) {
            string IP = Host.DisplayName;
            IpAddress.Add(IP);
        }
        IPAddress address = IPAddress.Parse(IpAddress.Last());
        return address.ToString();
    }
#else
    string GetIp() {
        string localIP;
        using (System.Net.Sockets.Socket socket = new System.Net.Sockets.Socket(System.Net.Sockets.AddressFamily.InterNetwork, System.Net.Sockets.SocketType.Dgram, 0)) {
            socket.Connect("8.8.8.8", 65530);
            System.Net.IPEndPoint endPoint = socket.LocalEndPoint as System.Net.IPEndPoint;
            localIP = endPoint.Address.ToString();
        }
        return localIP;
    }
#endif

    private void Update() {
        //Check if client is connected
        if (OSCHandler.Instance.Clients.Any()) { //Deactivate object
            gameObject.SetActive(false);        
        }
    }

}
