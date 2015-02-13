using System;

namespace UpdateDatabase.Util
{
    public class VersionHelper
    {
        private Version _original;

        public VersionHelper(Version v)
        {
            _original = v;
        }

        public Version NewBuild()
        {
            return new Version(_original.Major, _original.Minor, _original.Build + 1, 0);
        }

        public Version NewMinor()
        {
            return new Version(_original.Major, _original.Minor + 1, 0, 0);
        }

        public Version NewMajor()
        {
            return new Version(_original.Major + 1, 0, 0, 0);
        }

        public Version Revert()
        {
            return _original;
        }
    }
}