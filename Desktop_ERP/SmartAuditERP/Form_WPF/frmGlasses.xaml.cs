using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Windows;
using AuditorAPI.Models.DGVmodels;
using DevExpress.Xpf.Core;
using SmartAuditERP.Form_WPF;

namespace SmartAuditERP.Form_WPF
{
    public partial class frmGlasses : ThemedWindow
    {
        #region Fields

        public SqlConnection conn;
        public SqlConnection conn1;
        public List<Glass> Glasses;
        public string InvGlobalID;
        public int ItemId;
        public int code;

        private string _lblReSPH = "RE-SPH";
        private string _lblReCYL = "RE-CYL";
        private string _lblReAx = "RE-AX";
        private string _lblReAdd = "RE-ADD";
        private string _lblReIpd = "RE-IPD";
        private string _lblLeSph = "LE-SPH";
        private string _lblLeCyl = "LE-CYL";
        private string _lblLeAx = "LE-AX";
        private string _lblLeAdd = "LE-ADD";
        private string _lblLeIpd = "LE-IPD";

        #endregion

        #region Constructor

        public frmGlasses()
        {
            conn = MainClass.ConnObj();
            conn1 = MainClass.ConnObj();
            Glasses = new List<Glass>();
            InvGlobalID = string.Empty;
            ItemId = 0;
            code = 1;
            InitializeComponent();
        }

        #endregion

        #region Load

        private void frmGlasses_Load(object sender, RoutedEventArgs e)
        {
            if (Glasses.Count > 0)
                bindControls();

            if (code == 1)
            {
                TabControl1.SelectedIndex = 0;
                TabPage2.Visibility = Visibility.Collapsed;
                BtnSaveinsert.Visibility = Visibility.Collapsed;
            }
            else
            {
                BtnSaveinsert.Visibility = Visibility.Visible;
                loadcolumnOther();
            }

            loadNameLbl();
        }

        #endregion

        #region Bind Class / Controls

        private void bindClass()
        {
            Glass rightGlass = new Glass
            {
                orientation = "R",
                SPH = txtReSPH.Text,
                CYL = txtReCYL.Text,
                AX = txtReAx.Text,
                ADD = txtReAdd.Text,
                IPD = txtReIpd.Text,
                InvGlobalID = InvGlobalID,
                ItemId = ItemId
            };
            Glasses.Add(rightGlass);

            Glass leftGlass = new Glass
            {
                orientation = "L",
                SPH = txtLeSph.Text,
                CYL = txtLeCyl.Text,
                AX = txtLeAx.Text,
                ADD = txtLeAdd.Text,
                IPD = txtLeIpd.Text,
                InvGlobalID = InvGlobalID,
                ItemId = ItemId
            };
            Glasses.Add(leftGlass);
        }

        private void bindControls()
        {
            foreach (Glass glass in Glasses)
            {
                if (string.Equals(glass.orientation, "R", StringComparison.OrdinalIgnoreCase))
                {
                    txtReSPH.Text = glass.SPH;
                    txtReCYL.Text = glass.CYL;
                    txtReAx.Text = glass.AX;
                    txtReAdd.Text = glass.ADD;
                    txtReIpd.Text = glass.IPD;
                }
                else if (string.Equals(glass.orientation, "L", StringComparison.OrdinalIgnoreCase))
                {
                    txtLeSph.Text = glass.SPH;
                    txtLeCyl.Text = glass.CYL;
                    txtLeAx.Text = glass.AX;
                    txtLeAdd.Text = glass.ADD;
                    txtLeIpd.Text = glass.IPD;
                }
            }
        }

        #endregion

        #region CLR

        private void CLR()
        {
            Glasses.Clear();
            txtReSPH.Text = string.Empty;
            txtReCYL.Text = string.Empty;
            txtReAx.Text = string.Empty;
            txtReAdd.Text = string.Empty;
            txtReIpd.Text = string.Empty;
            txtLeSph.Text = string.Empty;
            txtLeCyl.Text = string.Empty;
            txtLeAx.Text = string.Empty;
            txtLeAdd.Text = string.Empty;
            txtLeIpd.Text = string.Empty;
        }

        #endregion

        #region Button Events

