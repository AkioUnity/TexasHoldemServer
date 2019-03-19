using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TexasHoldemServer.Network.ProtocolWork
{
    public class ServerProtocolWork
    {
        protected static TexasHoldemServer m_Server = null;

        public static void SetServer(TexasHoldemServer server)
        {
            m_Server = server;
        }

        public virtual bool RecvWork(ClientObject client, Protocols protocol, byte[] data)
        {
            return false;
        }
    }
}
