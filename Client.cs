﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net.Sockets;
using System.Net;
using System;
using System.Text;

public class Client : MonoBehaviour
{
    enum Operation : byte
    {
        Join, ShootBomb, SpawnObj, SendVelocity, ReceivePos, BombTimer, PlayerDie, JoinAck, Ack
    }

    public string Address = "127.0.0.1";
    public int Port = 9999;

    public delegate void SpawnObject(byte idObjToSpawn, int idObj, Vector3 pos);
    public static event SpawnObject OnSpawnPacketReceived;
    public delegate void BombTimer(int id, byte currTimer);
    public static event BombTimer OnBombTimerPacketReceived;
    public delegate void PlayerDie(int id);
    public static event PlayerDie OnPlayerDiePacketReceived;

    private delegate void ReceiveOperations();
    private Dictionary<Operation, ReceiveOperations> receiveCommands;
    private Dictionary<int, Packet> packetNeedAck;
    private Dictionary<int, Packet> ackToResendToServer;
    private List<int> packetsToRemove;
    private Dictionary<int, IClientPositionable> positionableObj;
    private IClientJoinable clientJoin;
    private Socket socket;
    private EndPoint endPoint;
    private byte[] receivedData;

    // Start is called before the first frame update
    void Start()
    {
        socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        socket.Blocking = false;
        endPoint = new IPEndPoint(IPAddress.Parse(Address), Port);

        packetNeedAck = new Dictionary<int, Packet>();
        ackToResendToServer = new Dictionary<int, Packet>();
        packetsToRemove = new List<int>();
        positionableObj = new Dictionary<int, IClientPositionable>();

        receiveCommands = new Dictionary<Operation, ReceiveOperations>();
        receiveCommands[Operation.ReceivePos] = PositionPacketCallback;
        receiveCommands[Operation.SpawnObj] = SpawnPacketCallback;
        receiveCommands[Operation.BombTimer] = BombTimerPacketCallback;
        receiveCommands[Operation.PlayerDie] = DiePacketCallback;
        receiveCommands[Operation.JoinAck] = JoinAckReceived;
        receiveCommands[Operation.Ack] = AckReceived;
    }

    // Update is called once per frame
    void Update()
    {
        //Receive operations
        DequeuePackets();
        if (receivedData != null && receiveCommands.ContainsKey((Operation)receivedData[0]))
        {
            Debug.Log("Pacchetto arrivato: ");
            PrintPacket(receivedData);

            receiveCommands[(Operation)receivedData[0]]();
        }

        //Resend packet without ack
        foreach (int id in packetNeedAck.Keys)
        {
            Packet packet = packetNeedAck[id];
            if (packet.TimeRemainingToResend <= 0)
            {
                socket.SendTo(packet.GetData(), endPoint);
                packet.ResetTimeRemaining();
            }
            else
                packet.TimeRemainingToResend -= Time.deltaTime;
        }

        //Resend the acks several times to make sure the server has received it
        foreach (int id in ackToResendToServer.Keys)
        {
            Packet packet = ackToResendToServer[id];
            if (packet.NumTimesToSend > 0)
            {
                socket.SendTo(packet.GetData(), endPoint);
                packet.NumTimesToSend--;
            }
            else
                packetsToRemove.Add(id);
        }

        //Remove ack packet sent to server
        for(int i=0; i < packetsToRemove.Count; i++)
        {
            ackToResendToServer.Remove(packetsToRemove[i]);
        }
        packetsToRemove.Clear();
    }

    public void Join(string playerName, IClientJoinable clientJoin)
    {
        this.clientJoin = clientJoin;

        byte command = (byte)Operation.Join;

        Packet joinPacket = new Packet(command, playerName);
        joinPacket.ResendAfter = 1f;
        socket.SendTo(joinPacket.GetData(), endPoint);

        packetNeedAck.Add(joinPacket.Id, joinPacket);
        PrintPacket(joinPacket.GetData());
    }

    public void SendShootBombPacket(Vector3 position)
    {
        byte command = (byte)Operation.ShootBomb;

        Packet shootBombPacket = new Packet(command, position.x, position.y, position.z);
        shootBombPacket.ResendAfter = .05f;
        socket.SendTo(shootBombPacket.GetData(), endPoint);

        packetNeedAck.Add(shootBombPacket.Id, shootBombPacket);
        PrintPacket(shootBombPacket.GetData());
    }

    public void SendVelocityPacket(Vector3 velocity)
    {
        byte command = (byte)Operation.SendVelocity;
        float x = velocity.x;
        float y = velocity.y;
        float z = velocity.z;

        Packet setVelocityPacket = new Packet(command, x, y, z);
        socket.SendTo(setVelocityPacket.GetData(), endPoint);
    }

