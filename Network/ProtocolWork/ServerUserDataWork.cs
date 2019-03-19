using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TexasHoldemServer.Network.ProtocolWork
{
    class ServerUserDataWork : ServerProtocolWork
    {

        public override bool RecvWork(ClientObject client, Protocols protocol, byte[] data)
        {
            switch(protocol)
            {
                case Protocols.Login:
                    LoginWork(client, data);
                    break;
                case Protocols.UserRegister:
                    UserRegisterWork(client, data);
                    break;
                case Protocols.UserInfo:
                    GetUserInfo(client, data);
                    break;
                case Protocols.BankIn:
                    BankInMoney(client, data);
                    break;
                case Protocols.BankOut:
                    BankOutMoney(client, data);
                    break;
                case Protocols.GetBankMoney:
                    GetBankMoney(client, data);
                    break;
                case Protocols.PlayerSetMoney:
                    SetUserMoney(client, data);
                    break;
                case Protocols.PlayerSetAvatar:
                    SetUserAvatar(client, data);
                    break;
                case Protocols.MoneyGift:
                    MoneyGift(client, data);
                    break;
                case Protocols.DepositRequest://no use
                    //DepositRequest(client, data);
                    break;
                case Protocols.ChargeRequest:
                    ChargeRequest(client, data);
                    break;
                case Protocols.Withdrawal:
                    Withdrawal(client, data);
                    break;
                case Protocols.UserMessage:
                    GetUserMessage(client, data);
                    break;
                case Protocols.UserMessageReceive:
                    ReceiveUserMessage(client, data);
                    break;
                case Protocols.UserMessageCount:
                    GetUserMessageCount(client, data);
                    break;
                case Protocols.CheckIDName:
                    CheckUserIDName(client, data);
                    break;
                case Protocols.ChangeNickname:
                    ChangeNickname(client, data);
                    break;
                case Protocols.ChangePhonenumber:
                    ChangePhonenumber(client, data);
                    break;
                case Protocols.ChangePassword:
                    ChangePassword(client, data);
                    break;
                case Protocols.ChangeName:
                    ChangeUserName(client, data);
                    break;
                case Protocols.GetUserNamePhonenumber:
                    GetUserNamePhonenumber(client, data);
                    break;
                default:
                    return false;
            }
            return true;
        }

        void LoginWork(ClientObject client, byte[] data)
        {
            //DB db = DB.Instance;
            ByteDataParser p = new ByteDataParser();
            p.Init(data);
            p.SetPos(12);
            string id = p.GetString();
            string pass = p.GetString();
            int idx = DB.CheckUserPassword(id, pass);
            if (idx > 0)
            {
                if (m_Server.GetUserClient(idx) != null)
                {
                    idx = -2;
                }
                else if (DB.CheckBlockUser(idx))
                {
                    idx = -3;
                }
            }
            if (idx > 0)
                LogMessageManager.AddLogMessage("User Login id : " + id + " - success", true);
            else
                LogMessageManager.AddLogMessage("User Login id : " + id + " - failed", true);

            client.SendInt(Protocols.Login, idx);
            if (idx > 0)
            {
                client.UserData = DB.GetUserInfo(idx);
            }
        }

        void UserRegisterWork(ClientObject client, byte[] data)
        {
            //DB db = DB.Instance;
            ByteDataParser p = new ByteDataParser();
            p.Init(data);
            p.SetPos(12);

            string id = p.GetString();
            string pass = p.GetString();
            string nickname = p.GetString();
            string name = p.GetString();
            string phone = p.GetString();

            if (DB.CheckUserNickName(nickname) == false)
            {
                client.SendInt(Protocols.UserRegister, 2);
                LogMessageManager.AddLogMessage("User Register - id : " + id + " - failed(nick)", true);
                return;
            }

            if (DB.RegisterUser(id, pass, nickname, name, phone))
            {
                client.SendInt(Protocols.UserRegister, 1);
                int idx = DB.GetUserIndex(id);
                DB.SetUserMoney(idx, (UInt64)DB.GetRegisterMoney(), 0);
                //db.SetUserMoney(idx, 100000, 0);
                DB.SetUserAvatar(idx, 0);
                LogMessageManager.AddLogMessage("User Register - id : " + id + " - success", true);
            }
            else
            {
                client.SendInt(Protocols.UserRegister, 0);
                LogMessageManager.AddLogMessage("User Register - id : " + id + " - failed(id)", true);
            }
        }

        void GetUserInfo(ClientObject client, byte[] data)
        {
            ByteDataMaker d = new ByteDataMaker();
            d.Init(200);
            int UserIdx = BitConverter.ToInt32(data, 12);
            AINode ai = m_Server.GetAIInfo(UserIdx);
            if (ai != null)
            {
                d.Add(UserIdx);
                d.Add(ai.GetBytes());
                client.Send(Protocols.UserInfo, d.GetBytes());
                return;
            }

            UserInfo info = DB.GetUserInfo(UserIdx);
            if (info == null)
            {
                client.Send(Protocols.UserInfo, ClientObject.GetBytes(UserIdx, 0));
                return;
            }
            d.Add(UserIdx);
            d.Add(info.GetBytes());
            byte[] s = d.GetBytes();

            client.Send(Protocols.UserInfo, s);
        }

        /*
        void GetUserInfo(ClientObject client, byte[] data)
        {
            ByteDataMaker d = new ByteDataMaker();
            d.Init(200);
            int UserIdx = BitConverter.ToInt32(data, 12);
            if (UserIdx >= AIMgr.AISrcIndex)
            {
                AINode ai = m_Server.GetAIInfo(UserIdx);
                if (ai == null)
                {
                    client.Send(Protocols.UserInfo, ClientObject.GetBytes(UserIdx, 0));
                    return;
                }
                d.Add(UserIdx);
                d.Add(ai.GetBytes());
                client.Send(Protocols.UserInfo, d.GetBytes());
                return;
            }


            UserInfo info = DB.Instance.GetUserInfo(UserIdx);
            if (info == null)
            {
                client.Send(Protocols.UserInfo, ClientObject.GetBytes(UserIdx, 0));
                return;
            }
            d.Add(UserIdx);
            d.Add(info.GetBytes());
            byte[] s = d.GetBytes();

            client.Send(Protocols.UserInfo, s);
        }
        */
        void BankInMoney(ClientObject client, byte[] data)
        {
            if (client.UserData == null)
                return;
            UInt64 InMoney = BitConverter.ToUInt64(data, 12);
            UInt64 BankMoney = client.UserData.GetMoney_DB(true);
            InMoney = client.UserData.RemoveMoney(InMoney);
            BankMoney += InMoney;
            client.UserData.SetBank(BankMoney);
        }

        void BankOutMoney(ClientObject client, byte[] data)
        {
            if (client.UserData == null)
                return;
            UInt64 OutMoney = BitConverter.ToUInt64(data, 12);
            UInt64 BankMoney = client.UserData.GetMoney_DB(true);
            if (OutMoney > BankMoney)
                OutMoney = BankMoney;
            BankMoney -= OutMoney;
            client.UserData.SetBank(BankMoney);
            client.UserData.SetMoney(client.UserData.Money + OutMoney);
        }

        void GetBankMoney(ClientObject client, byte[] data)
        {
            if (client.UserData == null)
            {
                client.SendUInt64(Protocols.GetBankMoney, 0);
                return;
            }
            //UInt64 Money = DB.Instance.GetUserMoney(client.UserData.UserIndex, true);
            UInt64 Money = client.UserData.GetMoney_DB(true);
            client.SendUInt64(Protocols.GetBankMoney, Money);
        }

        void SetUserMoney(ClientObject client, byte[] data)
        {
            if (client.UserData == null) return;
            UInt64 Money = BitConverter.ToUInt64(data, 12);
            client.UserData.SetMoney(Money);
        }

        void SetUserAvatar(ClientObject client, byte[] data)
        {
            if (client.UserData == null) return;
            int avn = BitConverter.ToInt32(data, 12);
            client.UserData.SetAvatar(avn);
        }

        void MoneyGift(ClientObject client, byte[] data)
        {
            if (client.UserData == null)
                return;
            ByteDataParser p = new ByteDataParser();
            p.Init(data);
            p.SetPos(12);
            //DB db = DB.Instance;
            string nick = p.GetString();
            UInt64 money = p.GetUInt64();

            UInt64 NowMoney = DB.GetUserMoney(client.UserData.UserIndex, false);
            if (NowMoney < money)
            {
                client.SendInt(Protocols.MoneyGift, -1);
                return;
            }

            //int dstIdx = db.GetUserIndex_Nickname(nick);
            int dstIdx = DB.GetUserIndex(nick);//user id

            if (dstIdx <= 0)
            {
                client.SendInt(Protocols.MoneyGift, 0);
                return;
            }

            DB.GiftMoney(client.UserData.UserIndex, money, dstIdx);
            client.SendInt(Protocols.MoneyGift, 1);
        }

        void DepositRequest(ClientObject client, byte[] data)
        {
            if (client.UserData == null)
                return;
            ByteDataParser p = new ByteDataParser();
            p.Init(data);
            p.SetPos(12);
            UInt64 Money = p.GetUInt64();
            //DB db = DB.Instance;
            DB.AddDepositRequest(client.UserData.UserIndex, Money);
        }

        void ChargeRequest(ClientObject client, byte[] data)
        {
            if (client.UserData == null)
                return;
            ByteDataParser p = new ByteDataParser();
            p.Init(data);
            p.SetPos(12);
            UInt64 Money = p.GetUInt64();
            string AccountName = p.GetString();
            //DB db = DB.Instance;
            //db.AddDepositRequest(client.UserData.UserIndex, Money);
            DB.AddChargeRequest(client.UserData.UserIndex, Money, AccountName);
        }

        void Withdrawal(ClientObject client, byte[] data)
        {
            if (client.UserData == null)
                return;
            ByteDataParser p = new ByteDataParser();
            p.Init(data);
            p.SetPos(12);
            UInt64 Money = p.GetUInt64();
            string AccountName = p.GetString();
            string BankName = p.GetString();
            string AccountNum = p.GetString();

            //DB db = DB.Instance;
            //db.AddDepositRequest(client.UserData.UserIndex, Money);
            DB.AddWithdrawal(client.UserData.UserIndex, Money, BankName, AccountName, AccountNum);
        }

        void GetUserMessage(ClientObject client, byte[] data)
        {
            if (client.UserData == null)
                return;
            //DB db = DB.Instance;
            List<UserMessageData> ArrMessage = DB.GetMessageDataList(client.UserData.UserIndex);
            if (ArrMessage != null)
            {
                int i, j;
                j = ArrMessage.Count;
                for (i = 0; i < j; i++)
                {
                    client.Send(Protocols.UserMessage, ArrMessage[i].GetBytes());
                }
            }
            client.SendInt(Protocols.UserMessage, 0);
        }

        void ReceiveUserMessage(ClientObject client, byte[] data)
        {
            if (client.UserData == null)
                return;
            ByteDataParser p = new ByteDataParser();
            p.Init(data);
            p.SetPos(12);
            int idx = p.GetInt();
            DB.SetMessageReceive(idx);
            client.UserData.ReflashMoney();
        }

        void GetUserMessageCount(ClientObject client, byte[] data)
        {
            if (client.UserData == null)
                return;
            int c = DB.GetMessageDataCount(client.UserData.UserIndex);
            client.SendInt(Protocols.UserMessageCount, c);
        }

        void CheckUserIDName(ClientObject client, byte[] data)
        {
            ByteDataMaker m = new ByteDataMaker();
            m.Init(20);
            ByteDataParser p = new ByteDataParser();
            p.Init(data);
            p.SetPos(12);
            
            int type = p.GetInt();
            string str = p.GetString();
            int rValue = 0;
            if (type == 0)
            {
                if (DB.GetUserIndex(str) == 0)
                {
                    rValue = 1;
                }
            }
            else
            {
                if (DB.CheckUserNickName(str) == true)
                {
                    rValue = 1;
                }
            }
            m.Add(rValue);
            client.Send(Protocols.CheckIDName, m.GetBytes());
        }

        void ChangeNickname(ClientObject client, byte[] data)
        {
            if (client.UserData == null)
                return;
            ByteDataParser p = new ByteDataParser();
            p.Init(data);
            p.SetPos(12);
            string newNick = p.GetString();
            bool succ = DB.ChangeNickname(client.UserData.UserIndex, newNick);

            client.SendInt(Protocols.ChangeNickname, succ ? 1 : 0);
        }

        void ChangePhonenumber(ClientObject client, byte[] data)
        {
            if (client.UserData == null)
                return;
            ByteDataParser p = new ByteDataParser();
            p.Init(data);
            p.SetPos(12);
            string pn = p.GetString();
            bool succ = DB.ChangePhonenumber(client.UserData.UserIndex, pn);
            client.SendInt(Protocols.ChangePhonenumber, succ ? 1 : 0);
        }

        void ChangePassword(ClientObject client, byte[] data)
        {
            if (client.UserData == null)
                return;
            ByteDataParser p = new ByteDataParser();
            p.Init(data);
            p.SetPos(12);
            string nowp = p.GetString();
            string newp = p.GetString();
            bool succ = DB.ChangePassword(client.UserData.UserIndex, nowp, newp);
            client.SendInt(Protocols.ChangePassword, succ ? 1 : 0);
        }

        void ChangeUserName(ClientObject client, byte[] data)
        {
            if (client.UserData == null)
                return;
            ByteDataParser p = new ByteDataParser();
            p.Init(data);
            p.SetPos(12);
            string name = p.GetString();
            //bool succ = DB.Instance.ChangeUserName(client.UserData.UserIndex, name);
            bool succ = DB.ChangeUserName(client.UserData.UserIndex, name);
            client.SendInt(Protocols.ChangeName, succ ? 1 : 0);
        }

        void GetUserNamePhonenumber(ClientObject client, byte[] data)
        {
            if (client.UserData == null)
                return;
            string name = "";
            string phone = "";
            //if (DB.Instance.GetUserNamePhoneNumber(client.UserData.UserIndex, ref name, ref phone) == false)
            //    return;
            if (DB.GetUserNamePhoneNumber(client.UserData.UserIndex, ref name, ref phone) == false)
                return;
            ByteDataMaker m = new ByteDataMaker();
            m.Init(200);
            m.Add(name);
            m.Add(phone);
            client.Send(Protocols.GetUserNamePhonenumber, m.GetBytes());
        }

        
    }
}
