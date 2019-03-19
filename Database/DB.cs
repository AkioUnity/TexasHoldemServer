using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Security.Cryptography;

namespace TexasHoldemServer
{
    
    public class UserMessageData
    {
        public int idx;
        public int UserIdx;
        public UInt64 Money;
        public string DateTime;
        public string Message;
        public int Type;
        public byte[] GetBytes()
        {
            ByteDataMaker m = new ByteDataMaker();
            m.Add(idx);
            m.Add(Money);
            m.Add(DateTime);
            m.Add(Message);
            m.Add(Type);
            return m.GetBytes();
        }
    }

    class DB : DBWrapper
    {
        //static DB m_Instance = null;

        const string UserInfoTable = "UserInfo";
        const string MoneyTable = "TexasHoldemMoney";

        /*public static DB Instance
        {
            get
            {
                if (m_Instance == null)
                    m_Instance = new DB();
                return m_Instance;
            }
        }*/

        public static DB CreateDB(string mes)
        {
            DB db = new DB();
            if (db.ConnectDatabase() == false)
            {
                LogMessageManager.AddLogMessage("ConnectDB Failed - " + mes, true);
                return null;
            }
            return db;
        }

        public static int GetUserIndex(string id)
        {
            DB db = DB.CreateDB("GetUserIndex");
            if (db == null)
                return -1;
            int idx = db.GetSimpleSelectQuery_int("select idx from "+ UserInfoTable + " where UserID='" + id + "'");
            db.DisconnectDatabase();
            return idx;
        }

        public static string MD5HashFunc(string str)
        {
            StringBuilder MD5Str = new StringBuilder();
            byte[] byteArr = Encoding.ASCII.GetBytes(str);
            byte[] resultArr = (new MD5CryptoServiceProvider()).ComputeHash(byteArr);
            //for (int cnti = 1; cnti < resultArr.Length; cnti++) (2010.06.27)
            for (int cnti = 0; cnti < resultArr.Length; cnti++)
            {
                MD5Str.Append(resultArr[cnti].ToString("X2"));
            }
            return MD5Str.ToString();
        }

        public static int CheckUserPassword(string id, string pass)
        {
            string str = MD5HashFunc(pass);
            string str2 = MD5HashFunc(pass);
            pass = str;
            DB db = DB.CreateDB("CheckUserPassword");
            if (db == null)
                return -1;
            int idx = db.GetSimpleSelectQuery_int("select Idx from "+ UserInfoTable + " where UserID='" + id + "' and Password='" + pass + "'");
            db.DisconnectDatabase();
            return idx;
        }

        public void ChangePassMD5(string id)
        {
            string pass = GetSimpleSelectQuery_string("select Password from UserInfo where UserID='" + id + "'");
            if (pass.Length > 15)
                return;
            string newpass = MD5HashFunc(pass);
            ExeQuery("update UserInfo set Password='" + newpass + "' where UserId='" + id + "'");
        }

        public void ChangePassAllMD5()
        {
            List<string> d = new List<string>();
            try
            {
                IDataReader r = SelectQuery("select UserId from UserInfo");
                while(r.Read())
                {
                    string id = r.GetString(0);
                    d.Add(id);
                }
                int i, j;
                j = d.Count;
                r.Close();
                for (i = 0; i < j; i++)
                {
                    ChangePassMD5(d[i]);
                }
            }
            catch(Exception e)
            {
                LogMessageManager.AddLogMessage("ChangePassAllMD5 : " + e.ToString(), true);
            }
            
        }

        public static bool CheckUserNickName(string name)
        {
            DB db = DB.CreateDB("CheckUserNickName");
            if (db == null)
                return false;
            int idx = db.GetSimpleSelectQuery_int("select Idx from "+ UserInfoTable + " where Nickname='" + name + "'");
            db.DisconnectDatabase();
            return idx == 0 ? true : false;
        }

        public static int GetRegisterMoney()
        {
            DB db = DB.CreateDB("GetRegisterMoney");
            if (db == null)
                return 0;
            int r = db.GetSimpleSelectQuery_int("select Money from RegisterMoney");
            db.DisconnectDatabase();
            return r;
        }

