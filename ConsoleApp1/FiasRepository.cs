using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp1
{
    public static class FiasRepository
    {
        private static  readonly string ConnectionSQL = "Data Source=SIGMA\\SIGMA;Initial Catalog=Grafias;MultipleActiveResultSets=True";
        public static SqlConnection GetConnection(bool needOpen = true)
        {
            SqlConnection connection = new SqlConnection(ConnectionSQL);

            if (needOpen)
                connection.Open();

            return connection;
        }
        internal static void UpdateDbVersion(int СurrentUpdate)
        {
            using (SqlConnection connection = GetConnection())
            using (SqlCommand cmd = new SqlCommand(string.Empty, connection))
            {
                cmd.CommandText = @"DELETE FROM [dbo].[DbInfo]
                                    INSERT INTO [dbo].[DbInfo] ([LastVersionID]) VALUES (@СurrentUpdate)";

                cmd.Parameters.AddWithValue("@СurrentUpdate", СurrentUpdate);
                cmd.ExecuteNonQuery();
            }
        }
    }
}
