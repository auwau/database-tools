using Microsoft.Build.Evaluation;
using Microsoft.SqlServer.Dac;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using UpdateDatabase.Util;

namespace UpdateDatabase
{
    class Program
    {
        static void Main(string[] args)
        {
            //Legacy.Run(args);

            var arguments = new Arguments(args);

            var publishSettings = arguments["publish"] ?? arguments["pub"] ?? arguments["p"] ?? arguments["deploy"];

            if (File.Exists(publishSettings))
            {
                var deployer = new Deployer(new Providers.DacVersionSqlProvider(), new Providers.DacHistory(new FileInfo(publishSettings).Directory));
                deployer.Deploy(publishSettings);
            }
            else
            {
                //Snapshot-mode
                var updater = new UpdaterApp(new DirectoryInfo(Environment.CurrentDirectory));
                var oldVersion = new Version(updater.SqlProject.GetPropertyValue("DacVersion"));
                var versionHelper = new VersionHelper(oldVersion);

                Console.WriteLine("Update mode for {0} with current version {1}", updater.SqlProject.GetPropertyValue("Name"), oldVersion);
                Console.WriteLine("WARNING: If the project is currently open, make sure you've saved your current changes (otherwise click 'Save All').");
                Console.WriteLine();

                Console.WriteLine("What kind of update is this?");
                Console.WriteLine("1) Build: only changed post deploy data => {0}", versionHelper.NewBuild());
                Console.WriteLine("2) Minor: changed data, added or renamed columns => {0}", versionHelper.NewMinor());
                Console.WriteLine("3) Major: add new tables, columns causing data motion => {0}", versionHelper.NewMajor());

                var newVersion = default(Version);
                Console.Write("Choose one: ");
                ConsoleKeyInfo key;
                do
                {
                    key = Console.ReadKey(true);
                    switch (key.KeyChar)
                    {
                        case '1':
                            newVersion = versionHelper.NewBuild();
                            break;
                        case '2':
                            newVersion = versionHelper.NewMinor();
                            break;
                        case '3':
                            newVersion = versionHelper.NewMajor();
                            break;
                        default:
                            break;
                    }
                } while (newVersion == null);

                updater.Snapshot(newVersion);
                Console.WriteLine("What kind of update is this?");
            }
        }
    }
}
