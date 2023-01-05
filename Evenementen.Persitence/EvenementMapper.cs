using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Intrinsics.X86;
using System.Threading.Tasks;
using System.Windows.Shapes;
using Evenementen.Domain;
using Microsoft.Data.SqlClient;

namespace Evenementen.Persitence
{

    public class EvenementMapper : IEvenementMapper
    {
        public event EventHandler<double>? RowAdded;
        public event EventHandler<int>? FileRead;
        public event EventHandler<int>? AllRowsMapped;

        private SqlConnection _connection;
        const string DefaultConnectionString = @"Server=.\SQLEXPRESS;Database=EvenementenDb;Trusted_Connection=True;Encrypt=False";

        public static List<Evenement> _evenementen = new();

        public static int _errors = 0;
        public static int _totalLines = 0;
        public static int _addedRows = 0;

        private string cleanDbTablesQuery = @"
               
                DROP TABLE IF EXISTS [dbo].[Evenementen]
                SET ANSI_NULLS ON
                SET QUOTED_IDENTIFIER ON
                CREATE TABLE [dbo].[Evenementen](
	                [Identifier] [nvarchar](36) NOT NULL,
	                [Naam] [nvarchar](250) NOT NULL,
	                [Beschrijving] [nvarchar](max) NULL,
	                [Prijs] [decimal](2, 0) NULL,
	                [StartDatum] [datetime] NULL,
	                [EindDatum] [datetime] NULL,
	                [ParentEvenementId] [nvarchar](36) NULL,
                 CONSTRAINT [PK_Evenementen] PRIMARY KEY CLUSTERED 
                (
	                [Identifier] ASC
                )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
                ) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

                DROP TABLE IF EXISTS [dbo].[Planner]
                SET ANSI_NULLS ON
                SET QUOTED_IDENTIFIER ON
                CREATE TABLE [dbo].[Planner] (
                    [PlannerEvenement] VARCHAR (36) NOT NULL,
                    CONSTRAINT [PK_Planner] PRIMARY KEY CLUSTERED ([PlannerEvenement] ASC)
                );
                ";

        public EvenementMapper()
        {
            _connection = new SqlConnection(DefaultConnectionString);
        }

        public int MapCsvIntoDatabase(string connectionString, string csvPath)
        {
            _connection = new SqlConnection(connectionString);
            EnsureDbCreated(connectionString);
            ReadCsvFile(csvPath);
            PostEvenementenIntoDb();
            return _totalLines;
        }


        public IEnumerable<Evenement> GetEvenementenByParentEvenementId(string? parentEvenementId, string? word = null)
        {
            List<Evenement> evenementen = new();
            try
            {
                _connection.Open();
                string query =
                    $"SELECT * FROM Evenementen WHERE ParentEvenementId {(parentEvenementId == null ? "is null" : "= @parentId")}"
                    + $"{((word == null || string.IsNullOrWhiteSpace(word)) ? "" : " AND LOWER(Naam) LIKE '%" + word + "%'")}";
                SqlCommand cmd = new(query, _connection);
                cmd.Parameters.Add("@parentId", SqlDbType.VarChar);
                cmd.Parameters["@parentId"].Value = string.IsNullOrWhiteSpace(parentEvenementId) ? DBNull.Value : parentEvenementId;

                SqlDataReader reader = cmd.ExecuteReader();
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        evenementen.Add(MapDbRowIntoEvenement(reader));
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Cannot connect to database.\n" + ex.Message);
            }
            finally
            {
                _connection.Close();
            }

            if (parentEvenementId != null)
            {
                foreach (var evn in evenementen)
                {
                    var subEvn = GetEvenementenByParentEvenementId(evn.Identifier, null);
                    if (subEvn != null)
                    {
                        evn.Subevenementen = subEvn.ToList();
                    }
                }
            }

            return evenementen;
        }

