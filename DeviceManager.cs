
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using zkemkeeper;
using Oracle.ManagedDataAccess.Client;

namespace TripodAccessWithDisplayAndLogSaveOracleServiceR
{
    class DeviceManager
    {

        public zkemkeeper.CZKEMClass axCZKEM1 = new zkemkeeper.CZKEMClass();
        private int idwErrorCode;

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


        //Database Part
        string constr = null;
        long areaCode = 0;
        //end Database Part

        public DeviceManager()
        {
            //constr = "Data Source=(DESCRIPTION=(ADDRESS =(PROTOCOL=tcp)(HOST=" + host + ")(PORT=" + port + "))(CONNECT_DATA=(SERVICE_NAME=" + service + ")));User Id=" + userId + ";Password=" + userPassword;
            areaCode = Convert.ToInt64(ConfigurationManager.AppSettings["AreaId"]);
            string dbid = ConfigurationManager.AppSettings["DBUSRID"];
            string dbpwd = ConfigurationManager.AppSettings["DBPASSWD"];
            constr = "User ID=" + dbid + "; Password=" + dbpwd + "; Data Source=DSATTNLOG;";
        }

        public void intervalRunner(int iMachineNumber, string IPAddr)
        {
            System.Timers.Timer timer = new System.Timers.Timer();
            int min = Convert.ToInt32(ConfigurationManager.AppSettings["ReRegisterEventInterval"]), factor = 60000;
            int interval = min * factor;// 4 * 1 minutes
            timer.Elapsed += new System.Timers.ElapsedEventHandler((object sender, System.Timers.ElapsedEventArgs e) =>
            {

                if (axCZKEM1.GetDeviceIP(iMachineNumber, IPAddr))
                {
                    Console.WriteLine("---Device(" + iMachineNumber + ")[" + IPAddr + "]-Connected!Checking after Interval:=" + min + "(minutes)---");
                    //this.realEvent_OnAttTransaction(iMachineNumber[i]);
                }

            });
            timer.Interval = interval;
            timer.Enabled = true;

        }
        /** Late night Internval **/
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


        public void intervalRunner(int iMachineNumber, bool task)
        {
            string DailyTime = ConfigurationManager.AppSettings["LogDailyDownloadTime"];
            string[] timeParts = DailyTime.Split(new char[1] { ':' });
            DateTime dateNow = DateTime.Now;
            DateTime date = new DateTime(dateNow.Year, dateNow.Month, dateNow.Day,
                       int.Parse(timeParts[0]), int.Parse(timeParts[1]), int.Parse(timeParts[2]));
            TimeSpan ts;
            if (date > dateNow)
                ts = date - dateNow;
            else
            {
                date = date.AddDays(1);
                ts = date - dateNow;
            }
            //Console.WriteLine("Device(" + iMachineNumber + ") Log Downloading At Daily ("+ DailyTime + ")");
            //waits certan time and run the code
            Task.Delay(ts).ContinueWith((x) => getLogData(iMachineNumber));
        }


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
                //Console.WriteLine("Device Conection Fialed" + idwErrorCode);
                //Console.ReadLine();
                Program.writeErrorLog("Device Conection Fialed" + idwErrorCode);

