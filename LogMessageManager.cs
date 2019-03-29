using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

public class FileWriter
{
    public FileStream file = null;
    public StreamWriter sw = null;

    public void WriteLine(string str)
    {
        if (sw == null)
            return;
        sw.WriteLine(str);
    }

    public void Open(string path)
    {
        Close();
        file = new FileStream(path, FileMode.Append, FileAccess.Write);
        sw = new StreamWriter(file);
    }

    public void Close()
    {
        if (sw != null)
            sw.Close();
        if (file != null)
            file.Close();
        sw = null;
        file = null;
    }


    public void FileLog(string str)
    {
        DateTime dt = DateTime.Now;
        //string path = dt.ToString("yyyyMMdd");
        string path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "/ServerLog/" + dt.ToString("yyyyMMdd");
        CheckCreatePath(path);
        path += "/";
        //path += dt.ToString("HHmm") + ".txt";
        path += dt.ToString("HH") + ".txt";
        Open(path);
        WriteLine(dt.ToString("yyyyMMdd-HHmmss(fff) : ") + str);
        Close();
    }

    public void RoutineDebugLog(int RoomIdx, string str)
    {
        DateTime dt = DateTime.Now;
        //string path = dt.ToString("yyyyMMdd");
        string path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "/ServerLog/DebugRoutine/" + dt.ToString("yyyyMMdd");
        CheckCreatePath(path);
        path += "/";
        //path += dt.ToString("HHmm") + ".txt";
        path += "Room-" + RoomIdx + ".txt";
        Open(path);
        WriteLine(dt.ToString("yyyyMMdd-HHmmss(fff) : ") + str);
        Close();
    }

    static public void CheckCreatePath(string path)
    {
        DirectoryInfo di = new DirectoryInfo(path);
        if (di.Exists == false)
            di.Create();
    }
}

class LogMessageManager
{
    static object m_Lock = new object();
    static List<string> m_Message = new List<string>();
    static FileWriter m_FileWriter = new FileWriter();

    public static void AddLogMessage(string message, bool FileLog)
    {
        lock (m_Lock)
        {
            m_Message.Add(message);
            if (FileLog)
            {
                m_FileWriter.FileLog(message);
            }
        }
        Debug.WriteLine(message);
    }

    public static void AddLogFile(string message)
    {
        lock (m_Lock)
        {
            m_FileWriter.FileLog(message);
        }
    }

    public static string GetLogMessage()
    {
        lock (m_Lock)
        {
            if (m_Message.Count == 0)
                return null;
            string str = m_Message[0];
            m_Message.RemoveAt(0);
            return str;
        }
    }
}