        public Evenement? GetEvenementById(string? evnId)
        {
            if (evnId == null) return null;
            Evenement? evenement = new();

            _connection.Open();
            string query = $"SELECT * FROM Evenementen WHERE Identifier = @Id";
            SqlCommand cmd = new(query, _connection);
            cmd.Parameters.Add("@Id", SqlDbType.VarChar);
            cmd.Parameters["@Id"].Value = string.IsNullOrWhiteSpace(evnId) ? DBNull.Value : evnId;
            SqlDataReader reader = cmd.ExecuteReader();
            if (reader.HasRows)
            {
                reader.Read();
                evenement = MapDbRowIntoEvenement(reader);
                _connection.Close();

            }
            return evenement;

        }

        public bool CheckDbExists(string cs)
        {
            bool res = false;

            var getCs = GetDatabaseNameAndMasterCS(cs);
            if (getCs == null) { throw new Exception(); };
            string databaseName = getCs[0];
            string masterConnectionString = getCs[1];
            SqlConnection initialSqlCns = new SqlConnection(masterConnectionString);
            initialSqlCns.Open();
            string checkDbQuery = $"USE [master] \r\n  SELECT COUNT(*) FROM sys.databases WHERE name = '{databaseName}' ";
            SqlCommand checkDBCmd = new SqlCommand(checkDbQuery, initialSqlCns);
            try
            {
                res = (int)checkDBCmd.ExecuteScalar() > 0;
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to check Database: \n" + ex.Message);
            }
            finally
            {
                initialSqlCns.Close();
            }
            if (res) _connection = new(cs);
            return res;
        }

        public void AddEvenementToPlanner(string evenementId)
        {
            try
            {
                _connection.Open();
                SqlCommand cmd = new SqlCommand("INSERT INTO Planner (PlannerEvenement) VALUES (@Id)", _connection);
                cmd.Parameters.Add("@Id", SqlDbType.VarChar);
                cmd.Parameters["@Id"].Value = evenementId;
                var res = cmd.ExecuteScalar();
                if (res != null && !((int)res > 0)) throw new Exception();
            }
            catch (Exception ex)
            {
                throw new Exception("Adding evenement to planner failed\n" + ex.Message);
            }
            finally
            {
                _connection.Close();
            }
        }

        public bool IsEvenementByIdAlreadyAddedToPlanner(string identifier)
        {
            if (string.IsNullOrEmpty(identifier)) return false;
            _connection.Open();
            try
            {
                string query = $"SELECT * FROM Planner WHERE PlannerEvenement = @Id";
                SqlCommand cmd = new(query, _connection);
                cmd.Parameters.Add("@Id", SqlDbType.VarChar);
                cmd.Parameters["@Id"].Value = identifier;
                SqlDataReader reader = cmd.ExecuteReader();
                if (reader.HasRows)
                {
                    return true;
                    //reader.Read();
                    //evenement = MapDbRowIntoEvenement(reader);
                }

            }
            catch (Exception ex)
            {

                throw new Exception(ex.Message);
            }
            finally
            {
                _connection.Close();
            }


            return false;
        }

        public IEnumerable<Evenement> GetEvenementenFromPlanner()
        {
            List<Evenement> plannersEvenementen = new();

            try
            {
                _connection.Open();
                string query =
                    $"select * from Planner join Evenementen \r\non Planner.PlannerEvenement = Evenementen.Identifier";
                SqlCommand cmd = new(query, _connection);

                SqlDataReader reader = cmd.ExecuteReader();
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        plannersEvenementen.Add(MapDbRowIntoEvenement(reader));
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Cannot connect to database.\n" + ex.Message);
            }
            finally
            {
                _connection.Close();
            }

            foreach (var item in plannersEvenementen)
            {
                // all children
                item.Subevenementen = GetEvenementenByParentEvenementId(item.Identifier).ToList();
            }

            return plannersEvenementen;
        }

