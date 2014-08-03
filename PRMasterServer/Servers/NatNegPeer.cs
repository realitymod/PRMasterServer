using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

namespace PRMasterServer.Servers
{
    public class NatNegPeer
    {
        public IPEndPoint PublicAddress;
        public IPEndPoint CommunicationAddress;
        public bool IsHost;
    }

    public class NatNegClient
    {
        public int ClientId;
        public NatNegPeer Host;
        public NatNegPeer Guest;
    }
}
