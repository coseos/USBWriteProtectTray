using System;
using System.Drawing;
using System.Security.Principal;
using System.Windows.Forms;
using Microsoft.Win32;

internal static class Program
{
    private const string RegPath = @"SYSTEM\CurrentControlSet\Control\StorageDevicePolicies";
    private const string RegValue = "WriteProtect";

    [STAThread]
    private static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new TrayAppContext());
    }

    private sealed class TrayAppContext : ApplicationContext
    {
        private readonly NotifyIcon _tray;
        private readonly ContextMenuStrip _menu;
        private readonly ToolStripMenuItem _toggleItem;
        private readonly ToolStripMenuItem _statusItem;

        public TrayAppContext()
        {
            _toggleItem = new ToolStripMenuItem();
            _toggleItem.Click += (_, __) => ToggleWriteProtect();

            _statusItem = new ToolStripMenuItem();
            _statusItem.Enabled = false;

            var exitItem = new ToolStripMenuItem("Exit");
            exitItem.Click += (_, __) => ExitThread();

            _menu = new ContextMenuStrip();
            _menu.Opening += (_, __) => RefreshMenu();
            _menu.Items.Add(_toggleItem);
            _menu.Items.Add(_statusItem);
            _menu.Items.Add(new ToolStripSeparator());
            _menu.Items.Add(exitItem);

            _tray = new NotifyIcon
            {
                Icon = GetTrayIconFromState(),
                Text = "USB Write Protect",
                Visible = true,
                ContextMenuStrip = _menu
            };

            RefreshMenu();
        }

        protected override void ExitThreadCore()
        {
            _tray.Visible = false;
            _tray.Dispose();
            _menu.Dispose();
            base.ExitThreadCore();
        }

        private void RefreshMenu()
        {
            try
            {
                int state = ReadWriteProtectValue();

                if (state == 1)
                {
                    _toggleItem.Text = "Set Read-Write";
                    _tray.Icon = LoadIconSafely("drive_green_usb.ico");
                    _tray.Text = "USB Write Protect: Read-Only";
                    _statusItem.Text = "Current state: Read-Only";
                }
                else
                {
                    _toggleItem.Text = "Set Read-Only";
                    _tray.Icon = LoadIconSafely("drive_red_usb.ico");
                    _tray.Text = "USB Write Protect: Read-Write";
                    _statusItem.Text = "Current state: Read-Write";
                }
            }
            catch (Exception ex)
            {
                _toggleItem.Text = "Toggle Read-Only";
                _tray.Text = "USB Write Protect: Error";
                _statusItem.Text = "Status: " + ex.Message;
            }
        }

        private void ToggleWriteProtect()
        {
            try
            {
                EnsureAdmin();

                int current = ReadWriteProtectValue();
                bool enableReadOnly = current == 0;

                using (var key = Registry.LocalMachine.CreateSubKey(RegPath, writable: true))
                {
                    if (key == null)
                        throw new InvalidOperationException("Could not open or create registry key.");

                    key.SetValue(RegValue, enableReadOnly ? 1 : 0, RegistryValueKind.DWord);
                }

                RefreshMenu();
                _tray.ShowBalloonTip(
                    2000,
                    "USB Write Protect",
                    enableReadOnly ? "Read-Only enabled." : "Read-Write enabled.",
                    ToolTipIcon.Info);
            }
            catch (Exception ex)
            {
                _tray.ShowBalloonTip(3500, "USB Write Protect", ex.Message, ToolTipIcon.Error);
            }
        }

        private static int ReadWriteProtectValue()
        {
            using (var key = Registry.LocalMachine.OpenSubKey(RegPath, writable: false))
            {
                if (key == null)
                    return 0;

                object val = key.GetValue(RegValue);
                if (val == null)
                    return 0;

                return Convert.ToInt32(val);
            }
        }

        private static void EnsureAdmin()
        {
            using (var id = WindowsIdentity.GetCurrent())
            {
                var p = new WindowsPrincipal(id);
                if (!p.IsInRole(WindowsBuiltInRole.Administrator))
                    throw new UnauthorizedAccessException("Administrator rights required.");
            }
        }

        private static Icon GetTrayIconFromState()
        {
            try
            {
                return LoadIconSafely(ReadWriteProtectValue() == 1 ? "drive_green_usb.ico" : "drive_red_usb.ico");
            }
            catch
            {
                return SystemIcons.Shield;
            }
        }

        private static Icon LoadIconSafely(string fileName)
        {
            try
            {
                return new Icon(fileName);
            }
            catch
            {
                return SystemIcons.Application;
            }
        }
    }
}

