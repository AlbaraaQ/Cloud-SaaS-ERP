using AuditorAPI.Models;
using DevExpress.Xpf.Core;
using ECRPaymentsAPI;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Drawing.Printing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace SmartAuditERP.Form_WPF
{
    public partial class frmSettings : ThemedWindow
    {
        #region Fields

        private string _name;
        private string _size;
        private int _fcolor;
        private int _bcolor;
        private string _style;

        private string CashierPrinter;
        private string kitchenprinter;
        private string serverName;

        private readonly ObservableCollection<PrinterGridRow> _printerRows;
        private readonly ObservableCollection<DatabaseManagementRow> _availableDatabaseRows;
        private readonly ObservableCollection<YearPreviewRow> _yearPreviewRows;

        private readonly OpenFileDialog OpenFileDialog1;
        private readonly SaveFileDialog saveFileDialog1;
        private readonly DispatcherTimer Timer1;
        private readonly DispatcherTimer Timer2;

        private DateTime startSyncDate;
        private DateTime endSyncDate;

        private bool _isLoaded;

        private SqlConnection conn;
        private SqlConnection conn1;
        public SqlConnection con1;

        #endregion

        #region Constructor

        public frmSettings()
        {
            _name = "Tahoma";
            _size = "8";
            _fcolor = 0;
            _bcolor = 0;
            _style = "عادي";

            CashierPrinter = string.Empty;
            kitchenprinter = string.Empty;
            serverName = MainClass.Server;

            conn = MainClass.ConnObj();
            conn1 = MainClass.ConnObj();
            con1 = new SqlConnection($"server={serverName}; integrated security = true");

            startSyncDate = DateTime.Now;
            endSyncDate = DateTime.Now.AddYears(1);

            _printerRows = new ObservableCollection<PrinterGridRow>();
            _availableDatabaseRows = new ObservableCollection<DatabaseManagementRow>();
            _yearPreviewRows = new ObservableCollection<YearPreviewRow>();

            OpenFileDialog1 = new OpenFileDialog();
            saveFileDialog1 = new SaveFileDialog
            {
                DefaultExt = "*.BAK",
                Filter = "Backup Files (*.bak)|*.bak|All Files (*.*)|*.*"
            };

            Timer1 = new DispatcherTimer();
            Timer2 = new DispatcherTimer();

            InitializeComponent();

            Loaded += frmUserSettings_Load;
            KeyDown += frmSettings_KeyDown;

            RegisterEvents();
            InitializeCollections();
            InitializeStaticUI();
        }

        #endregion

        #region Initialization

        private void RegisterEvents()
        {
            if (btnSaveGen != null) btnSaveGen.Click += btnSaveGen_Click;
            if (btnSavePOSetting != null) btnSavePOSetting.Click += btnSavePOSetting_Click;
            if (btnprintsettings != null) btnprintsettings.Click += btnprintsettings_Click;
            if (btnSaveEmail != null) btnSaveEmail.Click += btnSaveEmail_Click;
            if (btnOperPermiss != null) btnOperPermiss.Click += btnOperPermiss_Click;
            if (btnSyncSave != null) btnSyncSave.Click += btnSave_Click;
            if (btnCloseShiftSetting != null) btnCloseShiftSetting.Click += btnCloseShiftSetting_Click;
            if (BtnSaveGedia != null) BtnSaveGedia.Click += BtnSaveGedia_Click;
            if (BtnTestGedia != null) BtnTestGedia.Click += BtnTestGedia_Click;
            if (Btnsavneoleap != null) Btnsavneoleap.Click += Btnsavneoleap_Click;
            if (Btntestneoleap != null) Btntestneoleap.Click += Btntestneoleap_Click;

            if (btnDeleteInv != null) btnDeleteInv.Click += btnDeleteInv_Click_1;
            if (BtnDeleteEntry != null) BtnDeleteEntry.Click += BtnDeleteEntry_Click_1;
            if (BtndeleteRecept != null) BtndeleteRecept.Click += BtndeleteRecept_Click_1;
            if (BtnDeleteItems != null) BtnDeleteItems.Click += BtnDeleteItems_Click_1;
            if (btnDeleteCat != null) btnDeleteCat.Click += btnDeleteCat_Click;
            if (BtnResetData != null) BtnResetData.Click += BtnResetData_Click;

            if (Button5 != null) Button5.Click += Button5_Click;
            if (Button6 != null) Button6.Click += Button6_Click;
            if (Button3 != null) Button3.Click += Button3_Click;
            if (Button4 != null) Button4.Click += Button4_Click;
            if (Button1 != null) Button1.Click += Button1_Click_1;

            if (btnconnectit != null) btnconnectit.Click += BtnConnect_Click;
            if (btnSelectPath != null) btnSelectPath.Click += btnSelectPath_Click;
            if (AddColumnAdd != null) AddColumnAdd.Click += AddColumnAdd_Click;

            if (btnGetClientCode != null) btnGetClientCode.Click += async (s, e) => await btnGetClientCode_ClickAsync(s, e);
            if (btnSyncCards != null) btnSyncCards.Click += async (s, e) => await btnSyncCards_ClickAsync(s, e);
            if (btnGetBranches != null) btnGetBranches.Click += async (s, e) => await btnGetBranches_ClickAsync(s, e);

            if (ckDefaultSettings != null) ckDefaultSettings.Checked += ckDefaultSettings_CheckedChanged;
            if (ckAllpayMethods != null) ckAllpayMethods.Checked += ckAllpayMethods_CheckedChanged;
            if (ckAllPayType != null) ckAllPayType.Checked += ckAllPayType_CheckedChanged;
            if (ckShowAllOrders != null) ckShowAllOrders.Checked += ckShowAllOrders_CheckedChanged;
            if (ckAll != null) ckAll.Checked += ckAll_CheckedChanged;
            if (ckAllInvs != null) ckAllInvs.Checked += ckAllInvs_CheckedChanged;

            if (ckPurchInv != null) ckPurchInv.Checked += ckPurchInv_CheckedChanged;
            if (ckSaleInv != null) ckSaleInv.Checked += ckSaleInv_CheckedChanged;
            if (ckPOSInv != null) ckPOSInv.Checked += ckPOSInv_CheckedChanged;
            if (ckRentInv != null) ckRentInv.Checked += ckRentInv_CheckedChanged;
            if (ckContracts != null) ckContracts.Checked += ckContracts_CheckedChanged;
            if (ckReports != null) ckReports.Checked += ckReports_CheckedChanged;

            if (cbPurchInv != null) cbPurchInv.Checked += cbPurchInv_CheckedChanged;
            if (cbSalesInv != null) cbSalesInv.Checked += cbSalesInv_CheckedChanged;
            if (cbPOS != null) cbPOS.Checked += cbPOS_CheckedChanged;
            if (cbRentInv != null) cbRentInv.Checked += cbRentInv_CheckedChanged;
            if (ckProjcMng != null) ckProjcMng.Checked += ckProjcMng_CheckedChanged;
            if (rbProduction != null) rbProduction.Checked += rbProduction_CheckedChanged;
            if (rbPricingInv != null) rbPricingInv.Checked += rbPricingInv_CheckedChanged;
            if (rbTransfarInvertory != null) rbTransfarInvertory.Checked += rbTransfarInvertory_CheckedChanged;
            if (rbInvertoryCorrection != null) rbInvertoryCorrection.Checked += rbInvertoryCorrection_CheckedChanged;
            if (rbInvertoryOut != null) rbInvertoryOut.Checked += rbInvertoryOut_CheckedChanged;
            if (rbInvertoryIn != null) rbInvertoryIn.Checked += rbInvertoryIn_CheckedChanged;
            if (rbFirstInvrtory != null) rbFirstInvrtory.Checked += rbFirstInvrtory_CheckedChanged;
            if (rbRecieptVAT != null) rbRecieptVAT.Checked += rbRecieptVAT_CheckedChanged;
            if (btnInventoryOrder != null) btnInventoryOrder.Checked += btnInventoryOrder_CheckedChanged;
            if (rdsalcontract != null) rdsalcontract.Checked += rdsalcontract_CheckedChanged;

            if (rbCashier != null) rbCashier.Checked += rbCashier_CheckedChanged;
            if (rbA4 != null) rbA4.Checked += rbA4_CheckedChanged;

            if (chkPoleDisplay != null)
            {
                chkPoleDisplay.Checked += chkPoleDisplay_CheckedChanged;
                chkPoleDisplay.Unchecked += chkPoleDisplay_CheckedChanged;
            }

            if (ckSyncCloud != null)
            {
                ckSyncCloud.Checked += ckSyncCloud_CheckedChanged;
                ckSyncCloud.Unchecked += ckSyncCloud_CheckedChanged;
            }

            if (rbSecondaryBranch != null) rbSecondaryBranch.Checked += rbSecondaryBranch_CheckedChanged;
            if (rbMiddleBranch != null) rbMiddleBranch.Checked += rbSecondaryBranch_CheckedChanged;
            if (rbDataCenterBranch != null) rbDataCenterBranch.Checked += rbSecondaryBranch_CheckedChanged;

            if (TabOtherSetting != null)
            {
                TabOtherSetting.SelectionChanged += TabControl2_SelectedIndexChanged;
                TabOtherSetting.KeyDown += TabOtherSetting_KeyDown;
            }

            if (Cmbservername != null) Cmbservername.SelectionChanged += Cmbservername_SelectedIndexChanged;
            if (cmbCashPrinter != null) cmbCashPrinter.SelectionChanged += cmbcashierPrinter_SelectedIndexChanged;
            if (cmbKitchenprinter != null) cmbKitchenprinter.SelectionChanged += cmbBarcodePrinter_SelectedIndexChanged;

            if (ItemsColNo != null) ItemsColNo.TextChanged += ItemsColNo_ValueChanged;
            if (ItemsRowsNo != null) ItemsRowsNo.TextChanged += ItemsRowsNo_ValueChanged;

            if (lnkImgAdd1 != null) lnkImgAdd1.MouseLeftButtonDown += lnkImgAdd1_LinkClicked;
            if (lnkImgAdd2 != null) lnkImgAdd2.MouseLeftButtonDown += lnkImgAdd2_LinkClicked;
            if (lnkImgAdd3 != null) lnkImgAdd3.MouseLeftButtonDown += lnkImgAdd3_LinkClicked;
            if (lnkImgClr1 != null) lnkImgClr1.MouseLeftButtonDown += lnkImgClr1_LinkClicked;
            if (lnkImgClr2 != null) lnkImgClr2.MouseLeftButtonDown += lnkImgClr2_LinkClicked;
            if (lnkImgClr3 != null) lnkImgClr3.MouseLeftButtonDown += lnkImgClr3_LinkClicked;

            if (GridControl1 != null)
            {
                GridControl1.AddHandler(Button.ClickEvent, new RoutedEventHandler(GridControl1_ButtonClick));
            }

            if (GridControl2 != null)
            {
                GridControl2.AddHandler(Button.ClickEvent, new RoutedEventHandler(GridControl2_ButtonClick));
            }

            if (GridControl3 != null)
            {
                GridControl3.AddHandler(Button.ClickEvent, new RoutedEventHandler(GridControl3_ButtonClick));
            }

            Timer1.Interval = TimeSpan.FromSeconds(1);
            Timer1.Tick += Timer1_Tick;
        }

        private void InitializeCollections()
        {
            if (GridControl1 != null)
            {
                GridControl1.ItemsSource = _printerRows;
            }

            if (GridControl2 != null)
            {
                GridControl2.ItemsSource = _availableDatabaseRows;
            }

            if (GridControl3 != null)
            {
                GridControl3.ItemsSource = _yearPreviewRows;
            }
        }

        private void InitializeStaticUI()
        {
            if (txtDate != null && txtDate.SelectedDate == null)
            {
                txtDate.SelectedDate = DateTime.Today;
            }

            if (chkDigitalDisplay != null)
            {
                chkDigitalDisplay.Visibility = chkPoleDisplay?.IsChecked == true
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }

            if (chkSyncCloudFirst != null)
            {
                chkSyncCloudFirst.Visibility = ckSyncCloud?.IsChecked == true
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }
        }

        #endregion

        #region Window Load

        private void frmUserSettings_Load(object sender, RoutedEventArgs e)
        {
            if (_isLoaded)
            {
                return;
            }

            _isLoaded = true;

            try
            {
                LoadAccounts();
                LoadUnits();
                LoadTreasury();
                loadCustomers(2);
                LoadPrinters();
                loadData(1);
                loadPOSetting(3);
                LoadprintSetting(1);
                LoadEmailSetting();
                LoadCloseShiftSetting();
                loadGediaSetting();
                Loadserver();
                LoadSettingNotify();
                loadDBs();
                loadDB_by_DbAutoName();
                loadSyncSetting();

                LoadCurrencies();
                GridControl1.ItemsSource = _printerRows;

                if (MainClass.EmpNo > 0)
                {
                    if (GroupBoxReset != null) GroupBoxReset.Visibility = Visibility.Collapsed;
                    if (Panelnot1 != null) Panelnot1.Visibility = Visibility.Collapsed;
                    if (panelnot2 != null) panelnot2.Visibility = Visibility.Collapsed;
                }

                try
                {
                    txtdocuments.Text = Properties.Settings.Default.DocsRootPath;
                }
                catch
                {
                    txtdocuments.Text = string.Empty;
                }

                UpdateItemsInPage();
                Timer1.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadCurrencies()
        {
            try
            {
                cmbCurrency.Items.Clear();

                foreach (var currencyValue in Enum.GetValues(typeof(Currency)))
                {
                    cmbCurrency.Items.Add(currencyValue);
                }
            }
            catch
            {
                cmbCurrency.Items.Clear();
                cmbCurrency.Items.Add("ريال");
                cmbCurrency.Items.Add("دولار");
                cmbCurrency.Items.Add("يورو");
            }
        }

        #endregion

        #region Helper Methods

        private bool IsChecked(CheckBox checkBox)
        {
            return checkBox != null && checkBox.IsChecked == true;
        }

        private bool IsChecked(RadioButton radioButton)
        {
            return radioButton != null && radioButton.IsChecked == true;
        }

        private int ToBit(CheckBox checkBox)
        {
            return IsChecked(checkBox) ? 1 : 0;
        }

        private int ToBit(RadioButton radioButton)
        {
            return IsChecked(radioButton) ? 1 : 0;
        }

        private int SafeInt(string text, int defaultValue = 0)
        {
            return int.TryParse(text?.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out int value)
                ? value
                : defaultValue;
        }

        private double SafeDouble(string text, double defaultValue = 0)
        {
            return double.TryParse(text?.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double value)
                ? value
                : defaultValue;
        }

        private decimal SafeDecimal(string text, decimal defaultValue = 0)
        {
            return decimal.TryParse(text?.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal value)
                ? value
                : defaultValue;
        }

        private string GetComboText(ComboBox comboBox)
        {
            if (comboBox == null)
            {
                return string.Empty;
            }

            if (comboBox.SelectedItem is ComboBoxItem comboBoxItem)
            {
                return comboBoxItem.Content?.ToString() ?? string.Empty;
            }

            return comboBox.Text ?? string.Empty;
        }

        private void SelectComboItemByContent(ComboBox comboBox, string text)
        {
            if (comboBox == null)
            {
                return;
            }

            foreach (var item in comboBox.Items)
            {
                if (item is ComboBoxItem comboBoxItem)
                {
                    if (string.Equals(comboBoxItem.Content?.ToString(), text, StringComparison.OrdinalIgnoreCase))
                    {
                        comboBox.SelectedItem = comboBoxItem;
                        return;
                    }
                }
            }

            comboBox.Text = text;
        }

        private void BindComboBox(ComboBox comboBox, DataTable table, string displayMember, string valueMember)
        {
            if (comboBox == null)
            {
                return;
            }

            comboBox.ItemsSource = table.DefaultView;
            comboBox.DisplayMemberPath = displayMember;
            comboBox.SelectedValuePath = valueMember;
            comboBox.SelectedIndex = -1;
        }

        private BitmapImage LoadBitmapFromFile(string filePath)
        {
            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.UriSource = new Uri(filePath, UriKind.Absolute);
            image.EndInit();
            image.Freeze();
            return image;
        }

        private BitmapImage BytesToBitmapImage(byte[] imageBytes)
        {
            using var memoryStream = new MemoryStream(imageBytes);
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.StreamSource = memoryStream;
            bitmap.EndInit();
            bitmap.Freeze();
            return bitmap;
        }

        private byte[] BitmapImageToBytes(ImageSource imageSource)
        {
            if (imageSource is not BitmapSource bitmapSource)
            {
                return null;
            }

            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmapSource));

            using var memoryStream = new MemoryStream();
            encoder.Save(memoryStream);
            return memoryStream.ToArray();
        }

        private string SelectFolderPath()
        {
            var dialog = new OpenFileDialog
            {
                ValidateNames = false,
                CheckFileExists = false,
                CheckPathExists = true,
                FileName = "اختر مجلد"
            };

            return dialog.ShowDialog() == true
                ? Path.GetDirectoryName(dialog.FileName)
                : string.Empty;
        }

        private void SetImageSource(Image imageControl, byte[] imageBytes)
        {
            if (imageControl == null)
            {
                return;
            }

            imageControl.Source = imageBytes == null || imageBytes.Length == 0
                ? null
                : BytesToBitmapImage(imageBytes);
        }

        private string ShowInputDialog(string title, string defaultValue = "")
        {
            string result = defaultValue;

            var inputWindow = new System.Windows.Window
            {
                Title = title,
                Width = 420,
                Height = 170,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                ResizeMode = ResizeMode.NoResize,
                FlowDirection = System.Windows.FlowDirection.RightToLeft,
                Owner = this,
                Background = Brushes.White
            };

            var root = new Grid { Margin = new Thickness(12) };
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var textBlock = new TextBlock
            {
                Text = title,
                Margin = new Thickness(0, 0, 0, 8),
                FontWeight = FontWeights.Bold
            };

            var textBox = new TextBox
            {
                Text = defaultValue,
                Height = 32,
                Margin = new Thickness(0, 0, 0, 12)
            };

            var buttonsPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Left
            };

            var okButton = new Button
            {
                Content = "موافق",
                Width = 90,
                Height = 32,
                Margin = new Thickness(0, 0, 8, 0)
            };

            var cancelButton = new Button
            {
                Content = "إلغاء",
                Width = 90,
                Height = 32
            };

            okButton.Click += (_, __) =>
            {
                result = textBox.Text?.Trim() ?? string.Empty;
                inputWindow.DialogResult = true;
                inputWindow.Close();
            };

            cancelButton.Click += (_, __) =>
            {
                inputWindow.DialogResult = false;
                inputWindow.Close();
            };

            buttonsPanel.Children.Add(okButton);
            buttonsPanel.Children.Add(cancelButton);

            Grid.SetRow(textBlock, 0);
            Grid.SetRow(textBox, 1);
            Grid.SetRow(buttonsPanel, 2);

            root.Children.Add(textBlock);
            root.Children.Add(textBox);
            root.Children.Add(buttonsPanel);

            inputWindow.Content = root;
            bool? dialogResult = inputWindow.ShowDialog();

            return dialogResult == true ? result : string.Empty;
        }

        private T FindVisualParent<T>(DependencyObject child) where T : DependencyObject
        {
            if (child == null)
            {
                return null;
            }

            DependencyObject parentObject = VisualTreeHelper.GetParent(child);

            while (parentObject != null)
            {
                if (parentObject is T parent)
                {
                    return parent;
                }

                parentObject = VisualTreeHelper.GetParent(parentObject);
            }

            return null;
        }

        private void UpdateItemsInPage()
        {
            int columns = SafeInt(ItemsColNo?.Text, 0);
            int rows = SafeInt(ItemsRowsNo?.Text, 0);
            txtItemsInPage.Text = (columns * rows).ToString(CultureInfo.InvariantCulture);
        }

        private bool Confirm(string message, string title = "تأكيد")
        {
            return MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;
        }

        private void EnsureConnectionOpen(SqlConnection sqlConnection)
        {
            if (sqlConnection.State != ConnectionState.Open)
            {
                sqlConnection.Open();
            }
        }

        #endregion

        #region Load Methods

        public void LoadSettingNotify()
        {
            try
            {
                using var adapter = new SqlDataAdapter("select BranchId as id,name from Branches where IS_Deleted=0", conn);
                var branchTable = new DataTable();
                adapter.Fill(branchTable);

                BindComboBox(CmbBranchMqtt, branchTable, "name", "id");

                if (txtpasscode != null && string.IsNullOrWhiteSpace(txtpasscode.Text))
                {
                    txtpasscode.Text = GenerateSecureRandomKey(10);
                }

                if (txtclientid != null && string.IsNullOrWhiteSpace(txtclientid.Text))
                {
                    txtclientid.Text = GenerateSecureRandomKey(5);
                }

                if (txtfrequency != null && string.IsNullOrWhiteSpace(txtfrequency.Text))
                {
                    txtfrequency.Text = GenerateSecureRandomKey(2);
                }

                EnsureConnectionOpen(conn);

                using var command = new SqlCommand(
                    @"SELECT 
                        ISNULL([id], 0) AS id,
                        ISNULL([clientid], '') AS clientid,
                        ISNULL([nodeid], '') AS nodeid,
                        ISNULL([frequency], 0) AS frequency,
                        ISNULL([sale], 0) AS sale,
                        ISNULL([receipt], 0) AS receipt,
                        ISNULL([income], 0) AS income,
                        ISNULL([finance], 0) AS finance,
                        ISNULL([passcode], '') AS passcode,
                        ISNULL([servername], '') AS servername,
                        ISNULL([usrname], '') AS usrname,
                        ISNULL([pwd], '') AS pwd,
                        ISNULL([DB], '') AS DB,
                        ISNULL([publickey], '') AS publickey,
                        ISNULL([privatekey], '') AS privatekey,
                        ISNULL([IS_Play], 0) AS IS_Play,
                        ISNULL([inventory], 0) AS inventory,
                        ISNULL([is_active], 0) AS is_active,
                        ISNULL([BranchId], 0) AS BranchId,
                        ISNULL([IpServer], '') AS IpServer,
                        ISNULL([syncItems], 0) AS syncItems,
                        ISNULL([syncOper], 0) AS syncOper,
                        ISNULL([syncDefinitions], 0) AS syncDefinitions
                      FROM [Settingmqtt]", conn);

                using var reader = command.ExecuteReader();

                if (reader.Read())
                {
                    txtclientid.Text = reader["clientid"].ToString();
                    txtnodeid.Text = reader["nodeid"].ToString();
                    txtfrequency.Text = reader["frequency"].ToString();
                    txtsale.Text = reader["sale"].ToString();
                    txtreceipt.Text = reader["receipt"].ToString();
                    txtincome.Text = reader["income"].ToString();
                    txtfinance.Text = reader["finance"].ToString();
                    txtpasscode.Text = reader["passcode"].ToString();
                    Cmbservername.Text = reader["servername"].ToString();
                    usrname.Text = reader["usrname"].ToString();
                    pwdtxt.Text = reader["pwd"].ToString();
                    CmbTxtDataBase.Text = reader["DB"].ToString();
                    txtpublickey.Text = reader["publickey"].ToString();
                    txtprivatekey.Text = reader["privatekey"].ToString();
                    ChkIs_Play.IsChecked = Convert.ToBoolean(reader["IS_Play"]);
                    txtitem.Text = reader["inventory"].ToString();
                    chkis_active.IsChecked = Convert.ToBoolean(reader["is_active"]);
                    CmbBranchMqtt.SelectedValue = reader["BranchId"];
                    TxtIpserver.Text = reader["IpServer"].ToString();
                    chsyncItems.IsChecked = Convert.ToBoolean(reader["syncItems"]);
                    ChksyncOper.IsChecked = Convert.ToBoolean(reader["syncOper"]);
                    ChksyncDefinitions.IsChecked = Convert.ToBoolean(reader["syncDefinitions"]);
                }
            }
            catch
            {
                // intentionally silent to preserve legacy behavior
            }
            finally
            {
                if (conn.State == ConnectionState.Open)
                {
                    conn.Close();
                }
            }
        }

        public void Loadserver()
        {
            try
            {
                Cmbservername.Items.Clear();

                using RegistryKey baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
                using RegistryKey sqlServerKey = baseKey.OpenSubKey(@"SOFTWARE\Microsoft\Microsoft SQL Server");

                if (sqlServerKey == null)
                {
                    return;
                }

                if (sqlServerKey.GetValue("InstalledInstances") is string[] instances && instances.Length > 0)
                {
                    foreach (string instanceName in instances)
                    {
                        if (string.Equals(instanceName, "MSSQLSERVER", StringComparison.OrdinalIgnoreCase))
                        {
                            Cmbservername.Items.Add(Environment.MachineName);
                        }
                        else
                        {
                            Cmbservername.Items.Add($"{Environment.MachineName}\\{instanceName}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void LoadDataBase()
        {
            try
            {
                CmbTxtDataBase.Text = string.Empty;

                using var adapter = new SqlDataAdapter(
                    @"SELECT sysdatabases.name, DatabasesManagment.Dbname
                      FROM master.dbo.DatabasesManagment, master.dbo.sysdatabases
                      WHERE DatabasesManagment.dbid=sysdatabases.dbid
                      AND DatabasesManagment.IsActive=1
                      ORDER BY DatabasesManagment.ImportanceOrder", con1);

                var table = new DataTable();
                adapter.Fill(table);

                BindComboBox(CmbTxtDataBase, table, "Dbname", "name");
            }
            catch
            {
                // intentionally silent
            }
        }

        private void LoadPrinters()
        {
            try
            {
                cmbCashPrinter.Items.Clear();
                cmbKitchenprinter.Items.Clear();

                foreach (string printerName in PrinterSettings.InstalledPrinters)
                {
                    cmbCashPrinter.Items.Add(printerName);
                    cmbKitchenprinter.Items.Add(printerName);
                }
            }
            catch
            {
                // intentionally silent
            }
        }

        private void loadCustomers(int custType)
        {
            try
            {
                using var adapter = new SqlDataAdapter(
                    $"select id,name from Customers where IS_Deleted=0 and (type={custType} or type=3) order by id", conn);

                var table = new DataTable();
                adapter.Fill(table);

                BindComboBox(CmbDefaultCust, table, "name", "id");
            }
            catch
            {
                // intentionally silent
            }
        }

        private void LoadUnits()
        {
            try
            {
                using var adapter = new SqlDataAdapter("select id,name from units order by id", conn);
                var table = new DataTable();
                adapter.Fill(table);

                BindComboBox(cmbUnits, table, "name", "id");
            }
            catch
            {
                // intentionally silent
            }
        }

        private void LoadTreasury()
        {
            try
            {
                using var adapter = new SqlDataAdapter(
                    $"select id,name from Stocks where IS_Deleted=0 and branch={MainClass.BranchNo} order by id", conn);

                var table = new DataTable();
                adapter.Fill(table);

                BindComboBox(cmbTreasury, table, "name", "id");
            }
            catch
            {
                // intentionally silent
            }
        }

        private void LoadAccounts()
        {
            try
            {
                using var adapter = new SqlDataAdapter("select Code,AName from Accounts_Index where Type=2", conn);
                var table = new DataTable();
                adapter.Fill(table);

                BindComboBox(cmbAccDiscount, table.Copy(), "AName", "Code");
                BindComboBox(cmbAccAdditions, table.Copy(), "AName", "Code");
                BindComboBox(cmbAccInsurance, table.Copy(), "AName", "Code");
                BindComboBox(cmbAccItems, table.Copy(), "AName", "Code");
                BindComboBox(cmbAccStore, table.Copy(), "AName", "Code");
                BindComboBox(cmbAccVAT, table.Copy(), "AName", "Code");
                BindComboBox(cmbAccReturnItems, table.Copy(), "AName", "Code");
            }
            catch
            {
                // intentionally silent
            }
        }

        private void loadData(int invId)
        {
            try
            {
                using var adapter = new SqlDataAdapter($"select * from SettingGeneral where Inv_Id={invId}", conn);
                var table = new DataTable();
                adapter.Fill(table);

                if (table.Rows.Count != 1)
                {
                    return;
                }

                DataRow row = table.Rows[0];

                chkValueHaveTax.IsChecked = row["PriceIncVAT"] != DBNull.Value && Convert.ToBoolean(row["PriceIncVAT"]);
                AutoCheck.IsChecked = row["AutoCheck"] != DBNull.Value && Convert.ToBoolean(row["AutoCheck"]);
                chkTouch.IsChecked = row["TouchCheck"] != DBNull.Value && Convert.ToBoolean(row["TouchCheck"]);
                cksaleByMinus.IsChecked = row["SaleByMinus"] != DBNull.Value && Convert.ToBoolean(row["SaleByMinus"]);

                if (row["unit"] != DBNull.Value)
                {
                    cmbUnits.SelectedValue = row["unit"];
                }

                txtDevM.Text = row["DeliveryVal"] == DBNull.Value ? "0" : row["DeliveryVal"].ToString();
                txtInsure.Text = row["InsureVal"] == DBNull.Value ? "0" : row["InsureVal"].ToString();
                txtVatPer.Text = row["MainVAT"] == DBNull.Value ? "0" : row["MainVAT"].ToString();
                txtAdditionalVAT.Text = row["AdditionalTax"] == DBNull.Value ? "0" : row["AdditionalTax"].ToString();

                if (row["Treasury"] != DBNull.Value)
                {
                    cmbTreasury.SelectedValue = row["Treasury"];
                }

                if (row["prefixe"] != DBNull.Value)
                {
                    txtPrefixe.Text = row["prefixe"].ToString();
                }

                if (row["OperInvSale"] != DBNull.Value)
                {
                    CheckOperSale.IsChecked = Convert.ToBoolean(row["OperInvSale"]);
                }

                if (row["AtiveCustMeasur"] != DBNull.Value)
                {
                    chkAtiveCustMeasur.IsChecked = Convert.ToBoolean(row["AtiveCustMeasur"]);
                }

                if (row["DigitsNo"] != DBNull.Value)
                {
                    txtDigitsNo.Text = row["DigitsNo"].ToString();
                }

                if (row["DefaultCust"] != DBNull.Value)
                {
                    CmbDefaultCust.SelectedValue = row["DefaultCust"];
                }

                if (row["CloudSerial"] != DBNull.Value)
                {
                    txtCloudSerial.Text = row["CloudSerial"].ToString();
                }

                if (row["ReInvCloudSerial"] != DBNull.Value)
                {
                    txtReInvCloudSerial.Text = row["ReInvCloudSerial"].ToString();
                }

                if (row["VATCode"] != DBNull.Value && !string.IsNullOrWhiteSpace(row["VATCode"].ToString()))
                {
                    cmbAccVAT.SelectedValue = row["VATCode"];
                }

                if (row["ItemsAcc"] != DBNull.Value && !string.IsNullOrWhiteSpace(row["ItemsAcc"].ToString()))
                {
                    cmbAccItems.SelectedValue = row["ItemsAcc"];
                }

                if (row["ReturnItemsAcc"] != DBNull.Value && !string.IsNullOrWhiteSpace(row["ReturnItemsAcc"].ToString()))
                {
                    cmbAccReturnItems.SelectedValue = row["ReturnItemsAcc"];
                }

                if (row["DiscountAcc"] != DBNull.Value && !string.IsNullOrWhiteSpace(row["DiscountAcc"].ToString()))
                {
                    cmbAccDiscount.SelectedValue = row["DiscountAcc"];
                }

                if (row["InsureAcc"] != DBNull.Value && !string.IsNullOrWhiteSpace(row["InsureAcc"].ToString()))
                {
                    cmbAccInsurance.SelectedValue = row["InsureAcc"];
                }

                if (row["DeliveryAcc"] != DBNull.Value && !string.IsNullOrWhiteSpace(row["DeliveryAcc"].ToString()))
                {
                    cmbAccAdditions.SelectedValue = row["DeliveryAcc"];
                }

                if (row["StoreAcc"] != DBNull.Value && !string.IsNullOrWhiteSpace(row["StoreAcc"].ToString()))
                {
                    cmbAccStore.SelectedValue = row["StoreAcc"];
                }

                txtInvCode.Text = row["InvoiceCode"] == DBNull.Value ? string.Empty : row["InvoiceCode"].ToString();

                chkSalesmanRequired.IsChecked = row["SaleManIsRequire"] != DBNull.Value && Convert.ToBoolean(row["SaleManIsRequire"]);

                if (row["PayTypeDefault"] != DBNull.Value)
                {
                    string payTypeDefault = row["PayTypeDefault"].ToString();

                    if (payTypeDefault == "-1")
                    {
                        cmbPayTypeDefault.SelectedIndex = 0;
                    }
                    else if (payTypeDefault == "1")
                    {
                        cmbPayTypeDefault.SelectedIndex = 1;
                    }
                    else if (payTypeDefault == "2")
                    {
                        cmbPayTypeDefault.SelectedIndex = 2;
                    }
                }

                CkHasEntry.IsChecked = row["HasEntry"] != DBNull.Value && Convert.ToBoolean(row["HasEntry"]);
                ckShowPayForm.IsChecked = row["ShowPayForm"] != DBNull.Value && Convert.ToBoolean(row["ShowPayForm"]);

                if (row["Pricing"] != DBNull.Value)
                {
                    cmbPricing.SelectedIndex = Convert.ToInt32(row["Pricing"]);
                }

                if (row["Currency"] != DBNull.Value)
                {
                    cmbCurrency.SelectedIndex = Convert.ToInt32(row["Currency"]);
                }

                ckSyncInv.IsChecked = row["SyncInv"] != DBNull.Value && Convert.ToBoolean(row["SyncInv"]);
                ckSyncEntry.IsChecked = row["SyncEntry"] != DBNull.Value && Convert.ToBoolean(row["SyncEntry"]);
                ckCashCustRequire.IsChecked = row["CashCustRequire"] != DBNull.Value && Convert.ToBoolean(row["CashCustRequire"]);
                ckProcessFlow.IsChecked = row["ProcessFlow"] != DBNull.Value && Convert.ToBoolean(row["ProcessFlow"]);
                ckPaymentStatus.IsChecked = row["PaymentStatus"] != DBNull.Value && Convert.ToBoolean(row["PaymentStatus"]);
                chkDetailedItemEntry.IsChecked = row["DetailedItemEntry"] != DBNull.Value && Convert.ToBoolean(row["DetailedItemEntry"]);

                if (row["CostType"] != DBNull.Value)
                {
                    int costIndex = SafeInt(row["CostType"].ToString(), 0);
                    if (costIndex >= 0 && costIndex < cmbCosts.Items.Count)
                    {
                        cmbCosts.SelectedIndex = costIndex;
                    }
                }

                using var invAdapter = new SqlDataAdapter(
                    $"select id from Inv where branch={MainClass.BranchNo} and IS_deleted=0 and inv_type={invId}", conn1);

                var invTable = new DataTable();
                invAdapter.Fill(invTable);

                bool hasInvoices = invTable.Rows.Count > 0;

                bool controlsEditable = !(hasInvoices && MainClass.EmpNo != 0);

                chkValueHaveTax.IsEnabled = controlsEditable || MainClass.EmpNo == 0;
                txtVatPer.IsReadOnly = !controlsEditable && MainClass.EmpNo != 0;
                txtAdditionalVAT.IsReadOnly = !controlsEditable && MainClass.EmpNo != 0;
                cmbAccAdditions.IsEnabled = controlsEditable || MainClass.EmpNo == 0;
                cmbAccDiscount.IsEnabled = controlsEditable || MainClass.EmpNo == 0;
                cmbAccInsurance.IsEnabled = controlsEditable || MainClass.EmpNo == 0;
                cmbAccItems.IsEnabled = controlsEditable || MainClass.EmpNo == 0;
                cmbAccReturnItems.IsEnabled = controlsEditable || MainClass.EmpNo == 0;
                cmbAccStore.IsEnabled = controlsEditable || MainClass.EmpNo == 0;
                cmbAccVAT.IsEnabled = controlsEditable || MainClass.EmpNo == 0;
                cmbCosts.IsEnabled = controlsEditable || MainClass.EmpNo == 0;
            }
            catch
            {
                // intentionally silent
            }
        }

        private void loadPOSetting(int invId)
        {
            try
            {
                // SettingDisplayCotrl
                using (var adapter = new SqlDataAdapter($"select * from SettingDisplayCotrl where Inv_id={invId}", conn))
                {
                    var table = new DataTable();
                    adapter.Fill(table);

                    if (table.Rows.Count == 1)
                    {
                        DataRow row = table.Rows[0];
                        ckShowDiscountVal.IsChecked = row["discountVal"] != DBNull.Value && Convert.ToBoolean(row["discountVal"]);
                        ckShowDiscountPer.IsChecked = row["discountPer"] != DBNull.Value && Convert.ToBoolean(row["discountPer"]);
                        ckEditInsure.IsChecked = row["editinsurance"] != DBNull.Value && Convert.ToBoolean(row["editinsurance"]);
                        ckShowDelivery.IsChecked = row["delivery"] != DBNull.Value && Convert.ToBoolean(row["delivery"]);
                        ckEditDelivery.IsChecked = row["Editdelivery"] != DBNull.Value && Convert.ToBoolean(row["Editdelivery"]);
                        ckShowInsure.IsChecked = row["insurance"] != DBNull.Value && Convert.ToBoolean(row["insurance"]);
                    }
                }

                // SettingOrderMethods
                using (var adapter = new SqlDataAdapter(
                    $@"select isnull(local,1) as local,
                              isnull(takeaway,1) as takeaway,
                              isnull(family,0) as family,
                              isnull(car,0) as car,
                              isnull([table],0) as tt,
                              isnull(DefaultOrderType,'local') as DefaultOrderType,
                              isnull(SystemTables,0) as SystemTables,
                              isnull(hosting,0) as hosting
                       from SettingOrderMethods where Inv_id={invId}", conn))
                {
                    var table = new DataTable();
                    adapter.Fill(table);

                    if (table.Rows.Count == 1)
                    {
                        DataRow row = table.Rows[0];
                        ckShowlocal.IsChecked = Convert.ToBoolean(row["local"]);
                        ckShowtakeaway.IsChecked = Convert.ToBoolean(row["takeaway"]);
                        cbfamilyOrder.IsChecked = Convert.ToBoolean(row["family"]);
                        ckCarOrder.IsChecked = Convert.ToBoolean(row["car"]);
                        ckTableOrder.IsChecked = Convert.ToBoolean(row["tt"]);
                        ChkTableSystem.IsChecked = Convert.ToBoolean(row["SystemTables"]);
                        chShowHosting.IsChecked = Convert.ToBoolean(row["hosting"]);

                        string defaultOrderType = row["DefaultOrderType"].ToString();

                        switch (defaultOrderType)
                        {
                            case "local":
                                cmbDefaultOrderType.SelectedIndex = 0;
                                break;
                            case "takeaway":
                                cmbDefaultOrderType.SelectedIndex = 1;
                                break;
                            case "family":
                                cmbDefaultOrderType.SelectedIndex = 2;
                                break;
                            case "table":
                                cmbDefaultOrderType.SelectedIndex = 3;
                                break;
                            case "car":
                                cmbDefaultOrderType.SelectedIndex = 4;
                                break;
                            case "hosting":
                                cmbDefaultOrderType.SelectedIndex = 5;
                                break;
                            default:
                                cmbDefaultOrderType.SelectedIndex = 0;
                                break;
                        }
                    }
                }

                // SettingPayMethods
                using (var adapter = new SqlDataAdapter($"select * from SettingPayMethods where Inv_id={invId}", conn))
                {
                    var table = new DataTable();
                    adapter.Fill(table);

                    if (table.Rows.Count == 1)
                    {
                        DataRow row = table.Rows[0];
                        ckCashType.IsChecked = row["PayCash"] != DBNull.Value && Convert.ToBoolean(row["PayCash"]);
                        ckPostpon.IsChecked = row["PayPostpone"] != DBNull.Value && Convert.ToBoolean(row["PayPostpone"]);
                        ckShowCashMeth.IsChecked = row["cash"] != DBNull.Value && Convert.ToBoolean(row["cash"]);
                        ckShowNetwork.IsChecked = row["network"] != DBNull.Value && Convert.ToBoolean(row["network"]);
                        ckShowVisa.IsChecked = row["visa"] != DBNull.Value && Convert.ToBoolean(row["visa"]);
                        ckShowMulti.IsChecked = row["multi"] != DBNull.Value && Convert.ToBoolean(row["multi"]);
                        ckATM.IsChecked = row["ATM"] != DBNull.Value && Convert.ToBoolean(row["ATM"]);
                        ckShowBankPaytype.IsChecked = row["Bank"] != DBNull.Value && Convert.ToBoolean(row["Bank"]);
                        ckShowCreditMulti.IsChecked = row["CreditMulti"] != DBNull.Value && Convert.ToBoolean(row["CreditMulti"]);
                    }
                }

                // SettingsPOSDisplay
                using (var adapter = new SqlDataAdapter($"select * from SettingsPOSDisplay where Inv_id={invId}", conn))
                {
                    var table = new DataTable();
                    adapter.Fill(table);

                    if (table.Rows.Count == 1)
                    {
                        DataRow row = table.Rows[0];

                        ckShowGrps.IsChecked = row["ShowGrps"] != DBNull.Value && Convert.ToBoolean(row["ShowGrps"]);
                        ckShowItemsPage.IsChecked = row["ShowItemsPage"] != DBNull.Value && Convert.ToBoolean(row["ShowItemsPage"]);

                        GrpsColmNo.Text = row["GrpsColmNo"] == DBNull.Value ? "1" : row["GrpsColmNo"].ToString();
                        GrpRowsNo.Text = row["GrpRowsNo"] == DBNull.Value ? "4" : row["GrpRowsNo"].ToString();
                        ItemsColNo.Text = row["ItemsColNo"] == DBNull.Value ? "4" : row["ItemsColNo"].ToString();
                        ItemsRowsNo.Text = row["ItemsRowsNo"] == DBNull.Value ? "4" : row["ItemsRowsNo"].ToString();

                        ckSaleMan.IsChecked = row["AddSaleman"] != DBNull.Value && Convert.ToBoolean(row["AddSaleman"]);
                        ckShowItemsImages.IsChecked = row["ItemImages"] != DBNull.Value && Convert.ToBoolean(row["ItemImages"]);
                        ckShowGroupsImages.IsChecked = row["CatogryImages"] != DBNull.Value && Convert.ToBoolean(row["CatogryImages"]);
                        chkPoleDisplay.IsChecked = row["DisplayPrice"] != DBNull.Value && Convert.ToBoolean(row["DisplayPrice"]);
                        txtDisplayPort.Text = row["COMPort"] == DBNull.Value ? string.Empty : row["COMPort"].ToString();
                        chkDigitalDisplay.IsChecked = row["IsDigitalDisplay"] != DBNull.Value && Convert.ToBoolean(row["IsDigitalDisplay"]);
                        chkRepeatItem.IsChecked = row["RepeatItem"] != DBNull.Value && Convert.ToBoolean(row["RepeatItem"]);

                        UpdateItemsInPage();
                    }
                }

                // SettingPayForm
                using (var adapter = new SqlDataAdapter("select * from SettingPayForm where Inv_id=3", conn1))
                {
                    var table = new DataTable();
                    adapter.Fill(table);

                    if (table.Rows.Count > 0)
                    {
                        DataRow row = table.Rows[0];
                        bool showCoinImg = row["ShowCoinImg"] != DBNull.Value && Convert.ToBoolean(row["ShowCoinImg"]);
                        bool showCoinValue = row["ShowCoinValue"] != DBNull.Value && Convert.ToBoolean(row["ShowCoinValue"]);

                        if (!showCoinImg && !showCoinValue)
                        {
                            rbCoinHiden.IsChecked = true;
                        }

                        rbCoinImages.IsChecked = showCoinImg;
                        rbCoinNum.IsChecked = showCoinValue;
                    }
                }
            }
            catch
            {
                // intentionally silent
            }
        }

        private void LoadprintSetting(int invId)
        {
            try
            {
                using var adapter = new SqlDataAdapter($"select * from SettingPrint where Inv_Id={invId}", conn);
                var table = new DataTable();
                adapter.Fill(table);

                if (table.Rows.Count != 1)
                {
                    return;
                }

                DataRow row = table.Rows[0];
                int printType = row["printType"] == DBNull.Value ? 1 : Convert.ToInt32(row["printType"]);

                rbCashier.IsChecked = printType == 1;
                rbA4.IsChecked = printType == 2;

                chkInvFooter.IsChecked = row["PrintFooter"] != DBNull.Value && Convert.ToBoolean(row["PrintFooter"]);
                chkInvHeader.IsChecked = row["PrintHeader"] != DBNull.Value && Convert.ToBoolean(row["PrintHeader"]);
                ckPrintStamp.IsChecked = row["PrintStamp"] != DBNull.Value && Convert.ToBoolean(row["PrintStamp"]);

                cmbKitchenprinter.Text = row["kitchenprinter"] == DBNull.Value ? string.Empty : row["kitchenprinter"].ToString();
                cmbCashPrinter.Text = row["CasherPrinter"] == DBNull.Value ? string.Empty : row["CasherPrinter"].ToString();
                cmbPrintNo.Text = row["printNo"] == DBNull.Value ? "1" : row["printNo"].ToString();
                txtRptName.Text = row["RptName"] == DBNull.Value ? string.Empty : row["RptName"].ToString();
                txtRptPath.Text = row["RptUrl"] == DBNull.Value ? string.Empty : row["RptUrl"].ToString();
                txtNote.Text = row["note"] == DBNull.Value ? string.Empty : row["note"].ToString();

                if (row["PrintItemType"] != DBNull.Value)
                {
                    ckPrintItems.IsChecked = Convert.ToInt32(row["PrintItemType"]) == 3;
                }

                if (row["PrintComponentsItemsIndividually"] != DBNull.Value)
                {
                    chkPrintComponentsItemsIndividually.IsChecked = Convert.ToBoolean(row["PrintComponentsItemsIndividually"]);
                }

                if (row["printmakpay"] != DBNull.Value)
                {
                    printmakpay.IsChecked = Convert.ToBoolean(row["printmakpay"]);
                }

                if (printType == 2)
                {
                    SetImageSource(HeaderImg, row["HeaderImage"] == DBNull.Value ? null : (byte[])row["HeaderImage"]);
                    SetImageSource(FooterImg, row["FooterImage"] == DBNull.Value ? null : (byte[])row["FooterImage"]);
                    SetImageSource(StampImg, row["StampImage"] == DBNull.Value ? null : (byte[])row["StampImage"]);
                }
                else
                {
                    HeaderImg.Source = null;
                    FooterImg.Source = null;
                    StampImg.Source = null;
                }

                _printerRows.Clear();

                using var printerAdapter = new SqlDataAdapter($"select * from PrinterSettings where Inv_Id={invId}", conn);
                var printerTable = new DataTable();
                printerAdapter.Fill(printerTable);

                foreach (DataRow printerRow in printerTable.Rows)
                {
                    _printerRows.Add(new PrinterGridRow
                    {
                        DgvName = printerRow["PrintName"] == DBNull.Value ? string.Empty : printerRow["PrintName"].ToString(),
                        DgvPrinterName = printerRow["Printer"] == DBNull.Value ? string.Empty : printerRow["Printer"].ToString(),
                        DgvReport = printerRow["RptName"] == DBNull.Value ? string.Empty : printerRow["RptName"].ToString()
                    });
                }
            }
            catch
            {
                // intentionally silent
            }
        }

        private void LoadCloseShiftSetting()
        {
            try
            {
                using var adapter = new SqlDataAdapter("select * from SettingCloseShift", conn);
                var table = new DataTable();
                adapter.Fill(table);

                if (table.Rows.Count <= 0)
                {
                    return;
                }

                DataRow row = table.Rows[0];

                SaleInv.IsChecked = row["SaleInv"] != DBNull.Value && Convert.ToBoolean(row["SaleInv"]);
                PurchaseInv.IsChecked = row["PurchaseInv"] != DBNull.Value && Convert.ToBoolean(row["PurchaseInv"]);
                ckReceipts.IsChecked = row["Receipts"] != DBNull.Value && Convert.ToBoolean(row["Receipts"]);
                ckExpenses.IsChecked = row["Expenses"] != DBNull.Value && Convert.ToBoolean(row["Expenses"]);
                ckQuantPrint.IsChecked = row["PrintTotItem"] != DBNull.Value && Convert.ToBoolean(row["PrintTotItem"]);
                ckPrintGroups.IsChecked = row["PrintTotGroup"] != DBNull.Value && Convert.ToBoolean(row["PrintTotGroup"]);
                closesNo.Text = row["printNo"] == DBNull.Value ? "1" : row["printNo"].ToString();
                ckPOS.IsChecked = row["POS"] != DBNull.Value && Convert.ToBoolean(row["POS"]);
                ckSendEmail.IsChecked = row["SendEmail"] != DBNull.Value && Convert.ToBoolean(row["SendEmail"]);
                ckBalanceRequired.IsChecked = row["BalanceRequired"] != DBNull.Value && Convert.ToBoolean(row["BalanceRequired"]);
                chkShowCloseDetails.IsChecked = row["ShowCloseDetails"] != DBNull.Value && Convert.ToBoolean(row["ShowCloseDetails"]);
            }
            catch
            {
                // intentionally silent
            }
        }

        private void LoadEmailSetting()
        {
            try
            {
                using var adapter = new SqlDataAdapter($"Select * from SettingEmail where Branch_Id={MainClass.BranchNo}", conn);
                var table = new DataTable();
                adapter.Fill(table);

                if (table.Rows.Count <= 0)
                {
                    return;
                }

                DataRow row = table.Rows[0];

                if (row["ServerName"] != DBNull.Value)
                {
                    cmbServers.Text = row["ServerName"].ToString();
                }

                if (row["SendEmail"] != DBNull.Value)
                {
                    txtSenderEm.Text = row["SendEmail"].ToString();
                }

                if (row["RecEmail"] != DBNull.Value)
                {
                    txtReceiverEm.Text = row["RecEmail"].ToString();
                }

                if (row["SendPWd"] != DBNull.Value)
                {
                    txtSenderPwd.Text = row["SendPWd"].ToString();
                }

                if (row["port"] != DBNull.Value)
                {
                    txtPort.Text = row["port"].ToString();
                }

                if (row["bodyMsg"] != DBNull.Value)
                {
                    txtBodyMsg.Text = row["bodyMsg"].ToString();
                }

                if (row["SendInv"] != DBNull.Value)
                {
                    ChkSendInv.IsChecked = Convert.ToBoolean(row["SendInv"]);
                }

                ckactive.IsChecked = row["ActiveAuto"] != DBNull.Value && Convert.ToBoolean(row["ActiveAuto"]);
                ckSSL.IsChecked = row["SSL"] != DBNull.Value && Convert.ToBoolean(row["SSL"]);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void loadSyncSetting()
        {
            try
            {
                using (var adapter = new SqlDataAdapter("select BranchId as id,name from Branches where IS_Deleted=0", conn))
                {
                    var branchTable = new DataTable();
                    adapter.Fill(branchTable);

                    BindComboBox(cmbDistBranch, branchTable.Copy(), "name", "id");
                    BindComboBox(cmbSrcBranches, branchTable.Copy(), "name", "id");
                }

                using var syncAdapter = new SqlDataAdapter("select * from SettingSync", conn);
                var syncTable = new DataTable();
                syncAdapter.Fill(syncTable);

                if (syncTable.Rows.Count != 1)
                {
                    return;
                }

                DataRow row = syncTable.Rows[0];

                txtClientCode.Text = row["ClientCode"] == DBNull.Value ? string.Empty : row["ClientCode"].ToString();
                txtAPIUrl.Text = row["APIUrl"] == DBNull.Value ? string.Empty : row["APIUrl"].ToString();
                txtSyncUser.Text = row["Username"] == DBNull.Value ? string.Empty : row["Username"].ToString();
                txtPassword.Text = row["Password"] == DBNull.Value ? string.Empty : row["Password"].ToString();
                txtReadingInterval.Text = row["ReadingInterval"] == DBNull.Value ? string.Empty : row["ReadingInterval"].ToString();
                txtPostInterval.Text = row["POSTInterval"] == DBNull.Value ? string.Empty : row["POSTInterval"].ToString();
                txtInvCountToSync.Text = row["InvCountToSync"] == DBNull.Value ? string.Empty : row["InvCountToSync"].ToString();

                ChkSyncQuantity.IsChecked = row["SyncQuantity"] != DBNull.Value && Convert.ToBoolean(row["SyncQuantity"]);
                chkSyncCloudFirst.IsChecked = row["SyncCloudFirst"] != DBNull.Value && Convert.ToBoolean(row["SyncCloudFirst"]);
                ChkBranchSpecialSalePrice.IsChecked = row["BranchSpecialSalePrice"] != DBNull.Value && Convert.ToBoolean(row["BranchSpecialSalePrice"]);

                if (row["BranchType"] != DBNull.Value)
                {
                    int branchType = Convert.ToInt32(row["BranchType"]);

                    rbDataCenterBranch.IsChecked = branchType == 1;
                    rbSecondaryBranch.IsChecked = branchType == 2;
                    rbMiddleBranch.IsChecked = branchType == 3;
                    ckSyncCloud.IsChecked = branchType == 4;
                }

                if (row["DistBranch"] != DBNull.Value)
                {
                    cmbDistBranch.SelectedValue = row["DistBranch"];
                }

                if (row["BranchId"] != DBNull.Value)
                {
                    cmbSrcBranches.SelectedValue = row["BranchId"];
                }

                if (row["SyncType"] != DBNull.Value)
                {
                    int syncType = Convert.ToInt32(row["SyncType"]);
                    rbAutoSync.IsChecked = syncType == 1;
                    rbPeriodicSync.IsChecked = syncType == 2;
                    rbEndDay.IsChecked = syncType == 3;
                    rbEndShift.IsChecked = syncType == 4;
                    rbInactiveSync.IsChecked = syncType == 0;
                }

                if (row["StartDate"] != DBNull.Value)
                {
                    startSyncDate = Convert.ToDateTime(row["StartDate"]);
                }

                if (row["EndDate"] != DBNull.Value)
                {
                    endSyncDate = Convert.ToDateTime(row["EndDate"]);
                }
            }
            catch
            {
                // intentionally silent
            }
        }

        private void loadGediaSetting()
        {
            try
            {
                using SqlConnection masterConnection = CreateMasterConnection();
                EnsureConnectionOpen(masterConnection);

                using var adapter = new SqlDataAdapter("select * from GediaSetting where id=1", masterConnection);
                var table = new DataTable();
                adapter.Fill(table);

                if (table.Rows.Count <= 0)
                {
                    return;
                }

                DataRow row = table.Rows[0];

                CheckGedia.IsChecked = row["IsGediaActive"] != DBNull.Value && Convert.ToBoolean(row["IsGediaActive"]);
                txtGediaPort.Text = row["GediaPort"] == DBNull.Value ? "0" : row["GediaPort"].ToString();
                ChkGediaReceiptPrint.IsChecked = row["GediaEnableReceiptPrint"] != DBNull.Value && Convert.ToBoolean(row["GediaEnableReceiptPrint"]);
            }
            catch
            {
                // intentionally silent
            }
        }

        public void loadDBs()
        {
            try
            {
                _availableDatabaseRows.Clear();

                EnsureConnectionOpen(conn);

                using var adapter = new SqlDataAdapter(
                    "SELECT Dbname,DbAutoName FROM master.dbo.DatabasesManagment where IsDeleted=0 and IsActive=1 order by ImportanceOrder", conn);

                var table = new DataTable();
                adapter.Fill(table);

                foreach (DataRow row in table.Rows)
                {
                    _availableDatabaseRows.Add(new DatabaseManagementRow
                    {
                        DbName = row["Dbname"] == DBNull.Value ? string.Empty : row["Dbname"].ToString(),
                        DbAutoName = row["DbAutoName"] == DBNull.Value ? string.Empty : row["DbAutoName"].ToString()
                    });
                }
            }
            catch
            {
                // intentionally silent
            }
            finally
            {
                if (conn.State == ConnectionState.Open)
                {
                    conn.Close();
                }
            }
        }

        public void loadDB_by_DbAutoName()
        {
            try
            {
                _yearPreviewRows.Clear();

                EnsureConnectionOpen(conn);

                using var adapter = new SqlDataAdapter(
                    "SELECT Dbname,DbAutoName FROM Year_Previews where Is_Deleted=0 order by id", conn);

                var table = new DataTable();
                adapter.Fill(table);

                foreach (DataRow row in table.Rows)
                {
                    _yearPreviewRows.Add(new YearPreviewRow
                    {
                        Dbname = row["Dbname"] == DBNull.Value ? string.Empty : row["Dbname"].ToString()
                    });
                }
            }
            catch
            {
                // intentionally silent
            }
            finally
            {
                if (conn.State == ConnectionState.Open)
                {
                    conn.Close();
                }
            }
        }

        #endregion

        #region Save Methods

        private void btnSaveGen_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                EnsureConnectionOpen(conn);

                using SqlTransaction transaction = conn.BeginTransaction();

                if (!Confirm("هل انت متأكد من حفظ الإعدادات؟ ينصح بالتواصل مع الدعم الفني قبل إجراء أي تعديل.", "تأكيد"))
                {
                    transaction.Rollback();
                    return;
                }

                int invId = GetInvId();
                int unitId = cmbUnits.SelectedValue != null ? SafeInt(cmbUnits.SelectedValue.ToString(), -1) : -1;
                int treasuryId = cmbTreasury.SelectedValue != null ? SafeInt(cmbTreasury.SelectedValue.ToString(), -1) : -1;
                int defaultCustomerId = CmbDefaultCust.SelectedValue != null ? SafeInt(CmbDefaultCust.SelectedValue.ToString(), 0) : 0;

                string vatAccount = cmbAccVAT.SelectedValue?.ToString() ?? string.Empty;
                string itemsAccount = cmbAccItems.SelectedValue?.ToString() ?? string.Empty;
                string returnItemsAccount = cmbAccReturnItems.SelectedValue?.ToString() ?? string.Empty;
                string discountAccount = cmbAccDiscount.SelectedValue?.ToString() ?? string.Empty;
                string insuranceAccount = cmbAccInsurance.SelectedValue?.ToString() ?? string.Empty;
                string additionsAccount = cmbAccAdditions.SelectedValue?.ToString() ?? string.Empty;
                string storeAccount = cmbAccStore.SelectedValue?.ToString() ?? string.Empty;

                string payTypeDefault = string.Empty;
                if (cmbPayTypeDefault.SelectedIndex == 0) payTypeDefault = "-1";
                if (cmbPayTypeDefault.SelectedIndex == 1) payTypeDefault = "1";
                if (cmbPayTypeDefault.SelectedIndex == 2) payTypeDefault = "2";

                using (var deleteCommand = new SqlCommand("delete from SettingGeneral where Inv_id=@InvId", conn, transaction))
                {
                    deleteCommand.Parameters.AddWithValue("@InvId", invId);
                    deleteCommand.ExecuteNonQuery();
                }

                using var insertCommand = new SqlCommand(
                    @"insert into SettingGeneral
                      (
                        Inv_Id,AutoCheck,TouchCheck,unit,Treasury,DeliveryVal,InsureVal,PriceIncVAT,MainVAT,SaleByMinus,CostType,
                        Store,ShowPayForm,HasEntry,Pricing,Currency,VATCode,ItemsAcc,ReturnItemsAcc,DiscountAcc,InsureAcc,DeliveryAcc,StoreAcc,SyncInv,
                        SyncEntry,LinkedReturn,UnLinkedReturn,ShowPayfrmReturn,AdditionalTax,DigitsNo,ProcessFlow,PaymentStatus,InvoiceCode,SaleManIsRequire,
                        DefaultCust,CloudSerial,ReInvCloudSerial,CashCustRequire,DetailedItemEntry,OperInvSale,PayTypeDefault,prefixe,AtiveCustMeasur
                      )
                      values
                      (
                        @Inv_Id,@AutoCheck,@TouchCheck,@unit,@Treasury,@DeliveryVal,@InsureVal,@PriceIncVAT,@MainVAT,@SaleByMinus,@CostType,
                        @Store,@ShowPayForm,@HasEntry,@Pricing,@Currency,@VATCode,@ItemsAcc,@ReturnItemsAcc,@DiscountAcc,@InsureAcc,@DeliveryAcc,@StoreAcc,@SyncInv,
                        @SyncEntry,@LinkedReturn,@UnLinkedReturn,@ShowPayfrmReturn,@AdditionalTax,@DigitsNo,@ProcessFlow,@PaymentStatus,@InvoiceCode,@SaleManIsRequire,
                        @DefaultCust,@CloudSerial,@ReInvCloudSerial,@CashCustRequire,@DetailedItemEntry,@OperInvSale,@PayTypeDefault,@prefixe,@AtiveCustMeasur
                      )", conn, transaction);

                insertCommand.Parameters.AddWithValue("@Inv_Id", invId);
                insertCommand.Parameters.AddWithValue("@AutoCheck", IsChecked(AutoCheck));
                insertCommand.Parameters.AddWithValue("@TouchCheck", IsChecked(chkTouch));
                insertCommand.Parameters.AddWithValue("@unit", unitId);
                insertCommand.Parameters.AddWithValue("@Treasury", treasuryId);
                insertCommand.Parameters.AddWithValue("@DeliveryVal", SafeDouble(txtDevM.Text, 0));
                insertCommand.Parameters.AddWithValue("@InsureVal", SafeDouble(txtInsure.Text, 0));
                insertCommand.Parameters.AddWithValue("@PriceIncVAT", IsChecked(chkValueHaveTax));
                insertCommand.Parameters.AddWithValue("@MainVAT", SafeDouble(txtVatPer.Text, 0));
                insertCommand.Parameters.AddWithValue("@SaleByMinus", IsChecked(cksaleByMinus));
                insertCommand.Parameters.AddWithValue("@CostType", cmbCosts.SelectedIndex > 0 ? cmbCosts.SelectedIndex : 1);
                insertCommand.Parameters.AddWithValue("@Store", 1);
                insertCommand.Parameters.AddWithValue("@ShowPayForm", IsChecked(ckShowPayForm));
                insertCommand.Parameters.AddWithValue("@HasEntry", IsChecked(CkHasEntry));
                insertCommand.Parameters.AddWithValue("@Pricing", cmbPricing.SelectedIndex);
                insertCommand.Parameters.AddWithValue("@Currency", cmbCurrency.SelectedIndex);
                insertCommand.Parameters.AddWithValue("@VATCode", vatAccount);
                insertCommand.Parameters.AddWithValue("@ItemsAcc", itemsAccount);
                insertCommand.Parameters.AddWithValue("@ReturnItemsAcc", returnItemsAccount);
                insertCommand.Parameters.AddWithValue("@DiscountAcc", discountAccount);
                insertCommand.Parameters.AddWithValue("@InsureAcc", insuranceAccount);
                insertCommand.Parameters.AddWithValue("@DeliveryAcc", additionsAccount);
                insertCommand.Parameters.AddWithValue("@StoreAcc", storeAccount);
                insertCommand.Parameters.AddWithValue("@SyncInv", IsChecked(ckSyncInv));
                insertCommand.Parameters.AddWithValue("@SyncEntry", IsChecked(ckSyncEntry));
                insertCommand.Parameters.AddWithValue("@LinkedReturn", IsChecked(ckLinkedReturn));
                insertCommand.Parameters.AddWithValue("@UnLinkedReturn", IsChecked(ckUnlinkedReturn));
                insertCommand.Parameters.AddWithValue("@ShowPayfrmReturn", IsChecked(ckShowPayfrmReturn));
                insertCommand.Parameters.AddWithValue("@AdditionalTax", SafeDouble(txtAdditionalVAT.Text, 0));
                insertCommand.Parameters.AddWithValue("@DigitsNo", SafeInt(txtDigitsNo.Text, 2));
                insertCommand.Parameters.AddWithValue("@ProcessFlow", IsChecked(ckProcessFlow));
                insertCommand.Parameters.AddWithValue("@PaymentStatus", IsChecked(ckPaymentStatus));
                insertCommand.Parameters.AddWithValue("@InvoiceCode", txtInvCode.Text?.Trim() ?? string.Empty);
                insertCommand.Parameters.AddWithValue("@SaleManIsRequire", IsChecked(chkSalesmanRequired));
                insertCommand.Parameters.AddWithValue("@DefaultCust", defaultCustomerId);
                insertCommand.Parameters.AddWithValue("@CloudSerial", txtCloudSerial.Text?.Trim() ?? string.Empty);
                insertCommand.Parameters.AddWithValue("@ReInvCloudSerial", txtReInvCloudSerial.Text?.Trim() ?? string.Empty);
                insertCommand.Parameters.AddWithValue("@CashCustRequire", IsChecked(ckCashCustRequire));
                insertCommand.Parameters.AddWithValue("@DetailedItemEntry", IsChecked(chkDetailedItemEntry));
                insertCommand.Parameters.AddWithValue("@OperInvSale", IsChecked(CheckOperSale));
                insertCommand.Parameters.AddWithValue("@PayTypeDefault", payTypeDefault);
                insertCommand.Parameters.AddWithValue("@prefixe", SafeInt(txtPrefixe.Text, 1));
                insertCommand.Parameters.AddWithValue("@AtiveCustMeasur", IsChecked(chkAtiveCustMeasur));

                insertCommand.ExecuteNonQuery();

                transaction.Commit();

                MessageBox.Show("تم الحفظ", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);

                try
                {
                    Home homeWindow = System.Windows.Application.Current.Windows.OfType<Home>().FirstOrDefault();
                    if (homeWindow != null && homeWindow._is_active)
                    {
                        string publishTopic = $"{homeWindow.ns.cid}/{homeWindow.ns.frqid}/sticky/invsettings";
                        string publishPayload = Convert.ToString(homeWindow.ns.broadcastInvSettings()) ?? string.Empty;

                        homeWindow.ns.client.Publish(
                            publishTopic,
                            Encoding.UTF8.GetBytes(publishPayload),
                            0,
                            true);
                    }
                }
                catch
                {
                    // ignore broadcast issues
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ أثناء الحفظ{Environment.NewLine}تفاصيل الخطأ: {ex.Message}",
                    "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                if (conn.State == ConnectionState.Open)
                {
                    conn.Close();
                }
            }
        }

        private void btnSavePOSetting_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                EnsureConnectionOpen(conn);
                EnsureConnectionOpen(conn1);

                using SqlTransaction transaction = conn.BeginTransaction();

                if (!Confirm("تحذير!! سيتم تنفيذ الإعدادات الجديدة على كل الفواتير", "تحذير"))
                {
                    transaction.Rollback();
                    return;
                }

                // SettingDisplayCotrl
                using (var adapter = new SqlDataAdapter("select * from SettingDisplayCotrl where Inv_id=3", conn1))
                {
                    var table = new DataTable();
                    adapter.Fill(table);

                    if (table.Rows.Count == 0)
                    {
                        using var insert = new SqlCommand(
                            @"insert into SettingDisplayCotrl(Inv_id,discountVal,discountPer,insurance,editinsurance,delivery,Editdelivery)
                              values(3,@discountVal,@discountPer,@insurance,@editinsurance,@delivery,@Editdelivery)", conn, transaction);

                        insert.Parameters.AddWithValue("@discountVal", IsChecked(ckShowDiscountVal));
                        insert.Parameters.AddWithValue("@discountPer", IsChecked(ckShowDiscountPer));
                        insert.Parameters.AddWithValue("@insurance", IsChecked(ckShowInsure));
                        insert.Parameters.AddWithValue("@editinsurance", IsChecked(ckEditInsure));
                        insert.Parameters.AddWithValue("@delivery", IsChecked(ckShowDelivery));
                        insert.Parameters.AddWithValue("@Editdelivery", IsChecked(ckEditDelivery));
                        insert.ExecuteNonQuery();
                    }
                    else
                    {
                        using var update = new SqlCommand(
                            @"update SettingDisplayCotrl set
                                discountVal=@discountVal,
                                discountPer=@discountPer,
                                insurance=@insurance,
                                editinsurance=@editinsurance,
                                delivery=@delivery,
                                Editdelivery=@Editdelivery
                              where Inv_id=3", conn, transaction);

                        update.Parameters.AddWithValue("@discountVal", IsChecked(ckShowDiscountVal));
                        update.Parameters.AddWithValue("@discountPer", IsChecked(ckShowDiscountPer));
                        update.Parameters.AddWithValue("@insurance", IsChecked(ckShowInsure));
                        update.Parameters.AddWithValue("@editinsurance", IsChecked(ckEditInsure));
                        update.Parameters.AddWithValue("@delivery", IsChecked(ckShowDelivery));
                        update.Parameters.AddWithValue("@Editdelivery", IsChecked(ckEditDelivery));
                        update.ExecuteNonQuery();
                    }
                }

                // SettingPayMethods
                using (var adapter = new SqlDataAdapter("select * from SettingPayMethods where Inv_id=3", conn1))
                {
                    var table = new DataTable();
                    adapter.Fill(table);

                    if (table.Rows.Count == 0)
                    {
                        using var insert = new SqlCommand(
                            @"insert into SettingPayMethods
                              (Inv_id,PayCash,PayPostpone,cash,network,visa,multi,ATM,Bank,CreditMulti)
                              values
                              (3,@PayCash,@PayPostpone,@cash,@network,@visa,@multi,@ATM,@Bank,@CreditMulti)", conn, transaction);

                        insert.Parameters.AddWithValue("@PayCash", IsChecked(ckCashType));
                        insert.Parameters.AddWithValue("@PayPostpone", IsChecked(ckPostpon));
                        insert.Parameters.AddWithValue("@cash", IsChecked(ckShowCashMeth));
                        insert.Parameters.AddWithValue("@network", IsChecked(ckShowNetwork));
                        insert.Parameters.AddWithValue("@visa", IsChecked(ckShowVisa));
                        insert.Parameters.AddWithValue("@multi", IsChecked(ckShowMulti));
                        insert.Parameters.AddWithValue("@ATM", IsChecked(ckATM));
                        insert.Parameters.AddWithValue("@Bank", IsChecked(ckShowBankPaytype));
                        insert.Parameters.AddWithValue("@CreditMulti", IsChecked(ckShowCreditMulti));
                        insert.ExecuteNonQuery();
                    }
                    else
                    {
                        using var update = new SqlCommand(
                            @"update SettingPayMethods set
                                PayCash=@PayCash,
                                PayPostpone=@PayPostpone,
                                cash=@cash,
                                network=@network,
                                visa=@visa,
                                multi=@multi,
                                ATM=@ATM,
                                Bank=@Bank,
                                CreditMulti=@CreditMulti
                              where Inv_id=3", conn, transaction);

                        update.Parameters.AddWithValue("@PayCash", IsChecked(ckCashType));
                        update.Parameters.AddWithValue("@PayPostpone", IsChecked(ckPostpon));
                        update.Parameters.AddWithValue("@cash", IsChecked(ckShowCashMeth));
                        update.Parameters.AddWithValue("@network", IsChecked(ckShowNetwork));
                        update.Parameters.AddWithValue("@visa", IsChecked(ckShowVisa));
                        update.Parameters.AddWithValue("@multi", IsChecked(ckShowMulti));
                        update.Parameters.AddWithValue("@ATM", IsChecked(ckATM));
                        update.Parameters.AddWithValue("@Bank", IsChecked(ckShowBankPaytype));
                        update.Parameters.AddWithValue("@CreditMulti", IsChecked(ckShowCreditMulti));
                        update.ExecuteNonQuery();
                    }
                }

                // SettingOrderMethods
                string defaultOrderType = cmbDefaultOrderType.SelectedIndex switch
                {
                    0 => "local",
                    1 => "takeaway",
                    2 => "family",
                    3 => "table",
                    4 => "car",
                    5 => "hosting",
                    _ => "local"
                };

                using (var adapter = new SqlDataAdapter("select * from SettingOrderMethods where Inv_id=3", conn1))
                {
                    var table = new DataTable();
                    adapter.Fill(table);

                    string sql = table.Rows.Count == 0
                        ? @"INSERT INTO SettingOrderMethods
                            (Inv_id, local, takeaway, family, car, [table], hosting, DefaultOrderType, SystemTables)
                            VALUES
                            (@Inv_id, @local, @takeaway, @family, @car, @table, @hosting, @DefaultOrderType, @SystemTables)"
                        : @"UPDATE SettingOrderMethods SET
                            local=@local,
                            takeaway=@takeaway,
                            family=@family,
                            car=@car,
                            [table]=@table,
                            hosting=@hosting,
                            DefaultOrderType=@DefaultOrderType,
                            SystemTables=@SystemTables
                          WHERE Inv_id=@Inv_id";

                    using var command = new SqlCommand(sql, conn, transaction);
                    command.Parameters.AddWithValue("@Inv_id", 3);
                    command.Parameters.AddWithValue("@local", IsChecked(ckShowlocal));
                    command.Parameters.AddWithValue("@takeaway", IsChecked(ckShowtakeaway));
                    command.Parameters.AddWithValue("@family", IsChecked(cbfamilyOrder));
                    command.Parameters.AddWithValue("@car", IsChecked(ckCarOrder));
                    command.Parameters.AddWithValue("@table", IsChecked(ckTableOrder));
                    command.Parameters.AddWithValue("@hosting", IsChecked(chShowHosting));
                    command.Parameters.AddWithValue("@DefaultOrderType", defaultOrderType);
                    command.Parameters.AddWithValue("@SystemTables", IsChecked(ChkTableSystem));
                    command.ExecuteNonQuery();
                }

                // SettingsPOSDisplay
                using (var adapter = new SqlDataAdapter("select * from SettingsPOSDisplay where Inv_id=3", conn1))
                {
                    var table = new DataTable();
                    adapter.Fill(table);

                    if (table.Rows.Count == 0)
                    {
                        using var insert = new SqlCommand(
                            @"insert into SettingsPOSDisplay
                              (Inv_id,ShowGrps,ShowItemsPage,GrpsColmNo,GrpRowsNo,ItemsColNo,ItemsRowsNo,AddSaleman,ItemImages,CatogryImages,DisplayPrice,COMPort,RepeatItem,IsDigitalDisplay)
                              values
                              (3,@ShowGrps,@ShowItemsPage,@GrpsColmNo,@GrpRowsNo,@ItemsColNo,@ItemsRowsNo,@AddSaleman,@ItemImages,@CatogryImages,@DisplayPrice,@COMPort,@RepeatItem,@IsDigitalDisplay)", conn, transaction);

                        insert.Parameters.AddWithValue("@ShowGrps", IsChecked(ckShowGrps));
                        insert.Parameters.AddWithValue("@ShowItemsPage", IsChecked(ckShowItemsPage));
                        insert.Parameters.AddWithValue("@GrpsColmNo", SafeInt(GrpsColmNo.Text, 1));
                        insert.Parameters.AddWithValue("@GrpRowsNo", SafeInt(GrpRowsNo.Text, 4));
                        insert.Parameters.AddWithValue("@ItemsColNo", SafeInt(ItemsColNo.Text, 4));
                        insert.Parameters.AddWithValue("@ItemsRowsNo", SafeInt(ItemsRowsNo.Text, 4));
                        insert.Parameters.AddWithValue("@AddSaleman", IsChecked(ckSaleMan));
                        insert.Parameters.AddWithValue("@ItemImages", IsChecked(ckShowItemsImages));
                        insert.Parameters.AddWithValue("@CatogryImages", IsChecked(ckShowGroupsImages));
                        insert.Parameters.AddWithValue("@DisplayPrice", IsChecked(chkPoleDisplay));
                        insert.Parameters.AddWithValue("@COMPort", txtDisplayPort.Text?.Trim() ?? string.Empty);
                        insert.Parameters.AddWithValue("@RepeatItem", IsChecked(chkRepeatItem));
                        insert.Parameters.AddWithValue("@IsDigitalDisplay", IsChecked(chkDigitalDisplay));
                        insert.ExecuteNonQuery();
                    }
                    else
                    {
                        using var update = new SqlCommand(
                            @"update SettingsPOSDisplay set
                                ShowGrps=@ShowGrps,
                                ShowItemsPage=@ShowItemsPage,
                                GrpsColmNo=@GrpsColmNo,
                                GrpRowsNo=@GrpRowsNo,
                                ItemsColNo=@ItemsColNo,
                                ItemsRowsNo=@ItemsRowsNo,
                                AddSaleman=@AddSaleman,
                                ItemImages=@ItemImages,
                                CatogryImages=@CatogryImages,
                                DisplayPrice=@DisplayPrice,
                                COMPort=@COMPort,
                                RepeatItem=@RepeatItem,
                                IsDigitalDisplay=@IsDigitalDisplay
                              where Inv_id=3", conn, transaction);

                        update.Parameters.AddWithValue("@ShowGrps", IsChecked(ckShowGrps));
                        update.Parameters.AddWithValue("@ShowItemsPage", IsChecked(ckShowItemsPage));
                        update.Parameters.AddWithValue("@GrpsColmNo", SafeInt(GrpsColmNo.Text, 1));
                        update.Parameters.AddWithValue("@GrpRowsNo", SafeInt(GrpRowsNo.Text, 4));
                        update.Parameters.AddWithValue("@ItemsColNo", SafeInt(ItemsColNo.Text, 4));
                        update.Parameters.AddWithValue("@ItemsRowsNo", SafeInt(ItemsRowsNo.Text, 4));
                        update.Parameters.AddWithValue("@AddSaleman", IsChecked(ckSaleMan));
                        update.Parameters.AddWithValue("@ItemImages", IsChecked(ckShowItemsImages));
                        update.Parameters.AddWithValue("@CatogryImages", IsChecked(ckShowGroupsImages));
                        update.Parameters.AddWithValue("@DisplayPrice", IsChecked(chkPoleDisplay));
                        update.Parameters.AddWithValue("@COMPort", txtDisplayPort.Text?.Trim() ?? string.Empty);
                        update.Parameters.AddWithValue("@RepeatItem", IsChecked(chkRepeatItem));
                        update.Parameters.AddWithValue("@IsDigitalDisplay", IsChecked(chkDigitalDisplay));
                        update.ExecuteNonQuery();
                    }
                }

                // SettingPayForm
                using (var adapter = new SqlDataAdapter("select * from SettingPayForm where Inv_id=3", conn1))
                {
                    var table = new DataTable();
                    adapter.Fill(table);

                    bool showCoinPanel = !IsChecked(rbCoinHiden);

                    if (table.Rows.Count == 0)
                    {
                        using var insert = new SqlCommand(
                            @"insert into SettingPayForm(Inv_id,ShowCoinImg,ShowCoinValue,ShowCoinPnl)
                              values(3,@ShowCoinImg,@ShowCoinValue,@ShowCoinPnl)", conn, transaction);

                        insert.Parameters.AddWithValue("@ShowCoinImg", IsChecked(rbCoinImages));
                        insert.Parameters.AddWithValue("@ShowCoinValue", IsChecked(rbCoinNum));
                        insert.Parameters.AddWithValue("@ShowCoinPnl", showCoinPanel);
                        insert.ExecuteNonQuery();
                    }
                    else
                    {
                        using var update = new SqlCommand(
                            @"update SettingPayForm set
                                ShowCoinImg=@ShowCoinImg,
                                ShowCoinValue=@ShowCoinValue,
                                ShowCoinPnl=@ShowCoinPnl
                              where Inv_id=3", conn, transaction);

                        update.Parameters.AddWithValue("@ShowCoinImg", IsChecked(rbCoinImages));
                        update.Parameters.AddWithValue("@ShowCoinValue", IsChecked(rbCoinNum));
                        update.Parameters.AddWithValue("@ShowCoinPnl", showCoinPanel);
                        update.ExecuteNonQuery();
                    }
                }

                transaction.Commit();
                MessageBox.Show("تم الحفظ", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ أثناء الحفظ{Environment.NewLine}تفاصيل الخطأ: {ex.Message}",
                    "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                if (conn.State == ConnectionState.Open)
                {
                    conn.Close();
                }

                if (conn1.State == ConnectionState.Open)
                {
                    conn1.Close();
                }
            }
        }

        private void btnOperPermiss_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (MainClass.EmpNo > 1)
                {
                    MessageBox.Show("نأسف ليس لديك الصلاحية لتغيير إعدادات الصلاحيات", "تنبيه",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!Confirm($"تحذير!! سيتم تنفيذ الإعدادات الجديدة لصلاحيات المستخدم {txtUser.Text}", "تحذير"))
                {
                    return;
                }

                if (string.IsNullOrWhiteSpace(txtPass.Text))
                {
                    MessageBox.Show("ادخل كلمة المرور", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Information);
                    txtPass.Focus();
                    return;
                }

                EnsureConnectionOpen(conn);

                using SqlTransaction transaction = conn.BeginTransaction();

                SavePermission(transaction, 1, ckshiftClose);
                SavePermission(transaction, 2, ckReturn);
                SavePermission(transaction, 3, ckHosting);
                SavePermission(transaction, 4, ckDiscount);
                SavePermission(transaction, 5, ckAbsent);
                SavePermission(transaction, 6, ckExpention1);
                SavePermission(transaction, 7, ckRestOperPeriod);

                transaction.Commit();

                MessageBox.Show("تم الحفظ", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
                txtPass.Text = string.Empty;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ أثناء الحفظ{Environment.NewLine}تفاصيل الخطأ: {ex.Message}",
                    "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                if (conn.State == ConnectionState.Open)
                {
                    conn.Close();
                }
            }
        }

        private void SavePermission(SqlTransaction transaction, int operationNumber, CheckBox operationCheckBox)
        {
            if (!IsChecked(operationCheckBox))
            {
                return;
            }

            using (var deleteCommand = new SqlCommand("delete from OperationPermission where OperNo=@OperNo", conn, transaction))
            {
                deleteCommand.Parameters.AddWithValue("@OperNo", operationNumber);
                deleteCommand.ExecuteNonQuery();
            }

            using var insertCommand = new SqlCommand(
                @"insert into OperationPermission(emp,OperNo,pwd,lastChanged,IS_Deleted,OperVal,Activated)
                  values(@emp,@OperNo,@pwd,@lastChanged,@IS_Deleted,@OperVal,@Activated)", conn, transaction);

            insertCommand.Parameters.AddWithValue("@emp", MainClass.UserID);
            insertCommand.Parameters.AddWithValue("@OperNo", operationNumber);
            insertCommand.Parameters.AddWithValue("@pwd", txtPass.Text?.Trim() ?? string.Empty);
            insertCommand.Parameters.AddWithValue("@lastChanged", txtDate.SelectedDate ?? DateTime.Now);
            insertCommand.Parameters.AddWithValue("@IS_Deleted", false);
            insertCommand.Parameters.AddWithValue("@OperVal", 0);
            insertCommand.Parameters.AddWithValue("@Activated", true);
            insertCommand.ExecuteNonQuery();
        }

        private void btnprintsettings_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                EnsureConnectionOpen(conn);

                using SqlTransaction transaction = conn.BeginTransaction();

                int selectedInvId = 1;
                if (IsChecked(ckSaleInv)) selectedInvId = 2;
                if (IsChecked(ckPOSInv)) selectedInvId = 3;
                if (IsChecked(ckRentInv)) selectedInvId = 4;
                if (IsChecked(ckContracts)) selectedInvId = 5;
                if (IsChecked(ckReports)) selectedInvId = 6;

                int printType = IsChecked(rbCashier) ? 1 : 2;
                int printNo = SafeInt(cmbPrintNo.Text, 1);
                int printItemType = IsChecked(ckPrintItems) ? 3 : 1;

                string kitchenPrinterName = cmbKitchenprinter.SelectedItem?.ToString() ?? cmbKitchenprinter.Text ?? string.Empty;
                string cashierPrinterName = cmbCashPrinter.SelectedItem?.ToString() ?? cmbCashPrinter.Text ?? string.Empty;
                string reportName = txtRptName.Text?.Trim() ?? string.Empty;
                string reportPath = txtRptPath.Text?.Trim() ?? string.Empty;
                string note = txtNote.Text ?? string.Empty;

                byte[] headerImageBytes = printType == 2 ? BitmapImageToBytes(HeaderImg.Source) : null;
                byte[] footerImageBytes = printType == 2 ? BitmapImageToBytes(FooterImg.Source) : null;
                byte[] stampImageBytes = printType == 2 ? BitmapImageToBytes(StampImg.Source) : null;

                int invIdToSave = IsChecked(ckAll) ? 0 : selectedInvId;

                if (IsChecked(ckAll) &&
                    !Confirm("تحذير!! سيتم تنفيذ إعدادات الطباعة على كل الفواتير", "تحذير"))
                {
                    transaction.Rollback();
                    return;
                }

                using (var deleteCommand = new SqlCommand("delete from SettingPrint where Inv_id=@InvId", conn, transaction))
                {
                    deleteCommand.Parameters.AddWithValue("@InvId", invIdToSave);
                    deleteCommand.ExecuteNonQuery();
                }

                using (var insertCommand = new SqlCommand(
                    @"insert into SettingPrint
                      (Inv_Id,PrintTotItem,PrintHeader,PrintFooter,printType,CasherPrinter,kitchenprinter,RptName,RptURl,printNo,note,PrintTotGroup,PrintStamp,HeaderImage,FooterImage,StampImage,PrintItemType,PrintComponentsItemsIndividually,printmakpay)
                      values
                      (@Inv_Id,@PrintTotItem,@PrintHeader,@PrintFooter,@printType,@CasherPrinter,@kitchenprinter,@RptName,@RptURl,@printNo,@note,@PrintTotGroup,@PrintStamp,@HeaderImage,@FooterImage,@StampImage,@PrintItemType,@PrintComponentsItemsIndividually,@printmakpay)", conn, transaction))
                {
                    insertCommand.Parameters.AddWithValue("@Inv_Id", invIdToSave);
                    insertCommand.Parameters.AddWithValue("@PrintTotItem", IsChecked(ckQuantPrint));
                    insertCommand.Parameters.AddWithValue("@PrintHeader", IsChecked(chkInvHeader));
                    insertCommand.Parameters.AddWithValue("@PrintFooter", IsChecked(chkInvFooter));
                    insertCommand.Parameters.AddWithValue("@printType", printType);
                    insertCommand.Parameters.AddWithValue("@CasherPrinter", cashierPrinterName);
                    insertCommand.Parameters.AddWithValue("@kitchenprinter", kitchenPrinterName);
                    insertCommand.Parameters.AddWithValue("@RptName", reportName);
                    insertCommand.Parameters.AddWithValue("@RptURl", reportPath);
                    insertCommand.Parameters.AddWithValue("@printNo", printNo);
                    insertCommand.Parameters.AddWithValue("@note", note);
                    insertCommand.Parameters.AddWithValue("@PrintTotGroup", IsChecked(ckPrintGroups));
                    insertCommand.Parameters.AddWithValue("@PrintStamp", IsChecked(ckPrintStamp));
                    insertCommand.Parameters.AddWithValue("@HeaderImage", (object)headerImageBytes ?? DBNull.Value);
                    insertCommand.Parameters.AddWithValue("@FooterImage", (object)footerImageBytes ?? DBNull.Value);
                    insertCommand.Parameters.AddWithValue("@StampImage", (object)stampImageBytes ?? DBNull.Value);
                    insertCommand.Parameters.AddWithValue("@PrintItemType", printItemType);
                    insertCommand.Parameters.AddWithValue("@PrintComponentsItemsIndividually", IsChecked(chkPrintComponentsItemsIndividually));
                    insertCommand.Parameters.AddWithValue("@printmakpay", IsChecked(printmakpay));
                    insertCommand.ExecuteNonQuery();
                }

                if (!IsChecked(ckAll))
                {
                    using (var deletePrinterSettingsCommand = new SqlCommand("delete from PrinterSettings where Inv_id=@InvId", conn, transaction))
                    {
                        deletePrinterSettingsCommand.Parameters.AddWithValue("@InvId", selectedInvId);
                        deletePrinterSettingsCommand.ExecuteNonQuery();
                    }

                    foreach (var row in _printerRows.Where(x =>
                                 !string.IsNullOrWhiteSpace(x.DgvName) ||
                                 !string.IsNullOrWhiteSpace(x.DgvPrinterName) ||
                                 !string.IsNullOrWhiteSpace(x.DgvReport)))
                    {
                        using var insertPrinterSetting = new SqlCommand(
                            @"insert into PrinterSettings (Inv_Id,PrintName,Printer,RptUrl,RptName)
                              values(@Inv_Id,@PrintName,@Printer,@RptUrl,@RptName)", conn, transaction);

                        insertPrinterSetting.Parameters.AddWithValue("@Inv_Id", selectedInvId);
                        insertPrinterSetting.Parameters.AddWithValue("@PrintName", row.DgvName ?? string.Empty);
                        insertPrinterSetting.Parameters.AddWithValue("@Printer", row.DgvPrinterName ?? string.Empty);
                        insertPrinterSetting.Parameters.AddWithValue("@RptUrl", reportPath);
                        insertPrinterSetting.Parameters.AddWithValue("@RptName", row.DgvReport ?? string.Empty);
                        insertPrinterSetting.ExecuteNonQuery();
                    }
                }

                transaction.Commit();

                MainClass.ReportsPath = Path.GetDirectoryName(reportPath);
                MainClass.ReportsPrinter = cashierPrinterName;

                MessageBox.Show("تم الحفظ", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ أثناء الحفظ{Environment.NewLine}تفاصيل الخطأ: {ex.Message}",
                    "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                if (conn.State == ConnectionState.Open)
                {
                    conn.Close();
                }
            }
        }

        private void btnSaveEmail_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                EnsureConnectionOpen(conn);
                using SqlTransaction transaction = conn.BeginTransaction();

                string serverNameText = GetComboText(cmbServers);

                if (string.IsNullOrWhiteSpace(serverNameText))
                {
                    MessageBox.Show("يجب اختيار نوع الخادم", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                    transaction.Rollback();
                    return;
                }

                if (string.IsNullOrWhiteSpace(txtSenderEm.Text))
                {
                    MessageBox.Show("يجب إدخال إيميل المستخدم", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                    transaction.Rollback();
                    return;
                }

                if (string.IsNullOrWhiteSpace(txtReceiverEm.Text))
                {
                    MessageBox.Show("يجب إدخال إيميل المستلم", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                    transaction.Rollback();
                    return;
                }

                if (string.IsNullOrWhiteSpace(txtSenderPwd.Text))
                {
                    MessageBox.Show("يجب إدخال كلمة المرور", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                    transaction.Rollback();
                    return;
                }

                if (string.IsNullOrWhiteSpace(txtPort.Text))
                {
                    MessageBox.Show("يجب إدخال رقم المنفذ", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                    transaction.Rollback();
                    return;
                }

                if (!Confirm("تنبيه!! سيتم حفظ إعدادات البريد", "تأكيد"))
                {
                    transaction.Rollback();
                    return;
                }

                using (var deleteCommand = new SqlCommand("delete from SettingEmail where Branch_Id=@BranchId", conn, transaction))
                {
                    deleteCommand.Parameters.AddWithValue("@BranchId", MainClass.BranchNo);
                    deleteCommand.ExecuteNonQuery();
                }

                using var insertCommand = new SqlCommand(
                    @"insert into SettingEmail
                      (Branch_Id,ActiveAuto,ServerName,SendEmail,RecEmail,SendPWd,port,SSL,bodyMsg,SendInv)
                      values
                      (@Branch_Id,@ActiveAuto,@ServerName,@SendEmail,@RecEmail,@SendPWd,@port,@SSL,@bodyMsg,@SendInv)", conn, transaction);

                insertCommand.Parameters.AddWithValue("@Branch_Id", MainClass.BranchNo);
                insertCommand.Parameters.AddWithValue("@ActiveAuto", IsChecked(ckactive));
                insertCommand.Parameters.AddWithValue("@ServerName", serverNameText);
                insertCommand.Parameters.AddWithValue("@SendEmail", txtSenderEm.Text?.Trim() ?? string.Empty);
                insertCommand.Parameters.AddWithValue("@RecEmail", txtReceiverEm.Text?.Trim() ?? string.Empty);
                insertCommand.Parameters.AddWithValue("@SendPWd", txtSenderPwd.Text?.Trim() ?? string.Empty);
                insertCommand.Parameters.AddWithValue("@port", SafeInt(txtPort.Text, 587));
                insertCommand.Parameters.AddWithValue("@SSL", IsChecked(ckSSL));
                insertCommand.Parameters.AddWithValue("@bodyMsg", txtBodyMsg.Text ?? string.Empty);
                insertCommand.Parameters.AddWithValue("@SendInv", IsChecked(ChkSendInv));
                insertCommand.ExecuteNonQuery();

                transaction.Commit();

                MessageBox.Show("تم الحفظ", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ أثناء الحفظ{Environment.NewLine}تفاصيل الخطأ: {ex.Message}",
                    "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                if (conn.State == ConnectionState.Open)
                {
                    conn.Close();
                }
            }
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                EnsureConnectionOpen(conn);
                using SqlTransaction transaction = conn.BeginTransaction();

                if (MainClass.EmpNo > 0)
                {
                    MessageBox.Show("نأسف ليس لديك الصلاحية لتغيير الإعدادات", "تنبيه",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    transaction.Rollback();
                    return;
                }

                if (!Confirm("هل انت متأكد من حفظ الإعدادات؟ ينصح بالتواصل مع الدعم الفني قبل إجراء أي تعديل.", "تأكيد"))
                {
                    transaction.Rollback();
                    return;
                }

                int branchType = 1;
                if (IsChecked(rbSecondaryBranch)) branchType = 2;
                else if (IsChecked(rbMiddleBranch)) branchType = 3;
                else if (IsChecked(ckSyncCloud)) branchType = 4;

                int syncType = 0;
                if (IsChecked(rbAutoSync)) syncType = 1;
                else if (IsChecked(rbPeriodicSync)) syncType = 2;
                else if (IsChecked(rbEndDay)) syncType = 3;
                else if (IsChecked(rbEndShift)) syncType = 4;

                int distBranch = cmbDistBranch.SelectedValue != null ? SafeInt(cmbDistBranch.SelectedValue.ToString(), 0) : 0;
                int sourceBranch = cmbSrcBranches.SelectedValue != null ? SafeInt(cmbSrcBranches.SelectedValue.ToString(), MainClass.BranchNo) : MainClass.BranchNo;

                using (var deleteCommand = new SqlCommand("delete from SettingSync", conn, transaction))
                {
                    deleteCommand.ExecuteNonQuery();
                }

                using var insertCommand = new SqlCommand(
                    @"insert into SettingSync
                      (Id,ClientCode,APIUrl,UserName,Password,BranchId,BranchType,SyncType,EmpId,ReadingInterval,POSTInterval,DistBranch,SyncQuantity,InvCountToSync,InvoicesLastSync,SyncCloudFirst,BranchSpecialSalePrice,StartDate,EndDate)
                      values
                      (@Id,@ClientCode,@APIUrl,@UserName,@Password,@BranchId,@BranchType,@SyncType,@EmpId,@ReadingInterval,@POSTInterval,@DistBranch,@SyncQuantity,@InvCountToSync,@InvoicesLastSync,@SyncCloudFirst,@BranchSpecialSalePrice,@StartDate,@EndDate)", conn, transaction);

                insertCommand.Parameters.AddWithValue("@Id", 1);
                insertCommand.Parameters.AddWithValue("@ClientCode", txtClientCode.Text?.Trim() ?? string.Empty);
                insertCommand.Parameters.AddWithValue("@APIUrl", txtAPIUrl.Text?.Trim() ?? string.Empty);
                insertCommand.Parameters.AddWithValue("@UserName", txtSyncUser.Text?.Trim() ?? string.Empty);
                insertCommand.Parameters.AddWithValue("@Password", txtPassword.Text?.Trim() ?? string.Empty);
                insertCommand.Parameters.AddWithValue("@BranchId", sourceBranch);
                insertCommand.Parameters.AddWithValue("@BranchType", branchType);
                insertCommand.Parameters.AddWithValue("@SyncType", syncType);
                insertCommand.Parameters.AddWithValue("@EmpId", MainClass.EmpNo);
                insertCommand.Parameters.AddWithValue("@ReadingInterval", SafeInt(txtReadingInterval.Text, 0));
                insertCommand.Parameters.AddWithValue("@POSTInterval", SafeDouble(txtPostInterval.Text, 0));
                insertCommand.Parameters.AddWithValue("@DistBranch", distBranch);
                insertCommand.Parameters.AddWithValue("@SyncQuantity", IsChecked(ChkSyncQuantity));
                insertCommand.Parameters.AddWithValue("@InvCountToSync", SafeInt(txtInvCountToSync.Text, 0));
                insertCommand.Parameters.AddWithValue("@InvoicesLastSync", DateTime.Now);
                insertCommand.Parameters.AddWithValue("@SyncCloudFirst", IsChecked(chkSyncCloudFirst));
                insertCommand.Parameters.AddWithValue("@BranchSpecialSalePrice", IsChecked(ChkBranchSpecialSalePrice));
                insertCommand.Parameters.AddWithValue("@StartDate", startSyncDate);
                insertCommand.Parameters.AddWithValue("@EndDate", endSyncDate);
                insertCommand.ExecuteNonQuery();

                transaction.Commit();

                MessageBox.Show("تم الحفظ", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);

                try
                {
                    if (Common.GetLicenseClient() != null)
                    {
                        Home homeWindow = System.Windows.Application.Current.Windows.OfType<Home>().FirstOrDefault();
                        homeWindow?.CheckSyncAsync();
                    }
                    else
                    {
                        MessageBox.Show("لتفعيل المزامنة، يرجى تعبئة بيانات الترخيص", "تنبيه",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                    }

                    ManagerOnline.sendSyncData();
                }
                catch
                {
                    // ignore sync trigger issues
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ أثناء الحفظ{Environment.NewLine}تفاصيل الخطأ: {ex.Message}",
                    "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                if (conn.State == ConnectionState.Open)
                {
                    conn.Close();
                }
            }
        }

        private void btnCloseShiftSetting_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                EnsureConnectionOpen(conn);
                using SqlTransaction transaction = conn.BeginTransaction();

                if (!Confirm("تحذير!! سيتم تنفيذ إعدادات الإغلاق", "تحذير"))
                {
                    transaction.Rollback();
                    return;
                }

                using (var deleteCommand = new SqlCommand("delete from SettingCloseShift", conn, transaction))
                {
                    deleteCommand.ExecuteNonQuery();
                }

                using var insertCommand = new SqlCommand(
                    @"insert into SettingCloseShift(UserId,PrintTotItem,PrintTotGroup,SendEmail,POS,SaleInv,PurchaseInv,Expenses,Receipts,printNo,BalanceRequired,ShowCloseDetails)
                      values(@UserId,@PrintTotItem,@PrintTotGroup,@SendEmail,@POS,@SaleInv,@PurchaseInv,@Expenses,@Receipts,@printNo,@BalanceRequired,@ShowCloseDetails)", conn, transaction);

                insertCommand.Parameters.AddWithValue("@UserId", MainClass.EmpNo);
                insertCommand.Parameters.AddWithValue("@PrintTotItem", IsChecked(ckQuantPrint));
                insertCommand.Parameters.AddWithValue("@PrintTotGroup", IsChecked(ckPrintGroups));
                insertCommand.Parameters.AddWithValue("@SendEmail", IsChecked(ckSendEmail));
                insertCommand.Parameters.AddWithValue("@POS", IsChecked(ckPOS));
                insertCommand.Parameters.AddWithValue("@SaleInv", IsChecked(SaleInv));
                insertCommand.Parameters.AddWithValue("@PurchaseInv", IsChecked(PurchaseInv));
                insertCommand.Parameters.AddWithValue("@Expenses", IsChecked(ckExpenses));
                insertCommand.Parameters.AddWithValue("@Receipts", IsChecked(ckReceipts));
                insertCommand.Parameters.AddWithValue("@printNo", SafeInt(closesNo.Text, 1));
                insertCommand.Parameters.AddWithValue("@BalanceRequired", IsChecked(ckBalanceRequired));
                insertCommand.Parameters.AddWithValue("@ShowCloseDetails", IsChecked(chkShowCloseDetails));
                insertCommand.ExecuteNonQuery();

                transaction.Commit();

                MessageBox.Show("تم الحفظ", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ أثناء الحفظ{Environment.NewLine}تفاصيل الخطأ: {ex.Message}",
                    "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                if (conn.State == ConnectionState.Open)
                {
                    conn.Close();
                }
            }
        }

        private void BtnSaveGedia_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (MainClass.EmpNo > 0)
                {
                    MessageBox.Show("نأسف ليس لديك الصلاحية لتغيير الإعدادات", "تنبيه",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                frmCheckPwd checkPwdWindow = new frmCheckPwd
                {
                    operNo = 100,
                    CheckType = 200
                };

                checkPwdWindow.ShowDialog();

                if (!checkPwdWindow.Iscorrect)
                {
                    return;
                }

                using SqlConnection masterConnection = CreateMasterConnection();
                EnsureConnectionOpen(masterConnection);

                using var command = new SqlCommand(
                    "update GediaSetting set IsGediaActive=@IsGediaActive,GediaPort=@GediaPort,GediaEnableReceiptPrint=@GediaEnableReceiptPrint where id=1",
                    masterConnection);

                command.Parameters.AddWithValue("@IsGediaActive", IsChecked(CheckGedia));
                command.Parameters.AddWithValue("@GediaPort", SafeInt(txtGediaPort.Text, 0));
                command.Parameters.AddWithValue("@GediaEnableReceiptPrint", IsChecked(ChkGediaReceiptPrint));
                command.ExecuteNonQuery();

                MainSetting.IsGediaActive = IsChecked(CheckGedia);
                MainSetting.GediaPort = SafeInt(txtGediaPort.Text, 0);
                MainSetting.GediaEnableReceiptPrint = IsChecked(ChkGediaReceiptPrint);

                MessageBox.Show("تم حفظ الإعدادات", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch
            {
                // intentionally silent to preserve legacy behavior
            }
        }

        private async Task testGedia()
        {
            PaymentResponseViewModel response = await GeideaPament.Pay(new PaymentRequestViewModel
            {
                EnableReceiptPrint = IsChecked(ChkGediaReceiptPrint),
                Amount = SafeDouble(txtAmount.Text, 0.01),
                ComPort = SafeInt(txtGediaPort.Text, 0)
            });

            if (!response.IsSucess)
            {
                MessageBox.Show(response.ResposeMsg, "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private async void BtnTestGedia_Click(object sender, RoutedEventArgs e)
        {
            if (MainClass.EmpNo > 0)
            {
                MessageBox.Show("نأسف ليس لديك الصلاحية لتغيير الإعدادات", "تنبيه",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!IsChecked(CheckGedia))
            {
                MessageBox.Show("الرجاء تفعيل الدفع عن طريق جيديا!", "تنبيه",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            await testGedia();
        }

        private void Btnsavneoleap_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (MainClass.EmpNo > 0)
                {
                    MessageBox.Show("نأسف ليس لديك الصلاحية لتغيير الإعدادات", "تنبيه",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                frmCheckPwd checkPwdWindow = new frmCheckPwd
                {
                    operNo = 100,
                    CheckType = 200
                };

                checkPwdWindow.ShowDialog();

                if (!checkPwdWindow.Iscorrect)
                {
                    return;
                }

                if (!Confirm("هل انت متأكد من حفظ إعدادات نظام نيون ليب؟", "تأكيد"))
                {
                    return;
                }

                EnsureConnectionOpen(conn);

                using (var deleteCommand = new SqlCommand("delete from SettingNeoleap where id=1", conn))
                {
                    deleteCommand.ExecuteNonQuery();
                }

                using var insertCommand = new SqlCommand(
                    "insert into SettingNeoleap (IsNeoLeapActive,NeoLeapPort,NeoLeapEnableReceiptPrint,neoleaptoken) values(@IsNeoLeapActive,@NeoLeapPort,@NeoLeapEnableReceiptPrint,@neoleaptoken)",
                    conn);

                insertCommand.Parameters.AddWithValue("@IsNeoLeapActive", IsChecked(chkActivneoleap));
                insertCommand.Parameters.AddWithValue("@NeoLeapPort", SafeInt(txtportneoleap.Text, 0));
                insertCommand.Parameters.AddWithValue("@NeoLeapEnableReceiptPrint", IsChecked(ChkNeoLeapEnableReceiptPrint));
                insertCommand.Parameters.AddWithValue("@neoleaptoken", txtTokenneoleap.Text?.Trim() ?? string.Empty);
                insertCommand.ExecuteNonQuery();

                MainClass.IsNeoLeapActive = IsChecked(chkActivneoleap);
                MainClass.NeoLeapPort = txtportneoleap.Text?.Trim() ?? "0";
                MainClass.NeoLeapEnableReceiptPrint = IsChecked(ChkNeoLeapEnableReceiptPrint);
                MainClass.NeoLeapMerchantToken = txtTokenneoleap.Text?.Trim() ?? string.Empty;

                MessageBox.Show("تم حفظ الإعدادات", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                if (conn.State == ConnectionState.Open)
                {
                    conn.Close();
                }
            }
        }

        #endregion

        #region Grid Methods

        private ObservableCollection<PrinterGridRow> SetPrinterDataSource()
        {
            return new ObservableCollection<PrinterGridRow>();
        }

        private void GridControl1_ButtonClick(object sender, RoutedEventArgs e)
        {
            if ((e.OriginalSource as DependencyObject) == null)
            {
                return;
            }

            Button button = FindVisualParent<Button>(e.OriginalSource as DependencyObject);
            if (button == null)
            {
                return;
            }

            if (button.DataContext is not PrinterGridRow row)
            {
                return;
            }

            string buttonText = button.Content?.ToString() ?? string.Empty;

            if (buttonText.Contains("حذف"))
            {
                btnDeleteRowDgv_ButtonClick(row);
            }
            else if (buttonText.Contains("اختيار"))
            {
                RIBtn_ButtonClick(row);
            }
        }

        private void GridControl2_ButtonClick(object sender, RoutedEventArgs e)
        {
            Button button = FindVisualParent<Button>(e.OriginalSource as DependencyObject);
            if (button?.DataContext is not DatabaseManagementRow row)
            {
                return;
            }

            string buttonText = button.Content?.ToString() ?? string.Empty;
            if (buttonText.Contains("إضافة"))
            {
                InsertInto_YearPreviews(row.DbName, row.DbAutoName);
            }
        }

        private void GridControl3_ButtonClick(object sender, RoutedEventArgs e)
        {
            Button button = FindVisualParent<Button>(e.OriginalSource as DependencyObject);
            if (button?.DataContext is not YearPreviewRow row)
            {
                return;
            }

            string buttonText = button.Content?.ToString() ?? string.Empty;
            if (buttonText.Contains("حذف"))
            {
                Delete_YearPreviews(row.Dbname);
            }
        }

        private void RIBtn_ButtonClick(PrinterGridRow row)
        {
            try
            {
                OpenFileDialog1.Filter = "Repx (*.Repx)|*.Repx";
                OpenFileDialog1.FileName = string.Empty;

                if (OpenFileDialog1.ShowDialog() == true)
                {
                    row.DgvReport = Path.GetFileName(OpenFileDialog1.FileName);
                }
            }
            catch
            {
                // intentionally silent
            }
        }

        private void btnDeleteRowDgv_ButtonClick(PrinterGridRow row)
        {
            if (row == null)
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(row.DgvName) &&
                Confirm("هل انت متأكد من الحذف؟", "رسالة تأكيد"))
            {
                _printerRows.Remove(row);
            }
        }

        private void InsertInto_YearPreviews(string dbName, string dbAutoName)
        {
            try
            {
                EnsureConnectionOpen(conn);

                using (var deleteCommand = new SqlCommand("Delete from Year_Previews where Dbname=@Dbname", conn))
                {
                    deleteCommand.Parameters.AddWithValue("@Dbname", dbName);
                    deleteCommand.ExecuteNonQuery();
                }

                using var insertCommand = new SqlCommand(
                    "insert into Year_Previews(Dbname,DbAutoName,Is_Deleted) values(@Dbname,@DbAutoName,@Is_Deleted)", conn);

                insertCommand.Parameters.AddWithValue("@Dbname", dbName);
                insertCommand.Parameters.AddWithValue("@DbAutoName", dbAutoName);
                insertCommand.Parameters.AddWithValue("@Is_Deleted", false);
                insertCommand.ExecuteNonQuery();

                MessageBox.Show("تمت الإضافة بنجاح", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
                loadDB_by_DbAutoName();
            }
            catch
            {
                // intentionally silent
            }
            finally
            {
                if (conn.State == ConnectionState.Open)
                {
                    conn.Close();
                }
            }
        }

        private void Delete_YearPreviews(string dbName)
        {
            try
            {
                EnsureConnectionOpen(conn);

                using var deleteCommand = new SqlCommand("Delete from Year_Previews where Dbname=@Dbname", conn);
                deleteCommand.Parameters.AddWithValue("@Dbname", dbName);
                deleteCommand.ExecuteNonQuery();

                MessageBox.Show("تم الحذف بنجاح", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
                loadDB_by_DbAutoName();
            }
            catch
            {
                // intentionally silent
            }
            finally
            {
                if (conn.State == ConnectionState.Open)
                {
                    conn.Close();
                }
            }
        }

        #endregion

        #region Invoice Type Logic

        private short GetInvId()
        {
            if (IsChecked(cbPurchInv)) return 1;
            if (IsChecked(cbSalesInv)) return 2;
            if (IsChecked(cbPOS)) return 3;
            if (IsChecked(rbInvertoryIn)) return 4;
            if (IsChecked(rbInvertoryOut)) return 5;
            if (IsChecked(rbProduction)) return 6;
            if (IsChecked(rbInvertoryCorrection)) return 7;
            if (IsChecked(rbTransfarInvertory)) return 8;
            if (IsChecked(rbFirstInvrtory)) return 9;
            if (IsChecked(rbPricingInv)) return 10;
            if (IsChecked(cbRentInv)) return 11;
            if (IsChecked(ckProjcMng)) return 12;
            if (IsChecked(rbRecieptVAT)) return 13;
            if (IsChecked(btnInventoryOrder)) return 14;
            if (IsChecked(rdsalcontract)) return 23;
            return 0;
        }

        private void Clear()
        {
            cmbAccDiscount.SelectedIndex = -1;
            cmbAccAdditions.SelectedIndex = -1;
            cmbAccInsurance.SelectedIndex = -1;
            cmbAccItems.SelectedIndex = -1;
            cmbAccStore.SelectedIndex = -1;
            cmbAccVAT.SelectedIndex = -1;
            cmbAccReturnItems.SelectedIndex = -1;
            cmbCosts.SelectedIndex = -1;
            cmbPricing.SelectedIndex = -1;
            ckShowPayForm.IsChecked = false;
            ckSyncEntry.IsChecked = false;
            ckSyncInv.IsChecked = false;
        }

        private void ClearPrinting()
        {
            cmbKitchenprinter.Text = string.Empty;
            cmbCashPrinter.Text = string.Empty;
            cmbPrintNo.Text = "1";
            txtRptName.Text = string.Empty;
            txtRptPath.Text = string.Empty;
            txtNote.Text = string.Empty;
            HeaderImg.Source = null;
            FooterImg.Source = null;
            StampImg.Source = null;

            _printerRows.Clear();
        }

        #endregion

        #region UI Events

        private void cmbcashierPrinter_SelectedIndexChanged(object sender, SelectionChangedEventArgs e)
        {
            CashierPrinter = cmbCashPrinter.SelectedItem?.ToString() ?? cmbCashPrinter.Text ?? string.Empty;
        }

        private void cmbBarcodePrinter_SelectedIndexChanged(object sender, SelectionChangedEventArgs e)
        {
            kitchenprinter = cmbKitchenprinter.SelectedItem?.ToString() ?? cmbKitchenprinter.Text ?? string.Empty;
        }

        private void ckAllInvs_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (IsChecked(ckAllInvs))
            {
                lblDefaultCust.Visibility = Visibility.Collapsed;
                CmbDefaultCust.Visibility = Visibility.Collapsed;
            }
        }

        private void ckDefaultSettings_CheckedChanged(object sender, RoutedEventArgs e)
        {
            try
            {
                if (IsChecked(ckDefaultSettings))
                {
                    ckAllpayMethods.IsChecked = true;
                    ckAllPayType.IsChecked = true;
                    ckShowAllOrders.IsChecked = true;
                }
            }
            catch
            {
                // ignore
            }
        }

        private void ckAllpayMethods_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (IsChecked(ckAllpayMethods))
            {
                ckShowCashMeth.IsChecked = true;
                ckShowVisa.IsChecked = true;
                ckShowNetwork.IsChecked = true;
                ckShowMulti.IsChecked = true;
                ckATM.IsChecked = true;
                ckShowBankPaytype.IsChecked = true;
            }
        }

        private void ckAllPayType_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (IsChecked(ckAllPayType))
            {
                ckCashType.IsChecked = true;
                ckPostpon.IsChecked = true;
            }
        }

        private void ckShowAllOrders_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (IsChecked(ckShowAllOrders))
            {
                cbfamilyOrder.IsChecked = true;
                ckShowlocal.IsChecked = true;
                ckCarOrder.IsChecked = true;
                ckTableOrder.IsChecked = true;
                ckShowtakeaway.IsChecked = true;
                chShowHosting.IsChecked = true;
            }
        }

        private void ckAll_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (IsChecked(ckAll))
            {
                ClearPrinting();
                LoadprintSetting(0);
            }
        }

        private void ckPurchInv_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (IsChecked(ckPurchInv))
            {
                ClearPrinting();
                LoadprintSetting(1);
            }
        }

        private void ckSaleInv_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (IsChecked(ckSaleInv))
            {
                ClearPrinting();
                LoadprintSetting(2);
            }
        }

        private void ckPOSInv_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (IsChecked(ckPOSInv))
            {
                ClearPrinting();
                LoadprintSetting(3);
            }
        }

        private void ckRentInv_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (IsChecked(ckRentInv))
            {
                ClearPrinting();
                LoadprintSetting(4);
            }
        }

        private void ckContracts_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (IsChecked(ckContracts))
            {
                ClearPrinting();
                LoadprintSetting(5);
            }
        }

        private void ckReports_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (IsChecked(ckReports))
            {
                ClearPrinting();
                LoadprintSetting(6);
                cmbReportType.Visibility = Visibility.Visible;
                cmbReportType.Focus();
            }
            else
            {
                cmbReportType.Visibility = Visibility.Collapsed;
            }
        }

        private void cbPurchInv_CheckedChanged(object sender, RoutedEventArgs e)
        {
            Clear();

            if (IsChecked(cbPurchInv))
            {
                loadData(1);
            }

            chkDetailedItemEntry.Visibility = cbPurchInv.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
        }

        private void cbSalesInv_CheckedChanged(object sender, RoutedEventArgs e)
        {
            Clear();

            if (IsChecked(cbSalesInv))
            {
                loadCustomers(1);
                loadData(2);
                ckCashCustRequire.Visibility = Visibility.Visible;
                lblDefaultCust.Visibility = Visibility.Visible;
                CmbDefaultCust.Visibility = Visibility.Visible;
                chkDetailedItemEntry.Visibility = Visibility.Visible;
                CheckOperSale.Visibility = Visibility.Visible;
                AddColumnAdd.Visibility = Visibility.Visible;
                chkAtiveCustMeasur.Visibility = Visibility.Visible;
            }
            else
            {
                ckCashCustRequire.Visibility = Visibility.Collapsed;
                chkDetailedItemEntry.Visibility = Visibility.Collapsed;
                chkAtiveCustMeasur.Visibility = Visibility.Collapsed;
            }
        }

        private void cbPOS_CheckedChanged(object sender, RoutedEventArgs e)
        {
            Clear();

            if (IsChecked(cbPOS))
            {
                loadCustomers(1);
                loadData(3);
                lblCloudSerial.Visibility = Visibility.Visible;
                lblReInvCloudSerial.Visibility = Visibility.Visible;
                txtCloudSerial.Visibility = Visibility.Visible;
                txtReInvCloudSerial.Visibility = Visibility.Visible;
                ckCashCustRequire.Visibility = Visibility.Visible;
                lblDefaultCust.Visibility = Visibility.Visible;
                CmbDefaultCust.Visibility = Visibility.Visible;
                AddColumnAdd.Visibility = Visibility.Visible;
            }
            else
            {
                lblCloudSerial.Visibility = Visibility.Collapsed;
                lblReInvCloudSerial.Visibility = Visibility.Collapsed;
                txtCloudSerial.Visibility = Visibility.Collapsed;
                txtReInvCloudSerial.Visibility = Visibility.Collapsed;
                ckCashCustRequire.Visibility = Visibility.Collapsed;
                lblDefaultCust.Visibility = Visibility.Collapsed;
                CmbDefaultCust.Visibility = Visibility.Collapsed;
            }
        }

        private void cbRentInv_CheckedChanged(object sender, RoutedEventArgs e)
        {
            Clear();

            if (IsChecked(cbRentInv))
            {
                loadData(11);
                lblDefaultCust.Visibility = Visibility.Collapsed;
                CmbDefaultCust.Visibility = Visibility.Collapsed;
            }
        }

        private void ckProjcMng_CheckedChanged(object sender, RoutedEventArgs e)
        {
            Clear();

            if (IsChecked(ckProjcMng))
            {
                loadData(12);
                lblDefaultCust.Visibility = Visibility.Collapsed;
                CmbDefaultCust.Visibility = Visibility.Collapsed;
            }
        }

        private void rbProduction_CheckedChanged(object sender, RoutedEventArgs e)
        {
            Clear();

            if (IsChecked(rbProduction))
            {
                loadData(6);
                lblDefaultCust.Visibility = Visibility.Collapsed;
                CmbDefaultCust.Visibility = Visibility.Collapsed;
            }
        }

        private void rbPricingInv_CheckedChanged(object sender, RoutedEventArgs e)
        {
            Clear();

            if (IsChecked(rbPricingInv))
            {
                loadCustomers(1);
                loadData(10);
                ckCashCustRequire.Visibility = Visibility.Visible;
            }
            else
            {
                ckCashCustRequire.Visibility = Visibility.Collapsed;
            }
        }

        private void rbTransfarInvertory_CheckedChanged(object sender, RoutedEventArgs e)
        {
            Clear();

            if (IsChecked(rbTransfarInvertory))
            {
                loadData(8);
                lblDefaultCust.Visibility = Visibility.Collapsed;
                CmbDefaultCust.Visibility = Visibility.Collapsed;
            }
        }

        private void rbInvertoryCorrection_CheckedChanged(object sender, RoutedEventArgs e)
        {
            Clear();

            if (IsChecked(rbInvertoryCorrection))
            {
                loadData(7);
                lblDefaultCust.Visibility = Visibility.Collapsed;
                CmbDefaultCust.Visibility = Visibility.Collapsed;
            }
        }

        private void rbInvertoryOut_CheckedChanged(object sender, RoutedEventArgs e)
        {
            Clear();

            if (IsChecked(rbInvertoryOut))
            {
                loadData(5);
                lblDefaultCust.Visibility = Visibility.Collapsed;
                CmbDefaultCust.Visibility = Visibility.Collapsed;
            }
        }

        private void rbInvertoryIn_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (IsChecked(rbInvertoryIn))
            {
                lblDefaultCust.Visibility = Visibility.Collapsed;
                CmbDefaultCust.Visibility = Visibility.Collapsed;
            }
        }

        private void rbFirstInvrtory_CheckedChanged(object sender, RoutedEventArgs e)
        {
            Clear();

            if (IsChecked(rbFirstInvrtory))
            {
                loadData(9);
                lblDefaultCust.Visibility = Visibility.Collapsed;
                CmbDefaultCust.Visibility = Visibility.Collapsed;
            }
        }

        private void rbRecieptVAT_CheckedChanged(object sender, RoutedEventArgs e)
        {
            Clear();

            if (IsChecked(rbRecieptVAT))
            {
                loadData(13);
                lblDefaultCust.Visibility = Visibility.Collapsed;
                CmbDefaultCust.Visibility = Visibility.Collapsed;
            }
        }

        private void btnInventoryOrder_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (IsChecked(btnInventoryOrder))
            {
                lblDefaultCust.Visibility = Visibility.Collapsed;
                CmbDefaultCust.Visibility = Visibility.Collapsed;
            }
        }

        private void rdsalcontract_CheckedChanged(object sender, RoutedEventArgs e)
        {
            Clear();

            if (IsChecked(rdsalcontract))
            {
                loadCustomers(1);
                loadData(23);
                ckCashCustRequire.Visibility = Visibility.Visible;
                lblDefaultCust.Visibility = Visibility.Visible;
                CmbDefaultCust.Visibility = Visibility.Visible;
                chkDetailedItemEntry.Visibility = Visibility.Visible;
                CheckOperSale.Visibility = Visibility.Visible;
                AddColumnAdd.Visibility = Visibility.Visible;
            }
            else
            {
                ckCashCustRequire.Visibility = Visibility.Collapsed;
                chkDetailedItemEntry.Visibility = Visibility.Collapsed;
            }
        }

        private void ItemsColNo_ValueChanged(object sender, TextChangedEventArgs e)
        {
            UpdateItemsInPage();
        }

        private void ItemsRowsNo_ValueChanged(object sender, TextChangedEventArgs e)
        {
            UpdateItemsInPage();
        }

        private void chkPoleDisplay_CheckedChanged(object sender, RoutedEventArgs e)
        {
            chkDigitalDisplay.Visibility = IsChecked(chkPoleDisplay)
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        private void ckSyncCloud_CheckedChanged(object sender, RoutedEventArgs e)
        {
            chkSyncCloudFirst.Visibility = IsChecked(ckSyncCloud)
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        private void rbSecondaryBranch_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if ((IsChecked(rbSecondaryBranch) || IsChecked(rbMiddleBranch)) && cmbDistBranch.SelectedIndex == -1)
            {
                if (cmbDistBranch.Items.Count > 0)
                {
                    cmbDistBranch.IsEnabled = true;
                    cmbDistBranch.SelectedIndex = 0;
                    cmbSrcBranches.SelectedValue = MainClass.BranchNo;
                }
            }
            else if (IsChecked(rbDataCenterBranch))
            {
                cmbDistBranch.SelectedIndex = -1;
                cmbDistBranch.IsEnabled = false;
                cmbSrcBranches.SelectedValue = MainClass.BranchNo;
                cmbSrcBranches.IsEnabled = true;
            }
            else if (IsChecked(ckSyncCloud))
            {
                cmbDistBranch.SelectedIndex = -1;
                cmbSrcBranches.SelectedIndex = -1;
                cmbDistBranch.IsEnabled = false;
                cmbSrcBranches.IsEnabled = false;
            }
        }

        private void rbCashier_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (IsChecked(rbCashier))
            {
                GrpPrintImages.Visibility = Visibility.Collapsed;
            }
        }

        private void rbA4_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (IsChecked(rbA4))
            {
                GrpPrintImages.Visibility = Visibility.Visible;
            }
        }

        private void TabControl2_SelectedIndexChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TabOtherSetting.SelectedItem == TabPageSyncSetting)
            {
                loadSyncSetting();
            }
        }

        private void frmSettings_KeyDown(object sender, KeyEventArgs e)
        {
        }

        private void TabOtherSetting_KeyDown(object sender, KeyEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.LeftShift)
            {
                TxtIpserver.Visibility = Visibility.Visible;
                LblIpServer.Visibility = Visibility.Visible;
            }
        }

        #endregion

        #region Image Methods

        private void lnkImgAdd1_LinkClicked(object sender, MouseButtonEventArgs e)
        {
            UploadImage(1);
        }

        private void lnkImgAdd2_LinkClicked(object sender, MouseButtonEventArgs e)
        {
            UploadImage(2);
        }

        private void lnkImgAdd3_LinkClicked(object sender, MouseButtonEventArgs e)
        {
            UploadImage(3);
        }

        private void lnkImgClr1_LinkClicked(object sender, MouseButtonEventArgs e)
        {
            HeaderImg.Source = null;
        }

        private void lnkImgClr2_LinkClicked(object sender, MouseButtonEventArgs e)
        {
            FooterImg.Source = null;
        }

        private void lnkImgClr3_LinkClicked(object sender, MouseButtonEventArgs e)
        {
            StampImg.Source = null;
        }

        private void UploadImage(int img)
        {
            try
            {
                OpenFileDialog1.Filter = "Image Files|*.jpg;*.jpeg;*.bmp;*.gif;*.tiff;*.png|All Files|*.*";

                if (OpenFileDialog1.ShowDialog() == true)
                {
                    BitmapImage bitmap = LoadBitmapFromFile(OpenFileDialog1.FileName);

                    if (img == 1)
                    {
                        HeaderImg.Source = bitmap;
                    }
                    else if (img == 2)
                    {
                        FooterImg.Source = bitmap;
                    }
                    else if (img == 3)
                    {
                        StampImg.Source = bitmap;
                    }
                }
            }
            catch
            {
                // intentionally silent
            }
        }

        private void btnSelectPath_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                OpenFileDialog1.Filter = "Repx (*.Repx)|*.Repx";
                OpenFileDialog1.FileName = string.Empty;

                if (OpenFileDialog1.ShowDialog() == true)
                {
                    txtRptPath.Text = OpenFileDialog1.FileName;
                    txtRptName.Text = Path.GetFileName(txtRptPath.Text);
                }
            }
            catch
            {
                // intentionally silent
            }
        }

        #endregion

        #region Search / Open Windows

        private string AccountSearch(string cond)
        {
            frmAccountSrch searchWindow = new frmAccountSrch
            {
                cond = cond
            };

            searchWindow.ShowDialog();

            return searchWindow.Code > -1
                ? searchWindow.Code.ToString()
                : string.Empty;
        }

        private void cmbAccStore_Click(object sender, RoutedEventArgs e)
        {
            string accountCode = AccountSearch(string.Empty);
            if (!string.IsNullOrWhiteSpace(accountCode))
            {
                cmbAccStore.SelectedValue = accountCode;
            }
        }

        private void cmbAccItems_Click(object sender, RoutedEventArgs e)
        {
            string accountCode = AccountSearch(string.Empty);
            if (!string.IsNullOrWhiteSpace(accountCode))
            {
                cmbAccItems.SelectedValue = accountCode;
            }
        }

        private void cmbAccInsurance_Click(object sender, RoutedEventArgs e)
        {
            string accountCode = AccountSearch(string.Empty);
            if (!string.IsNullOrWhiteSpace(accountCode))
            {
                cmbAccInsurance.SelectedValue = accountCode;
            }
        }

        private void cmbAccAdditions_Click(object sender, RoutedEventArgs e)
        {
            string accountCode = AccountSearch(string.Empty);
            if (!string.IsNullOrWhiteSpace(accountCode))
            {
                cmbAccAdditions.SelectedValue = accountCode;
            }
        }

        private void cmbAccounts_Click(object sender, RoutedEventArgs e)
        {
            string accountCode = AccountSearch(string.Empty);
            if (!string.IsNullOrWhiteSpace(accountCode))
            {
                cmbAccDiscount.SelectedValue = accountCode;
            }
        }

        private void cmbAccVat_Click(object sender, RoutedEventArgs e)
        {
            string accountCode = AccountSearch(string.Empty);
            if (!string.IsNullOrWhiteSpace(accountCode))
            {
                cmbAccVAT.SelectedValue = accountCode;
            }
        }

        private void cmbAccReturnItem_Click(object sender, RoutedEventArgs e)
        {
            string accountCode = AccountSearch(string.Empty);
            if (!string.IsNullOrWhiteSpace(accountCode))
            {
                cmbAccReturnItems.SelectedValue = accountCode;
            }
        }

        private void Button1_Click_1(object sender, RoutedEventArgs e)
        {
            frm_ReportDesign reportDesignWindow = new frm_ReportDesign();
            reportDesignWindow.Show();
        }

        private void AddColumnAdd_Click(object sender, RoutedEventArgs e)
        {
            frmGlasses glassesWindow = new frmGlasses
            {
                code = 2
            };

            glassesWindow.TabControl1.SelectedIndex = 1;
            glassesWindow.TabControl1.Items.Remove(glassesWindow.TabPage1);
            glassesWindow.loadcolumnOther();
            glassesWindow.Show();
        }

        #endregion

        #region Sync Async

        private async Task btnGetClientCode_ClickAsync(object sender, RoutedEventArgs e)
        {
            await Task.CompletedTask;
        }

        private async Task btnSyncCards_ClickAsync(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!Sync.ValidAPIUrl)
                {
                    return;
                }

                if (Sync.BranchType == 2 && Sync.SyncType > 0)
                {
                    if (Confirm("هل تريد تحديث بيانات التعاريف عبر نظام المزامنة", "تأكيد"))
                    {
                        SyncOperation operation = new SyncOperation();
                        await operation.ReadCardsDataOnline();
                    }
                }
                else
                {
                    if (Confirm("هل تريد مزامنة بيانات التعاريف", "تأكيد"))
                    {
                        if (Sync.BranchType == 1 && Sync.ActiveSync && Sync.SyncType > 0)
                        {
                            SyncOperation operation = new SyncOperation();
                            await operation.PostCardDataOnline();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task btnGetBranches_ClickAsync(object sender, RoutedEventArgs e)
        {
            try
            {
                EntityOperations entityOperations = new EntityOperations();
                BranchCRUD branchCrud = new BranchCRUD(Sync.APIUrl);
                List<Branch> branches = (List<Branch>)await branchCrud.GetBranches(
                    Sync.ClientCode,
                    Sync.BranchId,
                    Sync.BranchType,
                    DateTime.MinValue);

                if (branches.Count > 0)
                {
                    entityOperations.SaveBranch(branches);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Delete / Reset Data

        private void btnDeleteInv_Click_1(object sender, RoutedEventArgs e)
        {
            if (!Confirm("يجب اخذ نسخة احتياطية قبل البدء بعملية الحذف", "Auditor"))
            {
                return;
            }

            Home homeWindow = new Home
            {
                IsClosed = true
            };
            homeWindow.CheckBackup();

            if (!Confirm("هل انت متأكد من حذف الفواتير؟", "Auditor"))
            {
                return;
            }

            try
            {
                EnsureConnectionOpen(conn);

                using (var truncateCommand = new SqlCommand(
                    @"truncate table inv
                      truncate table inv_sub
                      truncate table InvoicePayments
                      truncate table CasherClosed
                      truncate table CasherClosed_Sub", conn))
                {
                    truncateCommand.ExecuteNonQuery();
                }

                using (var deleteEntrySubCommand = new SqlCommand(
                    @"delete from Entry_sub where EntryGlobalID in
                      (
                        select GlobalID from entry
                        where type in (1,2,3,4,11,12,13,21,22,23,17,16,14,27,26,20,21,24,25,15,29,30,31,32)
                      )", conn))
                {
                    deleteEntrySubCommand.ExecuteNonQuery();
                }

                using (var deleteEntryCommand = new SqlCommand(
                    @"delete from Entry
                      where type in (1,2,3,4,11,12,13,21,22,23,17,16,14,24,25,27,26,15,20,21,29,30,31,32)", conn))
                {
                    deleteEntryCommand.ExecuteNonQuery();
                }

                MessageBox.Show("تمت العملية بنجاح", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                if (conn.State == ConnectionState.Open)
                {
                    conn.Close();
                }
            }
        }

        private void BtnDeleteEntry_Click_1(object sender, RoutedEventArgs e)
        {
            if (!Confirm("يجب اخذ نسخة احتياطية قبل البدء بعملية الحذف", "Auditor"))
            {
                return;
            }

            Home homeWindow = new Home();
            homeWindow.CheckBackup();

            if (!Confirm("هل انت متأكد من حذف القيود؟", "Auditor"))
            {
                return;
            }

            try
            {
                EnsureConnectionOpen(conn);

                using (var deleteSubCommand = new SqlCommand(
                    "delete from Entry_sub where EntryGlobalID in (select globalid from Entry where type in (0,10))", conn))
                {
                    deleteSubCommand.ExecuteNonQuery();
                }

                using (var deleteEntryCommand = new SqlCommand(
                    "delete from entry where type in (0,10)", conn))
                {
                    deleteEntryCommand.ExecuteNonQuery();
                }

                MessageBox.Show("تمت العملية بنجاح", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                if (conn.State == ConnectionState.Open)
                {
                    conn.Close();
                }
            }
        }

        private void BtndeleteRecept_Click_1(object sender, RoutedEventArgs e)
        {
            if (!Confirm("يجب اخذ نسخة احتياطية قبل البدء بعملية الحذف", "Auditor"))
            {
                return;
            }

            Home homeWindow = new Home();
            homeWindow.CheckBackup();

            if (!Confirm("هل انت متأكد من حذف السندات؟", "Auditor"))
            {
                return;
            }

            try
            {
                EnsureConnectionOpen(conn);

                using (var truncateCommand = new SqlCommand(
                    @"truncate table SandQ
                      truncate table SandQD
                      truncate table SandSD
                      truncate table SandVAT
                      truncate table Receipts", conn))
                {
                    truncateCommand.ExecuteNonQuery();
                }

                using (var deleteSubCommand = new SqlCommand(
                    "delete from Entry_sub where EntryGlobalID in(select GlobalID from entry where type in(5,6,7,8,9,26))", conn))
                {
                    deleteSubCommand.ExecuteNonQuery();
                }

                using (var deleteEntryCommand = new SqlCommand(
                    "delete from entry where type in(5,6,7,8,9,26)", conn))
                {
                    deleteEntryCommand.ExecuteNonQuery();
                }

                MessageBox.Show("تمت العملية بنجاح", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                if (conn.State == ConnectionState.Open)
                {
                    conn.Close();
                }
            }
        }

        private void BtnDeleteItems_Click_1(object sender, RoutedEventArgs e)
        {
            if (!Confirm("يجب اخذ نسخة احتياطية قبل البدء بعملية الحذف", "Auditor"))
            {
                return;
            }

            Home homeWindow = new Home();
            homeWindow.CheckBackup();

            if (!Confirm("هل انت متأكد من حذف الأصناف؟", "Auditor"))
            {
                return;
            }

            try
            {
                EnsureConnectionOpen(conn);

                using var command = new SqlCommand(
                    @"truncate TABLE items
                      truncate TABLE ItemComponents
                      truncate TABLE ItemPrices
                      truncate TABLE Itembarcodes
                      truncate table itemunits", conn);

                command.ExecuteNonQuery();

                MessageBox.Show("تمت العملية بنجاح", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                if (conn.State == ConnectionState.Open)
                {
                    conn.Close();
                }
            }
        }

        private void btnDeleteCat_Click(object sender, RoutedEventArgs e)
        {
            if (!Confirm("يجب اخذ نسخة احتياطية قبل البدء بعملية الحذف", "Auditor"))
            {
                return;
            }

            Home homeWindow = new Home();
            homeWindow.CheckBackup();

            if (!Confirm("هل انت متأكد من حذف المجموعات؟", "Auditor"))
            {
                return;
            }

            try
            {
                EnsureConnectionOpen(conn);

                using var command = new SqlCommand("truncate TABLE ItemsCategory", conn);
                command.ExecuteNonQuery();

                MessageBox.Show("تمت العملية بنجاح", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                if (conn.State == ConnectionState.Open)
                {
                    conn.Close();
                }
            }
        }

        private void BtnResetData_Click(object sender, RoutedEventArgs e)
        {
            if (!Confirm("هل انت متأكد من حذف جميع العمليات؟", "Auditor"))
            {
                return;
            }

            try
            {
                EnsureConnectionOpen(conn);

                using var command = new SqlCommand(
                    @"truncate table Entry
                      truncate table Entry_Sub
                      truncate table inv
                      truncate table inv_sub
                      truncate table CasherClosed
                      truncate table CasherClosed_Sub
                      truncate table SandQ
                      truncate table SandQD
                      truncate table SandSD
                      truncate table SandVAT
                      truncate table Receipts
                      truncate table InvoicePayments
                      truncate table InvContratct
                      truncate table InvContratct_Sub", conn);

                command.ExecuteNonQuery();

                MessageBox.Show("تمت العملية بنجاح", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                if (conn.State == ConnectionState.Open)
                {
                    conn.Close();
                }
            }
        }

        #endregion

        #region Notify / Connection

        private void BtnConnect_Click(object sender, RoutedEventArgs e)
        {
            frmCheckPwd checkPwdWindow = new frmCheckPwd
            {
                operNo = 100,
                CheckType = 300
            };

            checkPwdWindow.ShowDialog();

            if (!checkPwdWindow.Iscorrect)
            {
                return;
            }

            bool validInput =
                !string.IsNullOrWhiteSpace(txtclientid.Text) &&
                int.TryParse(txtnodeid.Text, out _) &&
                !string.IsNullOrWhiteSpace(txtpasscode.Text);

            if (!validInput)
            {
                MessageBox.Show("يرجى تعبئة البيانات", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(CmbTxtDataBase.Text))
            {
                MessageBox.Show("يرجى اختيار قاعدة البيانات", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(Cmbservername.Text))
            {
                MessageBox.Show("يرجى اختيار السيرفر", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (CmbBranchMqtt.SelectedValue == null)
            {
                MessageBox.Show("يرجى اختيار الفرع", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            insert_SettingNotify();

            Panelnot1.IsEnabled = false;
            panelnot2.IsEnabled = false;
            btnconnectit.Background = new SolidColorBrush(Color.FromRgb(34, 139, 34));
            btnconnectit.Content = "تم الاتصال بنجاح";

            try
            {
                ReceivedData.LoadReceived(ConnectBroker.connString);
            }
            catch
            {
                // ignore
            }
        }

        public void insert_SettingNotify()
        {
            try
            {
                EnsureConnectionOpen(conn);

                using (var checkCommand = new SqlCommand("select * from Settingmqtt where DB=@DB", conn))
                {
                    checkCommand.Parameters.AddWithValue("@DB", CmbTxtDataBase.Text.Trim());

                    using var adapter = new SqlDataAdapter(checkCommand);
                    var existingTable = new DataTable();
                    adapter.Fill(existingTable);

                    if (existingTable.Rows.Count > 0)
                    {
                        using var deleteCommand = new SqlCommand("delete from [dbo].[Settingmqtt] where DB=@DB", conn);
                        deleteCommand.Parameters.AddWithValue("@DB", CmbTxtDataBase.Text.Trim());
                        deleteCommand.ExecuteNonQuery();
                    }
                }

                using var insertCommand = new SqlCommand(
                    @"INSERT INTO [dbo].[Settingmqtt]
                      ([clientid],[nodeid],[frequency],[sale],[receipt],[income],[finance],[passcode],[servername],[usrname],[pwd],[DB],[publickey],[privatekey],[IS_Play],[inventory],is_active,BranchId,IpServer,syncItems,syncOper,syncDefinitions)
                      VALUES
                      (@clientid,@nodeid,@frequency,@sale,@receipt,@income,@finance,@passcode,@servername,@usrname,@pwd,@DB,@publickey,@privatekey,@IS_Play,@inventory,@is_active,@BranchId,@IpServer,@syncItems,@syncOper,@syncDefinitions)", conn);

                insertCommand.Parameters.AddWithValue("@clientid", txtclientid.Text?.Trim() ?? string.Empty);
                insertCommand.Parameters.AddWithValue("@nodeid", txtnodeid.Text?.Trim() ?? string.Empty);
                insertCommand.Parameters.AddWithValue("@frequency", txtfrequency.Text?.Trim() ?? string.Empty);
                insertCommand.Parameters.AddWithValue("@sale", txtsale.Text?.Trim() ?? string.Empty);
                insertCommand.Parameters.AddWithValue("@receipt", txtreceipt.Text?.Trim() ?? string.Empty);
                insertCommand.Parameters.AddWithValue("@income", txtincome.Text?.Trim() ?? string.Empty);
                insertCommand.Parameters.AddWithValue("@finance", txtfinance.Text?.Trim() ?? string.Empty);
                insertCommand.Parameters.AddWithValue("@passcode", txtpasscode.Text?.Trim() ?? string.Empty);
                insertCommand.Parameters.AddWithValue("@servername", Cmbservername.Text?.Trim() ?? string.Empty);
                insertCommand.Parameters.AddWithValue("@usrname", usrname.Text ?? string.Empty);
                insertCommand.Parameters.AddWithValue("@pwd", pwdtxt.Text ?? string.Empty);
                insertCommand.Parameters.AddWithValue("@DB", CmbTxtDataBase.Text?.Trim() ?? string.Empty);
                insertCommand.Parameters.AddWithValue("@publickey", txtpublickey.Text ?? string.Empty);
                insertCommand.Parameters.AddWithValue("@privatekey", txtprivatekey.Text ?? string.Empty);
                insertCommand.Parameters.AddWithValue("@IS_Play", IsChecked(ChkIs_Play));
                insertCommand.Parameters.AddWithValue("@inventory", txtitem.Text ?? string.Empty);
                insertCommand.Parameters.AddWithValue("@is_active", IsChecked(chkis_active));
                insertCommand.Parameters.AddWithValue("@BranchId", CmbBranchMqtt.SelectedValue ?? 0);
                insertCommand.Parameters.AddWithValue("@IpServer", TxtIpserver.Text?.Trim() ?? string.Empty);
                insertCommand.Parameters.AddWithValue("@syncItems", IsChecked(chsyncItems));
                insertCommand.Parameters.AddWithValue("@syncOper", IsChecked(ChksyncOper));
                insertCommand.Parameters.AddWithValue("@syncDefinitions", IsChecked(ChksyncDefinitions));
                insertCommand.ExecuteNonQuery();

                Panelnot1.IsEnabled = false;
                panelnot2.IsEnabled = false;
                btnconnectit.Background = new SolidColorBrush(Color.FromRgb(34, 139, 34));
                btnconnectit.Content = "تم الاتصال بنجاح";

                MessageBox.Show("تم الاتصال بنجاح", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                if (conn.State == ConnectionState.Open)
                {
                    conn.Close();
                }
            }
        }

        public string GenerateSecureRandomKey(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            StringBuilder builder = new StringBuilder();

            using var rng = RandomNumberGenerator.Create();
            byte[] randomBytes = new byte[length];
            rng.GetBytes(randomBytes);

            foreach (byte value in randomBytes)
            {
                builder.Append(chars[value % chars.Length]);
            }

            return builder.ToString();
        }

        private void Cmbservername_SelectedIndexChanged(object sender, SelectionChangedEventArgs e)
        {
            serverName = Cmbservername.Text;
            con1 = new SqlConnection($"server={serverName}; integrated security = true");
            LoadDataBase();
        }

        private void Timer1_Tick(object sender, EventArgs e)
        {
            try
            {
                Home homeWindow = System.Windows.Application.Current.Windows.OfType<Home>().FirstOrDefault();
                if (homeWindow?.ns != null && homeWindow.ns.isConnected)
                {
                    btnconnectit.IsEnabled = false;
                }
                else
                {
                    btnconnectit.IsEnabled = true;
                }
            }
            catch
            {
                btnconnectit.IsEnabled = true;
            }
        }

        private void Button3_Click(object sender, RoutedEventArgs e)
        {
            string value = ShowInputDialog("ادخل اسم الجهاز هذا");
            if (!string.IsNullOrWhiteSpace(value))
            {
                txtmynode.Text = value;
                File.WriteAllText(Path.Combine(Environment.CurrentDirectory, "mynodeid.txt"), value);
            }
        }

        private void Button4_Click(object sender, RoutedEventArgs e)
        {
            string value = ShowInputDialog("ادخل اسم جهاز الطرفية الاخر");
            if (!string.IsNullOrWhiteSpace(value))
            {
                ListBoxNodes.Items.Add(value);
                File.AppendAllText(Path.Combine(Environment.CurrentDirectory, "othernodeid.txt"), value + Environment.NewLine);
            }
        }

        private void Button5_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string selectedFolder = SelectFolderPath();
                if (!string.IsNullOrWhiteSpace(selectedFolder))
                {
                    txtdocuments.Text = selectedFolder;
                }
            }
            catch
            {
                // intentionally silent
            }
        }

        private void Button6_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Properties.Settings.Default.DocsRootPath = txtdocuments.Text;
                Properties.Settings.Default.Save();
                MessageBox.Show("تم الحفظ بنجاح", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Btntestneoleap_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                NeoleapService neoleapService = new NeoleapService(msg =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        txtLogging.AppendText(msg + Environment.NewLine);
                        txtLogging.ScrollToEnd();
                    });
                });

                _ = SafeDouble(txtAmountneoleap.Text, 0);
                neoleapService.TestConnection();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Misc Legacy-Compatible Methods

        private void ApplySett(DependencyObject parent)
        {
            // intentionally simplified for WPF
        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            // legacy placeholder
        }

        private void Button2_Click(object sender, RoutedEventArgs e)
        {
            // legacy placeholder
        }

        private void chkActive_CheckedChanged(object sender, RoutedEventArgs e)
        {
        }

        private void txtDevM_TextChanged(object sender, TextChangedEventArgs e)
        {
        }

        private void Cmbservername_Leave(object sender, RoutedEventArgs e)
        {
        }

        private void Cmbservername_Layout(object sender, EventArgs e)
        {
        }

        private void Label1_Click(object sender, RoutedEventArgs e)
        {
        }

        #endregion

        #region Master Connection Helper

        private SqlConnection CreateMasterConnection()
        {
            if (MainClass.Conn_type == 1)
            {
                return new SqlConnection($"server={MainClass.Server.Trim()};database=master;trusted_connection=true");
            }

            if (MainClass.Conn_type == 2)
            {
                return new SqlConnection(
                    $"server={MainClass.Server};database=master;MultipleActiveResultSets=True;user id='{MainClass.NetUserId}'; pwd='{MainClass.NetPwd}'");
            }

            return new SqlConnection($"server={MainClass.Server.Trim()};database=master;trusted_connection=true");
        }

        #endregion
    }

    #region Model Classes

    public class NotifyBase : INotifyPropertyChanged
    {
        private event PropertyChangedEventHandler _propertyChanged;

        event PropertyChangedEventHandler INotifyPropertyChanged.PropertyChanged
        {
            add { _propertyChanged += value; }
            remove { _propertyChanged -= value; }
        }

        protected void RaisePropertyChanged(string propertyName)
        {
            _propertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class PrinterGridRow : NotifyBase
    {
        private string _dgvName;
        private string _dgvPrinterName;
        private string _dgvReport;

        public string DgvName
        {
            get => _dgvName;
            set
            {
                _dgvName = value;
                RaisePropertyChanged(nameof(DgvName));
            }
        }

        public string DgvPrinterName
        {
            get => _dgvPrinterName;
            set
            {
                _dgvPrinterName = value;
                RaisePropertyChanged(nameof(DgvPrinterName));
            }
        }

        public string DgvReport
        {
            get => _dgvReport;
            set
            {
                _dgvReport = value;
                RaisePropertyChanged(nameof(DgvReport));
            }
        }
    }

    public class DatabaseManagementRow : NotifyBase
    {
        private string _dbName;
        private string _dbAutoName;

        public string DbName
        {
            get => _dbName;
            set
            {
                _dbName = value;
                RaisePropertyChanged(nameof(DbName));
            }
        }

        public string DbAutoName
        {
            get => _dbAutoName;
            set
            {
                _dbAutoName = value;
                RaisePropertyChanged(nameof(DbAutoName));
            }
        }
    }

    public class YearPreviewRow : NotifyBase
    {
        private string _dbname;

        public string Dbname
        {
            get => _dbname;
            set
            {
                _dbname = value;
                RaisePropertyChanged(nameof(Dbname));
            }
        }
    }

    #endregion
}