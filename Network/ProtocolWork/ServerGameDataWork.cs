using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TexasHoldemServer.Network.ProtocolWork
{
    class ServerGameDataWork : ServerProtocolWork
    {
        public override bool RecvWork(ClientObject client, Protocols protocol, byte[] data)
        {
            if (client.UserData == null)
                return false;
            switch (protocol)
            {
                case Protocols.RoomPlayerList:
                    RoomPlayerList(client, data);
                    break;
                case Protocols.RoomReady:
                    RoomReady(client, data);
                    break;
                case Protocols.PlayerBetting:
                    PlayerBetting(client, data);
                    break;
                case Protocols.PlayerCall:
                    PlayerCall(client, data);
                    break;
                case Protocols.PlayerFold:
                    PlayerFold(client, data);
                    break;
                case Protocols.GameRoomInfo:
                    GetGameRoomInfo(client, data);
                    break;
                case Protocols.GetOnCard:
                    GetGameOnCard(client, data);
                    break;
                case Protocols.PlayInfo:
                    GetPlayInfo(client, data);
                    break;
                default:
                    return false;
            }
            return true;
        }

        void RoomPlayerList(ClientObject client, byte[] data)
        {
            int roomIdx = BitConverter.ToInt32(data, 12);
            RoomData r = m_Server.GetRoomData(roomIdx);
            if (r == null)
            {
                client.SendInt(Protocols.RoomPlayerList, 0);
                return;
            }
            ByteDataMaker m = new ByteDataMaker();
            m.Init(500);
            m.Add(1);
            m.Add(r.GetBytes_PlayerList());
            client.Send(Protocols.RoomPlayerList, m.GetBytes());
        }

        void RoomReady(ClientObject client, byte[] data)
        {
            RoomData room = m_Server.GetRoomData(client);
            if (room == null)
                return;
            room.UserReady(client.UserData);
        }

        void PlayerBetting(ClientObject client, byte[] data)
        {
            RoomData room = m_Server.GetRoomData(client);
            if (room == null)
                return;
            ByteDataParser p = new ByteDataParser();
            p.Init(data);
            p.SetPos(12);
            UInt64 money = p.GetUInt64();
            room.UserBetting(client.UserData, money);
        }

        void PlayerCall(ClientObject client, byte[] data)
        {
            RoomData room = m_Server.GetRoomData(client);
            if (room == null || client.UserData == null)
                return;
            room.RecvCall(client.UserData.UserIndex);
        }

        void PlayerFold(ClientObject client, byte[] data)
        {
            RoomData room = m_Server.GetRoomData(client);
            if (room == null || client.UserData == null)
                return;
            room.RecvFold(client.UserData.UserIndex);
        }

        void GetGameRoomInfo(ClientObject client, byte[] data)
        {
            RoomData room = m_Server.GetRoomData(client);
            if (room == null || client.UserData == null)
                return;
            client.Send(Protocols.GameRoomInfo, room.GetBytesRoomInfo());
        }

        void GetGameOnCard(ClientObject client, byte[] data)
        {
            RoomData room = m_Server.GetRoomData(client);
            if (room == null || client.UserData == null)
            {
                client.SendInt(Protocols.GetOnCard, 0);
                return;
            }
            ByteDataMaker m = new ByteDataMaker();
            m.Add(1);
            m.Add(room.GetShowOnCard());
            client.Send(Protocols.GetOnCard, m.GetBytes());
        }

        void GetPlayInfo(ClientObject client, byte[] data)
        {
            int roomIdx = BitConverter.ToInt32(data, 12);
            RoomData room = m_Server.GetRoomData(roomIdx);
            if (room == null)
            {
                client.SendInt(Protocols.PlayInfo, 0);
                return;
            }
            client.Send(Protocols.PlayInfo, room.GetPlayInfo());
        }
    }
}
