using System.Collections;
using System;
using System.Data;
using System.Data.Sql;
using System.Data.SqlClient;
using System.IO;
using System.Diagnostics;

public class DBWrapper
{
    protected IDbConnection m_dbcon = null;
    /*static DBWrapper m_Instance = null;

    public static DBWrapper Instance
    {
        get
        {
            if (m_Instance == null)
                m_Instance = new DBWrapper();
            return m_Instance;
        }
    }*/

    public bool IsOpen
    {
        get { if (m_dbcon == null) return false; return true; }
    }

    static public void CheckCreatePath(string path)
    {
        DirectoryInfo di = new DirectoryInfo(path);
        if (di.Exists == false)
            di.Create();
    }

    public bool ConnectDatabase()
    {
        m_dbcon = new SqlConnection();
        try
        {
            //m_dbcon.ConnectionString = "Password=Dnsduqrhd777;Persist Security Info=True;User ID=smj2350;Initial Catalog=TexasHoldem;Data Source=211.238.13.182";
//            m_dbcon.ConnectionString = "Password=Dnsduqrhd777;Persist Security Info=True;User ID=smj2350;Initial Catalog=testGameServer;Data Source=211.238.13.182";
            m_dbcon.ConnectionString = "Password=gaja2010;Persist Security Info=True;User ID=weblogin;Initial Catalog=testGameServer;Data Source=124.158.124.3";
            m_dbcon.Open(); 
        }
        //catch (SqlException e)
        catch (Exception e)
        {
            LogMessageManager.AddLogMessage(e.ToString(), true);
            Debug.WriteLine(e.ToString());
            m_dbcon = null;
            return false;
        }
        return true;
    }

    public void DisconnectDatabase()
    {
        if (m_dbcon == null)
            return;
        m_dbcon.Close();
        m_dbcon = null;
    }

    public bool ExeQuery(string query)
    {
        if (m_dbcon == null)
            return false;
        try
        {
            IDbCommand dbcmd = m_dbcon.CreateCommand();
            dbcmd.CommandText = query;
            dbcmd.ExecuteNonQuery();
            dbcmd.Dispose();
        }
        //catch (SqlException e)
        catch (Exception e)
        {
            Debug.WriteLine("Query : " + query + "\nError : " + e);
            LogMessageManager.AddLogMessage("Query : " + query + "\nError : " + e, true);
            return false;
        }
        return true;
    }

    public int GetSimpleSelectQuery_int(string query)
    {
        if (m_dbcon == null)
            return 0;
        try
        {
            IDbCommand dbcmd = m_dbcon.CreateCommand();
            dbcmd.CommandText = query;
            IDataReader reader = dbcmd.ExecuteReader();
            if (reader.Read() == false)
            {
                dbcmd.Dispose();
                reader.Close();
                return 0;
            }
            if (reader.IsDBNull(0) == true)
            {
                dbcmd.Dispose();
                reader.Close();
                return 0;
            }
            
            dbcmd.Dispose();
            int value = reader.GetInt32(0);
            reader.Close();

            return value;
        }
        //catch (SqlException e)
        catch (Exception e)
        {
            Debug.WriteLine("Query : " + query + "\nError : " + e);
            LogMessageManager.AddLogMessage("Query : " + query + "\nError : " + e, true);
        }
        return 0;
    }

    public string GetSimpleSelectQuery_string(string query)
    {
        if (m_dbcon == null)
            return "";
        try
        {
            IDbCommand dbcmd = m_dbcon.CreateCommand();
            dbcmd.CommandText = query;
            IDataReader reader = dbcmd.ExecuteReader();
            if (reader.Read() == false)
            {
                reader.Close();
                dbcmd.Dispose();
                return "";
            }
            if (reader.IsDBNull(0) == true)
            {
                reader.Close();
                dbcmd.Dispose();
                return "";
            }
            
            string value = reader.GetString(0);
            reader.Close();
            dbcmd.Dispose();
            return value;
        }
        //catch (SqlException e)
        catch (Exception e)
        {
            Debug.WriteLine("Query : " + query + "\nError : " + e);
            LogMessageManager.AddLogMessage("Query : " + query + "\nError : " + e, true);
        }
        return "";
    }

    public float GetSimpleSelectQuery_float(string query)
    {
        if (m_dbcon == null)
            return 0;
        try
        {
            IDbCommand dbcmd = m_dbcon.CreateCommand();
            dbcmd.CommandText = query;
            IDataReader reader = dbcmd.ExecuteReader();
            if (reader.Read() == false)
            {
                reader.Close();
                dbcmd.Dispose();
                return 0;
            }
            if (reader.IsDBNull(0) == true)
            {
                reader.Close();
                dbcmd.Dispose();
                return 0;
            }
            
            float value = reader.GetFloat(0);
            reader.Close();
            dbcmd.Dispose();
            return value;
        }
        //catch (SqlException e)
        catch(Exception e)
        {
            Debug.WriteLine("Query : " + query + "\nError : " + e);
            LogMessageManager.AddLogMessage("Query : " + query + "\nError : " + e, true);
        }
        return 0;
    }

    public double GetSimpleSelectQuery_double(string query)
    {
        if (m_dbcon == null)
            return 0;
        try
        {
            IDbCommand dbcmd = m_dbcon.CreateCommand();
            dbcmd.CommandText = query;
            IDataReader reader = dbcmd.ExecuteReader();
            if (reader.Read() == false)
            {
                reader.Close();
                dbcmd.Dispose();
                return 0;
            }
            if (reader.IsDBNull(0) == true)
            {
                reader.Close();
                dbcmd.Dispose();
                return 0;
            }

            double value = reader.GetDouble(0);
            reader.Close();
            dbcmd.Dispose();
            return value;
        }
        //catch (SqlException e)
        catch (Exception e)
        {
            Debug.WriteLine("Query : " + query + "\nError : " + e);
            LogMessageManager.AddLogMessage("Query : " + query + "\nError : " + e, true);
        }
        return 0;
    }

    public IDataReader SelectQuery(string query)
    {
        if (m_dbcon == null)
            return null;
        try
        {
            IDbCommand dbcmd = m_dbcon.CreateCommand();
            dbcmd.CommandText = query;
            IDataReader reader = dbcmd.ExecuteReader();
            dbcmd.Dispose();
            return reader;
        }
        //catch (SqlException e)
        catch (Exception e)
        {
            Debug.WriteLine("Query : " + query + "\nError : " + e);
            LogMessageManager.AddLogMessage("Query : " + query + "\nError : " + e, true);
        }
        return null;
    }
}
