using System;
using System.Collections;
using System.Collections.Generic;
#if !(UNITY_EDITOR)
using TexasHoldemServer;
#endif

public class THRoutine_ETC {
    public class HoleCardData
    {
        public int UserIdx;
        public byte Card1;
        public byte Card2;
        public HoleCardData()
        {
            UserIdx = -1;
            Card2 = Card1 = 0;
        }
        public HoleCardData(int uIdx, byte c1, byte c2)
        {
            UserIdx = uIdx;
            Card1 = c1;
            Card2 = c2;
        }
    }

    public class ActionWork
    {
        public Action m_Work = null;
        public void Update()
        {
            try
            {
                if (m_Work == null)
                    return;
                m_Work();
            }
            catch(Exception e)
            {
                LogMessageManager.AddLogFile("Action work Update error - " + e.ToString());
            }
        }
    }

    static public string CardByteToString(byte c)
    {
        string r = "";
        switch (c & 0xF0)
        {
            case 0x10:
                r = "C";
                break;
            case 0x20:
                r = "D";
                break;
            case 0x30:
                r = "H";
                break;
            case 0x40:
                r = "S";
                break;
            default:
                return "";
        }
        int n = c & 0x0F;
        if (n == 1)
            n = 14;
        if (n <= 10)
        {
            r += n.ToString();
        }
        else
        {
            switch (n)
            {
                case 11:
                    r += "J";
                    break;
                case 12:
                    r += "Q";
                    break;
                case 13:
                    r += "K";
                    break;
                case 14:
                    r += "A";
                    break;
                default:
                    r += "EE";
                    break;
            }
        }
        return r;
    }

    static public string CardByteToString_Array(byte[] arr)
    {
        string str = "";
        int i, j;
        j = arr.Length;
        for (i = 0; i < j; i++)
        {
            if (i == 0)
            {
                str = CardByteToString(arr[i]);
            }
            else
            {
                str += "/" + CardByteToString(arr[i]);
            }
        }
        return str;
    }


#if !(UNITY_EDITOR)
    static FileWriter m_FileWriter = new FileWriter();
    public int m_GameRoomIndex = -1;
    public string m_GameRoomName = "";
#endif

    static public bool m_StopGame = false;

    public int m_GameLogIndex = -1;

    static public Action<AINode> OnRetrunAINode = null;
    public Action<int> OnExitUser = null;

    /// <summary>
    /// 외부에서 로그함수를 끌어오기 위한 함수
    /// </summary>
    public Action<string> m_Log = null;

    protected void Log(string str)
    {
        if (m_Log == null)
            return;
        m_Log(str);
    }

    /*public enum RoutineDBLogType
    {
        GameStart=0,
        Blind,
        
    }

    protected void RoutineDBLog()*/

    protected void SetGameLogIndex()
    {
#if !(UNITY_EDITOR)
        //m_GameLogIndex = DB.Instance.AddGameLog(m_GameRoomIndex, m_GameRoomName);
        m_GameLogIndex = DB.AddGameLog(m_GameRoomIndex, m_GameRoomName);
#endif
    }

    protected void AddGameLogUser(int UserIdx, byte c1, byte c2)
    {
#if !(UNITY_EDITOR)
        //DB.Instance.AddGameUser(m_GameLogIndex, UserIdx, c1, c2);
        DB.AddGameUser(m_GameLogIndex, UserIdx, c1, c2);
#endif
    }

    protected void AddGameLogDetail(int UserIdx, string type, UInt64 Money)
    {
        //DB.Instance.AddLogDetail(m_GameLogIndex, UserIdx, type, Money);
        DB.AddLogDetail(m_GameLogIndex, UserIdx, type, Money);
    }

    protected void AddGameLogDetail_Result(int UserIdx, byte[] rCard, UInt64 Money, bool IsWin)
    {
        //CardByteToString()
        string str = "";
        if (IsWin)
            str = "Win  /";
        else
            str = "Lose /";

        str += CardByteToString_Array(rCard);
        AddGameLogDetail(UserIdx, str, Money);
        //DB.AddLogDetail(m_GameLogIndex, UserIdx, str, Money);
        //DB.Instance.AddLogDetail(m_GameLogIndex, UserIdx, str, Money);
    }

    protected void AddCommission_Result(int UserIdx, UInt64 Money)
    {
        TexasHoldemServer.DB.AddCommissionResult(m_GameLogIndex, UserIdx, Money);
        //DB.Instance.AddCommissionResult(m_GameLogIndex, UserIdx, Money);
    }

    protected void RoutineDebugLog(string str)
    {
#if !(UNITY_EDITOR)
        m_FileWriter.RoutineDebugLog(m_GameRoomIndex, str);
#else
        Log(str);
#endif
    }
}