        public static bool RegisterUser(string id, string pass, string nickname, string name, string phone)
        {
            if (GetUserIndex(id) != 0)
                return false;
            DB db = DB.CreateDB("RegisterUser");
            if (db == null)
                return false;
            try
            {
                //if (db.GetUserIndex(id) != 0)return false;
                pass = MD5HashFunc(pass);
                string str = "Insert into "+ UserInfoTable + " (UserId,Password,Nickname,Name,Phonenumber) values ('" + id + "','" + pass + "','" +
                    nickname + "','" + name + "','" + phone + "')";

                if (db.ExeQuery(str) == false)
                {
                    db.DisconnectDatabase();
                    LogMessageManager.AddLogMessage(str, true);
                    return false;
                }
                db.DisconnectDatabase();
                return true;
            }
            catch(Exception e)
            {
                db.DisconnectDatabase();
                LogMessageManager.AddLogMessage("RegisterUser : " + e.ToString(), true);
                return false;
            }
        }

        
        public static bool ChangePassword(int UserIdx, string nowpass, string newpass)
        {
            string pass1 = MD5HashFunc(nowpass);
            string pass2 = MD5HashFunc(newpass);

            DB db = DB.CreateDB("ChangePassword");
            if (db == null)
                return false;
            //select count(*) from UserInfo where Idx=7 and Password='0CC175B9C0F1B6A831C399E269772661'
            string q = "select count(*) from UserInfo where Idx=" + UserIdx + " and Password='" + pass1 + "'";
            if (db.GetSimpleSelectQuery_int(q) == 0)
            {
                db.DisconnectDatabase();
                return false;
            }
            q = "update UserInfo set Password='" + pass2 + "' where Idx=" + UserIdx;
            db.ExeQuery(q);
            db.DisconnectDatabase();
            return true;
        }

        public static bool ChangeNickname(int UserIdx, string newNick)
        {
            if (CheckUserNickName(newNick) == false)
                return false;
            DB db = DB.CreateDB("ChangeNickname");
            if (db == null)
                return false;
            string q = "update UserInfo set Nickname='" + newNick + "' where Idx=" + UserIdx;
            bool br = db.ExeQuery(q);
            db.DisconnectDatabase();
            return br;
        }

        public static bool ChangePhonenumber(int UserIdx, string phoneNumber)
        {
            DB db = DB.CreateDB("ChangePhonenumber");
            if (db == null)
                return false;
            string q = "update UserInfo set Phonenumber='" + phoneNumber + "' where Idx=" + UserIdx;
            bool br = db.ExeQuery(q);
            db.DisconnectDatabase();
            return br;
        }

        public static bool ChangeUserName(int UserIdx, string Name)
        {
            DB db = DB.CreateDB("ChangeUserName");
            if (db == null)
                return false;
            string q = "update UserInfo set Name='" + Name + "' where Idx=" + UserIdx;
            bool r = db.ExeQuery(q);
            db.DisconnectDatabase();
            return r;
        }

        public static UserInfo GetUserInfo(int UserIdx)
        {
            DB db = DB.CreateDB("GetUserInfo");
            if (db == null)
                return null;
            try
            {
                if (db.GetSimpleSelectQuery_int("select count(*) from " + UserInfoTable + " where Idx=" + UserIdx) == 0)
                {
                    db.DisconnectDatabase();
                    return null;
                }
                UserInfo info = new UserInfo();
                info.UserIndex = UserIdx;
                info.UserName = db.GetSimpleSelectQuery_string("select Nickname from "+ UserInfoTable + " where Idx=" + UserIdx);
                info.Money = GetUserMoney(UserIdx, false);
                info.Avatar = db.GetSimpleSelectQuery_int("select Avatar from UserAvatar where UserIdx=" + UserIdx);
                db.DisconnectDatabase();
                return info;
            }
            catch(Exception e)
            {
                db.DisconnectDatabase();
                LogMessageManager.AddLogMessage("GetUserInfo : " + e.ToString(), true);
                return null;
            }
        }

        public static bool GetUserNamePhoneNumber(int UserIdx, ref string UserName, ref string PhoneNumber)
        {
            DB db = DB.CreateDB("GetUserNamePhoneNumber");
            if (db == null)
                return false;
            IDataReader r = db.SelectQuery("select Name,Phonenumber from UserInfo where Idx=" + UserIdx);
            if (r == null)
            {
                db.DisconnectDatabase();
                return false;
            }
            if(r.Read()==false)
            {
                r.Close();
                db.DisconnectDatabase();
                return false;
            }
            UserName = r.GetString(0);
            PhoneNumber = r.GetString(1);
            r.Close();
            db.DisconnectDatabase();
            return true;
        }