        private void btnNew_Click(object sender, RoutedEventArgs e)
        {
            CLR();
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            bindClass();
            Close();
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void BtnSaveinsert_Click(object sender, RoutedEventArgs e)
        {
            insertglasses();
            CLR();
        }

        #endregion

        #region Load Label Names

        public void loadNameLbl()
        {
            try
            {
                DataTable dataTable = new DataTable();
                new SqlDataAdapter(
                    "select isnull(L1,'LE-SPH') as L1, isnull(L2,'LE-CYL') as L2, " +
                    "isnull(L3,'LE-AX') as L3, isnull(L4,'LE-ADD') as L4, " +
                    "isnull(L5,'LE-IPD') as L5, isnull(R1,'RE-SPH') as R1, " +
                    "isnull(R2,'RE-CYL') as R2, isnull(R3,'RE-AX') as R3, " +
                    "isnull(R4,'RE-ADD') as R4, isnull(R5,'RE-IPD') as R5 " +
                    "from Other_Column", conn).Fill(dataTable);

                if (dataTable.Rows.Count > 0)
                {
                    lblLeSphText.Text = dataTable.Rows[0]["L1"].ToString();
                    lblLeCylText.Text = dataTable.Rows[0]["L2"].ToString();
                    lblLeAxText.Text = dataTable.Rows[0]["L3"].ToString();
                    lblLeAddText.Text = dataTable.Rows[0]["L4"].ToString();
                    lblLeIpdText.Text = dataTable.Rows[0]["L5"].ToString();
                    lblReSPHText.Text = dataTable.Rows[0]["R1"].ToString();
                    lblReCYLText.Text = dataTable.Rows[0]["R2"].ToString();
                    lblReAxText.Text = dataTable.Rows[0]["R3"].ToString();
                    lblReAddText.Text = dataTable.Rows[0]["R4"].ToString();
                    lblReIpdText.Text = dataTable.Rows[0]["R5"].ToString();
                }
            }
            catch (Exception ex)
            {
                DXMessageBox.Show(ex.Message, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Insert Glasses Column Names

        public void insertglasses()
        {
            try
            {
                EnsureConnectionOpen();

                new SqlCommand("delete from Other_Column", conn).ExecuteNonQuery();

                SqlCommand sqlCommand = new SqlCommand(
                    "insert into Other_Column (R1,R2,R3,R4,R5,L1,L2,L3,L4,L5) " +
                    "VALUES(@R1,@R2,@R3,@R4,@R5,@L1,@L2,@L3,@L4,@L5)", conn);

                sqlCommand.Parameters.AddWithValue("@R1", txtR1.Text);
                sqlCommand.Parameters.AddWithValue("@R2", txtR2.Text);
                sqlCommand.Parameters.AddWithValue("@R3", txtR3.Text);
                sqlCommand.Parameters.AddWithValue("@R4", txtR4.Text);
                sqlCommand.Parameters.AddWithValue("@R5", txtR5.Text);
                sqlCommand.Parameters.AddWithValue("@L1", txtR6.Text);
                sqlCommand.Parameters.AddWithValue("@L2", txtR7.Text);
                sqlCommand.Parameters.AddWithValue("@L3", txtR8.Text);
                sqlCommand.Parameters.AddWithValue("@L4", txtR9.Text);
                sqlCommand.Parameters.AddWithValue("@L5", txtR10.Text);
                sqlCommand.ExecuteNonQuery();

                DXMessageBox.Show("تم الحفظ بنجاح", "المدقق",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                DXMessageBox.Show(ex.Message, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                EnsureConnectionClosed();
            }
        }

        #endregion

        #region Load Column Other

        public void loadcolumnOther()
        {
            try
            {
                SqlDataAdapter sqlDataAdapter = new SqlDataAdapter(
                    "select isnull(L1,'LE-SPH') as L1, isnull(L2,'LE-CYL') as L2, " +
                    "isnull(L3,'LE-AX') as L3, isnull(L4,'LE-ADD') as L4, " +
                    "isnull(L5,'LE-IPD') as L5, isnull(R1,'RE-SPH') as R1, " +
                    "isnull(R2,'RE-CYL') as R2, isnull(R3,'RE-AX') as R3, " +
                    "isnull(R4,'RE-ADD') as R4, isnull(R5,'RE-IPD') as R5 " +
                    "from Other_Column", conn);

                DataTable dataTable = new DataTable();
                sqlDataAdapter.Fill(dataTable);

                if (dataTable.Rows.Count > 0)
                {
                    txtR1.Text = dataTable.Rows[0]["R1"].ToString();
                    txtR2.Text = dataTable.Rows[0]["R2"].ToString();
                    txtR3.Text = dataTable.Rows[0]["R3"].ToString();
                    txtR4.Text = dataTable.Rows[0]["R4"].ToString();
                    txtR5.Text = dataTable.Rows[0]["R5"].ToString();
                    txtR6.Text = dataTable.Rows[0]["L1"].ToString();
                    txtR7.Text = dataTable.Rows[0]["L2"].ToString();
                    txtR8.Text = dataTable.Rows[0]["L3"].ToString();
                    txtR9.Text = dataTable.Rows[0]["L4"].ToString();
                    txtR10.Text = dataTable.Rows[0]["L5"].ToString();
                }
            }
            catch (Exception ex)
            {
                DXMessageBox.Show(ex.Message, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Helpers

        private void EnsureConnectionOpen()
        {
            if (conn.State != ConnectionState.Open)
                conn.Open();
        }

        private void EnsureConnectionClosed()
        {
            if (conn.State != ConnectionState.Closed)
                conn.Close();
        }

        #endregion
    }
}