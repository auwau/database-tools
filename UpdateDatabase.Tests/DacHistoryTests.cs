using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UpdateDatabase.Providers;
using Xunit;

namespace UpdateDatabase.Tests
{
    public class DacHistoryTests
    {
        private string WorkingDir = @"C:\Development\Repos\Konsistency\DatabaseTools\DatabaseTools.SampleDatabase";

        [Fact]
        public void HistoryCanGetLatest()
        {
            //Arrange
            var sut = new DacHistory(new System.IO.DirectoryInfo(WorkingDir));

            //Act
            var latest = sut.GetLatest();

            //Assert
            Assert.Equal("1.2.0.1", latest.Version.ToString());
        }

        [Fact]
        public void HistoryCanGetHistorySinceSpecificVersion()
        {
            //Arrange
            var sut = new DacHistory(new System.IO.DirectoryInfo(WorkingDir));
            var v = new Version(1, 0, 0, 1);
            //Act

            var history = sut.GetHistory(v);

            //Assert
            Assert.Equal(2, history.Count);

            foreach (var item in history)
            {
                Assert.True(item.Version > v);
            }
        }
    }
}