        public static string UInt64ToStringPart(UInt64 v)
        {
            string str = v.ToString("X");
            return str.PadLeft(16, '0');
        }

        public static string UInt64To16String(UInt64 v)
        {
            string str = v.ToString("X");
            str = "0x" + str.PadLeft(16, '0');
            return str;
        }

        public static void SetUserMoney(int UserIdx, UInt64 Money)
        {
            DB db = DB.CreateDB("SetUserMoney m");
            if (db == null)
                return;
            string query = "";
            string MoneyStr = UInt64To16String(Money);
            if (db.GetSimpleSelectQuery_int("select count(*) from "+ MoneyTable + " where UserIdx=" + UserIdx) == 0)
            {
                db.DisconnectDatabase();
                SetUserMoney(UserIdx, Money, 0);
            }
            else
            {
                query = "update "+ MoneyTable + " set GameMoney=" + MoneyStr + " where UserIdx=" + UserIdx;
                db.ExeQuery(query);
                db.DisconnectDatabase();
            }
            
            
        }

        public static void SetUserBankMoney(int UserIdx, UInt64 Money)
        {
            DB db = DB.CreateDB("SetUserMoney b");
            if (db == null)
                return;
            string query = "";
            string MoneyStr = UInt64To16String(Money);
            if (db.GetSimpleSelectQuery_int("select count(*) from "+ MoneyTable + " where UserIdx=" + UserIdx) == 0)
            {
                db.DisconnectDatabase();
                SetUserMoney(UserIdx, 0, Money);
            }
            else
            {
                query = "update "+ MoneyTable + " set BankMoney=" + MoneyStr + " where UserIdx=" + UserIdx;
                db.ExeQuery(query);
                db.DisconnectDatabase();
            }
            
        }

        public static void SetUserMoney(int UserIdx, UInt64 Money, UInt64 Bank)
        {
            DB db = DB.CreateDB("SetUserMoney d");
            if (db == null)
                return;
            string query = "";
            string MoneyStr, BankStr;
            MoneyStr = UInt64To16String(Money);
            BankStr = UInt64To16String(Bank);

            if (db.GetSimpleSelectQuery_int("select count(*) from "+ MoneyTable + " where UserIdx=" + UserIdx) == 0)
            {
                query = "Insert into "+ MoneyTable + " (UserIdx,GameMoney,BankMoney) values ("
                    + UserIdx + "," + MoneyStr + "," + BankStr + ")";
            }
            else
            {
                query = "update "+ MoneyTable + " set GameMoney=" + MoneyStr + ", BankMoney=" + BankStr + " where UserIdx=" + UserIdx;
            }
            db.ExeQuery(query);
            db.DisconnectDatabase();
        }


        public static UInt64 GetUserMoney(int UserIdx, bool IsBank)
        {
            DB db = DB.CreateDB("GetUserMoney");
            if (db == null)
                return 0;
            string qstr = "select GameMoney from "+ MoneyTable + " where UserIdx=";
            if(IsBank)
                qstr= "select BankMoney from "+ MoneyTable + " where UserIdx=";
            System.Data.IDataReader r = db.SelectQuery(qstr + UserIdx);
            if (r.Read() == false)
            {
                r.Close();
                db.DisconnectDatabase();
                return 0;
            }
            byte[] buf = new byte[8];
            r.GetBytes(0, 0, buf, 0, 8);
            r.Close();
            buf = buf.Reverse().ToArray();
            UInt64 money = BitConverter.ToUInt64(buf, 0);
            db.DisconnectDatabase();
            return money;
        }

        public static void SetUserAvatar(int UserIdx, int n)
        {
            DB db = DB.CreateDB("SetUserAvatar");
            if (db == null)
                return;
            string query = "";
            if (db.GetSimpleSelectQuery_int("select count(*) from UserAvatar where UserIdx=" + UserIdx) == 0)
            {
                query = "insert into UserAvatar (UserIdx,Avatar) values (" + UserIdx + "," + n + ")";
            }
            else
            {
                query = "update UserAvatar set Avatar=" + n + " where UserIdx=" + UserIdx;
            }
            db.ExeQuery(query);
            db.DisconnectDatabase();
        }

