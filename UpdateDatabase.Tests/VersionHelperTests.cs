using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UpdateDatabase.Util;
using Xunit;

namespace UpdateDatabase.Tests
{
    public class VersionHelperTests
    {
        [Fact]
        public void CanIncrementBuild()
        {
            //Arrange
            var sut = new VersionHelper(new Version(1, 1, 1, 1));

            //Act
            var result = sut.NewBuild();

            //Assert
            Assert.Equal("1.1.2.0", result.ToString());
        }

        [Fact]
        public void CanIncrementMinor()
        {
            //Arrange
            var sut = new VersionHelper(new Version(1, 1, 1, 1));

            //Act
            var result = sut.NewMinor();

            //Assert
            Assert.Equal("1.2.0.0", result.ToString());
        }

        [Fact]
        public void CanIncrementMajor()
        {
            //Arrange
            var sut = new VersionHelper(new Version(1, 1, 1, 1));

            //Act
            var result = sut.NewMajor();

            //Assert
            Assert.Equal("2.0.0.0", result.ToString());
        }
    }
}
