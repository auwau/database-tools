using Microsoft.Build.Evaluation;
using Microsoft.SqlServer.Dac;
using System;
using System.IO;
using System.Linq;
using UpdateDatabase.Interfaces;
using UpdateDatabase.Providers;

namespace UpdateDatabase
{
    public class DeployApp
    {
        private readonly IProvideDacVersion versionProvider;
        private readonly DacHistory historyProvider;

        public DeployApp(IProvideDacVersion versionProvider, DacHistory historyProvider)
        {
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

            var currentVersion = versionProvider.GetVersion(connectionString, targetDatabaseName);

            Console.WriteLine("Deployment mode for {0} with version {1}.", targetDatabaseName, currentVersion);

            if (latest.Version == currentVersion)
            {
                Console.WriteLine("Target is latest version: {0}. Skipping deployment.", latest.Version);
                return;
            }

            var dacService = new DacServices(connectionString);

            dacService.Message += (s, e) =>
            {
                Console.WriteLine("DAC Message: {0}", e.Message);
            };

            dacService.ProgressChanged += (s, e) =>
            {
                Console.WriteLine("{0}: {1}", e.Status, e.Message);
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
                Console.WriteLine("Deploy latest version: {0}.", latest.Version);
                dacService.Deploy(latest, targetDatabaseName, true, options);
                return;
            }

            Console.WriteLine("Upgrading {0} -> {1}.", currentVersion, latest.Version);

            var count = 0;
            foreach (var package in historyProvider.GetHistory(currentVersion).OrderBy(x => x.Version))
            {
                Console.WriteLine();
                Console.WriteLine("Applying upgrade #{0}: {1} -> {2}.", ++count, currentVersion, package.Version);
                Console.WriteLine();
                dacService.Deploy(package, targetDatabaseName, true, options);
                currentVersion = package.Version;
            }
        }
    }
}