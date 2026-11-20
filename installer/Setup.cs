using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;

namespace PPTBoardSetup
{
    internal static class Program
    {
        internal const string Version = "0.1.3";

        [STAThread]
        private static void Main(string[] args)
        {
            bool silent = false;
            foreach (string a in args)
            {
                if (a.Equals("/S", StringComparison.OrdinalIgnoreCase) ||
                    a.Equals("--silent", StringComparison.OrdinalIgnoreCase))
                    silent = true;
            }

            Log("setup start silent=" + silent);
            string target = DefaultTarget();

            if (silent)
            {
                try
                {
                    Install(target, null);
                }
                catch (Exception ex)
                {
                    Log("silent failed: " + ex);
                    Environment.ExitCode = 1;
                }
                return;
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new SetupForm(target));
        }

        internal static string DefaultTarget()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\PPTBoard"))
                {
                    if (key != null)
                    {
                        string value = key.GetValue("InstallDir") as string;
                        if (!string.IsNullOrEmpty(value)) return value;
                    }
                }
            }
            catch { }
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PPTBoard");
        }

        internal static void Install(string target, Action<string> progress)
        {
            if (string.IsNullOrEmpty(target)) throw new ArgumentException("安装目录为空");
            target = Path.GetFullPath(target);
            Log("install target=" + target);

            Report(progress, "正在停止旧进程...");
            Kill("PPTBoard");
            Kill("PPTBoardWatcher");
            Thread.Sleep(800);

            string temp = Path.Combine(Path.GetTempPath(), "PPTBoardSetup_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(temp);
            try
            {
                Report(progress, "正在解压安装文件...");
                string zipPath = Path.Combine(temp, "payload.zip");
                using (Stream input = Assembly.GetExecutingAssembly().GetManifestResourceStream("payload.zip"))
                {
                    if (input == null) throw new InvalidOperationException("安装资源缺失");
                    using (FileStream output = File.Create(zipPath)) input.CopyTo(output);
                }

                string expanded = Path.Combine(temp, "expanded");
                ZipFile.ExtractToDirectory(zipPath, expanded);

                Report(progress, "正在复制到 " + target);
                Directory.CreateDirectory(target);
                CopyDirectory(Path.Combine(expanded, "app"), target);
                File.Copy(Path.Combine(expanded, "uninstall.exe"), Path.Combine(target, "uninstall.exe"), true);

                Report(progress, "正在写入注册表...");
                WriteRegistry(target);

                Report(progress, "正在启动监听器...");
                string watcher = Path.Combine(target, "PPTBoardWatcher.exe");
                if (File.Exists(watcher))
                {
                    ProcessStartInfo psi = new ProcessStartInfo(watcher);
                    psi.UseShellExecute = true;
                    psi.WindowStyle = ProcessWindowStyle.Hidden;
                    Process.Start(psi);
                }

                Log("install done " + target);
            }
            finally
            {
                try { Directory.Delete(temp, true); } catch { }
            }
        }

        private static void WriteRegistry(string target)
        {
            using (RegistryKey app = Registry.CurrentUser.CreateSubKey(@"Software\PPTBoard"))
                app.SetValue("InstallDir", target);

            using (RegistryKey run = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run"))
                run.SetValue("PPTBoardWatcher", "\"" + Path.Combine(target, "PPTBoardWatcher.exe") + "\"");

            using (RegistryKey un = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Uninstall\PPTBoard"))
            {
                un.SetValue("DisplayName", "PPTBoard");
                un.SetValue("DisplayVersion", Version);
                un.SetValue("Publisher", "shuixinggangzheng");
                un.SetValue("DisplayIcon", Path.Combine(target, "PPTBoard.exe"));
                un.SetValue("InstallLocation", target);
                un.SetValue("UninstallString", "\"" + Path.Combine(target, "uninstall.exe") + "\"");
                un.SetValue("QuietUninstallString", "\"" + Path.Combine(target, "uninstall.exe") + "\" /S");
                un.SetValue("NoModify", 1, RegistryValueKind.DWord);
                un.SetValue("NoRepair", 1, RegistryValueKind.DWord);
            }
        }

        private static void CopyDirectory(string source, string target)
        {
            Directory.CreateDirectory(target);
            foreach (string file in Directory.GetFiles(source))
                File.Copy(file, Path.Combine(target, Path.GetFileName(file)), true);
            foreach (string dir in Directory.GetDirectories(source))
                CopyDirectory(dir, Path.Combine(target, Path.GetFileName(dir)));
        }

        private static void Kill(string name)
        {
            foreach (Process p in Process.GetProcessesByName(name))
            {
                try
                {
                    p.Kill();
                    p.WaitForExit(2000);
                }
                catch { }
            }
        }

        private static void Report(Action<string> progress, string message)
        {
            if (progress != null) progress(message);
        }

        internal static void Log(string message)
        {
            try
            {
                File.AppendAllText(
                    Path.Combine(Path.GetTempPath(), "PPTBoardSetup.log"),
                    DateTime.Now.ToString("s") + " " + message + Environment.NewLine);
            }
            catch { }
        }
    }

    internal sealed class SetupForm : Form
    {
        private readonly TextBox _path;
        private readonly Label _status;
        private readonly Button _install;
        private readonly Button _browse;

        public SetupForm(string target)
        {
            Text = "PPTBoard " + Program.Version + " 安装";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterScreen;
            MaximizeBox = false;
            MinimizeBox = false;
            ClientSize = new System.Drawing.Size(470, 205);
            Font = new System.Drawing.Font("Microsoft YaHei UI", 9F);

            Label title = new Label();
            title.Text = "PPTBoard " + Program.Version;
            title.Font = new System.Drawing.Font("Microsoft YaHei UI", 15F, System.Drawing.FontStyle.Bold);
            title.AutoSize = true;
            title.Location = new System.Drawing.Point(20, 15);
            Controls.Add(title);

            Label hint = new Label();
            hint.Text = "白板浮层安装程序：复制文件、写入开机自启和卸载项。";
            hint.AutoSize = true;
            hint.ForeColor = System.Drawing.Color.DimGray;
            hint.Location = new System.Drawing.Point(22, 52);
            Controls.Add(hint);

            Label pathLabel = new Label();
            pathLabel.Text = "安装到：";
            pathLabel.AutoSize = true;
            pathLabel.Location = new System.Drawing.Point(22, 88);
            Controls.Add(pathLabel);

            _path = new TextBox();
            _path.Text = target;
            _path.Location = new System.Drawing.Point(88, 84);
            _path.Width = 280;
            Controls.Add(_path);

            _browse = new Button();
            _browse.Text = "浏览...";
            _browse.Location = new System.Drawing.Point(375, 82);
            _browse.Width = 72;
            _browse.Click += delegate
            {
                using (FolderBrowserDialog dialog = new FolderBrowserDialog())
                {
                    dialog.Description = "选择 PPTBoard 安装目录";
                    if (dialog.ShowDialog(this) == DialogResult.OK)
                        _path.Text = dialog.SelectedPath;
                }
            };
            Controls.Add(_browse);

            _status = new Label();
            _status.Text = "准备安装。";
            _status.AutoSize = true;
            _status.Location = new System.Drawing.Point(22, 122);
            Controls.Add(_status);

            _install = new Button();
            _install.Text = "安装";
            _install.Location = new System.Drawing.Point(270, 155);
            _install.Width = 85;
            _install.Height = 30;
            _install.Click += delegate { StartInstall(); };
            Controls.Add(_install);

            Button close = new Button();
            close.Text = "关闭";
            close.Location = new System.Drawing.Point(362, 155);
            close.Width = 85;
            close.Height = 30;
            close.Click += delegate { Close(); };
            Controls.Add(close);

            AcceptButton = _install;
        }

        private void StartInstall()
        {
            string target = _path.Text.Trim();
            if (target.Length == 0)
            {
                MessageBox.Show(this, "请填写安装目录。", "PPTBoard", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _install.Enabled = false;
            _browse.Enabled = false;
            _path.Enabled = false;
            _status.Text = "正在安装...";

            Thread thread = new Thread(delegate()
            {
                try
                {
                    Program.Install(target, delegate(string message)
                    {
                        BeginInvoke(new Action<string>(SetStatus), message);
                    });
                    BeginInvoke(new Action(Finished));
                }
                catch (Exception ex)
                {
                    Program.Log("ui failed: " + ex);
                    BeginInvoke(new Action<string>(Failed), ex.Message);
                }
            });
            thread.IsBackground = true;
            thread.Start();
        }

        private void SetStatus(string message)
        {
            _status.Text = message;
        }

        private void Finished()
        {
            _status.Text = "安装完成。";
            MessageBox.Show(this, "PPTBoard " + Program.Version + " 已安装。", "安装完成",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            Close();
        }

        private void Failed(string message)
        {
            _status.Text = "安装失败。";
            MessageBox.Show(this, "安装失败：" + message, "PPTBoard",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            _install.Enabled = true;
            _browse.Enabled = true;
            _path.Enabled = true;
        }
    }
}
