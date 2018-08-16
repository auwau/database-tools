using Microsoft.Build.Evaluation;
using System;
using System.IO;
using System.Linq;
using UpdateDatabase.Interfaces;

namespace UpdateDatabase
{
    public class VersionApp
    {
        public Project SqlProject { get; set; }

        private readonly DirectoryInfo workingDirectory;
        private readonly IProvideDacHistory history;

        public VersionApp(DirectoryInfo workingDirectory, IProvideDacHistory history)
        {
            this.workingDirectory = workingDirectory;
            this.history = history;

            var projectFile = workingDirectory.EnumerateFiles("*" + Constants.PROJECT_EXT, SearchOption.TopDirectoryOnly).FirstOrDefault();
            if (projectFile.Exists)
            {
                SqlProject = new Project(projectFile.FullName);
            }
        }

        /// <summary>
        /// Sets the new version on the project, saves and then builds it.
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

            if (!built)
            {
                throw new InvalidOperationException("Project does not build!");
            }

            //Locate the latest written .dacpac file (assumption here is that it's the most recently build)
            var latestDacpacFile = Directory.EnumerateFiles(Path.Combine(this.SqlProject.DirectoryPath, "bin"), "*" + Constants.DACPAC_EXT, SearchOption.AllDirectories)
                                            .Select(x => new FileInfo(x))
                                            .OrderByDescending(x => x.LastWriteTimeUtc)
                                            .First();

            if (latestDacpacFile == null)
            {
                throw new Exception(string.Format("No {0} was found in the SqlProject.", Constants.DACPAC_EXT));
            }

            Console.WriteLine(string.Format("Found {0}. Creating snapshot.", latestDacpacFile.FullName));

            var snapshot = string.Format("{0}_{1}v{3}{2}", SqlProject.GetPropertyValue("Name"), DateTime.Now.ToString("yyyyMMdd_HH-mm-ss"), Constants.DACPAC_EXT, newVersion);

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

        public void RollBack()
        {
            var latest = history.GetLatest();
            Console.WriteLine("Rolling back version {0}.", latest.Version);

            var latestFile = history.GetLatestFile();
            var itemValue = latestFile.FullName.Substring(latestFile.FullName.IndexOf(Constants.SnapshotDirectory));
            File.Delete(latestFile.FullName);

            var item = SqlProject.GetItems("None").FirstOrDefault(x => x.UnevaluatedInclude == itemValue);
            SqlProject.RemoveItem(item);

            var current = history.GetLatest();
            Console.WriteLine("Current version is now {0}.", current.Version);
            SqlProject.SetProperty("DacVersion", current.Version.ToString());
            SqlProject.Save();
        }
    }
}