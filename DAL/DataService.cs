using Dapper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Web;

namespace SawyerSight.Web.DAL
{
    public abstract class DataService : IDisposable
    {
        protected DataService(string connectionString)
        {
            this.connectionString = connectionString;
        }
        
        protected DbConnection GetConnection()
        {         
            if (connection == null)
            {
                var factory = DbProviderFactories.GetFactory("System.Data.SqlClient");
                connection = factory.CreateConnection();
                connection.ConnectionString = connectionString;
            }
            if (connection.State == ConnectionState.Closed)
            {
                var factory = DbProviderFactories.GetFactory("System.Data.SqlClient");
                connection = factory.CreateConnection();
                connection.ConnectionString = connectionString;
            }

            return connection;
        }
        private DbConnection connection;
        private string connectionString;

        protected IEnumerable<T> Query<T>(string sql, object param = null, IDbTransaction transaction = null, bool buffered = true, int? commandTimeout = null, CommandType? commandType = null)
        {
            using (var conn = GetConnection())
            {
                return SqlMapper.Query<T>(conn, sql, param, transaction, buffered, 6000, commandType);
            }
        }

        protected int Execute(string sql, object param = null, IDbTransaction transaction = null, int? commandTimeout = null, CommandType? commandType = null)
        {
            using (var conn = GetConnection())
            {
                return SqlMapper.Execute(conn, sql, param, transaction, 6000, commandType);
            }
        }

        protected DataTable DataTableExecute(string sql, object param = null, IDbTransaction transaction = null, int? commandTimeout = null, CommandType? commandType = null)
        {
            using (var conn = GetConnection())
            {
                var dataReader = conn.ExecuteReader(sql, param, transaction, 6000, commandType);
                var dataTable = new DataTable();
                dataTable.Load(dataReader);

                return dataTable;
            }
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (connection != null)
                    {
                        connection.Close();
                    }
                }                
                disposedValue = true;
            }
        }
     
        public void Dispose()
        {            
            Dispose(true);            
        }        
        #endregion
    }
}