        public static int GetUserIndex_Nickname(string Nickname)
        {
            DB db = DB.CreateDB("GetUserIndex_Nickname");
            if (db == null)
                return -1;
            string query = "select idx from UserInfo where Nickname='" + Nickname + "'";
            int idx = db.GetSimpleSelectQuery_int(query);
            db.DisconnectDatabase();
            return idx;
        }

        

        public static void GiftMoney(int SrcUserIdx, UInt64 Money, int DstUserIdx)
        {
            UInt64 m = GetUserMoney(SrcUserIdx, false);
            if (m < Money)
                Money = m;
            m -= Money;
            SetUserMoney(SrcUserIdx, m);

            string message = "Gift from " + GetUserNickName(SrcUserIdx);
            AddUserMessage(DstUserIdx, Money, message);
        }

        public static string GetNowDateTime()
        {
            DateTime dt = DateTime.Now;
            return dt.ToString("yyyyMMddHHmmss");
        }

        public static int AddGameLog(int roomIdx,string roomName)
        {
            DB db = DB.CreateDB("AddGameLog");
            if (db == null)
                return -1;
            string query = "insert into GameLog (RoomIdx,RoomName,DateTime,OnCard) values (" + roomIdx + ",'" + roomName + "','" + GetNowDateTime() + "','');";
            if (db.ExeQuery(query) == false)
            {
                LogMessageManager.AddLogMessage("AddGameLog failed - Room Index: " + roomIdx, true);
                db.DisconnectDatabase();
                return -1;
            }

            query = "select top 1 * from GameLog where RoomIdx = " + roomIdx + " order by DateTime desc;";
            int LogIdx = db.GetSimpleSelectQuery_int(query);
            db.DisconnectDatabase();
            return LogIdx;
        }

        public static void SetGameLogOnCard(int GameIdx, byte[] OnCard)
        {
            DB db = DB.CreateDB("SetGameLogOnCard");
            if (db == null)
                return;
            string OnCardString = "";
            int i;
            for (i = 0; i < 4; i++)
            {
                OnCardString += THRoutine_ETC.CardByteToString(OnCard[i]) + " / ";
            }
            OnCardString += THRoutine_ETC.CardByteToString(OnCard[4]);

            string query = "update GameLog set OnCard='" + OnCardString + "' where Idx=" + GameIdx;
            db.ExeQuery(query);
            db.DisconnectDatabase();
        }

        public static void AddLogDetail(int GameLogIdx, int UserIdx, string type, UInt64 Money)
        {
            DB db = DB.CreateDB("AddLogDetail");
            if (db == null)
                return;
            string str = "insert into GameLogDetail (GameIdx,UserIdx,Type,Money) values (" + GameLogIdx + "," + UserIdx + ",'" +
                type + "'," + Money + ")";
            db.ExeQuery(str);
            db.DisconnectDatabase();
        }
        

        public static void AddGameUser(int GameIdx, int UserIdx, byte c1, byte c2)
        {
            DB db = DB.CreateDB("AddGameUser");
            if (db == null)
                return;

            string query = "insert into GameLogMember (GameIdx,UserIdx,Card1,Card2) values (" + GameIdx + "," + UserIdx + ",'" +
                THRoutine_ETC.CardByteToString(c1) + "','" + THRoutine_ETC.CardByteToString(c2) + "')";
            db.ExeQuery(query);
            db.DisconnectDatabase();
        }

        public static bool CheckMaster(int UserIdx)
        {
            DB db = DB.CreateDB("CheckMaster");
            if (db == null)
                return false;
            int c = db.GetSimpleSelectQuery_int("select count(*) from GameMaster where UserIdx=" + UserIdx);
            db.DisconnectDatabase();
            return c == 0 ? false : true;
        }

        public string GetUserID(int UserIdx)
        {
            return GetSimpleSelectQuery_string("select UserID from UserInfo where idx=" + UserIdx);
        }

        public static string GetUserNickName(int UserIdx)
        {
            DB db = DB.CreateDB("GetUserNickName");
            if (db == null)
                return "";
            string r = db.GetSimpleSelectQuery_string("select Nickname from UserInfo where idx=" + UserIdx);
            db.DisconnectDatabase();
            return r;
        }

