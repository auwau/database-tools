using Microsoft.SqlServer.Dac;
using System;
using System.Data.SqlClient;
using System.IO;
using System.Linq;

namespace UpdateDatabase
{
    public class Legacy
    {
        public static void Run(string[] args)
        {
            ConsoleAppConfig app = null;
            Console.WriteLine(String.Format("Running from {0}.", AppDomain.CurrentDomain.BaseDirectory));

            try
            {
                app = ConsoleAppConfig.Register(args);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);
                if (Environment.UserInteractive)
                {
                    Console.ReadKey();
                }
                Environment.Exit(1);
                return;
            }

            if (app.PublishSettings != null)
            {
                try
                {
                    UpgradeDatabase(app);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine(ex.Message);
                    if (Environment.UserInteractive)
                    {
                        Console.ReadKey();
                    }
                    Environment.Exit(1);
                }

                return;
            }

            try
            {
                UpdateVersionAndCreateSnapshot(app);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);
                if (Environment.UserInteractive)
                {
                    Console.ReadKey();
                }
                Environment.Exit(1);
                return;
            }

            if (Environment.UserInteractive)
            {
                Console.ReadKey();
            }
        }

        private static void UpgradeDatabase(ConsoleAppConfig app)
        {
            var snapshotDir = Path.Combine(app.Project.DirectoryPath, "Snapshots");

            if (!Directory.Exists(snapshotDir))
            {
                throw new IOException("No Snapshots folder found.");
            }

            var snapshots = Directory.GetFiles(snapshotDir).Where(x => x.EndsWith(".dacpac")).ToArray();

            Array.Sort(snapshots);

            //TODO: Make no assumptions to naming conventions
            var latest = snapshots.Last();
            var dac = DacPackage.Load(latest);

            Console.WriteLine(string.Format("Latest DAC version is {0}.", dac.Version));

            //Target
            var connectionString = app.PublishSettings.GetPropertyValue("TargetConnectionString");
            var targetDatabaseName = app.PublishSettings.GetPropertyValue("TargetDatabaseName");

            Console.WriteLine("Connecting to target database to look up current version...");
            Version existing = null;
            using (var connection = new SqlConnection(connectionString))
            {
                var cmd = new SqlCommand(string.Format("select top(1) type_version from msdb.dbo.sysdac_instances_internal where instance_name = '{0}'", targetDatabaseName), connection);

                connection.Open();
                var result = cmd.ExecuteReader();
                if (result.Read())
                {
                    existing = new Version(result["type_version"].ToString());
                }
            }

            if (existing == null || dac.Version > existing)
            {
                if (existing == null)
                {
                    Console.WriteLine("No database found. Deploying...");
                }
                else
                {
                    Console.WriteLine(string.Format("Database found. Running version is {0}. Starting upgrade...", existing));
                }

                var svc = new DacServices(connectionString);
                svc.Message += dacServices_Message;
                svc.ProgressChanged += dacServices_ProgressChanged;
                var options = new DacDeployOptions();

                //Load the publish settings
                foreach (var item in app.PublishSettings.Properties)
                {
                    var prop = options.GetType().GetProperty(item.Name);
                    if (prop != null)
                    {
                        var val = Convert.ChangeType(item.UnevaluatedValue, prop.PropertyType);
                        prop.SetValue(options, val);
                    }
                }
                svc.Deploy(dac, targetDatabaseName, existing != null, options);
            }
            else
            {
                Console.WriteLine("Version is up to date. Skipping deployment.");
            }
        }

        private static void UpdateVersionAndCreateSnapshot(ConsoleAppConfig app)
        {
            var oldVersion = new Version(app.Project.GetPropertyValue("DacVersion"));
            var newVersion = new Version(oldVersion.Major, oldVersion.Minor + 1, oldVersion.Build, oldVersion.Revision);

            app.Project.SetProperty("DacVersion", newVersion.ToString());
            app.Project.Save();

            Console.WriteLine(string.Format("Set version to {0}.", newVersion));

            Console.WriteLine(string.Format("Building {0} with the new version number.", Constants.DACPAC_EXT));
            var built = app.Project.Build();

            if (!built)
            {
                app.Project.SetProperty("DacVersion", oldVersion.ToString());
                app.Project.Save();

                throw new Exception("Build failed - no update done. Rolling back version.");
            }

            Console.WriteLine("Project built.");

            #region Copy new dacpac to Snapshots folder

            //Locate the latest written .dacpac file (assumption here is that it's the most recently build)
            var latestDacpacFile = app.FindNewestDacPac();
            if (latestDacpacFile == null)
            {
                throw new Exception(string.Format("No {0} was found in the project.", Constants.DACPAC_EXT));
            }

            Console.WriteLine(string.Format("Found {0}. Creating snapshot.", latestDacpacFile.FullName));

            var snapshot = app.GenerateNewSnapshotFileName(app.Project.GetPropertyValue("Name"));

            if (!Directory.Exists(Path.Combine(app.Project.DirectoryPath, "Snapshots")))
            {
                Directory.CreateDirectory(Path.Combine(app.Project.DirectoryPath, "Snapshots"));
            }

            var snapshotPath = Path.Combine(app.Project.DirectoryPath, "Snapshots\\", snapshot);

            //Copy latest .dacpac-file to snapshots
            File.Copy(latestDacpacFile.ToString(), snapshotPath);

            app.Project.AddItem("None", "Snapshots\\" + snapshot);
            app.Project.Save();

            Console.WriteLine(string.Format("Snapshot created at {0}", snapshotPath));

            #endregion Copy new dacpac to Snapshots folder

            Console.WriteLine("A snapshot has been created of the new version.");
        }

        private static void dacServices_Message(object sender, DacMessageEventArgs e)
        {
            Console.WriteLine("DAC Message: {0}", e.Message);
        }

        private static void dacServices_ProgressChanged(object sender, DacProgressEventArgs e)
        {
            Console.WriteLine(e.Status + ": " + e.Message);
        }
    }
}