using Microsoft.Build.Evaluation;
using Microsoft.SqlServer.Dac;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UpdateDatabase.Providers
{
    public class DacHistory : Interfaces.IProvideDacHistory
    {
        public DirectoryInfo WorkingDirectory { get; private set; }

        public DacHistory(DirectoryInfo workingDirectory)
        {
            if (!workingDirectory.Exists)
            {
                throw new DirectoryNotFoundException(string.Format("Working directory '{0}' not found.", workingDirectory));
            }

            WorkingDirectory = workingDirectory;
        }

        public DacPackage GetLatest()
        {
            var files = Directory.EnumerateFiles(Path.Combine(WorkingDirectory.ToString(), Constants.SnapshotDirectory), "*" + Constants.DACPAC_EXT, SearchOption.AllDirectories);

            var latest = default(DacPackage);

            foreach (var item in files)
            {
                var current = DacPackage.Load(item);
                if (latest == null || current.Version > latest.Version)
                {
                    latest = current;
                }
            }

            return latest;
        }

        public FileInfo GetLatestFile()
        {
            var files = Directory.EnumerateFiles(Path.Combine(WorkingDirectory.ToString(), Constants.SnapshotDirectory), "*" + Constants.DACPAC_EXT, SearchOption.AllDirectories);

            var latest = default(DacPackage);
            var file = default(FileInfo);

            foreach (var item in files)
            {
                var current = DacPackage.Load(item);
                if (latest == null || current.Version > latest.Version)
                {
                    latest = current;
                    file = new FileInfo(item);
                }
            }

            return file;
        }

        public ICollection<DacPackage> GetHistory(Version fromVersion)
        {
            var files = Directory.EnumerateFiles(Path.Combine(WorkingDirectory.ToString(), Constants.SnapshotDirectory), "*" + Constants.DACPAC_EXT, SearchOption.AllDirectories);

            var result = new List<DacPackage>();

            foreach (var item in files)
            {
                var current = DacPackage.Load(item);
                if (current.Version > fromVersion)
                {
                    result.Add(current);
                }
            }

            return result;
        }
    }
}