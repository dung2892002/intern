﻿namespace Cukcuk.Core.Auth
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class PermissionAttribute : Attribute
    {
        public string Permission { get; }

        public PermissionAttribute(string permission)
        {
            Permission = permission;
        }
    }
}