        /// <summary>
        /// no use
        /// </summary>
        /// <param name="UserIdx"></param>
        /// <param name="Money"></param>
        public static void AddDepositRequest(int UserIdx, UInt64 Money)
        {
            DB db = DB.CreateDB("AddDepositRequest");
            if (db == null)
                return;
            string query = "insert into DepositRequest(UserIdx,Money,DateTime,UserID,Result) values (" + UserIdx + "," + UInt64To16String(Money) +
                ",'" + GetNowDateTime() + "','" + db.GetUserID(UserIdx) + "',0)";
            db.ExeQuery(query);
            db.DisconnectDatabase();
        }

        public static void AddChargeRequest(int UserIdx, UInt64 Money, string AccountName)
        {
            DB db = DB.CreateDB("AddChargeRequest");
            if (db == null)
                return;
            string query = "insert into ChargeRequest(UserIdx,Money,DateTime,UserID,AccountName,Result) values (" + UserIdx + "," + UInt64To16String(Money) +
                ",'" + GetNowDateTime() + "','" + db.GetUserID(UserIdx) + "','" + AccountName + "',0)";
            db.ExeQuery(query);
            string message = db.GetSimpleSelectQuery_string("select Message from DefineMessage where MessageNumber=0");
            db.DisconnectDatabase();
            AddUserMessage(UserIdx, 0, message);
        }

        public static void AddWithdrawal(int UserIdx, UInt64 Money, string BankName, string AccountName, string AccountNumber)
        {
            UInt64 BMoney = GetUserMoney(UserIdx, true);
            if (BMoney < Money)
                return;
            
            BMoney -= Money;
            SetUserBankMoney(UserIdx, BMoney);

            DB db = DB.CreateDB("AddWithdrawal");
            if (db == null)
                return;


            string query = "insert into Withdrawal(UserIdx,Money,DateTime,UserID,BankName,AccountNumber,AccountName,Result) values (" + UserIdx + "," + UInt64To16String(Money) +
                ",'" + GetNowDateTime() + "','" + db.GetUserID(UserIdx) + "','" + BankName + "','" + AccountNumber + "','" + AccountName + "',0)";
            
            db.ExeQuery(query);
            db.DisconnectDatabase();
        }

        public static void AddBlockUser(string UserID)
        {
            DB db = DB.CreateDB("AddBlockUser");
            if (db == null)
                return;
            string query = "insert into BlockUser (UserIdx) (select Idx from UserInfo where UserID='" + UserID + "')";
            if (db.ExeQuery(query) == true)
                LogMessageManager.AddLogMessage("User Block - id : " + UserID, true);
            db.DisconnectDatabase();
        }

        public static bool CheckBlockUser(int UserIdx)
        {
            DB db = DB.CreateDB("CheckBlockUser");
            if (db == null)
                return false;
            string query = "select count(*) from BlockUser where UserIdx=" + UserIdx;
            int c = db.GetSimpleSelectQuery_int(query);
            db.DisconnectDatabase();
            if (c == 0)
                return false;
            return true;
        }

        public static List<UserMessageData> GetMessageDataList(int UserIdx)
        {
            List<UserMessageData> r = new List<UserMessageData>();

            DB db = DB.CreateDB("GetMessageDataList");
            if (db == null)
                return r;

            string query = "select Idx,Money,DateTime,Message,Type from UserMessage where Useridx=" + UserIdx + " and receive = 0";

            IDataReader reader = db.SelectQuery(query);
            if (reader == null)
            {
                db.DisconnectDatabase();
                return r;
            }
            try
            {
                while (reader.Read())
                {
                    byte[] buf = new byte[8];
                    UserMessageData data = new UserMessageData();
                    data.UserIdx = UserIdx;
                    data.idx = reader.GetInt32(0);
                    reader.GetBytes(1, 0, buf, 0, 8);
                    buf = buf.Reverse().ToArray();
                    data.Money = BitConverter.ToUInt64(buf, 0);
                    data.DateTime = reader.GetString(2);
                    data.Message = reader.GetString(3);
                    data.Type = reader.GetInt32(4);
                    r.Add(data);
                }
                reader.Close();
                //return r;
            }
            catch(Exception e)
            {
                reader.Close();
                LogMessageManager.AddLogMessage("GetMessageDataList error : " + e.ToString(), true);
            }
            db.DisconnectDatabase();
            return r;
        }

