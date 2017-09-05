using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;


public class Player
{
    public string playerName;
    public GameObject avatar;
    public int conid;

}


public class Client : MonoBehaviour
{
    private const int MAX_POS_CONNECTION = 10;
    private int port = 5701;
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
    public GameObject playerPrefab;
    public Dictionary<int,Player> players = new Dictionary<int,Player>();

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
                            SpawnPlayer(splitData[1], int.Parse(splitData[2]));
                            break;
                        case "DC":
                            PlayerDisconnected(int.Parse(splitData[1]));
                            break;
                        case "ASKPOSITION":
                            OnAskPosition(splitData);
                            break;
                        default:
                            break;
                    }
                    break;
            }
        }          
        

    }
    private void OnAskPosition(string[] data)
    {
        if(isStarted == false)
        {
            return;
        }
        for (int i = 1; i < data.Length-1; i++)
        {
            string[] d = data[i].Split('%');
            //Udate everyone else, but mine
            if (ourClientID != int.Parse(d[0]))
            {
                Vector3 position = Vector3.zero;
                position.x = float.Parse(d[1]);
                position.y = float.Parse(d[2]);
                players[int.Parse(d[0])].avatar.transform.position = position;
            }
           
            
        }
        //Update my position
        Vector3 myPos = players[ourClientID].avatar.transform.position;
        string m = "MYPOSITION|" + myPos.x.ToString() + '|' + myPos.y.ToString();
        Send(m, unreliableChannel);
    }
    private void OnAskName(string[] data)
    {

        ourClientID = int.Parse(data[1]);

        Send("NAMEIS|" + playerName, reliableChannel);

        for (int i = 2; i < data.Length; i++)//make sure to debug here
        {
            string[] d = data[i].Split('%');
            if (d[0] != "TEMP")
                SpawnPlayer(d[0], int.Parse(d[1]));

        }
    }
    private void SpawnPlayer(string name, int id)
    {
        GameObject go = Instantiate(playerPrefab) as GameObject;

        if(id == ourClientID)
        {
            go.AddComponent<PlayerMotor>();
            GameObject.Find("Canvas").SetActive(false);
            isStarted = true;
        }

        Player p = new Player();
        p.avatar = go;
        p.playerName = name;
        p.conid = id;
        p.avatar.GetComponentInChildren<TextMesh>().text = name;
        players.Add(id, p);
    }
    private void PlayerDisconnected(int cnnID)
    {
        Destroy(players[cnnID].avatar);
        players.Remove(cnnID);
    }
    private void Send(string message, int channelID)
    {
        Debug.Log("Sending" + message);
        byte[] msg = Encoding.Unicode.GetBytes(message);
        NetworkTransport.Send(hostID, connectionID, channelID, msg, message.Length * sizeof(char), out error);
    }
}
