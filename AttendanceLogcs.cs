using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TripodAccessWithDisplayAndLogSaveOracleServiceR
{
    class AttendanceLogcs
    {
        public zkemkeeper.CZKEMClass axCZKEM1 = new zkemkeeper.CZKEMClass();
        int idwErrorCode = 0;

        string sdwEnrollNumber = "";
        int idwVerifyMode = 0;
        int idwInOutMode = 0;
        int idwYear = 0;
        int idwMonth = 0;
        int idwDay = 0;
        int idwHour = 0;
        int idwMinute = 0;
        int idwSecond = 0;
        int idwWorkcode = 0;

        int iGLCount = 0;

        public int isConected(String div_ip, int machineNo)
        {


            bool isConnected = axCZKEM1.Connect_Net(div_ip, 4370);
            Console.WriteLine("Device(" + machineNo + ")[" + div_ip + "]=" + isConnected);
            if (isConnected == true)
            {
                int machineNumber = machineNo;
                bool gg = axCZKEM1.EnableDevice(machineNumber, true);
                return 1;
            }
            else
            {
                axCZKEM1.GetLastError(ref idwErrorCode);
                Console.WriteLine("Device Conection Fialed" + idwErrorCode);
                Console.ReadLine();

                return idwErrorCode;
            }

        }

        public void intervalRunner(int iMachineNumber)
        {
            System.Timers.Timer timer = new System.Timers.Timer();
            int min = Convert.ToInt32(ConfigurationManager.AppSettings["ReLogDownloadEventInterval"]), factor = 60000;
            int interval = min * factor;// 4 * 1 minutes
            timer.Elapsed += new System.Timers.ElapsedEventHandler((object sender, System.Timers.ElapsedEventArgs e) =>
            {
                getLogData(iMachineNumber);
            });
            timer.Interval = interval;
            timer.Enabled = true;
            Console.WriteLine("Device(" + iMachineNumber + ") Register For Late Night Download");
        }

        private void getLogData(int iMachineNumber)
        {

            //Database Part
            string host = "114.31.10.244";
            string port = "1521";
            string service = "msoft";
            string userId = "maximlocal";
            string userPassword = "maxim1234";
            string constr = "Data Source=(DESCRIPTION=(ADDRESS =(PROTOCOL=tcp)(HOST=" + host + ")(PORT=" + port + "))(CONNECT_DATA=(SERVICE_NAME=" + service + ")));User Id=" + userId + ";Password=" + userPassword;

            int id = 0;

            //end Database Part
            OracleConnection con = new OracleConnection(constr);
            con.Open();

            //global data for setting

            string name = "";
            string password = "";
            int privilage = 0;
            bool bEnabled = false;

            //end global data for setting

            axCZKEM1.EnableDevice(iMachineNumber, false);
            if (axCZKEM1.ReadGeneralLogData(iMachineNumber))//read all the attendance records to the memory
            {
                while (axCZKEM1.SSR_GetGeneralLogData(iMachineNumber, out sdwEnrollNumber, out idwVerifyMode,
                            out idwInOutMode, out idwYear, out idwMonth, out idwDay, out idwHour, out idwMinute, out idwSecond, ref idwWorkcode))//get records from the memory
                {
                    iGLCount++;
                    axCZKEM1.SSR_GetUserInfo(iMachineNumber, sdwEnrollNumber, out name, out password, out privilage, out bEnabled);
                    string time = idwYear + "-" + idwMonth + "-" + idwDay + " " + idwHour + ":" + idwMinute + ":" + idwSecond;
                    Console.WriteLine("[" + iGLCount + "] User Id:" + sdwEnrollNumber + "(" + name + "), Time: (" + time + ")");


                    //Database save
                    try
                    {
                        OracleCommand cmd = con.CreateCommand();

                        cmd.CommandText = @"SELECT device_log_sq.nextval FROM dual";
                        Object dataObject = cmd.ExecuteScalar();

                        if (dataObject != null)
                        {
                            id = Convert.ToInt32(dataObject);
                        }
                        Console.WriteLine("ID: " + id);
                        cmd.CommandText = @"INSERT INTO device_log(id, userid, checktime, terminalid, name)"
                                    + "VALUES(:id, :userid, :checktime, :terminalid, :name)";

                        OracleParameter idParam = new OracleParameter();
                        idParam.DbType = System.Data.DbType.Int32;
                        idParam.Value = id;
                        idParam.ParameterName = "id";

                        OracleParameter userParam = new OracleParameter();
                        userParam.Value = sdwEnrollNumber;
                        userParam.ParameterName = "userid";

                        OracleParameter checktimeParam = new OracleParameter();
                        checktimeParam.DbType = System.Data.DbType.DateTime;
                        checktimeParam.Value = DateTime.Parse(time);
                        userParam.ParameterName = "checktime";

                        OracleParameter terminalParam = new OracleParameter();
                        terminalParam.DbType = System.Data.DbType.Int32;
                        terminalParam.Value = iMachineNumber;
                        terminalParam.ParameterName = "terminalid";

                        OracleParameter nameParam = new OracleParameter();
                        nameParam.Value = name;
                        nameParam.ParameterName = "name";

                        cmd.Parameters.Add(idParam);
                        cmd.Parameters.Add(userParam);
                        cmd.Parameters.Add(checktimeParam);
                        cmd.Parameters.Add(terminalParam);
                        cmd.Parameters.Add(nameParam);
                        int rowCount = cmd.ExecuteNonQuery();
                        Console.WriteLine("Row Count affected = " + rowCount);
                    }
                    catch (OracleException ex)
                    {
                        Console.WriteLine("Database Insert Exception: " + ex.ToString());
                    }

                    //end database save
                }

            }
            else
            {

                axCZKEM1.GetLastError(ref idwErrorCode);

                if (idwErrorCode != 0)
                {
                    Console.WriteLine("Reading data from terminal failed,ErrorCode: " + idwErrorCode.ToString(), "Error");
                }
                else
                {
                    Console.WriteLine("No data from terminal returns!", "Error");
                }
            }

            con.Close();
            con.Dispose();



        }


    }
}
