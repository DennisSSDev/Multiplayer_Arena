using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class Client : MonoBehaviour
{
    private const int MAX_POS_CONNECTION = 10;
    private int port = 5705;
    private int hostID;
    private int connectionID;
    private int ourClientID;
    private int reliableChannel;
    private int unreliableChannel;
    private float connectionTime;
    private bool isConnected = false;
    private bool isStarted = false;
    private byte error;
    private string playerName;

    public void Connect()
    {
        string pName = GameObject.Find("NameInput").GetComponent<InputField>().text;
        if(pName == "")
        {
            Debug.Log("You must enter a name");
            return;
        }
        else
        {
            playerName = pName;
        }
        
        NetworkTransport.Init();
        ConnectionConfig cc = new ConnectionConfig();
        reliableChannel = cc.AddChannel(QosType.Reliable);
        unreliableChannel = cc.AddChannel(QosType.Unreliable);
        HostTopology topology = new HostTopology(cc, MAX_POS_CONNECTION);
        hostID = NetworkTransport.AddHost(topology, 0);
        connectionID = NetworkTransport.Connect(hostID, "127.0.0.1", port, 0, out error);
        connectionTime = Time.time;
        isConnected = true;
    }
    void Update()
    {

        if (isConnected == false)
        {
            return;
        }
        else
        {
            int recHostId;
            int connectionId;
            int channelId;
            byte[] recBuffer = new byte[1024];
            int bufferSize = 1024;
            int dataSize;
            byte error;
            NetworkEventType recData = NetworkTransport.Receive(out recHostId, out connectionId, out channelId, recBuffer, bufferSize, out dataSize, out error);
            switch (recData)
            {
                case NetworkEventType.DataEvent:
                    string msg = Encoding.Unicode.GetString(recBuffer, 0, dataSize);
                    Debug.Log("Recieving " + msg);
                    string[] splitData = msg.Split('|');
                    switch (splitData[0])
                    {
                        case "ASKNAME":
                            OnAskName(splitData);
                            break;
                        case "CNN":
                            break;
                        case "DC":
                            break;
                        default:
                            break;
                    }
                    break;
            }
        }          
        

    }
    private void OnAskName(string[] data)
    {

        ourClientID = int.Parse(data[1]);

        Send("NAMEIS|" + playerName, reliableChannel);

        for (int i = 2; i < data.Length -1; i++)//make sure to debug here
        {
            string[] d = data[i].Split('%');
            SpawnPlayer(d[0], int.Parse(d[1]));
        }
    }
    private void SpawnPlayer(string name, int id)
    {

    }
    private void Send(string message, int channelID)
    {
        Debug.Log("Sending" + message);
        byte[] msg = Encoding.Unicode.GetBytes(message);
        NetworkTransport.Send(hostID, connectionID, channelID, msg, message.Length * sizeof(char), out error);
        
    }
}
