using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Configuration;

namespace Repo
{
    public class DatabaseContext
    {
        
        private readonly string connectionString;
        [Obsolete]
        private readonly string connectionStringLocal = ConfigurationSettings.AppSettings["ConnnectionString"].ToString();

        public DatabaseContext(string connectionString)
        {
            this.connectionString = connectionString;
        }

        public DatabaseContext()
        {
            this.connectionString = connectionStringLocal;
        }

        public DataTable ExecuteStoredProcedure(string procedureName, SqlParameter[] parameters)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    SqlCommand command = new SqlCommand(procedureName, connection);
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddRange(parameters);
                    DataTable dataTable = new DataTable();
                    using (SqlDataAdapter adapter = new SqlDataAdapter(command))
                    {
                        adapter.Fill(dataTable);
                    }
                    return dataTable;
                }
            }
            catch (Exception ex)
            {                
                throw ex;
            }            
        }
    
        // Add a method to execute a stored procedure with multiple parameters
        public void ExecuteScalarStoredProcedure(string procedureName, params SqlParameter[] parameters)
        {
            try            
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    SqlCommand command = new SqlCommand(procedureName, connection);
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddRange(parameters);
                    command.ExecuteNonQuery();
                }

            }
            catch (Exception ex) 
            {
                throw ex;
            }
            
        }

        public DataTable ExecuteQuery(string query, SqlParameter[] parameters)
        {
            try 
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    SqlCommand command = new SqlCommand(query, connection);
                    command.Parameters.AddRange(parameters);
                    DataTable dataTable = new DataTable();
                    using (SqlDataAdapter adapter = new SqlDataAdapter(command))
                    {
                        adapter.Fill(dataTable);
                    }
                    return dataTable;
                }
            }
            catch(Exception ex) 
            {
                throw ex;
            }
            
        }
    }
}
