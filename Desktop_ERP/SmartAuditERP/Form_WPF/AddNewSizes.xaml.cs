using Microsoft.VisualBasic;
using Microsoft.VisualBasic.CompilerServices;
using SmartAuditERP.Properties;
using System;
using System.Data;
using System.Data.SqlClient;
using System.Drawing.Imaging;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using ComboBox = System.Windows.Controls.ComboBox;
using MessageBox = System.Windows.MessageBox;

namespace SmartAuditERP.Form_WPF
{
    public partial class AddNewSizes : Window
    {
        #region Private Fields

        private SqlConnection conn;
        private int lastCurrencyId;
        public double res;
        private int numOfPoket;
        public int forlastcode;

        #endregion

        #region Constructor

        public AddNewSizes()
        {
            InitializeComponent();

            this.conn = MainClass.ConnObj();
            this.lastCurrencyId = 1;
            this.res = 0.0;
            this.numOfPoket = 0;
            this.forlastcode = -1;

            this.Loaded += AddNewSizes_Load;
        }

        #endregion

        #region Window Events

        private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        private void Button11_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Button12_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        #endregion

        #region Form Load Events

        private void AddNewSizes_Load(object sender, RoutedEventArgs e)
        {
            this.cmbCharShape.SelectedIndex = 0;
            this.cmbHandShape.SelectedIndex = 0;
            this.cmbNeckShape.SelectedIndex = 0;
            this.LoadNextNo();
            this.load_Customers();
            this.lbDate.Text = DateTime.Now.ToShortDateString();

            if (this.forlastcode == -1)
            {
                this.lblCode.Text = Conversions.ToString(this.getLastCode());
            }

            this.stateCB.SelectedIndex = 0;
        }

        #endregion

        #region Data Loading Methods

