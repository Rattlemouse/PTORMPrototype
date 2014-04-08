using System.Data.SqlClient;
using NUnit.Framework;

namespace PTORTMTests
{
    public abstract class BaseSqlTest
    {
        protected SqlConnection SqlConnection;
        [TestFixtureSetUp]
        public void SetupClass()
        {
            SqlConnection = new SqlConnection(@"Data Source=(localdb)\v11.0;Integrated Security=True");
            SqlConnection.Open();
            string createDatabase =
                string.Format("if not exists(select * from sys.databases where name = '{0}') CREATE DATABASE {0};", DataBaseName);
            var cmd = SqlConnection.CreateCommand();
            cmd.CommandText = createDatabase;
            cmd.ExecuteNonQuery();
            cmd = SqlConnection.CreateCommand();
            cmd.CommandText = "USE " + DataBaseName;
            cmd.ExecuteNonQuery();
        }
        [TestFixtureTearDown]
        public void TearDownClass()
        {
            var dropTable = SqlConnection.CreateCommand();
            dropTable.CommandText =string.Format("use master; DROP DATABASE {0};", DataBaseName);
            dropTable.ExecuteNonQuery();
            SqlConnection.Close();
        }

        protected virtual string DataBaseName
        {
            get { return GetType().Name.ToLower(); }
        }
    }
}