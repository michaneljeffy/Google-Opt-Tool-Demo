using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace NettyClient
{
    public static class ClientSettings
    {
        public static bool IsSsl { get; set; } = false;

        public static int Port { get; set; } = 8888;

        public static IPAddress Host { get; set; } = IPAddress.Parse("127.0.0.1");

        public static int Size { get; set; } = 1024;
    }
}
