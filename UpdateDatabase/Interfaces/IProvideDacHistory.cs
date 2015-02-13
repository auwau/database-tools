using Microsoft.SqlServer.Dac;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UpdateDatabase.Interfaces
{
    public interface IProvideDacHistory
    {
        DacPackage GetLatest();

        ICollection<DacPackage> GetHistory(Version fromVersion);
    }
}