                return idwErrorCode;
            }

        }

        public void isDisconnected()
        {
            axCZKEM1.Disconnect();
        }

        /** Event Maincast **/
        public void realEvent_OnAttTransaction(int iMachineNumber, string deviceIp)
        {

            if (axCZKEM1.RegEvent(iMachineNumber, 65535))
            {
                Console.WriteLine("Registering Realtime Event For Machine:" + iMachineNumber);
                axCZKEM1.OnAttTransactionEx += new zkemkeeper._IZKEMEvents_OnAttTransactionExEventHandler((string sEnrollNumber, int iIsInValid, int iAttState, int iVerifyMethod, int iYear, int iMonth, int iDay, int iHour, int iMinute, int iSecond, int iWorkCode) =>
                {
                    axCZKEM1_OnAttTransaction_SaveOnly(sEnrollNumber, iIsInValid, iAttState, iVerifyMethod, iYear, iMonth, iDay, iHour, iMinute, iSecond, iWorkCode, iMachineNumber, deviceIp);
                });
            }

        }

        //the actual event


        //save only
        public void axCZKEM1_OnAttTransaction_SaveOnly(string sEnrollNumber, int iIsInValid, int iAttState, int iVerifyMethod, int iYear, int iMonth, int iDay, int iHour, int iMinute, int iSecond, int iWorkCode, int MachineNo, string deviceIp)
        {

            Console.WriteLine("--------------- Per Log Device(" + MachineNo + ")[" + deviceIp + "] --------------");
            string time = iYear.ToString() + "-" + iMonth.ToString() + "-" + iDay.ToString() + " " + iHour.ToString() + ":" + iMinute.ToString() + ":" + iSecond.ToString();
            Console.WriteLine("Verified(" + MachineNo + ") [ UserID=" + sEnrollNumber + " isInvalid=" + iIsInValid.ToString() + " state=" + iAttState.ToString() + " verifystyle=" + iVerifyMethod.ToString() + " time=" + time + "]");
            //global data for setting

            string name = "";
            string password = "";
            int privilage = 0;
            bool bEnabled = false;

            //end global data for setting

            //Read Data From Machine



            axCZKEM1.EnableDevice(MachineNo, false);
            axCZKEM1.SSR_GetUserInfo(MachineNo, sEnrollNumber, out name, out password, out privilage, out bEnabled);
            Console.WriteLine("Name: " + name);

            axCZKEM1.EnableDevice(MachineNo, true);


            //end Read Data From Machine

            //Database Part
            int id = 0;
            //end Database Part
            OracleConnection con = new OracleConnection(constr);
            con.Open();



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
                cmd.CommandText = @"INSERT INTO device_log(id, userid, checktime, terminalid, name, area_id)"
                            + "VALUES(:id, :userid, :checktime, :terminalid, :name, :areaid)";

                OracleParameter idParam = new OracleParameter();
                idParam.DbType = System.Data.DbType.Int32;
                idParam.Value = id;
                idParam.ParameterName = "id";

                OracleParameter userParam = new OracleParameter();
                userParam.Value = sEnrollNumber;
                userParam.ParameterName = "userid";

                OracleParameter checktimeParam = new OracleParameter();
                checktimeParam.DbType = System.Data.DbType.DateTime;
                checktimeParam.Value = DateTime.Parse(time);
                userParam.ParameterName = "checktime";

                OracleParameter terminalParam = new OracleParameter();
                terminalParam.DbType = System.Data.DbType.Int32;
                terminalParam.Value = MachineNo;
                terminalParam.ParameterName = "terminalid";

                OracleParameter nameParam = new OracleParameter();
                nameParam.Value = name;
                nameParam.ParameterName = "name";

                OracleParameter arealParam = new OracleParameter();
                arealParam.DbType = System.Data.DbType.Int64;
                arealParam.Value = areaCode;
                arealParam.ParameterName = "areaid";

                cmd.Parameters.Add(idParam);
                cmd.Parameters.Add(userParam);
                cmd.Parameters.Add(checktimeParam);
                cmd.Parameters.Add(terminalParam);
                cmd.Parameters.Add(nameParam);
                cmd.Parameters.Add(arealParam);
                int rowCount = cmd.ExecuteNonQuery();
                Console.WriteLine("Row Count affected = " + rowCount);
            }
            catch (OracleException ex)
            {
                //Console.WriteLine("Database Insert Exception: " + ex.ToString());
                Program.writeErrorLog("Database Insert Exception: " + ex.ToString());
            }
            finally
            {
                con.Close();
                con.Dispose();
                con = null;
            }





            Console.WriteLine("-------End Per Log(" + MachineNo + ")[" + deviceIp + "] --------");
        }
        //end save only
        //end actual event

        /** End Event MainCast **/

        /** late night log **/

        private void getLogData(int iMachineNumber)
        {

            //Database Part
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
                        cmd.CommandText = @"INSERT INTO device_log(id, userid, checktime, terminalid, name, area_id)"
                                    + "VALUES(:id, :userid, :checktime, :terminalid, :name, :areaid)";

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

                        OracleParameter arealParam = new OracleParameter();
                        arealParam.DbType = System.Data.DbType.Int64;
                        arealParam.Value = areaCode;
                        arealParam.ParameterName = "areaid";

                        cmd.Parameters.Add(idParam);
                        cmd.Parameters.Add(userParam);
                        cmd.Parameters.Add(checktimeParam);
                        cmd.Parameters.Add(terminalParam);
                        cmd.Parameters.Add(nameParam);
                        cmd.Parameters.Add(arealParam);
                        int rowCount = cmd.ExecuteNonQuery();
                        Console.WriteLine("Row Count affected = " + rowCount);
                    }
                    catch (OracleException ex)
                    {
                        continue;
                        //Console.WriteLine("Database Insert Exception: " + ex.ToString());

                    }

                    //end database save
                }

            }
            else
            {

                axCZKEM1.GetLastError(ref idwErrorCode);

                if (idwErrorCode != 0)
                {
                    //Console.WriteLine("Reading data from terminal failed,ErrorCode: " + idwErrorCode.ToString(), "Error");
                    Program.writeErrorLog("Reading data from terminal failed,ErrorCode: " + idwErrorCode.ToString());
                }
                else
                {
                    Program.writeErrorLog("No data from terminal returns!");
                }
            }
            axCZKEM1.EnableDevice(iMachineNumber, true);

            con.Close();
            con.Dispose();



        }

        /** end late night log **/
    }
}
