using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows.Forms;

using Oracle.ManagedDataAccess.Client;
using System.ServiceProcess;

namespace TripodAccessWithDisplayAndLogSaveOracleServiceR
{
    class Program
    {
        
        //[STAThread]
        
        static void Main(string[] args)
        {
            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[]
            {
                new ServiceLog()
            };
            ServiceBase.Run(ServicesToRun);
            //OracleTest();
            //LoadSingleRealtimer();
            //Console.ReadLine();


            




        }
        /*
        public static void OracleTest()
        {
            //string constr = "User Id=maximlocal;Password=maxim1234;Data Source=114.31.10.244:1521:msoft";
            string constr = "Data Source=(DESCRIPTION=(ADDRESS =(PROTOCOL=tcp)(HOST=114.31.10.244)(PORT=1521))(CONNECT_DATA=(SERVICE_NAME=msoft)));User Id=maximlocal;Password=maxim12345";

            OracleConnection con = new OracleConnection();
            //con.ConnectionString = "User ID=USRID; Password=PASSWD; Data Source=DSATTNLOG;";
            con.ConnectionString = constr;
            try
            {
                con.Open();
                // Display Version Number
                Console.WriteLine("Connected to Oracle " + con.ServerVersion);
                // Close and Dispose OracleConnection
            }
            catch (OracleException ex)
            {
                Console.WriteLine("DataBase Failed:"+ex.ToString());
            }
            finally
            {
                con.Close();
                con.Dispose();
            }
        }

        public static void LoadSingleRealtimer()
        {
            DeviceManager dm = new DeviceManager();
            //AttendanceLogcs al = new AttendanceLogcs();
            string singleDeviceIp = ConfigurationManager.AppSettings["singleDeviceIp"];
            int singleDeviceNumber = Convert.ToInt32(ConfigurationManager.AppSettings["singleDeviceNumber"]);
            int connected = dm.isConected(singleDeviceIp, singleDeviceNumber);
            if (connected == 1)
            {
                dm.realEvent_OnAttTransaction(singleDeviceNumber, singleDeviceIp);
                dm.intervalRunner(singleDeviceNumber, singleDeviceIp);
                dm.intervalRunner(singleDeviceNumber, true);
            }
        }*/
        /*
        public static void GetDeviceLog()
        {
            AttendanceLogcs al = new AttendanceLogcs();
            string singleDeviceIp = ConfigurationManager.AppSettings["singleDeviceIp"];
            int singleDeviceNumber = Convert.ToInt32(ConfigurationManager.AppSettings["singleDeviceNumber"]);
            int connected = al.isConected(singleDeviceIp, singleDeviceNumber);
            if (connected == 1)
            {
                al.getLogData(singleDeviceNumber);
            }
        }*/
        public static void writeErrorLog(string message)
        {
            StreamWriter sw = null;
            try
            {
                sw = new StreamWriter(AppDomain.CurrentDomain.BaseDirectory+"\\log.txt", true);
                sw.WriteLine(DateTime.Now.ToString()+":"+message);
                sw.Flush();
                sw.Close();
            }
            catch
            {

            }
        }

    }
}
