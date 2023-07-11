using System.Diagnostics;
using BedBrigade.Data;
using System.IO;
using System.Data.SqlClient;
using Syncfusion.Blazor.CircularGauge.Internal;
using Microsoft.Extensions.FileProviders;
using Syncfusion.Blazor.FileManager.Internal;
using Path = System.IO.Path;
using Org.BouncyCastle.Crypto.Tls;

namespace BedBrigade.Client
{  //temporary solution for seed Schedules from program.cs
    public static class SeedSchedules
    {       
        public static void LoadTestSchedules(bool bTruncateData = false)
        {
            Debug.WriteLine("SeedSchedules Started");
            string sqlConnectionString = "server=localhost\\sqlexpress;database=bedbrigade;trusted_connection=SSPI;Encrypt=False"; //connection string
            string script = String.Empty;

            if (bTruncateData) // clear table
            {
                script = "truncate table dbo.Schedules";
  
            }
            else // load data to table
            {
                var path = Environment.CurrentDirectory + "\\wwwroot\\data\\" + ($"CreateSchedules.sql");
                FileInfo file = new FileInfo(path);
                Debug.WriteLine("File: " + file.FullName);
                script = file.OpenText().ReadToEnd();
            }
            if (script.Length > 0)
            {
                SqlConnection tmpConn;                
                tmpConn = new SqlConnection();
                tmpConn.ConnectionString = sqlConnectionString;

                SqlCommand myCommand = new SqlCommand(script, tmpConn);
                try
                {
                    tmpConn.Open();
                    var result = myCommand.ExecuteNonQuery();
                    Debug.WriteLine(result);                    
                   
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                }


            }

            


        }
    }
}