        private void load_Customers()
        {
            try
            {
                SqlDataAdapter customersAdapter = new SqlDataAdapter(
                    "select id,name from Customers where type= 1 order by id",
                    this.conn);
                DataTable customersTable = new DataTable();
                customersAdapter.Fill(customersTable);

                this.txtName.ItemsSource = customersTable.DefaultView;
                this.txtName.DisplayMemberPath = "name";
                this.txtName.SelectedValuePath = "id";
                this.txtName.SelectedIndex = -1;
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ في تحميل العملاء: " + ex.Message,
                    "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadNextNo()
        {
            try
            {
                SqlDataAdapter itemsAdapter = new SqlDataAdapter(
                    "select max(id) from Items where is_deleted=0",
                    this.conn);
                DataTable itemsTable = new DataTable();
                itemsAdapter.Fill(itemsTable);

                if (itemsTable.Rows.Count > 0 &&
                    Operators.CompareString(itemsTable.Rows[0][0].ToString(), "", TextCompare: false) != 0)
                {
                    this.lastCurrencyId = checked((int)Math.Round(
                        Convert.ToDouble(Operators.ConcatenateObject("", itemsTable.Rows[0][0])) + 1.0));
                }
            }
            catch (Exception)
            {
            }
        }

        #endregion

        #region Helper Methods

        private object getLastCode()
        {
            this.conn.Close();
            SqlCommand getMaxCodeCommand = new SqlCommand(
                "SELECT MAX(code) FROM Inv_Tailor",
                this.conn);
            object result;

            try
            {
                this.conn.Open();
                int maxCode = Conversions.ToInteger(getMaxCodeCommand.ExecuteScalar());
                this.conn.Close();
                result = checked(maxCode + 1);
            }
            catch (Exception)
            {
                this.conn.Close();
                result = 1;
            }

            return result;
        }

        public void UpdatePaidOnly()
        {
            try
            {
                SqlDataAdapter paidAdapter = new SqlDataAdapter(
                    "select paid from Inv_Sub_Tailor where inv_code =" + this.lblCode.Text,
                    this.conn);
                DataTable paidTable = new DataTable();
                paidAdapter.Fill(paidTable);

                if (paidTable.Rows.Count > 0)
                {
                    this.txtPaid.Text = Conversions.ToString(paidTable.Rows[0]["paid"]);
                    this.txtRest.Text = Conversions.ToString(
                        Convert.ToDouble(this.txtPriceWithTax.Text) -
                        Convert.ToDouble(paidTable.Rows[0]["paid"]));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ في تحديث المدفوعات: " + ex.Message,
                    "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Button Click Events - Save

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            if (Operators.CompareString(this.txtPrice_sale.Text, "0", TextCompare: false) != 0)
            {
                if (this.txtName.SelectedIndex != -1)
                {
                    this.save();
                }
                else
                {
                    MessageBox.Show("برجاء اختيار العميل", "تنبيه",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            else
            {
                MessageBox.Show("يرجي إدخال السعر", "تنبيه",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void save()
        {
            string invoiceCode = Conversions.ToString(this.getLastCode());

            try
            {
                SqlDataAdapter checkAdapter = new SqlDataAdapter(
                    "select * from Inv_Tailor where code = " + this.lblCode.Text,
                    this.conn);
                DataTable existingData = new DataTable();
                checkAdapter.Fill(existingData);

                if (existingData.Rows.Count == 0)
                {
                    // Insert new record
                    InsertNewTailorInvoice(invoiceCode);
                    InsertNewTailorInvoiceSub(invoiceCode);

                    this.btnMakeInv.IsEnabled = true;
                    this.btnRinv.IsEnabled = true;
                    this.btnPrint.IsEnabled = true;
                    this.btnRecievePaid.IsEnabled = true;

                    MessageBox.Show("تم الحفظ بنجاح", "نجاح",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    // Update existing record
                    UpdateTailorInvoice();
                    UpdateTailorInvoiceSub();

                    MessageBox.Show("تم التعديل بنجاح", "نجاح",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ أثناء الحفظ" + Environment.NewLine +
                    "تفاصيل الخطأ: " + ex.Message,
                    "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void InsertNewTailorInvoice(string code)
        {
            SqlCommand insertCommand = new SqlCommand(
                "insert into Inv_Tailor(code,name, quantity, phone_num,Date,sale_price, state, type,bill) " +
                "values(@code,@name,@quantity,@phone_num,@Date,@sale_price, @state, @type,0)",
                this.conn);

            insertCommand.Parameters.AddWithValue("@code", code);
            insertCommand.Parameters.AddWithValue("@name",
                ((DataRowView)this.txtName.SelectedItem)["name"].ToString());
            insertCommand.Parameters.Add("@quantity", SqlDbType.NVarChar).Value = this.txtQuantity.Text;
            insertCommand.Parameters.Add("@phone_num", SqlDbType.NVarChar).Value = this.txtPhoneNum.Text;
            insertCommand.Parameters.Add("@Date", SqlDbType.DateTime).Value = DateTime.Now;
            insertCommand.Parameters.Add("@sale_price", SqlDbType.Float).Value = this.txtPrice_sale.Text;
            insertCommand.Parameters.Add("@state", SqlDbType.Int).Value = this.stateCB.SelectedIndex;
            insertCommand.Parameters.Add("@type", SqlDbType.Int).Value = this.typeCB.SelectedIndex;

            this.conn.Open();
            insertCommand.ExecuteNonQuery();
            this.conn.Close();
        }

        private void InsertNewTailorInvoiceSub(string code)
        {
            SqlCommand insertSubCommand = new SqlCommand(
                "insert into Inv_Sub_Tailor(inv_code, height1, height2, shoulder, hand1, hand2, neck1, neck2, " +
                "expand1, expand2,expand3, expandHand1, expandHand2,characterShape,handShape,handShape1,handShape2," +
                "neckShape,neckShape1,neckShape2,pocketShape, pocketPen, trangle, square, handShapecb, nickShape, " +
                "PocketCB, pocketCheck, shoulderCB, handDownTB, pho1, pho2, save1, save2, down, title, disappear," +
                "txtCahrSize,txtPoketLong,txtPoketSize1,txtPoketSize2,paid)" +
                "values(@inv_code,@height1,@height2,@shoulder,@hand1,@hand2,@neck1,@neck2,@expand1,@expand2,@expand3," +
                "@expandHand1,@expandHand2,@characterShape,@handShape,@handShape1,@handShape2,@neckShape,@neckShape1," +
                "@neckShape2,@pocketShape, @pocketPen, @trangle, @square, @handShapecb, @nickShape, @PocketCB, " +
                "@pocketCheck, @shoulderCB, @handDownTB, @pho1, @pho2, @save1, @save2, @down, @title, @disappear," +
                "@txtCahrSize,@txtPoketLong,@txtPoketSize1,@txtPoketSize2,0)",
                this.conn);

            AddSubCommandParameters(insertSubCommand, code);

            this.conn.Open();
            insertSubCommand.ExecuteNonQuery();
            this.conn.Close();
        }

        private void UpdateTailorInvoice()
        {
            SqlCommand updateCommand = new SqlCommand(
                "update Inv_Tailor set name = @name, quantity = @quantity, phone_num = @phone_num," +
                "Date = @Date, sale_price = @sale_price, state = @state, type = @type " +
                "where code =" + this.lblCode.Text,
                this.conn);

            updateCommand.Parameters.AddWithValue("@name",
                ((DataRowView)this.txtName.SelectedItem)["name"].ToString());
            updateCommand.Parameters.Add("@quantity", SqlDbType.NVarChar).Value = this.txtQuantity.Text;
            updateCommand.Parameters.Add("@phone_num", SqlDbType.NVarChar).Value = this.txtPhoneNum.Text;
            updateCommand.Parameters.Add("@Date", SqlDbType.DateTime).Value = DateTime.Now;
            updateCommand.Parameters.Add("@sale_price", SqlDbType.Float).Value = this.txtPrice_sale.Text;
            updateCommand.Parameters.Add("@state", SqlDbType.Int).Value = this.stateCB.SelectedIndex;
            updateCommand.Parameters.Add("@type", SqlDbType.Int).Value = this.typeCB.SelectedIndex;

            this.conn.Open();
            updateCommand.ExecuteNonQuery();
            this.conn.Close();
        }

        private void UpdateTailorInvoiceSub()
        {
            SqlCommand updateSubCommand = new SqlCommand(
                "update Inv_Sub_Tailor set height1 = @height1, height2 = @height2, shoulder = @shoulder, " +
                "hand1 = @hand1, hand2= @hand2, neck1 = @neck1, neck2 = @neck2, expand1 = @expand1, " +
                "expand2 = @expand2, expand3 = @expand3, expandHand1 = @expandHand1, expandHand2 = @expandHand2, " +
                "characterShape = @characterShape, handShape = @handShape, handShape1 = @handShape1, " +
                "handShape2 = @handShape2, neckShape = @neckShape, neckShape1 = @neckShape1, neckShape2 = @neckShape2, " +
                "pocketShape = @pocketShape, pocketPen = @pocketPen, trangle = @trangle, square = @square, " +
                "handShapecb = @handShapecb, nickShape = @nickShape, PocketCB = @PocketCB, pocketCheck = @pocketCheck, " +
                "shoulderCB = @shoulderCB, handDownTB = @handDownTB, pho1 = @pho1, pho2 = @pho2, save1 = @save1, " +
                "save2 = @save2, down = @down, title = @title, txtCahrSize = @txtCahrSize, txtPoketLong = @txtPoketLong, " +
                "txtPoketSize1 = @txtPoketSize1, txtPoketSize2 = @txtPoketSize2 " +
                "where inv_code = " + this.lblCode.Text,
                this.conn);

            AddSubCommandParameters(updateSubCommand, this.lblCode.Text);

            this.conn.Open();
            updateSubCommand.ExecuteNonQuery();
            this.conn.Close();
        }

        private void AddSubCommandParameters(SqlCommand command, string invoiceCode)
        {
            command.Parameters.Add("@inv_code", SqlDbType.Int).Value = invoiceCode;
            command.Parameters.Add("@height1", SqlDbType.Float).Value = this.txtheight1.Text;
            command.Parameters.Add("@height2", SqlDbType.Float).Value = this.txtheight2.Text;
            command.Parameters.Add("@shoulder", SqlDbType.Float).Value = this.txtshoulder.Text;
            command.Parameters.Add("@hand1", SqlDbType.Float).Value = this.txthand1.Text;
            command.Parameters.Add("@hand2", SqlDbType.Float).Value = this.txthand2.Text;
            command.Parameters.Add("@neck1", SqlDbType.Float).Value = this.txtneck1.Text;
            command.Parameters.Add("@neck2", SqlDbType.Float).Value = this.txtneck2.Text;
            command.Parameters.Add("@expand1", SqlDbType.Float).Value = this.txtexpand1.Text;
            command.Parameters.Add("@expand2", SqlDbType.Float).Value = this.txtexpand2.Text;
            command.Parameters.Add("@expand3", SqlDbType.Float).Value = this.txtexpand3.Text;
            command.Parameters.Add("@pocketShape", SqlDbType.Float).Value = this.numOfPoket;
            command.Parameters.Add("@expandHand1", SqlDbType.Float).Value = this.txtexpandHand1.Text;
            command.Parameters.Add("@expandHand2", SqlDbType.Float).Value = this.txtexpandHand2.Text;
            command.Parameters.AddWithValue("@characterShape",
                ((ComboBoxItem)this.cmbCharShape.SelectedItem).Content.ToString());
            command.Parameters.AddWithValue("@handShape",
                ((ComboBoxItem)this.cmbHandShape.SelectedItem).Content.ToString());
            command.Parameters.Add("@handShape1", SqlDbType.Float).Value = this.txtHandShape1.Text;
            command.Parameters.Add("@handShape2", SqlDbType.Float).Value = this.txtHandShape2.Text;
            command.Parameters.AddWithValue("@neckShape",
                ((ComboBoxItem)this.cmbNeckShape.SelectedItem).Content.ToString());
            command.Parameters.Add("@neckShape1", SqlDbType.Float).Value = this.txtNeckShape1.Text;
            command.Parameters.Add("@neckShape2", SqlDbType.Float).Value = this.txtNeckShape2.Text;
            command.Parameters.Add("@pocketPen", SqlDbType.Bit).Value = this.pocketPenCB.IsChecked;
            command.Parameters.Add("@trangle", SqlDbType.Bit).Value = this.tranRB.IsChecked;
            command.Parameters.Add("@square", SqlDbType.Bit).Value = this.squareRB.IsChecked;
            command.Parameters.Add("@handShapecb", SqlDbType.Int).Value = this.handShapeCB.SelectedIndex;
            command.Parameters.Add("@nickShape", SqlDbType.Int).Value = this.nickShapeCB.SelectedIndex;
            command.Parameters.Add("@PocketCB", SqlDbType.Int).Value = this.PocketCB.SelectedIndex;
            command.Parameters.Add("@pocketCheck", SqlDbType.VarChar).Value = this.pocketCheck.Text;
            command.Parameters.Add("@shoulderCB", SqlDbType.Int).Value = this.shoulderCB.SelectedIndex;
            command.Parameters.Add("@handDownTB", SqlDbType.VarChar).Value = this.handDownTB.Text;
            command.Parameters.Add("@pho1", SqlDbType.VarChar).Value = this.pho1.Text;
            command.Parameters.Add("@pho2", SqlDbType.VarChar).Value = this.pho2.Text;
            command.Parameters.Add("@save1", SqlDbType.VarChar).Value = this.save1.Text;
            command.Parameters.Add("@save2", SqlDbType.VarChar).Value = this.save2.Text;
            command.Parameters.Add("@down", SqlDbType.VarChar).Value = this.down.Text;
            command.Parameters.Add("@title", SqlDbType.VarChar).Value = this.title.Text;
            command.Parameters.Add("@disappear", SqlDbType.Bit).Value = this.disappearCB.IsChecked;
            command.Parameters.AddWithValue("@txtCahrSize", this.txtCahrSize.Text);
            command.Parameters.AddWithValue("@txtPoketLong", this.txtPoketLong.Text);
            command.Parameters.AddWithValue("@txtPoketSize1", this.txtPoketSize1.Text);
            command.Parameters.AddWithValue("@txtPoketSize2", this.txtPoketSize2.Text);
        }

        #endregion

        #region Button Click Events - Other Actions

        private void btnMakeInv_Click(object sender, RoutedEventArgs e)
        {
            this.CreateInvoice();
        }

        private void CreateInvoice()
        {
            try
            {
                frmPOS frmPOS2 = new frmPOS();
                frmPOS2.ISTailor = true;
                frmPOS2.InvType = 2;
                frmPOS2.Tag = "SalePurch2";
                frmPOS2.Show();
                frmPOS2.WindowState = WindowState.Maximized;
                MainClass.ApplyPermissionToForm(frmPOS2);
                frmPOS2.ProcType = 1;
                frmPOS2.txtVal1.Text = "1";

                this.btnMakeInv.IsEnabled = false;
                frmPOS2.txtTotVAT.Text = Conversions.ToString(Convert.ToDouble(this.txtPrice_sale.Text) * 0.05);
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ في إنشاء الفاتورة: " + ex.Message,
                    "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnRinv_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("تكرار المقاس ؟", "تأكيد",
                MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                this.lblCode.Text = Conversions.ToString(this.getLastCode());
                this.btnPocket1.Visibility = Visibility.Visible;
                this.btnPocket2.Visibility = Visibility.Visible;
                this.btnPocket3.Visibility = Visibility.Visible;
                this.btnPocket4.Visibility = Visibility.Visible;
                this.btnPocket5.Visibility = Visibility.Visible;
                this.btnPocket6.Visibility = Visibility.Visible;
                this.btnMakeInv.IsEnabled = false;
            }
        }

        private void Button3_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("المغادرة بدون حفظ ؟", "تأكيد",
                MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                ClearForm();
            }
        }

        private void ClearForm()
        {
            this.lblCode.Text = "0";
            this.txtheight1.Text = "0";
            this.txtheight2.Text = "0";
            this.txtshoulder.Text = "0";
            this.txthand1.Text = "0";
            this.txthand2.Text = "0";
            this.txtneck1.Text = "0";
            this.txtneck2.Text = "0";
            this.txtexpand1.Text = "0";
            this.txtexpand2.Text = "0";
            this.txtexpand3.Text = "0";
            this.txtexpandHand1.Text = "0";
            this.txtexpandHand2.Text = "0";
            this.txtHandShape1.Text = "0";
            this.txtHandShape2.Text = "0";
            this.txtNeckShape1.Text = "0";
            this.txtNeckShape2.Text = "0";
            this.pocketPenCB.IsChecked = false;
            this.tranRB.IsChecked = false;
            this.squareRB.IsChecked = false;
            this.handShapeCB.SelectedIndex = 0;
            this.nickShapeCB.SelectedIndex = 0;
            this.PocketCB.SelectedIndex = 0;
            this.pocketCheck.Text = "0";
            this.shoulderCB.SelectedIndex = 0;
            this.handDownTB.Text = "0";
            this.pho1.Text = "0";
            this.pho2.Text = "0";
            this.down.Text = "0";
            this.title.Text = "";
            this.txtCahrSize.Text = "0";
            this.txtPoketLong.Text = "0";
            this.txtPoketSize1.Text = "0";
            this.txtPoketSize2.Text = "0";
            this.txtQuantity.Text = "1";
            this.txtPrice.Text = "0";
            this.save1.Text = "0";
            this.save2.Text = "0";
            this.txtName.Text = "";
            this.txtPhoneNum.Text = "";
            this.btnPocket1.Visibility = Visibility.Visible;
            this.btnPocket2.Visibility = Visibility.Visible;
            this.btnPocket3.Visibility = Visibility.Visible;
            this.btnPocket4.Visibility = Visibility.Visible;
            this.btnPocket5.Visibility = Visibility.Visible;
            this.btnPocket6.Visibility = Visibility.Visible;
            this.lblCode.Text = Conversions.ToString(this.getLastCode());
            this.btnMakeInv.IsEnabled = false;
        }

        private void btnPrint_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                frmPrintBill printForm = new frmPrintBill();

                // Set all the values
                printForm.lblCode.Text = this.lblCode.Text;
                printForm.txtName.Text = this.txtName.Text;
                printForm.txtPhoneNum.Text = this.txtPhoneNum.Text;
                printForm.typeCB.Text = ((ComboBoxItem)this.typeCB.SelectedItem)?.Content.ToString();
                printForm.txtQuantity.Text = this.txtQuantity.Text;
                printForm.txtPrice.Text = this.txtPrice.Text;
                printForm.txtPrice_sale.Text = this.txtPriceWithTax.Text;
                printForm.txtheight1.Text = this.txtheight1.Text;
                printForm.txtheight2.Text = this.txtheight2.Text;
                printForm.txtshoulder.Text = this.txtshoulder.Text;
                printForm.txthand1.Text = this.txthand1.Text;
                printForm.txthand2.Text = this.txthand2.Text;
                printForm.txtneck1.Text = this.txtneck1.Text;
                printForm.txtneck2.Text = this.txtneck2.Text;
                printForm.txtexpand1.Text = this.txtexpand1.Text;
                printForm.txtexpand2.Text = this.txtexpand2.Text;
                printForm.txtexpand3.Text = this.txtexpand3.Text;
                printForm.txtexpandHand1.Text = this.txtexpandHand1.Text;
                printForm.txtexpandHand2.Text = this.txtexpandHand2.Text;
                printForm.cmbNeckShape.Text = ((ComboBoxItem)this.cmbNeckShape.SelectedItem)?.Content.ToString();
                printForm.nickShapeCB.Text = ((ComboBoxItem)this.nickShapeCB.SelectedItem)?.Content.ToString();
                printForm.txtNeckShape1.Text = this.txtNeckShape1.Text;
                printForm.txtNeckShape2.Text = this.txtNeckShape2.Text;
                printForm.cmbHandShape.Text = ((ComboBoxItem)this.cmbHandShape.SelectedItem)?.Content.ToString();
                printForm.handShapeCB.Text = ((ComboBoxItem)this.handShapeCB.SelectedItem)?.Content.ToString();
                printForm.txtHandShape1.Text = this.txtHandShape1.Text;
                printForm.shoulderCB.Text = ((ComboBoxItem)this.shoulderCB.SelectedItem)?.Content.ToString();
                printForm.tranRB.IsChecked = this.tranRB.IsChecked == true;
                printForm.squareRB.IsChecked = this.squareRB.IsChecked == true;
                printForm.name2.Text = this.txtName.Text;
                printForm.phone2.Text = this.txtPhoneNum.Text;
                printForm.txtQuantity2.Text = this.txtQuantity.Text;
                printForm.txtPriceWithTax.Text = this.txtPriceWithTax.Text;
                printForm.txtPaid.Text = this.txtPaid.Text;
                printForm.txtRest.Text = this.txtRest.Text;
                printForm.lblCode2.Text = this.lblCode.Text;
                printForm.title.Text = this.title.Text;
                printForm.txtPoketLong.Text = this.txtPoketLong.Text;
                printForm.txtPoketSize1.Text = this.txtPoketSize1.Text;
                printForm.txtPoketSize2.Text = this.txtPoketSize2.Text;
                printForm.txtCahrSize.Text = this.txtCahrSize.Text;
                printForm.disappearCB.IsChecked = this.disappearCB.IsChecked == true;

                // Load pocket image
                LoadPocketImage(printForm);

                printForm.txtHandShape1.Text = this.txtHandShape1.Text;
                printForm.txtHandShape2.Text = this.txtHandShape2.Text;
                printForm.cmbCharShape.Text = ((ComboBoxItem)this.cmbCharShape.SelectedItem)?.Content.ToString();
                printForm.PocketCB.Text = ((ComboBoxItem)this.PocketCB.SelectedItem)?.Content.ToString();
                printForm.pocketCheck.Text = this.pocketCheck.Text;
                printForm.handDownTB.Text = this.handDownTB.Text;
                printForm.pho1.Text = this.pho1.Text;
                printForm.pho2.Text = this.pho2.Text;
                printForm.save1.Text = this.save1.Text;
                printForm.save2.Text = this.save2.Text;
                printForm.down.Text = this.down.Text;
                printForm.pocketPenCB.IsChecked = this.pocketPenCB.IsChecked == true;

                printForm.Show();
                printForm.Activate();
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ في الطباعة: " + ex.Message,
                    "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadPocketImage(frmPrintBill printForm)
        {
            try
            {
                // ─── استعلام آمن بـ Parameters لتجنب SQL Injection ───
                using (SqlDataAdapter pocketAdapter = new SqlDataAdapter(
                    "SELECT pocketShape FROM Inv_Sub_Tailor WHERE inv_code = @invCode",
                    this.conn))
                {
                    pocketAdapter.SelectCommand.Parameters.AddWithValue(
                        "@invCode", this.lblCode.Text.Trim());

                    DataTable pocketTable = new DataTable();
                    pocketAdapter.Fill(pocketTable);

                    if (pocketTable.Rows.Count == 0)
                        return;

                    object pocketValue = pocketTable.Rows[0]["pocketShape"];

                    // ─── تجاهل القيم الفارغة ───
                    if (pocketValue == null || pocketValue == DBNull.Value)
                        return;

                    // ─── تحويل القيمة إلى int بأمان ───
                    if (!int.TryParse(pocketValue.ToString(), out int pocketShape))
                        return;

                    // ─── تحديد الصورة المناسبة من Resources ───
                    System.Drawing.Bitmap selectedBitmap = pocketShape switch
                    {
                        1 => Properties.Resources.pic1,
                        2 => Properties.Resources.pic2,
                        3 => Properties.Resources.pic3,
                        4 => Properties.Resources.pic4,
                        5 => Properties.Resources.pic5,
                        6 => Properties.Resources.pic6,
                        _ => null
                    };

                    if (selectedBitmap == null)
                        return;

                    // ─── تحويل Bitmap (WinForms) → BitmapImage (WPF) ───
                    BitmapImage wpfImage = ConvertBitmapToBitmapImage(selectedBitmap);

                    // ─── تعيين الصورة على عنصر Image في WPF ───
                    printForm.PictureBox1.Source = wpfImage;
                }
            }
            catch (Exception ex)
            {
                // ─── إظهار الخطأ بدل إخفائه ───
                System.Windows.MessageBox.Show(
                    $"خطأ أثناء تحميل صورة الجيب:\n{ex.Message}",
                    "خطأ",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Warning);
            }
        }

        /// <summary>
        /// تحويل System.Drawing.Bitmap إلى BitmapImage القابل للاستخدام في WPF
        /// </summary>
        private BitmapImage ConvertBitmapToBitmapImage(System.Drawing.Bitmap bitmap)
        {
            using (MemoryStream memoryStream = new MemoryStream())
            {
                // ─── حفظ الصورة في الذاكرة بصيغة PNG ───
                bitmap.Save(memoryStream, ImageFormat.Png);
                memoryStream.Position = 0;

                BitmapImage bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();

                // ─── CacheOnLoad لضمان بقاء الصورة بعد إغلاق الـ Stream ───
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.StreamSource = memoryStream;
                bitmapImage.EndInit();
                bitmapImage.Freeze(); // ─── تحسين الأداء في WPF ───

                return bitmapImage;
            }
        }

        private void btnRecievePaid_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                frmSandQ paymentForm = new frmSandQ();
                paymentForm.ISTailor = true;

                paymentForm.Loaded += (loadSender, loadArgs) =>
                {
                    paymentForm.cmbSalesMan.SelectedIndex =
                        FindComboBoxIndexByText(paymentForm.cmbSalesMan, MainClass.UserName);

                    paymentForm.cmbClients.SelectedIndex =
                        FindComboBoxIndexByText(paymentForm.cmbClients, this.txtName.Text.Trim());

                    paymentForm.txtRequest.Text = this.txtRest.Text;
                    paymentForm.txtNotes.Text = $"تم استلام دفعة من عملية رقم {this.lblCode.Text}";
                };

                paymentForm.Show();
                paymentForm.Activate();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"خطأ في استلام الدفعة: {ex.Message}",
                    "خطأ",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private int FindComboBoxIndexByText(ComboBox comboBox, string searchText)
        {
            if (comboBox == null || string.IsNullOrWhiteSpace(searchText))
                return -1;

            string targetText = searchText.Trim();

            for (int index = 0; index < comboBox.Items.Count; index++)
            {
                object currentItem = comboBox.Items[index];
                if (currentItem == null)
                    continue;

                // إذا كان العنصر ComboBoxItem عادي
                if (currentItem is ComboBoxItem comboBoxItem)
                {
                    string itemText = comboBoxItem.Content?.ToString()?.Trim() ?? string.Empty;

                    if (string.Equals(itemText, targetText, StringComparison.OrdinalIgnoreCase))
                        return index;
                }
                // إذا كان العنصر مربوطًا ببيانات وله DisplayMemberPath
                else if (currentItem is System.Data.DataRowView rowView)
                {
                    if (!string.IsNullOrWhiteSpace(comboBox.DisplayMemberPath) &&
                        rowView.Row.Table.Columns.Contains(comboBox.DisplayMemberPath))
                    {
                        string itemText = rowView[comboBox.DisplayMemberPath]?.ToString()?.Trim() ?? string.Empty;

                        if (string.Equals(itemText, targetText, StringComparison.OrdinalIgnoreCase))
                            return index;
                    }
                    else
                    {
                        string itemText = rowView[0]?.ToString()?.Trim() ?? string.Empty;

                        if (string.Equals(itemText, targetText, StringComparison.OrdinalIgnoreCase))
                            return index;
                    }
                }
                else
                {
                    // إذا كان العنصر object عادي ومعه DisplayMemberPath
                    if (!string.IsNullOrWhiteSpace(comboBox.DisplayMemberPath))
                    {
                        var propertyInfo = currentItem.GetType().GetProperty(comboBox.DisplayMemberPath);
                        if (propertyInfo != null)
                        {
                            string itemText = propertyInfo.GetValue(currentItem)?.ToString()?.Trim() ?? string.Empty;

                            if (string.Equals(itemText, targetText, StringComparison.OrdinalIgnoreCase))
                                return index;
                        }
                    }

                    // fallback
                    string fallbackText = currentItem.ToString()?.Trim() ?? string.Empty;
                    if (string.Equals(fallbackText, targetText, StringComparison.OrdinalIgnoreCase))
                        return index;
                }
            }

            return -1;
        }

        private void stateSaveBtN_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SqlCommand updateStateCommand = new SqlCommand(
                    "update Inv_Tailor set state = @state where code = @code",
                    this.conn);
                updateStateCommand.Parameters.AddWithValue("@code", this.lblCode.Text);
                updateStateCommand.Parameters.AddWithValue("@state", this.stateCB.SelectedIndex);

                this.conn.Open();
                updateStateCommand.ExecuteNonQuery();
                this.conn.Close();

                MessageBox.Show("تم التعديل بنجاح", "نجاح",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ في تعديل الحالة: " + ex.Message,
                    "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Button1_Click(object sender, RoutedEventArgs e)
        {
            new frmCustomers().ShowDialog();
            this.load_Customers();
        }

        private void Button10_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        #endregion

        #region Pocket Selection Methods

        private void btnPocket1_Click(object sender, RoutedEventArgs e)
        {
            SelectPocket(1, btnPocket1);
        }

        private void btnPocket2_Click(object sender, RoutedEventArgs e)
        {
            SelectPocket(2, btnPocket2);
        }

        private void btnPocket3_Click(object sender, RoutedEventArgs e)
        {
            SelectPocket(3, btnPocket3);
        }

        private void btnPocket4_Click(object sender, RoutedEventArgs e)
        {
            SelectPocket(4, btnPocket4);
        }

        private void btnPocket5_Click(object sender, RoutedEventArgs e)
        {
            SelectPocket(5, btnPocket5);
        }

        private void btnPocket6_Click(object sender, RoutedEventArgs e)
        {
            SelectPocket(6, btnPocket6);
        }

        private void SelectPocket(int pocketNumber, System.Windows.Controls.Button selectedButton)
        {
            this.numOfPoket = pocketNumber;

            // Clear all buttons
            btnPocket1.Content = "";
            btnPocket2.Content = "";
            btnPocket3.Content = "";
            btnPocket4.Content = "";
            btnPocket5.Content = "";
            btnPocket6.Content = "";

            // Set selected button
            selectedButton.Content = "تم الأختيار";
        }

        #endregion

        #region Calculation Methods

        private void txtPrice_TextChanged(object sender, TextChangedEventArgs e)
        {
            CalculateTotalPrice();
        }

        private void txtQuantity_TextChanged(object sender, TextChangedEventArgs e)
        {
            CalculateTotalPrice();
        }

        private void CalculateTotalPrice()
        {
            try
            {
                int.TryParse(this.txtQuantity.Text, out var quantity);
                double.TryParse(this.txtPrice.Text, out var price);

                double totalPrice = price * quantity;
                this.txtPrice_sale.Text = Conversions.ToString(totalPrice);

                double totalWithTax = totalPrice + (totalPrice * 0.05);
                this.txtPriceWithTax.Text = Conversions.ToString(totalWithTax);
            }
            catch (Exception)
            {
            }
        }

        #endregion

        #region Navigation Methods

        private void Button9_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                showResult(1);
            }
            catch (Exception)
            {
            }
        }

        private void Button8_Click(object sender, RoutedEventArgs e)
        {
            int.TryParse(this.lblCode.Text, out var currentCode);
            if (currentCode > 1)
            {
                showResult(checked(currentCode - 1));
            }
        }

        private void Button7_Click(object sender, RoutedEventArgs e)
        {
            int.TryParse(this.lblCode.Text, out var currentCode);
            int.TryParse(Conversions.ToString(this.getLastCode()), out var lastCode);

            if (currentCode < lastCode)
            {
                showResult(checked(currentCode + 1));
            }
        }

        private void Button5_Click(object sender, RoutedEventArgs e)
        {
            int.TryParse(Conversions.ToString(this.getLastCode()), out var lastCode);
            showResult(checked(lastCode - 1));
        }

        public void showResult(int code)
        {
            try
            {
                SqlDataAdapter mainAdapter = new SqlDataAdapter(
                    "select * FROM Inv_Tailor where code = " + Conversions.ToString(code),
                    this.conn);
                SqlDataAdapter subAdapter = new SqlDataAdapter(
                    "select * FROM Inv_Sub_Tailor where inv_code = " + Conversions.ToString(code),
                    this.conn);

                DataTable mainTable = new DataTable();
                DataTable subTable = new DataTable();

                mainAdapter.Fill(mainTable);
                subAdapter.Fill(subTable);

                this.forlastcode = 1;

                if (mainTable.Rows.Count != 0)
                {
                    LoadMainData(mainTable);
                }

                if (subTable.Rows.Count != 0)
                {
                    LoadSubData(subTable);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ في تحميل البيانات: " + ex.Message,
                    "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadMainData(DataTable mainTable)
        {
            if (Conversions.ToBoolean(Operators.NotObject(mainTable.Rows[0]["bill"])))
            {
                this.btnMakeInv.IsEnabled = true;
            }
            else
            {
                this.btnMakeInv.IsEnabled = false;
            }

            this.txtName.Text = "";
            this.txtName.SelectedIndex = -1;
            this.lblCode.Text = Conversions.ToString(mainTable.Rows[0][0]);

            string customerName = mainTable.Rows[0]["name"].ToString();
            for (int i = 0; i < this.txtName.Items.Count; i++)
            {
                DataRowView item = (DataRowView)this.txtName.Items[i];
                if (item["name"].ToString() == customerName)
                {
                    this.txtName.SelectedIndex = i;
                    break;
                }
            }

            this.txtQuantity.Text = Conversions.ToString(mainTable.Rows[0][2]);
            this.txtPhoneNum.Text = Conversions.ToString(mainTable.Rows[0][3]);
            this.txtPrice_sale.Text = Conversions.ToString(mainTable.Rows[0][5]);
            this.stateCB.SelectedIndex = Conversions.ToInteger(mainTable.Rows[0][6]);
            this.typeCB.SelectedIndex = Conversions.ToInteger(mainTable.Rows[0][7]);
        }

        private void LoadSubData(DataTable subTable)
        {
            // Load pocket visibility
            LoadPocketVisibility(subTable.Rows[0][14]);

            // Load measurements
            this.txtheight1.Text = Conversions.ToString(subTable.Rows[0][2]);
            this.txtheight2.Text = Conversions.ToString(subTable.Rows[0][3]);
            this.txtshoulder.Text = Conversions.ToString(subTable.Rows[0][4]);
            this.txthand1.Text = Conversions.ToString(subTable.Rows[0][5]);
            this.txthand2.Text = Conversions.ToString(subTable.Rows[0][6]);
            this.txtneck1.Text = Conversions.ToString(subTable.Rows[0][7]);
            this.txtneck2.Text = Conversions.ToString(subTable.Rows[0][8]);
            this.txtexpand1.Text = Conversions.ToString(subTable.Rows[0][9]);
            this.txtexpand2.Text = Conversions.ToString(subTable.Rows[0][10]);

            if (!Information.IsDBNull(subTable.Rows[0][11]))
                this.txtexpand3.Text = Conversions.ToString(subTable.Rows[0][11]);

            this.txtexpandHand1.Text = Conversions.ToString(subTable.Rows[0][12]);
            this.txtexpandHand2.Text = Conversions.ToString(subTable.Rows[0][13]);

            if (!Information.IsDBNull(subTable.Rows[0][17]))
                this.txtHandShape1.Text = Conversions.ToString(subTable.Rows[0][17]);

            if (!Information.IsDBNull(subTable.Rows[0][18]))
                this.txtHandShape2.Text = Conversions.ToString(subTable.Rows[0][18]);

            if (!Information.IsDBNull(subTable.Rows[0][20]))
                this.txtNeckShape1.Text = Conversions.ToString(subTable.Rows[0][20]);

            if (!Information.IsDBNull(subTable.Rows[0][21]))
                this.txtNeckShape2.Text = Conversions.ToString(subTable.Rows[0][21]);

            this.pocketPenCB.IsChecked = Conversions.ToBoolean(subTable.Rows[0][22]);
            this.tranRB.IsChecked = Conversions.ToBoolean(subTable.Rows[0][23]);
            this.squareRB.IsChecked = Conversions.ToBoolean(subTable.Rows[0][24]);
            this.handShapeCB.SelectedIndex = Conversions.ToInteger(subTable.Rows[0][25]);
            this.nickShapeCB.SelectedIndex = Conversions.ToInteger(subTable.Rows[0][26]);
            this.PocketCB.SelectedIndex = Conversions.ToInteger(subTable.Rows[0][27]);
            this.pocketCheck.Text = Conversions.ToString(subTable.Rows[0][28]);
            this.shoulderCB.SelectedIndex = Conversions.ToInteger(subTable.Rows[0][29]);
            this.handDownTB.Text = Conversions.ToString(subTable.Rows[0][30]);
            this.pho1.Text = Conversions.ToString(subTable.Rows[0][31]);
            this.pho2.Text = Conversions.ToString(subTable.Rows[0][32]);
            this.save1.Text = Conversions.ToString(subTable.Rows[0][33]);
            this.save2.Text = Conversions.ToString(subTable.Rows[0][34]);
            this.down.Text = Conversions.ToString(subTable.Rows[0][35]);
            this.title.Text = Conversions.ToString(subTable.Rows[0][36]);
            this.disappearCB.IsChecked = Conversions.ToBoolean(subTable.Rows[0][37]);

            // Load combo selections
            LoadComboBoxSelection(this.cmbCharShape, subTable.Rows[0][15]);
            LoadComboBoxSelection(this.cmbHandShape, subTable.Rows[0][16]);
            LoadComboBoxSelection(this.cmbNeckShape, subTable.Rows[0][19]);

            this.txtPrice.Text = Conversions.ToString(
                (double)int.Parse(this.txtPrice_sale.Text) / (double)int.Parse(this.txtQuantity.Text));

            this.txtCahrSize.Text = Conversions.ToString(subTable.Rows[0]["txtCahrSize"]);
            this.txtPoketLong.Text = Conversions.ToString(subTable.Rows[0]["txtPoketLong"]);
            this.txtPoketSize1.Text = Conversions.ToString(subTable.Rows[0]["txtPoketSize1"]);
            this.txtPoketSize2.Text = Conversions.ToString(subTable.Rows[0]["txtPoketSize2"]);

            this.btnRinv.IsEnabled = true;
            this.btnPrint.IsEnabled = true;
            this.btnRecievePaid.IsEnabled = true;

            this.UpdatePaidOnly();
        }

        private void LoadPocketVisibility(object pocketValue)
        {
            btnPocket1.Visibility = Visibility.Collapsed;
            btnPocket2.Visibility = Visibility.Collapsed;
            btnPocket3.Visibility = Visibility.Collapsed;
            btnPocket4.Visibility = Visibility.Collapsed;
            btnPocket5.Visibility = Visibility.Collapsed;
            btnPocket6.Visibility = Visibility.Collapsed;

            if (Operators.ConditionalCompareObjectEqual(pocketValue, 1, TextCompare: false))
                btnPocket1.Visibility = Visibility.Visible;
            else if (Operators.ConditionalCompareObjectEqual(pocketValue, 2, TextCompare: false))
                btnPocket2.Visibility = Visibility.Visible;
            else if (Operators.ConditionalCompareObjectEqual(pocketValue, 3, TextCompare: false))
                btnPocket3.Visibility = Visibility.Visible;
            else if (Operators.ConditionalCompareObjectEqual(pocketValue, 4, TextCompare: false))
                btnPocket4.Visibility = Visibility.Visible;
            else if (Operators.ConditionalCompareObjectEqual(pocketValue, 5, TextCompare: false))
                btnPocket5.Visibility = Visibility.Visible;
            else if (Operators.ConditionalCompareObjectEqual(pocketValue, 6, TextCompare: false))
                btnPocket6.Visibility = Visibility.Visible;
        }

        private void LoadComboBoxSelection(ComboBox combo, object value)
        {
            try
            {
                string searchValue = value.ToString();
                for (int i = 0; i < combo.Items.Count; i++)
                {
                    if (((ComboBoxItem)combo.Items[i]).Content.ToString() == searchValue)
                    {
                        combo.SelectedIndex = i;
                        break;
                    }
                }
            }
            catch (Exception)
            {
            }
        }

        #endregion

        #region TextBox Click Events (SelectAll)

        private void txtQuantity_Click(object sender, MouseButtonEventArgs e)
        {
            txtQuantity.SelectAll();
        }

        private void txtPrice_Click(object sender, MouseButtonEventArgs e)
        {
            txtPrice.SelectAll();
        }

        private void txtheight1_Click(object sender, MouseButtonEventArgs e)
        {
            txtheight1.SelectAll();
        }

        private void txtheight2_Click(object sender, MouseButtonEventArgs e)
        {
            txtheight2.SelectAll();
        }

        private void txtshoulder_Click(object sender, MouseButtonEventArgs e)
        {
            txtshoulder.SelectAll();
        }

        private void txthand1_Click(object sender, MouseButtonEventArgs e)
        {
            txthand1.SelectAll();
        }

        private void txthand2_Click(object sender, MouseButtonEventArgs e)
        {
            txthand2.SelectAll();
        }

        private void txtneck1_Click(object sender, MouseButtonEventArgs e)
        {
            txtneck1.SelectAll();
        }

        private void txtneck2_Click(object sender, MouseButtonEventArgs e)
        {
            txtneck2.SelectAll();
        }

        private void txtexpand1_Click(object sender, MouseButtonEventArgs e)
        {
            txtexpand1.SelectAll();
        }

        private void txtexpand2_Click(object sender, MouseButtonEventArgs e)
        {
            txtexpand2.SelectAll();
        }

        private void txtexpand3_Click(object sender, MouseButtonEventArgs e)
        {
            txtexpand3.SelectAll();
        }

        private void txtexpandHand1_Click(object sender, MouseButtonEventArgs e)
        {
            txtexpandHand1.SelectAll();
        }

        private void txtexpandHand2_Click(object sender, MouseButtonEventArgs e)
        {
            txtexpandHand2.SelectAll();
        }

        private void txtNeckShape1_Click(object sender, MouseButtonEventArgs e)
        {
            txtNeckShape1.SelectAll();
        }

        private void txtNeckShape2_Click(object sender, MouseButtonEventArgs e)
        {
            txtNeckShape2.SelectAll();
        }

        private void txtHandShape1_Click(object sender, MouseButtonEventArgs e)
        {
            txtHandShape1.SelectAll();
        }

        private void txtHandShape2_Click(object sender, MouseButtonEventArgs e)
        {
            txtHandShape2.SelectAll();
        }

        private void pocketCheck_Click(object sender, MouseButtonEventArgs e)
        {
            pocketCheck.SelectAll();
        }

        private void pho1_Click(object sender, MouseButtonEventArgs e)
        {
            pho1.SelectAll();
        }

        private void pho2_Click(object sender, MouseButtonEventArgs e)
        {
            pho2.SelectAll();
        }

        private void handDownTB_Click(object sender, MouseButtonEventArgs e)
        {
            handDownTB.SelectAll();
        }

        private void save1_Click(object sender, MouseButtonEventArgs e)
        {
            save1.SelectAll();
        }

        private void save2_Click(object sender, MouseButtonEventArgs e)
        {
            save2.SelectAll();
        }

        private void down_Click(object sender, MouseButtonEventArgs e)
        {
            down.SelectAll();
        }

        private void txtCahrSize_Click(object sender, MouseButtonEventArgs e)
        {
            txtCahrSize.SelectAll();
        }

        private void txtPoketLong_Click(object sender, MouseButtonEventArgs e)
        {
            txtPoketLong.SelectAll();
        }

        private void txtPoketSize1_Click(object sender, MouseButtonEventArgs e)
        {
            txtPoketSize1.SelectAll();
        }

        private void txtPoketSize2_Click(object sender, MouseButtonEventArgs e)
        {
            txtPoketSize2.SelectAll();
        }

        #endregion

        #region ComboBox Events

        private void txtName_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.txtName.SelectedIndex != -1)
            {
                try
                {
                    SqlDataAdapter phoneAdapter = new SqlDataAdapter(
                        Conversions.ToString(Operators.ConcatenateObject(
                            "select mobile from Customers where id =",
                            this.txtName.SelectedValue)),
                        this.conn);
                    DataTable phoneTable = new DataTable();
                    phoneAdapter.Fill(phoneTable);

                    if (phoneTable.Rows.Count != 0)
                    {
                        this.txtPhoneNum.Text = phoneTable.Rows[0][0].ToString();
                    }
                }
                catch (Exception)
                {
                }
            }
        }

        #endregion
    }
}