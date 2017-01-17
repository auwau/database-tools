using Microsoft.Build.Evaluation;
using Microsoft.SqlServer.Dac;
using System;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using UpdateDatabase.Interfaces;
using UpdateDatabase.Providers;

namespace UpdateDatabase
{
    public class DeployApp
    {
        private readonly IProvideDacVersion versionProvider;
        private readonly DacHistory historyProvider;
        private StringBuilder logBuilder;

        public DeployApp(IProvideDacVersion versionProvider, DacHistory historyProvider)
        {
            this.logBuilder = new StringBuilder();
            this.versionProvider = versionProvider;
            this.historyProvider = historyProvider;
        }

        public void Deploy(string publishSettingsFile)
        {
            if (!File.Exists(publishSettingsFile))
            {
                throw new FileNotFoundException(string.Format("Provided publish settings '{0}' could not be found!", publishSettingsFile));
            }

            var latest = historyProvider.GetLatest();
            var publishData = new Project(publishSettingsFile);

            var connectionString = publishData.GetPropertyValue("TargetConnectionString");
            var targetDatabaseName = publishData.GetPropertyValue("TargetDatabaseName");
            var buildVersion = publishData.GetPropertyValue("BuildVersion");
            Log("Known parameters: Connection string: {0}; Database: {1}; Build version: {2};", connectionString, targetDatabaseName, buildVersion);

            var currentVersion = versionProvider.GetVersion(connectionString, targetDatabaseName);

            Log("Deployment mode for {0} with version {1}.", targetDatabaseName, currentVersion);

            if (latest.Version == currentVersion)
            {
                Log("Target is latest version: {0}. Skipping deployment.", latest.Version);
                UpdateDatabaseBuildNumber(buildVersion, connectionString, targetDatabaseName);
                return;
            }

            var dacService = new DacServices(connectionString);

            dacService.Message += (s, e) =>
            {
                Log("DAC Message: {0}", e.Message);
            };

            dacService.ProgressChanged += (s, e) =>
            {
                Log("{0}: {1}", e.Status, e.Message);
            };

            var options = new DacDeployOptions();
            //Load the publish settings
            foreach (var item in publishData.Properties)
            {
                var prop = options.GetType().GetProperty(item.Name);
                if (prop != null)
                {
                    var val = Convert.ChangeType(item.UnevaluatedValue, prop.PropertyType);
                    prop.SetValue(options, val);
                }
            }

            if (currentVersion == null)
            {
                //Deploy latest
                Log("Deploy latest version: {0}.", latest.Version);
                dacService.Deploy(latest, targetDatabaseName, true, options);
                UpdateDatabaseBuildNumber(buildVersion, connectionString, targetDatabaseName);
                return;
            }

            Log("Upgrading {0} -> {1}.", currentVersion, latest.Version);

            try
            {
                var count = 0;
                foreach (var package in historyProvider.GetHistory(currentVersion).OrderBy(x => x.Version))
                {
                    Log();
                    Log("Applying upgrade #{0}: {1} -> {2}.", ++count, currentVersion, package.Version);
                    Log();

                    if (count > 0)
                    {
                        options.BackupDatabaseBeforeChanges = false;
                    }

                    dacService.Deploy(package, targetDatabaseName, true, options);
                    currentVersion = package.Version;
                }
                UpdateDatabaseBuildNumber(buildVersion, connectionString, targetDatabaseName);
            }
            catch
            {
                var file = new FileInfo(publishSettingsFile);
                var name = file.Name.Substring(0, file.Name.LastIndexOf(file.Extension));
                File.WriteAllText(Path.Combine(publishData.DirectoryPath, string.Format("{0}v{1}_error.log", name, currentVersion)), logBuilder.ToString());

                throw;
            }
        }

        private readonly object _lockObj = new { };

        private void Log()
        {
            lock (_lockObj)
            {
                Console.WriteLine();
                logBuilder.AppendLine();
            }
        }

        private void Log(string format, params object[] args)
        {
            lock (_lockObj)
            {
                Console.WriteLine(format, args);
                logBuilder.AppendLine(string.Format(format, args));
            }
        }

        private void UpdateDatabaseBuildNumber(string buildVersion, string connectionString, string databaseName)
        {
            if (string.IsNullOrEmpty(buildVersion))
            {
                buildVersion = "Not available";
            }

            var query =
@"USE [" + databaseName + @"]
IF (EXISTS(SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'SystemSetting'))
BEGIN
	SET IDENTITY_INSERT [SystemSetting] ON;
	MERGE INTO [SystemSetting] AS TARGET
	    USING (VALUES(17, N'Build version', 'BuildVersion', '" + buildVersion + @"', 5, GETUTCDATE()))
	    AS SOURCE ([Id], [Name], [Key], [Value], [SystemSettingTypeId], [CreatedDate])
	    ON TARGET.[Id] = SOURCE.[Id]
	WHEN MATCHED THEN
	    UPDATE SET [Name] = SOURCE.[Name], [Key] = SOURCE.[Key], [SystemSettingTypeId] = SOURCE.[SystemSettingTypeId], [Value] = SOURCE.[VALUE]
	WHEN NOT MATCHED BY TARGET THEN
	    INSERT ([Id], [Name], [Key], [Value], [SystemSettingTypeId], [CreatedDate]) VALUES ([Id], [Name], [Key], [Value], [SystemSettingTypeId], [CreatedDate]);
	SET IDENTITY_INSERT [SystemSetting] OFF;
END";

            SqlConnection connection = new SqlConnection(connectionString);
            SqlCommand cmd = new SqlCommand(query, connection);
            connection.Open();
            cmd.ExecuteNonQuery();
            connection.Close();
        }
    }
}