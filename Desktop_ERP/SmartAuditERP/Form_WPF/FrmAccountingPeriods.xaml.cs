using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Color = System.Windows.Media.Color;
using MessageBox = System.Windows.MessageBox;

namespace SmartAuditERP.Form_WPF
{
    public partial class FrmAccountingPeriods : Window
    {
        #region Fields
        private AccountingPeriodManager _periodManager;
        private AccountingPeriod _currentPeriod;
        private string _connectionString;
        #endregion

        #region Constructor & Load
        public FrmAccountingPeriods()
        {
            InitializeComponent();
            _connectionString = MainClass.connstr;
            this.Loaded += FrmAccountingPeriods_Loaded;
        }

        private void FrmAccountingPeriods_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                InitializeForm();
                _periodManager = new AccountingPeriodManager(_connectionString);
                LoadPeriods();
                LoadActivePeriod();
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ في تحميل النافذة: " + ex.Message, "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void InitializeForm()
        {
            // تهيئة التواريخ الافتراضية
            DtpStartDate.SelectedDate = new DateTime(DateTime.Now.Year, 1, 1);
            DtpEndDate.SelectedDate = new DateTime(DateTime.Now.Year, 12, 31);
        }
        #endregion

        #region Data Loading
        private void LoadPeriods()
        {
            try
            {
                List<AccountingPeriod> periods = _periodManager.GetAllPeriods();
                DataGridView1.ItemsSource = periods;
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ في تحميل الفترات: " + ex.Message, "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadActivePeriod()
        {
            try
            {
                AccountingPeriod activePeriod = _periodManager.GetActivePeriod();
                if (activePeriod != null)
                {
                    LblActivePeriod.Text = $"الفترة النشطة: {activePeriod.PeriodName} " +
                        $"({activePeriod.StartDate:dd/MM/yyyy} - {activePeriod.EndDate:dd/MM/yyyy})";
                    LblActivePeriod.Foreground = new SolidColorBrush(Color.FromRgb(255, 217, 61)); // Yellow
                }
                else
                {
                    LblActivePeriod.Text = "لا توجد فترة نشطة";
                    LblActivePeriod.Foreground = new SolidColorBrush(Color.FromRgb(255, 165, 0)); // Orange
                }
            }
            catch (Exception)
            {
                LblActivePeriod.Text = "خطأ في تحميل الفترة النشطة";
                LblActivePeriod.Foreground = System.Windows.Media.Brushes.Red;
            }
        }
        #endregion

        #region Button Events
        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!ValidateInputs()) return;

                AccountingPeriod period = new AccountingPeriod
                {
                    PeriodName = TxtPeriodName.Text.Trim(),
                    StartDate = DtpStartDate.SelectedDate ?? DateTime.Now,
                    EndDate = DtpEndDate.SelectedDate ?? DateTime.Now,
                    IsActive = ChkIsActive.IsChecked ?? false,
                    Notes = TxtNotes.Text.Trim()
                };

                if (_periodManager.AddPeriod(period))
                {
                    MessageBox.Show("تم إضافة الفترة المحاسبية بنجاح", "نجاح",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    LoadPeriods();
                    LoadActivePeriod();
                    ClearForm();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnUpdate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_currentPeriod == null)
                {
                    MessageBox.Show("يرجى اختيار فترة محاسبية للتعديل", "تنبيه",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (_currentPeriod.IsClosed)
                {
                    MessageBox.Show("لا يمكن تعديل فترة مغلقة. يرجى إعادة فتحها أولاً", "تنبيه",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!ValidateInputs()) return;

                _currentPeriod.PeriodName = TxtPeriodName.Text.Trim();
                _currentPeriod.StartDate = DtpStartDate.SelectedDate ?? DateTime.Now;
                _currentPeriod.EndDate = DtpEndDate.SelectedDate ?? DateTime.Now;
                _currentPeriod.IsActive = ChkIsActive.IsChecked ?? false;
                _currentPeriod.Notes = TxtNotes.Text.Trim();

                if (_periodManager.UpdatePeriod(_currentPeriod))
                {
                    MessageBox.Show("تم تحديث الفترة المحاسبية بنجاح", "نجاح",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    LoadPeriods();
                    LoadActivePeriod();
                    ClearForm();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnClosePeriod_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (DataGridView1.SelectedItem == null)
                {
                    MessageBox.Show("يرجى اختيار فترة محاسبية للإغلاق", "تنبيه",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                AccountingPeriod selected = DataGridView1.SelectedItem as AccountingPeriod;
                if (selected == null) return;

                if (selected.IsClosed)
                {
                    MessageBox.Show("هذه الفترة مغلقة بالفعل", "تنبيه",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var result = MessageBox.Show(
                    $"هل أنت متأكد من إغلاق الفترة المحاسبية '{selected.PeriodName}'?\n" +
                    "بعد الإغلاق لن تستطيع إدخال أي عمليات جديدة في هذه الفترة",
                    "تأكيد الإغلاق", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    if (_periodManager.ClosePeriod(selected.PeriodID, Environment.UserName))
                    {
                        MessageBox.Show("تم إغلاق الفترة المحاسبية بنجاح", "نجاح",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                        LoadPeriods();
                        LoadActivePeriod();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnReopen_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (DataGridView1.SelectedItem == null)
                {
                    MessageBox.Show("يرجى اختيار فترة محاسبية لإعادة الفتح", "تنبيه",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                AccountingPeriod selected = DataGridView1.SelectedItem as AccountingPeriod;
                if (selected == null) return;

                if (!selected.IsClosed)
                {
                    MessageBox.Show("هذه الفترة مفتوحة بالفعل", "تنبيه",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var result = MessageBox.Show(
                    $"هل أنت متأكد من إعادة فتح الفترة المحاسبية '{selected.PeriodName}'?",
                    "تأكيد إعادة الفتح", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    if (_periodManager.ReopenPeriod(selected.PeriodID))
                    {
                        MessageBox.Show("تم إعادة فتح الفترة المحاسبية بنجاح", "نجاح",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                        LoadPeriods();
                        LoadActivePeriod();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnActivate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (DataGridView1.SelectedItem == null)
                {
                    MessageBox.Show("يرجى اختيار فترة محاسبية للتفعيل", "تنبيه",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                AccountingPeriod selected = DataGridView1.SelectedItem as AccountingPeriod;
                if (selected == null) return;

                if (selected.IsActive)
                {
                    MessageBox.Show("هذه الفترة نشطة بالفعل", "تنبيه",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var result = MessageBox.Show(
                    $"هل أنت متأكد من تفعيل الفترة المحاسبية '{selected.PeriodName}'?\n" +
                    "سيتم إلغاء تفعيل الفترة الحالية",
                    "تأكيد التفعيل", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    if (_periodManager.ActivatePeriod(selected.PeriodID))
                    {
                        MessageBox.Show("تم تفعيل الفترة المحاسبية بنجاح", "نجاح",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                        LoadPeriods();
                        LoadActivePeriod();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (DataGridView1.SelectedItem == null)
                {
                    MessageBox.Show("يرجى اختيار فترة محاسبية للحذف", "تنبيه",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                AccountingPeriod selected = DataGridView1.SelectedItem as AccountingPeriod;
                if (selected == null) return;

                if (selected.IsClosed)
                {
                    MessageBox.Show("لا يمكن حذف فترة مغلقة", "تنبيه",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var result = MessageBox.Show(
                    $"هل أنت متأكد من حذف الفترة المحاسبية '{selected.PeriodName}'?\n" +
                    "هذه العملية لا يمكن التراجع عنها!",
                    "تأكيد الحذف", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    if (_periodManager.DeletePeriod(selected.PeriodID))
                    {
                        MessageBox.Show("تم حذف الفترة المحاسبية بنجاح", "نجاح",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                        LoadPeriods();
                        LoadActivePeriod();
                        ClearForm();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnClear_Click(object sender, RoutedEventArgs e)
        {
            ClearForm();
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadPeriods();
            LoadActivePeriod();
        }
        #endregion

        #region DataGrid Events
        private void DataGridView1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (DataGridView1.SelectedItem != null)
                {
                    _currentPeriod = DataGridView1.SelectedItem as AccountingPeriod;
                    if (_currentPeriod != null)
                    {
                        TxtPeriodID.Text = _currentPeriod.PeriodID.ToString();
                        TxtPeriodName.Text = _currentPeriod.PeriodName;
                        DtpStartDate.SelectedDate = _currentPeriod.StartDate;
                        DtpEndDate.SelectedDate = _currentPeriod.EndDate;
                        ChkIsActive.IsChecked = _currentPeriod.IsActive;
                        TxtNotes.Text = _currentPeriod.Notes ?? "";

                        // تلوين الصف حسب الحالة
                        HighlightSelectedRow();
                    }
                }
            }
            catch (Exception) { }
        }

        private void DataGridView1_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (_currentPeriod != null && !_currentPeriod.IsClosed)
            {
                TxtPeriodName.Focus();
            }
        }

        private void HighlightSelectedRow()
        {
            // يمكن تطبيق تلوين مخصص للصف المحدد برمجياً إذا لزم الأمر
            // في WPF يفضل استخدام DataTriggers في XAML
        }
        #endregion

        #region Helper Methods
        private bool ValidateInputs()
        {
            if (string.IsNullOrWhiteSpace(TxtPeriodName.Text))
            {
                MessageBox.Show("يرجى إدخال اسم الفترة المحاسبية", "تنبيه",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtPeriodName.Focus();
                return false;
            }

            if (DtpStartDate.SelectedDate == null || DtpEndDate.SelectedDate == null)
            {
                MessageBox.Show("يرجى تحديد تاريخ البداية والنهاية", "تنبيه",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (DtpEndDate.SelectedDate <= DtpStartDate.SelectedDate)
            {
                MessageBox.Show("تاريخ النهاية يجب أن يكون بعد تاريخ البداية", "تنبيه",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                DtpEndDate.Focus();
                return false;
            }

            return true;
        }

        private void ClearForm()
        {
            TxtPeriodID.Clear();
            TxtPeriodName.Clear();
            DtpStartDate.SelectedDate = new DateTime(DateTime.Now.Year, 1, 1);
            DtpEndDate.SelectedDate = new DateTime(DateTime.Now.Year, 12, 31);
            ChkIsActive.IsChecked = false;
            TxtNotes.Clear();
            _currentPeriod = null;
            DataGridView1.SelectedItem = null;
            TxtPeriodName.Focus();
        }
        #endregion
    }
}