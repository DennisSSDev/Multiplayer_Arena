using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class ServerClient
{
    public int connectionID;
    public string playerName;
    public Vector3 position;
}

public class Server : MonoBehaviour
{

    private const int MAX_POS_CONNECTION = 10;
    private int port = 5701;
    private int hostID;
    private int webHostID;
    private int reliableChannel;
    private int unreliableChannel;
    private float lastMovementUpdate;
    private float movementUpdateRate = 0.5f;

    private List<ServerClient> clients = new List<ServerClient>();
    private bool isStarted = false;
    private byte error;
    private void Start()
    {
        Debug.Log("entering server start");
        NetworkTransport.Init();
        ConnectionConfig cc = new ConnectionConfig();
        reliableChannel = cc.AddChannel(QosType.Reliable);
        unreliableChannel = cc.AddChannel(QosType.Unreliable);
        HostTopology topology = new HostTopology(cc, MAX_POS_CONNECTION);
        hostID = NetworkTransport.AddHost(topology, port, null);
        webHostID = NetworkTransport.AddWebsocketHost(topology, port, null);
        isStarted = true;
    }
    private void Update()
    {
        if (isStarted == false)
            return;
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
            case NetworkEventType.ConnectEvent://2
                Debug.Log(connectionId + " has connected");
                OnConnection(connectionId);
                break;
            case NetworkEventType.DataEvent:
                string msg = Encoding.Unicode.GetString(recBuffer, 0, dataSize);
                Debug.Log("Recieving from " + connectionId + " : " + msg);//3

                string[] splitData = msg.Split('|');
                switch (splitData[0])
                {
                    case "NAMEIS":
                        OnNameIs(connectionId, splitData[1]);
                        break;
                    case "MYPOSITION":
                        OnMyPosition(connectionId,float.Parse(splitData[1]), float.Parse(splitData[2]));
                        break;
                    default:
                        Debug.Log("Invalid message " + msg);
                        break;
                }
                break;

            case NetworkEventType.DisconnectEvent:
                Debug.Log(connectionId + " has disconnected");//4
                OnDisconnection(connectionId);
                break;
        }
        if(Time.time - lastMovementUpdate > movementUpdateRate)
        {
            lastMovementUpdate = Time.time;
            string m = "ASKPOSITION|";
            foreach (ServerClient item in clients)
                m += item.connectionID.ToString() + '%' + item.position.x.ToString() + '%' + item.position.y.ToString() + '|';
            m.Trim('|');
            Send(m, unreliableChannel, clients);
        }
    }
    private void OnConnection(int cnnid)
    {
        ServerClient c = new ServerClient();
        c.connectionID = cnnid;
        c.playerName = "TEMP";
        clients.Add(c);

        string msg = "ASKNAME|" + cnnid + "|";
        foreach (ServerClient item in clients)
            msg += item.playerName + '%' + item.connectionID + '|';

        msg = msg.Trim('|');
        Send(msg, reliableChannel, cnnid);
    }
    private void OnNameIs(int cnnID, string playerN)
    {
        //tell everyone that a new player has connected
        clients.Find(x => x.connectionID == cnnID).playerName = playerN;
        Send("CNN|" + playerN + '|' + cnnID, reliableChannel, clients);
    }
    private void OnMyPosition(int cnnID, float x, float y)
    {
        clients.Find(c => c.connectionID == cnnID).position = new Vector3(x,y,0);
    }
    private void OnDisconnection(int id)
    {
        clients.Remove(clients.Find(x => x.connectionID == id));
        Send("DC|" + id, reliableChannel, clients);
    }
    private void Send(string message, int channelID,int plID)
    {
        List<ServerClient> c = new List<ServerClient>();
        c.Add(clients.Find(x => x.connectionID == plID));
        Send(message, channelID, c);
    }
    private void Send(string message, int channelID, List<ServerClient> c)
    {
        Debug.Log("Sending : " + message);
        byte[] msg = Encoding.Unicode.GetBytes(message);
        foreach (ServerClient item in c)
        {
            NetworkTransport.Send(hostID, item.connectionID, channelID, msg, message.Length * sizeof(char), out error);
        }
    }
}