        public void RemoveEvenementFromPlannerById(string id)
        {
            try
            {
                _connection.Open();
                SqlCommand command = new SqlCommand($"DELETE FROM Planner WHERE PlannerEvenement = @Id;", _connection);
                SqlParameter idParameter = new SqlParameter("@Id", SqlDbType.VarChar) { Value = id };
                command.Parameters.Add(idParameter);
                command.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to remove evenement from planner\n" + ex.Message);
            }
            finally
            {
                _connection.Close();
            }
        }



        #region Private Methodes

        private void EnsureDbCreated(string cs)
        {

            var getCs = GetDatabaseNameAndMasterCS(cs);
            if (getCs == null) { throw new Exception(); };
            string databaseName = getCs[0];
            string masterConnectionString = getCs[1];
            SqlConnection initialSqlCns = new SqlConnection(masterConnectionString);
            initialSqlCns.Open();
            string checkDbQuery = $"USE [master] \r\n  SELECT COUNT(*) FROM sys.databases WHERE name = '{databaseName}' ";
            SqlCommand checkDBCmd = new SqlCommand(checkDbQuery, initialSqlCns);

            if (!((int)checkDBCmd.ExecuteScalar() > 0))
            {
                string createDbQuery = $"CREATE DATABASE {databaseName}";
                SqlCommand createDbCmd = new SqlCommand(createDbQuery, initialSqlCns);

                try
                {
                    createDbCmd.ExecuteScalar();
                }
                catch (Exception ex)
                {

                    throw new Exception("Failed to create Database: \n" + ex.Message);
                }

            }

            string resetDbTablesQuery = $"USE [{databaseName}] \r\n";
            resetDbTablesQuery += cleanDbTablesQuery;
            SqlCommand resetDbTablesCmd = new SqlCommand(resetDbTablesQuery, initialSqlCns);
            resetDbTablesCmd.ExecuteScalar();

            initialSqlCns.Close();

        }

        private static Evenement? MapEvenementRow(string[] row)
        {
            var hasStart = DateTime.TryParse(row[2], out DateTime startTime);
            var hasEnd = DateTime.TryParse(row[1], out DateTime endTime);
            var hasPrice = decimal.TryParse(row[^1], out decimal prijs);
            string beschrijving = string.Join("; ", row.ToList().GetRange(5, row.Length - 7));

            Evenement eve = new Evenement
            {
                Identifier = row[0].Trim(),
                StartDatum = hasStart == true ? startTime : null,
                EindDatum = hasEnd == true ? endTime : null,
                Prijs = hasPrice == true ? prijs : null,
                Naam = row[^2],
                Beschrijving = string.IsNullOrWhiteSpace(beschrijving) ? null : beschrijving,
                ParentEvenementId = string.IsNullOrEmpty(row[4]) ? null : row[4].Trim(),
            };
            return eve;
        }

        private void ReadCsvFile(string filepath)
        {
            using (StreamReader reader = new StreamReader(filepath))
            {
                string? streamLine;

                while (!reader.EndOfStream)
                {
                    streamLine = reader.ReadLine();
                    if (streamLine != null)
                    {
                        var row = streamLine.Split(';');
                        _totalLines++;
                        try
                        {
                            var mappedEvnmt = MapEvenementRow(row);
                            if (mappedEvnmt != null && !_evenementen.Any(e => e.Identifier == mappedEvnmt.Identifier))
                            {
                                _evenementen.Add(mappedEvnmt);
                            }
                            else _errors++;
                        }
                        catch (Exception ex)
                        {

                            Debug.WriteLine(ex.Message);
                        }
                    }
                }
            }
            FileRead?.Invoke(this, _totalLines);
        }