        public static int GetMessageDataCount(int UserIdx)
        {
            DB db = DB.CreateDB("GetMessageDataCount");
            if (db == null)
                return 0;
            string query = "select count(idx) receive from UserMessage where Useridx=" + UserIdx + " and receive = 0";
            int r = db.GetSimpleSelectQuery_int(query);
            db.DisconnectDatabase();
            return r;
        }

        public static void SetMessageReceive(int MessageIdx)
        {
            DB db = DB.CreateDB("SetMessageReceive");
            if (db == null)
                return;
            string query = "update UserMessage set receive=1, ReceiveDateTime='" + GetNowDateTime() + "' where Idx=" + MessageIdx;
            if (db.ExeQuery(query) == false)
            {
                db.DisconnectDatabase();
                return;
            }
            LogMessageManager.AddLogFile("MessageReceive - " + MessageIdx);

            query = "select UserIdx,Money from UserMessage where Idx=" + MessageIdx;
            
            IDataReader r = db.SelectQuery(query);
            if (r.Read() == false)
            {
                r.Close();
                db.DisconnectDatabase();
                return;
            }
            int userIdx = r.GetInt32(0);
            byte[] buf = new byte[8];
            r.GetBytes(1, 0, buf, 0, 8);
            r.Close();
            db.DisconnectDatabase();

            buf = buf.Reverse().ToArray();
            UInt64 AddMoney = BitConverter.ToUInt64(buf, 0);
            if (AddMoney == 0)
                return;
            /*
            UInt64 NowMoney = GetUserMoney(userIdx, false);
            SetUserMoney(userIdx, NowMoney + AddMoney);
            */
            UInt64 NowMoney = GetUserMoney(userIdx, true);
            SetUserBankMoney(userIdx, NowMoney + AddMoney);
        }

        public static void AddUserMessage(int UserIdx, UInt64 Money, string Message)
        {
            DB db = DB.CreateDB("AddUserMessage");
            if (db == null)
                return;
            string query = "insert into UserMessage (UserIdx,Money,DateTime,Message,Receive,ReceiveDateTime,Type) values (" + UserIdx + "," +
               UInt64To16String(Money) + ",'" + GetNowDateTime() + "','" + Message + "',0,'00000000000000',0);";
            db.ExeQuery(query);
            db.DisconnectDatabase();
        }



        public class BonusData
        {
            //public string str;
            public int Type;
            public UInt64 Money;
        }

        public static BonusData CheckBonusMoney(int score)
        {
            DB db = DB.CreateDB("CheckBonusMoney");
            if (db == null)
                return null;
            BonusData d = null;
            int t = (score & 0x0F000000) >> 24;
            string query = "select Type,Money from TexasHoldemBonus where Criterion=" + t;
            IDataReader r = db.SelectQuery(query);
            if (r == null)
            {
                db.DisconnectDatabase();
                return null;
            }
            if (r.Read()==false)
            {
                r.Close();
                db.DisconnectDatabase();
                return null;
            }
            d = new BonusData();
            d.Type = r.GetInt32(0);
            //d.str = r.GetString(1);
            byte[] buf = new byte[8];
            r.GetBytes(1, 0, buf, 0, 8);
            buf = buf.Reverse().ToArray();
            d.Money = BitConverter.ToUInt64(buf, 0);
            r.Close();
            db.DisconnectDatabase();
            return d;
        }

        public static void ResetRoomData()
        {
            DB db = DB.CreateDB("ResetRoomData");
            if (db == null)
                return;
            db.ExeQuery("delete from RoomInfo");
            db.ExeQuery("delete from RoomMember");
            db.DisconnectDatabase();
        }

        public static int CreateRoom(string RoomName, int BlindType)
        {
            if (GetRoomIndex(RoomName) > 0)
                return -1;
            DB db = DB.CreateDB("CreateRoom");
            if (db == null)
                return -1;
            string query = "insert into RoomInfo (RoomName,BlindType) values ('" + RoomName + "'," + BlindType + ")";
            if (db.ExeQuery(query) == false)
                return -1;
            query = "select RoomIdx from RoomInfo where RoomName='" + RoomName + "' and BlindType=" + BlindType;
            int r = db.GetSimpleSelectQuery_int(query);
            db.DisconnectDatabase();
            return r;
        }

