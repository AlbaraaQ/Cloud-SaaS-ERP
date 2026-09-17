using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using Microsoft.VisualBasic.CompilerServices;
using System.Windows.Input;
using Brushes = System.Windows.Media.Brushes;
using ButtonBase = System.Windows.Controls.Primitives.ButtonBase;
using Control = System.Windows.Controls.Control;

namespace SmartAuditERP
{
    public class FormPermission
    {
        #region Public Properties

        public bool Editable { get; set; }

        public bool Addable { get; set; }

        public bool Delete { get; set; }

        public bool Search { get; set; }

        public bool Print { get; set; }

        #endregion

        #region Constructor

        public FormPermission()
        {
            Editable = true;
            Addable = true;
            Delete = true;
            Search = true;
            Print = true;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// œ«·… ⁄«„… „ Ê«›ﬁ… „⁄ WPF.
        ///  „ ≈»ﬁ«¡ «·«”„ ﬂ„« ÂÊ Õ Ï ·«  ‰ﬂ”— «·«” œ⁄«¡«  «·ﬁœÌ„….
        /// </summary>
        public void ApplyFrmPermission(object frm)
        {
            if (frm == null)
                return;

            if (frm is System.Windows.Window window)
            {
                ApplyFrmPermissionInternal(window);
                return;
            }

            if (frm is FrameworkElement frameworkElement)
            {
                ApplyFrmPermissionInternal(frameworkElement);
                return;
            }
        }

        /// <summary>
        /// Overload „»«‘— ·ÊÌ‰œÊ“ WPF
        /// </summary>
        public void ApplyFrmPermission(System.Windows.Window frm)
        {
            if (frm == null)
                return;

            ApplyFrmPermissionInternal(frm);
        }

        #endregion

        #region Internal Logic

        private void ApplyFrmPermissionInternal(FrameworkElement rootElement)
        {
            try
            {
                string formName = ResolveFormName(rootElement);
                if (string.IsNullOrWhiteSpace(formName))
                    return;

                DataTable permissionTable = LoadPermissionData(formName);
                if (permissionTable.Rows.Count <= 0)
                    return;

                DataRow permissionRow = permissionTable.Rows[0];

                bool canNew = GetBool(permissionRow, "IS_New");
                bool canSave = GetBool(permissionRow, "IS_Save");
                bool canDelete = GetBool(permissionRow, "IS_Delete");
                bool canSearch = GetBool(permissionRow, "IS_Search");
                bool canPrint = GetBool(permissionRow, "IS_Print");

                IEnumerable<ButtonBase> targetButtons = GetTargetButtons(rootElement);

                foreach (ButtonBase button in targetButtons)
                {
                    if (button == null || string.IsNullOrWhiteSpace(button.Name))
                        continue;

                    string buttonName = button.Name.Trim();

                    // ’·«ÕÌ… «·≈÷«›… / «·ÃœÌœ
                    if (!canNew && IsAddPermissionButton(buttonName))
                    {
                        DisableButton(button);
                        Addable = false;
                    }

                    // ’·«ÕÌ… «·Õ›Ÿ / «· ⁄œÌ·
                    if (!canSave && IsSavePermissionButton(buttonName))
                    {
                        DisableButton(button);
                        Editable = false;
                    }

                    // ’·«ÕÌ… «·Õ–›
                    if (!canDelete && IsDeletePermissionButton(buttonName))
                    {
                        DisableButton(button);
                        Delete = false;
                    }

                    // ’·«ÕÌ… «·ÿ»«⁄… / «·„⁄«Ì‰…
                    if (!canPrint && IsPrintPermissionButton(buttonName))
                    {
                        DisableButton(button);
                        Print = false;
                    }

                    // ’·«ÕÌ… «·»ÕÀ / «· ‰ﬁ·
                    if (!canSearch && IsSearchPermissionButton(buttonName))
                    {
                        DisableButton(button);
                        Search = false;
                    }
                }
            }
            catch
            {
                // ‰ Ã‰» —„Ì Exception Õ Ï ·« ‰ﬂ”—  Õ„Ì· «·Ê«ÃÂ…
            }
        }

        #endregion

        #region Database

        private DataTable LoadPermissionData(string formName)
        {
            DataTable dataTable = new DataTable();

            using SqlConnection connection = MainClass.ConnObj();
            using SqlDataAdapter adapter = new SqlDataAdapter(
                "select IS_New, IS_Save, IS_Delete, IS_Search, IS_Print, Forms.id " +
                "from User_Permissions, Forms " +
                "where User_Permissions.Form_id = Forms.id " +
                "and Forms.FormName = @FormName " +
                "and user_id = @UserId",
                connection);

            adapter.SelectCommand.Parameters.AddWithValue("@FormName", formName);
            adapter.SelectCommand.Parameters.AddWithValue("@UserId", MainClass.UserID);

            adapter.Fill(dataTable);
            return dataTable;
        }

        private bool GetBool(DataRow row, string columnName)
        {
            try
            {
                if (row == null || !row.Table.Columns.Contains(columnName))
                    return false;

                return row[columnName] != DBNull.Value
                    && Convert.ToBoolean(row[columnName]);
            }
            catch
            {
                return false;
            }
        }

        #endregion

        #region Button Discovery

        private IEnumerable<ButtonBase> GetTargetButtons(FrameworkElement rootElement)
        {
            // ‰Õ«Ê· √Ê·« ≈ÌÃ«œ pnlOperBtns ≈‰ ÊÃœ
            FrameworkElement operationPanel = FindElementByName(rootElement, "pnlOperBtns");

            if (operationPanel != null)
            {
                List<ButtonBase> panelButtons = FindVisualChildren<ButtonBase>(operationPanel).ToList();
                if (panelButtons.Count > 0)
                    return panelButtons;
            }

            // fallback: «·»ÕÀ ›Ì ﬂ«„· «·‰«›–…
            return FindVisualChildren<ButtonBase>(rootElement).ToList();
        }

        private FrameworkElement FindElementByName(FrameworkElement rootElement, string elementName)
        {
            if (rootElement == null || string.IsNullOrWhiteSpace(elementName))
                return null;

            if (string.Equals(rootElement.Name, elementName, StringComparison.OrdinalIgnoreCase))
                return rootElement;

            foreach (FrameworkElement child in FindVisualChildren<FrameworkElement>(rootElement))
            {
                if (string.Equals(child.Name, elementName, StringComparison.OrdinalIgnoreCase))
                    return child;
            }

            return null;
        }

        private IEnumerable<T> FindVisualChildren<T>(DependencyObject dependencyObject)
            where T : DependencyObject
        {
            if (dependencyObject == null)
                yield break;

            int childrenCount = System.Windows.Media.VisualTreeHelper.GetChildrenCount(dependencyObject);

            for (int index = 0; index < childrenCount; index++)
            {
                DependencyObject child = System.Windows.Media.VisualTreeHelper.GetChild(dependencyObject, index);

                if (child is T typedChild)
                    yield return typedChild;

                foreach (T childOfChild in FindVisualChildren<T>(child))
                    yield return childOfChild;
            }
        }

        #endregion

        #region Button Classification

        private bool IsAddPermissionButton(string buttonName)
        {
            return Matches(buttonName,
                "btnNew",
                "btnAdd",
                "btnAddItem",
                "btnAddNewItem",
                "btnAddbook",
                "btnCustAdd",
                "btnAddUnit",
                "cmbAddSalesMen");
        }

        private bool IsSavePermissionButton(string buttonName)
        {
            return Matches(buttonName,
                "btnSave",
                "btnSavePrint",
                "btnPay",
                "btnHoldOrd");
        }

        private bool IsDeletePermissionButton(string buttonName)
        {
            return Matches(buttonName,
                "btnDelete",
                "btnDeleteRow");
        }

        private bool IsPrintPermissionButton(string buttonName)
        {
            return Matches(buttonName,
                "btnPrint",
                "btnPreview",
                "btnView",
                "btnPrintCashier");
        }

        private bool IsSearchPermissionButton(string buttonName)
        {
            return Matches(buttonName,
                "btnSearch",
                "btnFirst",
                "btnLast",
                "btnNext",
                "btnPrevious",
                "btnShow",
                "btnsrch",
                "btnInvSrch");
        }

        private bool Matches(string sourceName, params string[] targetNames)
        {
            return targetNames.Any(name =>
                string.Equals(sourceName, name, StringComparison.OrdinalIgnoreCase));
        }

        #endregion

        #region UI Helpers

        private void DisableButton(ButtonBase button)
        {
            if (button == null)
                return;

            button.IsEnabled = false;

            if (button is Control control)
            {
                control.Background = Brushes.Gray;
                control.Foreground = Brushes.White;
                control.Opacity = 0.75;
                control.Cursor = System.Windows.Input.Cursors.Arrow;
            }
        }

        private string ResolveFormName(FrameworkElement rootElement)
        {
            if (rootElement == null)
                return string.Empty;

            // «·√Ê·ÊÌ… ··«”„ «·„’—Õ »Â
            if (!string.IsNullOrWhiteSpace(rootElement.Name))
                return rootElement.Name.Trim();

            // fallback: «”„ «·ﬂ·«”
            return rootElement.GetType().Name.Trim();
        }

        #endregion
    }
}