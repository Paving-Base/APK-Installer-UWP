﻿using AAPTForUWP.Models;
using System.Collections.Generic;

namespace AAPTForUWP.Filters
{
    internal class PermissionFilter : BaseFilter
    {
        private readonly List<string> Permissions = new();

        public override bool CanHandle(string msg)
        {
            return msg.StartsWith("uses-permission:");
        }

        public override void AddMessage(string msg)
        {
            // uses-permission: name='<per>'
            // -> ["uses-permission: name=", "<per, get this value!!!>", ""]
            Permissions.Add(msg.Split(Seperator)[1]);
        }

        public override ApkInfo GetAPK()
        {
            return new ApkInfo()
            {
                Permissions = Permissions
            };
        }

        public override void Clear() => Permissions.Clear();
    }
}