        public static int GetRoomIndex(string RoomName)
        {
            DB db = DB.CreateDB("GetRoomIndex");
            if (db == null)
                return -1;
            int r = db.GetSimpleSelectQuery_int("select RoomIdx from RoomInfo where RoomName='" + RoomName + "'");
            db.DisconnectDatabase();
            return r;
        }

        public static void RemoveRoom(int RoomIndex)
        {
            DB db = DB.CreateDB("RemoveRoom");
            if (db == null)
                return;
            db.ExeQuery("delete from RoomInfo where RoomIdx=" + RoomIndex);
            db.DisconnectDatabase();
        }

        public static void AddRoomMember(int RoomIndex, int UserIdx)
        {
            DB db = DB.CreateDB("AddRoomMember");
            if (db == null)
                return;
            db.ExeQuery("insert into RoomMember (RoomIdx,UserIdx) values (" + RoomIndex + "," + UserIdx + ")");
            db.DisconnectDatabase();
        }

        public static void RemoveRoomMemeber(int RoomIndex, int UserIdx)
        {
            DB db = DB.CreateDB("RemoveRoomMemeber");
            if (db == null)
                return;
            db.ExeQuery("delete from RoomMember where RoomIdx=" + RoomIndex + " and UserIdx=" + UserIdx);
            db.DisconnectDatabase();
        }

        public static void SetRoomMember(int RoomIndex, List<int> member)
        {
            DB db = DB.CreateDB("SetRoomMember");
            if (db == null)
                return;
            db.ExeQuery("delete from RoomMember where RoomIdx=" + RoomIndex);
            int i, j;
            j = member.Count;
            for (i = 0; i < j; i++)
            {
                db.ExeQuery("insert into RoomMember (RoomIdx,UserIdx) values (" + RoomIndex + "," + member[i] + ")");
            }
            db.DisconnectDatabase();
        }

        public struct AIReadyData
        {
            public int BotIdx;
            public int RoomIdx;
            public UInt64 Money;
        }

        static public UInt64 GetUInt64(IDataReader r, int n)
        {
            byte[] buf = new byte[8];
            r.GetBytes(n, 0, buf, 0, 8);
            buf = buf.Reverse().ToArray();
            UInt64 money = BitConverter.ToUInt64(buf, 0);
            return money;
        }

        public static AINode CreateAINode(int AI_Idx)
        {
            DB db = DB.CreateDB("AddAINode_DB");
            if (db == null)
                return null;
            IDataReader r = null;
            try
            {
                r = db.SelectQuery("select BotNickname,Avatar from BotInfo where Idx=" + AI_Idx);
                AINode ai = new AINode();
                if (r.Read() == false)
                    return null;
                ai.UserName = r.GetString(0);
                ai.Avatar = r.GetInt32(1);
                ai.UserIndex = AI_Idx;
                r.Close();
                db.DisconnectDatabase();
                return ai;
            }
            catch(Exception e)
            {
                LogMessageManager.AddLogMessage("GetMessageDataList error : " + e.ToString(), true);
                try
                {
                    r.Close();
                }
                catch (Exception ex) { }
            }
            db.DisconnectDatabase();
            return null;
        }

        public static void SetAIState(int AI_Idx,int state)
        {
            DB db = DB.CreateDB("SetAIState");
            if (db == null)
                return;
            db.ExeQuery("update BotPlay set State=" + state + " where BotIdx=" + AI_Idx);
            db.DisconnectDatabase();
        }

        public static List<AIReadyData> GetReadyAIIndex()
        {
            DB db = DB.CreateDB("GetReadyAIIndex");
            if (db == null)
                return new List<AIReadyData>();
            //select BotIdx, Money,RoomIdx from BotPlay where State=0
            List<AIReadyData> d = new List<AIReadyData>();
            IDataReader r = null;
            try
            {
                r = db.SelectQuery("select BotIdx, Money,RoomIdx from BotPlay where State=0");
                
                while (r.Read())
                {
                    AIReadyData ai = new AIReadyData();
                    ai.BotIdx = r.GetInt32(0);
                    ai.Money = GetUInt64(r, 1);
                    ai.RoomIdx = r.GetInt32(2);
                    d.Add(ai);
                }
                r.Close();
                //return d;
            }
            catch(Exception e)
            {
                d.Clear();

                LogMessageManager.AddLogMessage("GetMessageDataList error : " + e.ToString(), true);
                try
                {
                    r.Close();
                }
                catch(Exception ex)
                {
                }
            }
            db.DisconnectDatabase();
            return d;
        }