        private Evenement MapDbRowIntoEvenement(SqlDataReader reader)
        {
            return new Evenement
            {
                Identifier = (string)reader["Identifier"],
                StartDatum = reader["StartDatum"] == DBNull.Value ? null : (DateTime?)reader["StartDatum"],
                EindDatum = reader["EindDatum"] == DBNull.Value ? null : (DateTime?)reader["EindDatum"],
                Prijs = reader["Prijs"] == DBNull.Value ? null : (decimal)reader["Prijs"],
                Naam = (string)reader["Naam"],
                Beschrijving = reader["Beschrijving"] == DBNull.Value ? null : (string?)reader["Beschrijving"],
                ParentEvenementId = reader["ParentEvenementId"] == DBNull.Value ? null : (string?)reader["ParentEvenementId"],
            };

        }

        private void AddEvenementToDatabase(Evenement item)
        {
            try
            {
                _connection.Open();
                SqlCommand cmd = new SqlCommand(
                    "INSERT INTO Evenementen (Identifier, Naam, Beschrijving, Prijs, StartDatum, EindDatum, ParentEvenementId ) " +
                    "VALUES (@Identifier, @Naam, @Beschrijving, @Prijs, @StartDatum, @EindDatum, @ParentEvenementId); " +
                    "SELECT CAST(scope_identity() AS int)", _connection);

                cmd.Parameters.Add("@Identifier", SqlDbType.VarChar);
                cmd.Parameters.Add("@Naam", SqlDbType.VarChar);
                cmd.Parameters.Add("@Beschrijving", SqlDbType.VarChar);
                cmd.Parameters.Add("@Prijs", SqlDbType.Decimal);
                cmd.Parameters.Add("@StartDatum", SqlDbType.DateTime);
                cmd.Parameters.Add("@EindDatum", SqlDbType.DateTime);
                cmd.Parameters.Add("@ParentEvenementId", SqlDbType.VarChar);

                cmd.Parameters["@Identifier"].Value = item.Identifier;
                cmd.Parameters["@Naam"].Value = item.Naam;

                cmd.Parameters["@Beschrijving"].Value = item.Beschrijving == null ? DBNull.Value : item.Beschrijving;
                cmd.Parameters["@Prijs"].Value = item.Prijs == null ? DBNull.Value : item.Prijs;
                cmd.Parameters["@StartDatum"].Value = item.StartDatum == null ? DBNull.Value : item.StartDatum;
                cmd.Parameters["@EindDatum"].Value = item.EindDatum == null ? DBNull.Value : item.EindDatum;
                cmd.Parameters["@ParentEvenementId"].Value =
                     item.ParentEvenementId == null ? DBNull.Value : item.ParentEvenementId;

                cmd.ExecuteScalar();
                _addedRows++;
            }
            finally
            {
                _connection.Close();
            }
        }

        private async void PostEvenementenIntoDb()
        {
            foreach (var evn in _evenementen)
            {
                await Task.Run(() => AddEvenementToDatabase(evn));
                double prg = (_addedRows / (double)_totalLines) * 100;
                RowAdded?.Invoke(this, prg);
            }
            AllRowsMapped?.Invoke(this, _addedRows);
        }

        private string[]? GetDatabaseNameAndMasterCS(string connectionString)
        {
            string dbName = "";
            string masterConnetcionString = "";

            List<string> dbCallNames = new() { "database=", "initial catalog=" };
            string? dbCall = dbCallNames.Where(n => connectionString.ToLower().Contains(n)).FirstOrDefault();

            if (dbCall == null) return null;

            if (connectionString.ToLower().Contains(dbCall.ToLower()))
            {
                int dbPos = connectionString.ToLower().IndexOf(dbCall);
                if (dbPos > -1)
                {
                    int dbEndPos = connectionString.Substring(dbPos).IndexOf(';');
                    dbName = connectionString.Substring(dbPos + dbCall.Length, dbEndPos - dbCall.Length);
                    masterConnetcionString = connectionString
                        .Remove(dbPos + dbCall.Length, dbName.Length).Insert(dbPos + dbCall.Length, "master");
                }
            }
            return new string[] { dbName, masterConnetcionString };
        }

        #endregion
    }
}
