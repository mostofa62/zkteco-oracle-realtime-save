using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace TripodAccessWithDisplayAndLogSaveOracleServiceR
{
    partial class ServiceLog : ServiceBase
    {
        public ServiceLog()
        {
            InitializeComponent();
        }

        DeviceManager dm = null;

        protected override void OnStart(string[] args)
        {
            // TODO: Add code here to start your service.
            bool dbconnected = DataBaseConnectionTest();
            if (!dbconnected)
            {
                Program.writeErrorLog("Database Connection Error..Service Not Starting");
                Stop();
            }
            int dvconnected = LoadSingleRealtimer();
            if(dvconnected == 0)
            {
                Program.writeErrorLog("Device Connection Error...Service Not Starting");
                Stop();
            }
            
        }

        protected override void OnStop()
        {
            // TODO: Add code here to perform any tear-down necessary to stop your service.
            if (dm != null)
            {
                dm.isDisconnected();
            }
        }

        protected bool DataBaseConnectionTest()
        {

            bool consuccess = true;
            string dbid = ConfigurationManager.AppSettings["DBUSRID"];
            string dbpwd = ConfigurationManager.AppSettings["DBPASSWD"];
            string constr = "User ID=" + dbid + "; Password=" + dbpwd + "; Data Source=DSATTNLOG;";
            OracleConnection con = new OracleConnection();
            con.ConnectionString = constr;
            try
            {
                con.Open();
            }
            catch (OracleException ex)
            {
                Program.writeErrorLog(ex.ToString());
                consuccess = false;
            }
            finally
            {
                con.Close();
                con.Dispose();
            }
            return consuccess;
        }

        protected int LoadSingleRealtimer()
        {
            dm = new DeviceManager();
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

            return connected;
        }
    }
}
