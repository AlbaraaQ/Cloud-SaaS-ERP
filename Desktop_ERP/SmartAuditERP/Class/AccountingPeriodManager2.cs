using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace SmartAuditERP
{
    public class AccountingPeriodManager2
    {
        private string _connectionString;

        public AccountingPeriodManager2(string connectionString)
        {
            _connectionString = connectionString;
        }

        public List<AccountingPeriod> GetAllPeriods()
        {
            List<AccountingPeriod> periods = new List<AccountingPeriod>();

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                string query = "SELECT * FROM AccountingPeriods ORDER BY StartDate DESC";
                SqlCommand cmd = new SqlCommand(query, conn);

                conn.Open();
                SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    periods.Add(new AccountingPeriod
                    {
                        PeriodID = Convert.ToInt32(reader["PeriodID"]),
                        PeriodName = reader["PeriodName"].ToString(),
                        StartDate = Convert.ToDateTime(reader["StartDate"]),
                        EndDate = Convert.ToDateTime(reader["EndDate"]),
                        IsActive = Convert.ToBoolean(reader["IsActive"]),
                        IsClosed = Convert.ToBoolean(reader["IsClosed"]),
                        ClosedBy = reader["ClosedBy"]?.ToString(),
                        ClosedDate = reader["ClosedDate"] != DBNull.Value ? Convert.ToDateTime(reader["ClosedDate"]) : (DateTime?)null,
                        Notes = reader["Notes"]?.ToString(),
                        CreatedDate = Convert.ToDateTime(reader["CreatedDate"])
                    });
                }
            }

            return periods;
        }

        public AccountingPeriod GetActivePeriod()
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                string query = "SELECT TOP 1 * FROM AccountingPeriods WHERE IsActive = 1";
                SqlCommand cmd = new SqlCommand(query, conn);

                conn.Open();
                SqlDataReader reader = cmd.ExecuteReader();

                if (reader.Read())
                {
                    return new AccountingPeriod
                    {
                        PeriodID = Convert.ToInt32(reader["PeriodID"]),
                        PeriodName = reader["PeriodName"].ToString(),
                        StartDate = Convert.ToDateTime(reader["StartDate"]),
                        EndDate = Convert.ToDateTime(reader["EndDate"]),
                        IsActive = true
                    };
                }
            }

            return null;
        }

        public bool AddPeriod(AccountingPeriod period)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                string query = @"INSERT INTO AccountingPeriods (PeriodName, StartDate, EndDate, IsActive, IsClosed, Notes, CreatedDate)
                                 VALUES (@Name, @Start, @End, @IsActive, 0, @Notes, GETDATE())";

                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Name", period.PeriodName);
                cmd.Parameters.AddWithValue("@Start", period.StartDate);
                cmd.Parameters.AddWithValue("@End", period.EndDate);
                cmd.Parameters.AddWithValue("@IsActive", period.IsActive);
                cmd.Parameters.AddWithValue("@Notes", period.Notes ?? "");

                conn.Open();

                // إلغاء تفعيل جميع الفترات الأخرى إذا كانت هذه نشطة
                if (period.IsActive)
                {
                    SqlCommand deactivateCmd = new SqlCommand("UPDATE AccountingPeriods SET IsActive = 0", conn);
                    deactivateCmd.ExecuteNonQuery();
                }

                return cmd.ExecuteNonQuery() > 0;
            }
        }

        public bool UpdatePeriod(AccountingPeriod period)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                string query = @"UPDATE AccountingPeriods 
                                 SET PeriodName = @Name, StartDate = @Start, EndDate = @End, 
                                     IsActive = @IsActive, Notes = @Notes
                                 WHERE PeriodID = @ID";

                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@ID", period.PeriodID);
                cmd.Parameters.AddWithValue("@Name", period.PeriodName);
                cmd.Parameters.AddWithValue("@Start", period.StartDate);
                cmd.Parameters.AddWithValue("@End", period.EndDate);
                cmd.Parameters.AddWithValue("@IsActive", period.IsActive);
                cmd.Parameters.AddWithValue("@Notes", period.Notes ?? "");

                conn.Open();

                if (period.IsActive)
                {
                    SqlCommand deactivateCmd = new SqlCommand("UPDATE AccountingPeriods SET IsActive = 0 WHERE PeriodID <> @ID", conn);
                    deactivateCmd.Parameters.AddWithValue("@ID", period.PeriodID);
                    deactivateCmd.ExecuteNonQuery();
                }

                return cmd.ExecuteNonQuery() > 0;
            }
        }

        public bool ClosePeriod(int periodID, string closedBy)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                string query = @"UPDATE AccountingPeriods 
                                 SET IsClosed = 1, IsActive = 0, ClosedBy = @ClosedBy, ClosedDate = GETDATE()
                                 WHERE PeriodID = @ID";

                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@ID", periodID);
                cmd.Parameters.AddWithValue("@ClosedBy", closedBy);

                conn.Open();
                return cmd.ExecuteNonQuery() > 0;
            }
        }

        public bool ReopenPeriod(int periodID)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                string query = @"UPDATE AccountingPeriods 
                                 SET IsClosed = 0, ClosedBy = NULL, ClosedDate = NULL
                                 WHERE PeriodID = @ID";

                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@ID", periodID);

                conn.Open();
                return cmd.ExecuteNonQuery() > 0;
            }
        }

        public bool ActivatePeriod(int periodID)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();

                // إلغاء تفعيل جميع الفترات
                SqlCommand deactivateCmd = new SqlCommand("UPDATE AccountingPeriods SET IsActive = 0", conn);
                deactivateCmd.ExecuteNonQuery();

                // تفعيل الفترة المحددة
                string query = "UPDATE AccountingPeriods SET IsActive = 1 WHERE PeriodID = @ID";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@ID", periodID);

                return cmd.ExecuteNonQuery() > 0;
            }
        }

        public bool DeletePeriod(int periodID)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                string query = "DELETE FROM AccountingPeriods WHERE PeriodID = @ID";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@ID", periodID);

                conn.Open();
                return cmd.ExecuteNonQuery() > 0;
            }
        }
    }
}