using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UpdateDatabase.Interfaces;

namespace UpdateDatabase.Providers
{
    public class DacVersionSqlProvider : IProvideDacVersion
    {
        public Version GetVersion(string targetConnectionString, string targetDatabaseName)
        {
            using (var connection = new SqlConnection(targetConnectionString))
            {
                var cmd = new SqlCommand(string.Format("select top(1) type_version from msdb.dbo.sysdac_instances_internal where instance_name = '{0}'", targetDatabaseName), connection);

                connection.Open();
                var result = cmd.ExecuteReader();
                if (result.Read())
                {
                    return new Version(result["type_version"].ToString());
                }
            }

            return null;
        }
    }
}