    public bool RegisterObjPositionable(int id, IClientPositionable positionable)
    {
        if (!positionableObj.ContainsKey(id))
        {
            positionableObj.Add(id, positionable);
            return true;
        }
        return false;
    }

    private void SpawnPacketCallback()
    {
        if (receivedData.Length != 22)
            return;

        byte idObjToSpawn = receivedData[1];
        int idObj = BitConverter.ToInt32(receivedData, 2);
        float x = BitConverter.ToSingle(receivedData, 6);
        float y = BitConverter.ToSingle(receivedData, 10);
        float z = BitConverter.ToSingle(receivedData, 14);

        if (OnSpawnPacketReceived != null)
        {
            OnSpawnPacketReceived(idObjToSpawn, idObj, new Vector3(x, y, z));
        }

        int idPacket = BitConverter.ToInt32(receivedData, 18);
        SendAck(idPacket);
    }

    private void PositionPacketCallback()
    {
        if (receivedData.Length != 21)
            return;

        int id = BitConverter.ToInt32(receivedData, 1);
        float x = BitConverter.ToSingle(receivedData, 5);
        float y = BitConverter.ToSingle(receivedData, 9);
        float z = BitConverter.ToSingle(receivedData, 13);

        //Send ack does not necessary
        positionableObj[id].OnPositionPacketReceived(x, y, z);
    }

    private void BombTimerPacketCallback()
    {
        if (receivedData.Length != 10)
            return;

        int bombId = BitConverter.ToInt32(receivedData, 1);
        byte currTimer = receivedData[5];

        //Is sendAck necessary???
        if (OnBombTimerPacketReceived != null)
        {
            OnBombTimerPacketReceived(bombId, currTimer);
        }
    }

    private void DiePacketCallback()
    {
        if (receivedData.Length != 9)
            return;

        int playerId = BitConverter.ToInt32(receivedData, 1);
        if (OnPlayerDiePacketReceived != null)
        {
            OnPlayerDiePacketReceived(playerId);
        }

        int idPacket = BitConverter.ToInt32(receivedData, 5);
        SendAck(idPacket);
    }

    private void JoinAckReceived()
    {
        //join failed packet [command, 0, idPacket]
        if (receivedData.Length == 6)
        {
            int idPacket = BitConverter.ToInt32(receivedData, 2);
            packetNeedAck.Remove(idPacket);
            clientJoin.OnJoinFailed();
            return;
        }

        //join succes packet [command, 1, idPlayer, x, y, z, idPacket]
        if (receivedData.Length == 22)
        {
            bool isJoined = receivedData[1] == 1;
            if (isJoined)
            {
                int idPlayer = BitConverter.ToInt32(receivedData, 2);
                float x = BitConverter.ToSingle(receivedData, 6);
                float y = BitConverter.ToSingle(receivedData, 10);
                float z = BitConverter.ToSingle(receivedData, 14);

                clientJoin.OnJoinSucces(idPlayer, new Vector3(x, y, z));
            }
            else
                clientJoin.OnJoinFailed();

            int idPacket = BitConverter.ToInt32(receivedData, 18);
            packetNeedAck.Remove(idPacket);
        }
    }

    private void AckReceived()
    {
        if (receivedData.Length != 5)
            return;

        int idPacket = BitConverter.ToInt32(receivedData, 1);
        if (packetNeedAck.ContainsKey(idPacket))
            packetNeedAck.Remove(idPacket);
    }

    private void SendAck(int packetId, int numTimesToSend = 10)
    {
        byte command = (byte)Operation.Ack;
        Packet ackPacket = new Packet(command, packetId);
        ackPacket.NumTimesToSend = numTimesToSend;
        ackToResendToServer.Add(packetId, ackPacket);

        socket.SendTo(ackPacket.GetData(), endPoint);
    }

    private void DequeuePackets()
    {
        receivedData = null;
        byte[] receivedPacket = new byte[256];
        int rlen = 0;
        try
        {
            rlen = socket.Receive(receivedPacket);
            receivedData = new byte[rlen];
            Buffer.BlockCopy(receivedPacket, 0, receivedData, 0, rlen);
        }
        catch
        {
            return;
        }
    }

    private void PrintPacket(byte[] packet)
    {
        String stringToPrint = "[";
        for (int i = 0; i < packet.Length; i++)
        {
            stringToPrint += packet[i] + ", ";
        }
        stringToPrint += "]";

        Debug.Log(stringToPrint);
    }
}