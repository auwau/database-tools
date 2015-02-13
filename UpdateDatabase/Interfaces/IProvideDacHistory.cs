using Microsoft.SqlServer.Dac;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UpdateDatabase.Interfaces
{
    public interface IProvideDacHistory
    {
        DacPackage GetLatest();
        FileInfo GetLatestFile();

        ICollection<DacPackage> GetHistory(Version fromVersion);
    }
}
