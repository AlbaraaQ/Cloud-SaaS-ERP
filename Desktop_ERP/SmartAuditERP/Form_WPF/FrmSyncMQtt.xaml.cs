using System;
using System.Text;
using System.Windows;
using DevExpress.Xpf.Core;
using DevExpress.Xpf.Editors;

namespace SmartAuditERP.Form_WPF
{
    public partial class FrmSyncMQtt : DevExpress.Xpf.Core.ThemedWindow
    {
        #region Fields

        private string Cond   = "";
        private object cnstr;

        #endregion

        #region Constructor

        public FrmSyncMQtt()
        {
            InitializeComponent();
            cnstr = ConnectBroker.connString;
        }

        #endregion

        #region Window Events

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            txtFromDate.DateTime = DateTime.Now.Date;
            txtToDate.DateTime   = DateTime.Now.Date;
        }

        #endregion

        #region Upload Data

        private void btnUploadData_Click(object sender, RoutedEventArgs e)
        {
            cnstr = ConnectBroker.connString;
            var Home = new Home();
            if (!Home._is_active)
                return;

            if (!ConnectBroker.CheckConnectionAndBroker())
                return;

            if (ckitems.IsChecked == true)
            {
                ConnectBroker.mqttClient.Publish(ConnectBroker.ClientCode + "Items",
                    Encoding.UTF8.GetBytes(SendData.GetItems(Cond)), 0, retain: true);
                ConnectBroker.mqttClient.Publish(ConnectBroker.ClientCode + "ItemsUnit",
                    Encoding.UTF8.GetBytes(SendData.GetItemsUnit(Cond)), 0, retain: true);
                ConnectBroker.mqttClient.Publish(ConnectBroker.ClientCode + "ItemPrices",
                    Encoding.UTF8.GetBytes(SendData.GetItemPrices(Cond)), 0, retain: true);
                ConnectBroker.mqttClient.Publish(ConnectBroker.ClientCode + "ItemComponents",
                    Encoding.UTF8.GetBytes(SendData.GetItemComponents(Cond)), 0, retain: true);
                ConnectBroker.mqttClient.Publish(ConnectBroker.ClientCode + "ItemSerialNo",
                    Encoding.UTF8.GetBytes(SendData.GetItemComponents(Cond)), 0, retain: true);
                ConnectBroker.mqttClient.Publish(ConnectBroker.ClientCode + "ItemBarcode",
                    Encoding.UTF8.GetBytes(SendData.GetItemBarcode(Cond)), 0, retain: true);
            }

            if (ckgroups.IsChecked == true)
                ConnectBroker.mqttClient.Publish(ConnectBroker.ClientCode + "ItemsCategory",
                    Encoding.UTF8.GetBytes(SendData.GetItemsCategory(Cond)), 0, retain: true);

            if (ckclients.IsChecked == true)
            {
                ConnectBroker.mqttClient.Publish(ConnectBroker.ClientCode + "Customers",
                    Encoding.UTF8.GetBytes(SendData.GetCustomers(Cond)), 0, retain: true);
                ConnectBroker.mqttClient.Publish(ConnectBroker.ClientCode + "AccountCust",
                    Encoding.UTF8.GetBytes(SendData.GetAccountCust(Cond)), 0, retain: true);
            }

            if (ckunits.IsChecked == true)
                ConnectBroker.mqttClient.Publish(ConnectBroker.ClientCode + "Units",
                    Encoding.UTF8.GetBytes(SendData.GetUnit(Cond)), 0, retain: true);

            if (ckaccounts.IsChecked == true)
                ConnectBroker.mqttClient.Publish(ConnectBroker.ClientCode + "AccountIndex",
                    Encoding.UTF8.GetBytes(SendData.GetAccountIndex(Cond)), 0, retain: true);

            if (ckEmp.IsChecked == true)
            {
                ConnectBroker.mqttClient.Publish(ConnectBroker.ClientCode + "Employee",
                    Encoding.UTF8.GetBytes(SendData.GeteEmployee(Cond)), 0, retain: true);
                ConnectBroker.mqttClient.Publish(ConnectBroker.ClientCode + "AccountEmp",
                    Encoding.UTF8.GetBytes(SendData.GetAccountEmp(Cond)), 0, retain: true);
                ConnectBroker.mqttClient.Publish(ConnectBroker.ClientCode + "EmpBranches",
                    Encoding.UTF8.GetBytes(SendData.GetEmpBranch(Cond)), 0, retain: true);
            }

            if (chkUser.IsChecked == true)
            {
                ConnectBroker.mqttClient.Publish(ConnectBroker.ClientCode + "Users",
                    Encoding.UTF8.GetBytes(SendData.GetUsers(Cond)), 0, retain: true);
                ConnectBroker.mqttClient.Publish(ConnectBroker.ClientCode + "UserPermissions",
                    Encoding.UTF8.GetBytes(SendData.GetUserPermissions(Cond)), 0, retain: true);
                ConnectBroker.mqttClient.Publish(ConnectBroker.ClientCode + "OperationPermission",
                    Encoding.UTF8.GetBytes(SendData.GetOperationPermission(Cond)), 0, retain: true);
            }

            if (chkOffers.IsChecked == true)
            {
                ConnectBroker.mqttClient.Publish(ConnectBroker.ClientCode + "Offer",
                    Encoding.UTF8.GetBytes(SendData.GetOffers(Cond)), 0, retain: true);
                ConnectBroker.mqttClient.Publish(ConnectBroker.ClientCode + "OfferClient",
                    Encoding.UTF8.GetBytes(SendData.GetOfferForClientList()), 0, retain: true);
                ConnectBroker.mqttClient.Publish(ConnectBroker.ClientCode + "OfferItem",
                    Encoding.UTF8.GetBytes(SendData.GetOfferItems()), 0, retain: true);
            }

            if (ckboxes.IsChecked == true)
            {
                ConnectBroker.mqttClient.Publish(ConnectBroker.ClientCode + "Stock",
                    Encoding.UTF8.GetBytes(SendData.GetStocks()), 0, retain: true);
                ConnectBroker.mqttClient.Publish(ConnectBroker.ClientCode + "StockEmp",
                    Encoding.UTF8.GetBytes(SendData.GetStockEmps()), 0, retain: true);
            }

            if (ckInventories.IsChecked == true)
            {
                ConnectBroker.mqttClient.Publish(ConnectBroker.ClientCode + "Safe",
                    Encoding.UTF8.GetBytes(SendData.GetSafes()), 0, retain: true);
                ConnectBroker.mqttClient.Publish(ConnectBroker.ClientCode + "SafeEmp",
                    Encoding.UTF8.GetBytes(SendData.GetSafesEmp()), 0, retain: true);
            }

            if (ckBranches.IsChecked == true)
                ConnectBroker.mqttClient.Publish(ConnectBroker.ClientCode + "Branches",
                    Encoding.UTF8.GetBytes(SendData.GetBranches()), 0, retain: true);

            if (ckbanks.IsChecked == true)
                ConnectBroker.mqttClient.Publish(ConnectBroker.ClientCode + "Banks",
                    Encoding.UTF8.GetBytes(SendData.GetBanks()), 0, retain: true);

            if (ChkAllsend.IsChecked == true)
                SendStoredInv();

            DXMessageBox.Show("تم إرسال البيانات بنجاح", "",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        #endregion

        #region Download Data

        private void btnDownloadData_Click(object sender, RoutedEventArgs e)
        {
            cnstr = ConnectBroker.connString;
            var Home = new Home();
            if (!Home._is_active)
                return;

            if (!ConnectBroker.CheckConnectionAndBroker())
                return;

            if (ckitems.IsChecked == true)
                ReceivedData.LoadReceived(cnstr.ToString());
            if (ckgroups.IsChecked == true)
                ReceivedData.LoadReceived(cnstr.ToString());
            if (ckclients.IsChecked == true)
                ReceivedData.LoadReceived(cnstr.ToString());
            if (ckunits.IsChecked == true)
                ReceivedData.LoadReceived(cnstr.ToString());
            if (ckaccounts.IsChecked == true)
                ReceivedData.LoadReceived(cnstr.ToString());
            if (ckEmp.IsChecked == true)
                ReceivedData.LoadReceived(cnstr.ToString());
            if (chkUser.IsChecked == true)
                ReceivedData.LoadReceived(cnstr.ToString());
            if (chkOffers.IsChecked == true)
                ReceivedData.LoadReceived(cnstr.ToString());
            if (ckboxes.IsChecked == true)
                ReceivedData.LoadReceived(cnstr.ToString());
            if (ckInventories.IsChecked == true)
                ReceivedData.LoadReceived(cnstr.ToString());
            if (ckBranches.IsChecked == true)
                ReceivedData.LoadReceived(cnstr.ToString());
            if (ckbanks.IsChecked == true)
                ReceivedData.LoadReceived(cnstr.ToString());

            DXMessageBox.Show("تم استلام البيانات بنجاح", "",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        #endregion

        #region ChkAllsend Toggle

        private void ChkAllsend_CheckedChanged(object sender, RoutedEventArgs e)
        {
            bool isVisible = ChkAllsend.IsChecked == true;
            pnlDateRange.Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
        }

        #endregion

        #region Send Stored Invoices

        public void SendStoredInv()
        {
            if (!ConnectBroker.CheckConnectionAndBroker())
                return;

            string fromDate  = txtFromDate.DateTime.ToShortDateString();
            string toDate    = txtToDate.DateTime.ToShortDateString();

            string cond      = $"WHERE Date>=N'{fromDate}' AND Date<=N'{toDate}'";
            string invGlobalID = $"WHERE TRY_CAST(InvGlobalID AS nvarchar) IN (" +
                                 $"SELECT InvGlobalID FROM inv " +
                                 $"WHERE Date>=N'{fromDate}' AND Date<=N'{toDate}')";
            string cond2     = $"WHERE Date>=N'{fromDate}' AND Date<=N'{toDate}'";
            string cond3     = $"WHERE TRY_CAST(EntryGLobalID AS nvarchar) IN (" +
                               $"SELECT GlobalID FROM Entry " +
                               $"WHERE Date>=N'{fromDate}' AND Date<=N'{toDate}')";

            SendData.SendDataa("Inv",          "1", SendData.GetInv(cond));
            SendData.SendDataa("InvSub",       "1", SendData.GetInvSub(invGlobalID));
            SendData.SendDataa("entry",        "1", SendData.GetEntryData(cond2));
            SendData.SendDataa("entryDetails", "1", SendData.GetEntrySubData(cond3));
            SendData.SendDataa("Receipts",     "1", SendData.GetReceipts(cond3));
        }

        #endregion
    }
}