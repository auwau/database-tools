using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace UpdateDatabase.Tests
{
    public class Tests
    {
        [Fact]
        public void ConsoleAppConfigRegistrationThrowsIOExceptionWithoutProjectArgument()
        {
            List<string> args = new List<string>();

            Assert.Throws(typeof(System.IO.IOException), () => ConsoleAppConfig.Register(args));
        }

        //TODO: More tests...
    }
}
