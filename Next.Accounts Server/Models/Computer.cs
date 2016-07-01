﻿using Next.Accounts_Server.Application_Space;

namespace Next.Accounts_Server.Models
{
    public class Computer
    {
        public string IpAddress { get; set; } = "";

        public string Name { get; set; } = Const.NoName;

        public string ClientVersion { get; set; } = "0.0.0.0";
    }
}