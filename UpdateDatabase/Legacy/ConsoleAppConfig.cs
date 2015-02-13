using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Microsoft.Build.Evaluation;

namespace UpdateDatabase
{
    public class ConsoleAppConfig
    {
        public Project Project { get; set; }
        public Project PublishSettings { get; set; }

        /// <summary>
        /// Generate a snapshot file name formatted like Visual Studio: [ProjectName]_[year][month][day]_[hour]-[minute]-[second].dacpac
        /// </summary>
        /// <returns>A string with formated file name.</returns>
        public string GenerateNewSnapshotFileName(string projectName = null)
        {
            if (String.IsNullOrWhiteSpace(projectName))
            {
                projectName = Project.GetPropertyValue("Name");
            }

            return string.Format("{0}_{1}{2}", projectName, DateTime.Now.ToString("yyyyMMdd_HH-mm-ss"), Constants.DACPAC_EXT);
        }

        System.Reflection.Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            string dllName = args.Name.Contains(',') ? args.Name.Substring(0, args.Name.IndexOf(',')) : args.Name.Replace(".dll", "");

            dllName = dllName.Replace(".", "_");

            if (dllName.EndsWith("_resources")) return null;

            System.Resources.ResourceManager rm = new System.Resources.ResourceManager(GetType().Namespace + ".Properties.Resources", System.Reflection.Assembly.GetExecutingAssembly());

            byte[] bytes = (byte[])rm.GetObject(dllName);

            return System.Reflection.Assembly.Load(bytes);
        }

        private ConsoleAppConfig(IEnumerable<string> arguments)
        {
            var args = arguments.ToArray();
            AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(CurrentDomain_AssemblyResolve);

            if (args.Length > 0)
            {
                foreach (var arg in args)
                {
                    Console.WriteLine("With arg: " + arg);

                    if (arg[0] == '/')
                    {

                        //if (arg == "/cli")
                        //{
                        //    UserMode = false;
                        //}

                        continue;
                    }

                    var isAbsolutelyFile = File.Exists(arg);
                    if (isAbsolutelyFile)
                    {
                        if (arg.EndsWith(Constants.PUBLISH_EXT))
                        {
                            this.PublishSettings = new Project(arg);
                            continue;
                        }

                        if (arg.EndsWith(Constants.PROJECT_EXT))
                        {
                            this.Project = new Project(arg);
                            continue;
                        }
                    }
                    else if (arg.EndsWith(Constants.PUBLISH_EXT) && File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, arg)))
                    {
                        //File is relative
                        PublishSettings = new Project(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, arg));
                    }

                    if (File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, arg + Constants.PUBLISH_EXT)))
                    {
                        //File is relative and specified without extension.
                        PublishSettings = new Project(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, arg + Constants.PUBLISH_EXT));
                        continue;
                    }
                }
            }

            if (this.Project == null)
            {
                string file = string.Empty;

                if (PublishSettings != null)
                {
                    file = Directory.GetFiles(PublishSettings.DirectoryPath).FirstOrDefault(x => x.EndsWith(Constants.PROJECT_EXT));
                }
                else
                {
                    file = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory).FirstOrDefault(x => x.EndsWith(Constants.PROJECT_EXT));
                }

                if (String.IsNullOrEmpty(file))
                {
                    throw new IOException(string.Format("No project file ({0}) file found.", Constants.PROJECT_EXT));
                }

                this.Project = new Project(file);
            }

            Console.WriteLine(String.Format("Using project file: {0}", this.Project.GetPropertyValue("Name")));
        }


        public static ConsoleAppConfig Register(IEnumerable<string> args)
        {
            return new ConsoleAppConfig(args);
        }

        public FileInfo FindNewestDacPac()
        {
            Console.WriteLine(string.Format("Locating {0} for snapshot.", Constants.DACPAC_EXT));
            if (this.Project == null)
            {
                return null;
            }

            return FindLatestDacPacRecursive(this.Project.DirectoryPath);
        }

        private static FileInfo FindLatestDacPacRecursive(String directory)
        {
            FileInfo result = null, current = null;

            foreach (var item in Directory.GetFiles(directory).Where(x => x.EndsWith(Constants.DACPAC_EXT)))
            {
                current = new FileInfo(item);

                if (result == null)
                {
                    result = current;
                    continue;
                }

                if (result.LastWriteTimeUtc < current.LastWriteTimeUtc)
                {
                    result = current;
                }
            }

            foreach (var dir in Directory.GetDirectories(directory))
            {
                current = FindLatestDacPacRecursive(dir);

                if (current == null)
                {
                    continue;
                }

                if (result == null)
                {
                    result = current;
                    continue;
                }

                if (result.LastWriteTimeUtc < current.LastWriteTimeUtc)
                {
                    result = current;
                }
            }

            return result;
        }
    }
}
