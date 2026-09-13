using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using DevExpress.Xpf.Core;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;

namespace SmartAuditERP.Form_WPF
{
    public partial class frmInputs : ThemedWindow
    {
        #region ── Win32 Imports ────────────────────────────────────────────

        [DllImport("kernel32", CharSet = CharSet.Ansi,
                   ExactSpelling = true, SetLastError = true)]
        private static extern bool Wow64DisableWow64FsRedirection(ref long oldValue);

        [DllImport("kernel32", CharSet = CharSet.Ansi,
                   ExactSpelling = true, SetLastError = true)]
        private static extern bool Wow64EnableWow64FsRedirection(ref long oldValue);

        #endregion

        #region ── Fields ──────────────────────────────────────────────────

        private Process _onScreenKeyboardProcess;

        private readonly string _oskPath = @"C:\Windows\System32\osk.exe";

        // Public fields (محافظ على الأسماء الأصلية)
        public string tableNo = string.Empty;
        public bool isDone = false;

        #endregion

        #region ── Constructor ─────────────────────────────────────────────

        public frmInputs()
        {
            InitializeComponent();
        }

        #endregion

        #region ── Window Events ───────────────────────────────────────────

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            txtInput.Focus();
            OpenOnScreenKeyboard();
        }

        #endregion

        #region ── Keyboard ────────────────────────────────────────────────

        private void OpenOnScreenKeyboard()
        {
            try
            {
                if (Environment.Is64BitOperatingSystem)
                {
                    long oldValue = 0L;
                    if (Wow64DisableWow64FsRedirection(ref oldValue))
                    {
                        _onScreenKeyboardProcess = Process.Start(_oskPath);
                        Wow64EnableWow64FsRedirection(ref oldValue);
                    }
                }
                else
                {
                    _onScreenKeyboardProcess = Process.Start(_oskPath);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"خطأ في فتح لوحة المفاتيح: {ex.Message}");
            }
        }

        private void CloseOnScreenKeyboard()
        {
            try
            {
                if (_onScreenKeyboardProcess != null &&
                    !_onScreenKeyboardProcess.HasExited)
                {
                    _onScreenKeyboardProcess.Kill();
                }
            }
            catch { /* تجاهل */ }
        }

        #endregion

        #region ── Save Logic ───────────────────────────────────────────────

        private void Save()
        {
            tableNo = txtInput.Text ?? string.Empty;
            isDone = !string.IsNullOrEmpty(tableNo);

            CloseOnScreenKeyboard();
            this.Close();
        }

        #endregion

        #region ── Button & Key Events ──────────────────────────────────────

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            Save();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                CloseOnScreenKeyboard();
                this.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"خطأ في الإلغاء: {ex.Message}");
            }
        }

        private void txtInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return || e.Key == Key.Enter)
                Save();
        }

        #endregion
    }
}