        public static List<int> GetOutRequestAIList()
        {
            DB db = DB.CreateDB("GetOutRequestAIList");
            if (db == null)
                return new List<int>();
            List<int> rd = new List<int>();
            IDataReader r = null;
            try
            {
                r = db.SelectQuery("select BotIdx from BotPlay where State=3");
                while(r.Read())
                {
                    rd.Add(r.GetInt32(0));
                }
                r.Close();
            }
            catch(Exception e)
            {
                LogMessageManager.AddLogMessage("GetMessageDataList error : " + e.ToString(), true);
                try
                {
                    r.Close();
                }
                catch (Exception ex)
                {
                }
            }
            db.DisconnectDatabase();
            return rd;
        }

        public static void ClearAIPlayDB()
        {
            DB db = DB.CreateDB("ClearAIPlayDB");
            if (db == null)
                return;
            try
            {
                db.ExeQuery("delete from BotPlay");
            }
            catch (Exception e)
            {
                LogMessageManager.AddLogMessage("GetMessageDataList error : " + e.ToString(), true);
            }
            db.DisconnectDatabase();
        }

        public static double GetCommission()
        {
            DB db = DB.CreateDB("GetCommission");
            if (db == null)
                return 0;
            double r = db.GetSimpleSelectQuery_double("select Commission from THCommissionSetting");
            db.DisconnectDatabase();
            return r;
        }

        public static void AddCommissionResult(int GameIdx, int UserIdx, UInt64 Money)
        {
            DB db = DB.CreateDB("AddCommissionResult");
            if (db == null)
                return;
            string q = "insert into TexasHoldemCommission (GameIdx,UserIdx,Commission) values (" + GameIdx + "," + UserIdx + "," + Money + ")";
            db.ExeQuery(q);
            db.DisconnectDatabase();
        }

        public static bool StopGame()
        {
            DB db = DB.CreateDB("StopGame");
            if (db == null)
                return false;
            if (db.GetSimpleSelectQuery_int("select count(CheckStop) from StopPlay") == 0)
            {
                db.DisconnectDatabase();
                return false;
            }
            if (db.GetSimpleSelectQuery_int("select CheckStop from StopPlay") > 0)
            {
                db.DisconnectDatabase();
                return true;
            }
            db.DisconnectDatabase();
            return false;
        }

        public static bool GetBotCreateRoom(out int BlindType,out int BotIdx,out UInt64 Money,out string RoomName)
        {
            BlindType = 0;
            BotIdx = 0;
            Money = 0;
            RoomName = "";

            DB db = DB.CreateDB("GetBotCreateRoom");
            if (db == null)
                return false;
            
            IDataReader r = db.SelectQuery("select BlindType,BotIdx,BotMoney,RoomName,Id from CreateBotRoom");
            if (r == null)
            {
                db.DisconnectDatabase();
                return false;
            }
            if(r.Read()==false)
            {
                r.Close();
                db.DisconnectDatabase();
                return false;
            }
            BlindType = r.GetInt32(0);
            BotIdx = r.GetInt32(1);
            Money = GetUInt64(r, 2);
            RoomName = r.GetString(3);
            int id = r.GetInt32(4);
            r.Close();
            db.ExeQuery("delete from CreateBotRoom where Id=" + id);
            db.DisconnectDatabase();
            return true;
        }

        public static void InsertAIPlay(int BotIndex, UInt64 BotMoney, int RoomIndex)
        {
            DB db = DB.CreateDB("InsertAIPlay");
            if (db == null)
                return;
            db.ExeQuery("insert into BotPlay (BotIdx,Money,RoomIdx,State) values (" + BotIndex + "," + BotMoney + "," + RoomIndex + ",0)");
            db.DisconnectDatabase();
        }
    }
}