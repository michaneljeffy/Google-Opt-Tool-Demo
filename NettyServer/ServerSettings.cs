using System;
using System.Collections.Generic;
using System.Text;

namespace NettyServer
{
    public static class ServerSettings
    {
        public static bool IsSsl { get; set; } = false;

        public static int Port { get; set; } = 8888;
    }
}
