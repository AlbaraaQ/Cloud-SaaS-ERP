using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using AuditorAPI.Models;
using Microsoft.AspNet.SignalR.Client;

namespace SmartAuditERP
{
    public class GetCustFromAndroid
    {
        #region Fields

        private HubConnection hubConnection;
        private IHubProxy hubProxy;

        #endregion

        #region Public Methods

        public async void StartConnection()
        {
            try
            {
                hubConnection = new HubConnection(Sync.APIUrl);
                hubProxy = hubConnection.CreateHubProxy("CustomerHub");

                hubProxy.On<AuditorAPI.Models.Customer>("CustomerAdded", (cust) =>
                {
                    // في WPF نستخدم Dispatcher بدل Invoke على Application.OpenForms
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        SaveCustomer(cust);
                    });
                });

                await hubConnection.Start();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"خطأ في StartConnection: {ex.Message}");
            }
        }

        #endregion

        #region Private Methods

        private void SaveCustomer(AuditorAPI.Models.Customer cust)
        {
            try
            {
                new EntityOperations().SaveCustomer(
                    new List<AuditorAPI.Models.Customer> { cust },
                    AddedLocally: true);

                MessageBox.Show("تم إضافة العميل: " + cust.Name,
                    "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("❌ خطأ في حفظ العميل: " + ex.Message,
                    "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion
    }
}