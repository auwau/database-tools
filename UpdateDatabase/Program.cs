using System;
using System.IO;
using UpdateDatabase.Util;
using System.Windows.Forms;

namespace UpdateDatabase
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            try
            {
                //Legacy.Run(args);

                var arguments = new Arguments(args);

                var publishSettings = arguments["publish"] ?? arguments["pub"] ?? arguments["p"] ?? arguments["deploy"];

                if (!string.IsNullOrEmpty(publishSettings))
                {
                    if (!File.Exists(publishSettings) && Environment.CurrentDirectory != AppDomain.CurrentDomain.BaseDirectory)
                    {
                        publishSettings = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, publishSettings);
                    }

                    if (!File.Exists(publishSettings))
                    {
                        throw new FileNotFoundException(string.Format("Could not find file {0} in directory {1}.", publishSettings, Environment.CurrentDirectory));
                    }

                    var deployer = new DeployApp(new Providers.DacVersionSqlProvider(), new Providers.DacHistory(new FileInfo(publishSettings).Directory));
                    deployer.Deploy(publishSettings);
                }
                else
                {
                    //Snapshot-mode
                    var workingDir = new DirectoryInfo(Environment.CurrentDirectory);
                    var updater = new VersionApp(workingDir, new Providers.DacHistory(workingDir));
                    var oldVersion = new Version(updater.SqlProject.GetPropertyValue("DacVersion"));
                    var versionHelper = new VersionHelper(oldVersion);

                    Console.WriteLine("Update mode for {0} with current version {1}", updater.SqlProject.GetPropertyValue("Name"), oldVersion);
                    Console.WriteLine("WARNING: If the project is currently open, make sure you've saved your current changes (otherwise click 'Save All').");
                    Console.WriteLine();

                    Console.WriteLine("What kind of update is this?");
                    Console.WriteLine("1) Build: only changed post deploy data => {0}", versionHelper.NewBuild());
                    Console.WriteLine("2) Minor: changed data, added or renamed columns => {0}", versionHelper.NewMinor());
                    Console.WriteLine("3) Major: add new tables, columns causing data motion => {0}", versionHelper.NewMajor());
                    Console.WriteLine("");
                    Console.WriteLine("0) Roll back: Removes the latest versioned snapshot and downgrades the project version to match the new current version.");

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

                            case '0':
                                updater.RollBack();
                                return;

                            default:
                                break;
                        }
                    } while (newVersion == null);

                    Console.WriteLine(key.KeyChar);
                    Console.WriteLine("Creating snapshot with version {0}...", newVersion);
                    try
                    {
                        updater.Snapshot(newVersion);
                    }
                    catch (InvalidOperationException)
                    {
                        //Revert to old version
                        updater.SqlProject.SetProperty("DacVersion", oldVersion.ToString());
                        updater.SqlProject.Save();
                        throw;
                    }
                }
            }
            catch (Exception ex)
            {
                AutoClosingMessageBox.Show("An error occurred", ex.ToString(), 60000);
                throw ex;
            }
        }
    }

    /// <summary>
    /// Class for adding messagebox that closes after a timeout
    /// https://stackoverflow.com/questions/14522540/close-a-messagebox-after-several-seconds
    /// </summary>
    public class AutoClosingMessageBox
    {
        private System.Threading.Timer timer;
        private string caption;

        private AutoClosingMessageBox(string text, string caption, int timeout)
        {
            this.caption = caption;
            timer = new System.Threading.Timer(OnTimerElapsed, null, timeout, System.Threading.Timeout.Infinite);
            MessageBox.Show(text, caption);
        }

        public static void Show(string text, string caption, int timeout)
        {
            new AutoClosingMessageBox(text, caption, timeout);
        }

        private void OnTimerElapsed(object state)
        {
            IntPtr mbWnd = FindWindow(null, caption);
            if (mbWnd != IntPtr.Zero)
            {
                SendMessage(mbWnd, WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
            }
            timer.Dispose();
        }

        private const int WM_CLOSE = 0x0010;

        [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, UInt32 Msg, IntPtr wParam, IntPtr lParam);
    }
}