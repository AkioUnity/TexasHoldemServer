using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TexasHoldemServer.Network.ProtocolWork
{
    class ServerRoomDataWork : ServerProtocolWork
    {


        public override bool RecvWork(ClientObject client, Protocols protocol, byte[] data)
        {
            switch (protocol)
            {
                case Protocols.RoomCount:
                    RoomCount(client, data);
                    break;
                case Protocols.RoomData:
                    RoomData(client, data);
                    break;
                case Protocols.RoomCreate:
                    RoomCreate(client, data);
                    break;
                case Protocols.RoomIn:
                    RoomIn(client, data);
                    break;
                case Protocols.RoomOut:
                    RoomOut(client, data);
                    break;
                case Protocols.TestRoomInComplete:
                    RoomInComplete(client, data);
                    break;
                default:
                    return false;
            }
            return true;
        }

        void RoomCount(ClientObject client, byte[] data)
        {
            int BlindType = BitConverter.ToInt32(data, 12);
            ByteDataMaker d = new ByteDataMaker();
            d.Init(10);
            d.Add(m_Server.GetRoomCount(BlindType));
            client.Send(Protocols.RoomCount, d.GetBytes());
        }

        void RoomData(ClientObject client, byte[] data)
        {
            int type = (int)data[12];//0 number  1 index
            int n = BitConverter.ToInt32(data, 13);
            RoomData room = null;
            if (type == 0)
            {
                int num = BitConverter.ToInt32(data, 17);
                room = m_Server.GetRoomData_Number(n, num);
            }
            else
                room = m_Server.GetRoomData(n);
            if (room == null)
            {
                client.SendInt(Protocols.RoomData, 0);
                return;
            }

            byte[] rData = room.GetBytes();

            ByteDataMaker d = new ByteDataMaker();
            d.Init(500);
            d.Add((int)1);
            d.Add(rData);
            client.Send(Protocols.RoomData, d.GetBytes());
        }

        void RoomCreate(ClientObject client, byte[] data)
        {
            if (client.UserData == null)
                return;
            if (client.m_RoomIdx != -1)
                return;
            ByteDataParser p = new ByteDataParser();
            p.Init(data);
            p.SetPos(12);
            string name = p.GetString();
            int blindType = p.GetInt();
            RoomData r = m_Server.CreateRoom(blindType, name);
            if(r==null)
            {
                client.SendInt(Protocols.RoomCreate, -10);
                return;
            }
            client.SendInt(Protocols.RoomCreate, r.m_RoomIndex);
            r.AddUser(client.UserData.UserIndex);
            r.RecvRoomInComplete(client.UserData.UserIndex);
        }

        void RoomIn(ClientObject client, byte[] data)
        {
            if (client.UserData == null)
            {
                client.SendInt(Protocols.RoomIn, 3);//유저정보가 없음(로그인이 안됨)
                return;
            }
            ByteDataParser p = new ByteDataParser();
            p.Init(data);
            p.SetPos(12);
            int roomIdx = p.GetInt();
            RoomData room = m_Server.GetRoomData(roomIdx);
            if (room == null)
            {
                client.SendInt(Protocols.RoomIn, 1);//방이 없음
                return;
            }
            if (room.AddUser(client.UserData.UserIndex) == false)
            {
                client.SendInt(Protocols.RoomIn, 2);//방이 꽉참
            }
            else
            {
                client.SendInt(Protocols.RoomIn, 0);//입장성공
            }
            //room.CheckStartGame();
        }

        void RoomInComplete(ClientObject client, byte[] data)
        {
            if (client.UserData == null)
                return;
            RoomData room = m_Server.GetRoomData(client);
            if (room == null)
                return;
            room.RecvRoomInComplete(client.UserData.UserIndex);
        }

        void RoomOut(ClientObject client, byte[] data)
        {
            m_Server.RoomOut(client);
        }

        
    }
}
