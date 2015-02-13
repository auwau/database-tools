using Microsoft.Build.Evaluation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UpdateDatabase.Interfaces;
using UpdateDatabase.Providers;

namespace UpdateDatabase
{
    public class UpdaterApp
    {
        public Project SqlProject { get; set; }

        private readonly DirectoryInfo workingDirectory;

        public UpdaterApp(DirectoryInfo workingDirectory)
        {
            this.workingDirectory = workingDirectory;

            var projectFile = workingDirectory.EnumerateFiles("*" + Constants.PROJECT_EXT, SearchOption.TopDirectoryOnly).FirstOrDefault();
            if (projectFile.Exists)
            {
                SqlProject = new Project(projectFile.FullName);
            }
        }

        /// <summary>
        /// Sets the new version on the project, saves and then build it.
        /// The result is a dacpac file in the bin folder (depending on build configuration).
        /// The dacpac is located (by searching for the newest .dacpac) and copied into the Snapshots-folder.
        /// Then it's added to the project file.
        /// </summary>
        /// <param name="newVersion"></param>
        public void Snapshot(Version newVersion)
        {
            //Reload
            SqlProject.ReevaluateIfNecessary();

            if (SqlProject == null)
            {
                throw new FileNotFoundException(string.Format("A project file (*.{0}) could not be found in {1}.", Constants.PROJECT_EXT, workingDirectory));
            }

            SqlProject.SetProperty("DacVersion", newVersion.ToString());
            SqlProject.Save();

            var built = SqlProject.Build();

            //Locate the latest written .dacpac file (assumption here is that it's the most recently build)
            var latestDacpacFile = FindNewlyCreatedRecursive(this.SqlProject.DirectoryPath);
            if (latestDacpacFile == null)
            {
                throw new Exception(string.Format("No {0} was found in the SqlProject.", Constants.DACPAC_EXT));
            }

            Console.WriteLine(string.Format("Found {0}. Creating snapshot.", latestDacpacFile.FullName));

            var snapshot = string.Format("{0}_{1}{2}", SqlProject.GetPropertyValue("Name"), DateTime.Now.ToString("yyyyMMdd_HH-mm-ss"), Constants.DACPAC_EXT);

            if (!Directory.Exists(Path.Combine(SqlProject.DirectoryPath, "Snapshots")))
            {
                Directory.CreateDirectory(Path.Combine(SqlProject.DirectoryPath, "Snapshots"));
            }

            var snapshotPath = Path.Combine(SqlProject.DirectoryPath, "Snapshots\\", snapshot);

            //Copy latest .dacpac-file to snapshots
            File.Copy(latestDacpacFile.ToString(), snapshotPath);

            SqlProject.AddItem("None", Constants.SnapshotDirectory + @"\" + snapshot);
            SqlProject.Save();
        }

        private FileInfo FindNewlyCreatedRecursive(String directory)
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
                current = FindNewlyCreatedRecursive(dir);

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