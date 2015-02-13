using System;

namespace UpdateDatabase.Interfaces
{
    public interface IProvideDacVersion
    {
        Version GetVersion(string targetConnectionString, string targetName);
    }
}