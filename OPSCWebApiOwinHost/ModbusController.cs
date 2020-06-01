using HslCommunication;
using HslCommunication.ModBus;
using JNRSWebApiOwinHost.Models;
using log4net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web;
using System.Web.Http;

namespace JNRSWebApiOwinHost
{
    [RoutePrefix("api/Modbus")]
    public class ModbusController : ApiController
    {
        static JsonConfigHelper _JsonConfigHelperMitQ = new JsonConfigHelper(@"IpConfig/fcModbus.json");
        string _IPConfigMitQ = _JsonConfigHelperMitQ["IPConfig"];
        protected readonly ILog log = LogManager.GetLogger("JNRSLogger");
        
        [Route("Test")]
        [HttpGet]
        //测试
        public IHttpActionResult TestAPI(string a, string b)
        {
            //http://192.168.1.104:9086/api/Omron/Test?a=1&b=2
            Console.WriteLine(a + "-" + b);
            return Ok();
        }

        [Route("GetState")]
        [HttpGet]
        public IHttpActionResult GetState()
        {
            //http://127.0.0.1:9086/api/Siemens/GetState
            DateTime begintime = DateTime.Now;
            Console.WriteLine("MitQ/GetState开始调用-> " + begintime.ToString("yyyy-MM-dd HH:mm:ss.fff"));
            log.Info("MitQ/GetState开始调用-> " + begintime.ToString("yyyy-MM-dd HH:mm:ss.fff"));

            List<ParaConfig> lstParaConfig = new List<ParaConfig>();
            List<IPConfig> lstIPConfig = JsonConvert.DeserializeObject<List<IPConfig>>(_IPConfigMitQ);
            try
            {
                foreach (IPConfig item in lstIPConfig)
                {
                    //连接
                    ModbusTcpNet busTcpClient = null;
                    busTcpClient?.ConnectClose();
                    busTcpClient = new ModbusTcpNet(item.ip, item.port, 1);
                    busTcpClient.AddressStartWithZero = true;
                    busTcpClient.SetLoginAccount("", "");
                    busTcpClient.IsStringReverse = true;

                    OperateResult connect = busTcpClient.ConnectServer();
                    if (connect.IsSuccess)
                    {
                        log.Info("PLC连接成功！" + item.ip);
                        Console.WriteLine("PLC连接成功！" + item.ip);
                        //采集
                        foreach (ParaConfig pc in item.ParaConfig)
                        {
                            switch (pc.fields_name)
                            {
                                case "run_state":
                                    bool[] leds = ReadBoolMore(pc.data_addr, ushort.Parse("3"), busTcpClient);
                                    int run_state = 0;
                                    if (leds.Length == 3)
                                    {
                                        run_state = GetStateFromThreeColor(Convert.ToInt16(leds[0]), Convert.ToInt16(leds[1]), Convert.ToInt16(leds[2]));
                                    }
                                    pc.data_setup = new string[] { run_state.ToString() };
                                    break;
                                case "product_category":
                                    pc.data_setup = new string[] { ReadInt16DataOnly(pc.data_addr, busTcpClient).ToString() };
                                    break;
                                case "slcount_day":
                                case "slcount_shift":
                                case "xlcount_day":
                                case "xlcount_shift":
                                case "npass_prod_num_day":
                                case "npass_prod_num_shift":
                                case "achieving_rate_day":
                                case "achieving_rate_shift":
                                case "qualified_rate_day":
                                case "qualified_rate_shift":
                                case "unqualified_rate_day":
                                case "unqualified_rate_shift":
                                case "act_axis_0":
                                case "act_axis_1":
                                case "act_axis_2":
                                case "robot_speed":
                                case "robot_override":
                                    pc.data_setup = new string[] { ReadInt32DataOnly(pc.data_addr, busTcpClient).ToString() };
                                    break;
                                case "alarm_no":
                                    List<string> alarmsgByte = readUserAlarmInfo(pc.data_addr, ushort.Parse(pc.data_len.ToString()), busTcpClient);
                                    pc.data_setup = alarmsgByte.ToArray();
                                    break;
                                default: break;
                            }
                            lstParaConfig.Add(pc);
                        }

                        //断开
                        busTcpClient.ConnectClose();
                        //log.Info("PLC断开连接成功！");
                    }
                    else
                    {
                        log.Error("PLC连接失败！" + item.ip);
                        //
                        foreach (ParaConfig pc in item.ParaConfig)
                        {
                            pc.data_setup = new string[] { "0" };
                            lstParaConfig.Add(pc);
                        }
                        //return BadRequest("PLC连接失败：" + profinet.IpAddress);
                    }
                }

                DateTime endtime = DateTime.Now;
                string tick = ExecDateDiff(begintime, endtime);
                Console.WriteLine("耗时" + tick + "毫秒\n");
                log.Info("耗时" + tick + "毫秒\n");
                //
                return Json(lstParaConfig);
            }
            catch (Exception ex)
            {
                log.Error(ex.Message);
                //log.Error("Driver Error\n\nCould not initalize adapter - TCP/IP    \nConnection aborted");
                return BadRequest("获取状态失败：" + ex.Message);
            }
        }
        /// <summary>
        /// 程序执行时间测试
        /// </summary>
        /// <param name="dateBegin">开始时间</param>
        /// <param name="dateEnd">结束时间</param>
        /// <returns>返回(秒)单位，比如: 0.00239秒</returns>
        public static string ExecDateDiff(DateTime dateBegin, DateTime dateEnd)
        {
            TimeSpan ts1 = new TimeSpan(dateBegin.Ticks);
            TimeSpan ts2 = new TimeSpan(dateEnd.Ticks);
            TimeSpan ts3 = ts1.Subtract(ts2).Duration();
            //你想转的格式
            return ts3.TotalMilliseconds.ToString();
        }
        /// <summary>
        /// 三色灯
        /// </summary>    
        public int GetStateFromThreeColor(int run_bit, int alarm_bit, int pause_bit)
        {
            int runState = 0;
            bool run = Convert.ToBoolean(run_bit);
            bool alarm = Convert.ToBoolean(alarm_bit);
            bool pause = Convert.ToBoolean(pause_bit);

            if (alarm)
            {
                runState = 2;
            }
            if (pause)
            {
                runState = 3;
            }
            if (run)
            {
                runState = 1;
            }

            if (!run && !alarm && !pause)
            {
                runState = 4;
            }
            if (run && alarm) 
            {
                runState = 1;
            }

            return runState;
        }
        #region 2020-2-12 根据地址从PLC获取数据
        public bool ReadboolDataOnly(string address, ModbusTcpNet busTcpClient)
        {
            bool rValue = false;
            string area = address.Substring(0, 2);
            string address_u = address.Substring(2);
            try
            {
                if (area == "CS")//读线圈-COIL STATUS
                {
                    OperateResult<bool> result = busTcpClient.ReadCoil(address_u);
                    if (result.IsSuccess)
                    {
                        rValue = result.Content;
                    }
                }
                else if (area == "IS")//读输入-INPUT STATUS
                {
                    OperateResult<bool> result = busTcpClient.ReadDiscrete(address_u);
                    if (result.IsSuccess)
                    {
                        rValue = result.Content;
                    }
                }
                else
                {
                    log.Error("ReadboolDataOnly配置地址不是区域类型");
                    return false;
                }
            }
            catch (Exception ex)
            {
                log.Error("ReadboolDataOnly读取异常：" + ex.Message);
            }
            return rValue;
        }
        public int ReadInt16DataOnly(string address, ModbusTcpNet busTcpClient)
        {
            int rValue = 0;
            string area = address.Substring(0, 2);
            string address_u = address.Substring(2);
            try
            {
                if (area == "IR")//读取输入寄存器-INPUT REGISTER
                {
                    OperateResult<short> result = busTcpClient.ReadInt16(address_u);
                    if (result.IsSuccess)
                    {
                        rValue = result.Content;
                    }
                }
                else if (area == "HR")//读取保持/输出寄存器-HOLDING REGISTER
                {
                    OperateResult<short> result = busTcpClient.ReadInt16(address_u);
                    if (result.IsSuccess)
                    {
                        rValue = result.Content;
                    }
                }
                else
                {
                    log.Error("ReadInt16DataOnly配置地址不是区域类型");
                    return 0;
                }
            }
            catch (Exception ex)
            {
                log.Error("ReadInt16DataOnly读取异常：" + ex.Message);
            }
            return rValue;
        }
        public int ReadInt32DataOnly(string address, ModbusTcpNet busTcpClient)
        {
            int rValue = 0;
            string area = address.Substring(0, 2);
            string address_u = address.Substring(2);
            try
            {
                if (area == "IR")//读取输入寄存器-INPUT REGISTER
                {
                    OperateResult<int> result = busTcpClient.ReadInt32(address_u);
                    if (result.IsSuccess)
                    {
                        rValue = result.Content;
                    }
                }
                else if (area == "HR")//读取保持/输出寄存器-HOLDING REGISTER
                {
                    OperateResult<int> result = busTcpClient.ReadInt32(address_u);
                    if (result.IsSuccess)
                    {
                        rValue = result.Content;
                    }
                }
                else
                {
                    log.Error("ReadInt32DataOnly配置地址不是区域类型");
                    return 0;
                }
            }
            catch (Exception ex)
            {
                log.Error("ReadInt32DataOnly读取异常：" + ex.Message);
            }
            return rValue;
        }
        private bool[] ReadBoolMore(string address, ushort length, ModbusTcpNet busTcpClient)
        {
            bool[] bArr = new bool[length];
            string area = address.Substring(0, 2);
            string address_u = address.Substring(2);
            try
            {
                if (area == "CS")//读线圈-COIL STATUS
                {
                    OperateResult<bool[]> result = busTcpClient.ReadBool(address_u, length);
                    if (result.IsSuccess)
                    {
                        bArr = result.Content;
                    }
                }
                else if (area == "IS")//读输入-INPUT STATUS
                {
                    OperateResult<bool[]> result = busTcpClient.ReadBool(address_u, length);
                    if (result.IsSuccess)
                    {
                        bArr = result.Content;
                    }
                }
                else
                {
                    log.Error("ReadBoolMore配置地址不是区域类型");
                }
            }
            catch (Exception ex)
            {
                log.Error("ReadBoolMore读取异常：" + ex.Message);
            }
            return bArr;
        }
        /// <summary>
        /// 报警点
        /// </summary>
        private List<string> readUserAlarmInfo(string pDataAddr, int pDataLen,  ModbusTcpNet master)
        {
            Console.WriteLine("readUserAlarmInfo");
            bool[] liststr = new bool[900];
            List<string> lstAlarmNo = new List<string>();
            try
            {
                //采集报警信息                
                string str = pDataAddr.Substring(0, 2);
                int startAddr = Convert.ToInt16(pDataAddr.Substring(2));
                liststr = ReadInputsMore(ushort.Parse(startAddr.ToString()), ushort.Parse(pDataLen.ToString()), master);
                Console.WriteLine("liststr.Count:" + liststr.Length);
               
                int his_no = 0;
                string his_msg = "";
                for (int i = 0; i < pDataLen; i++)
                {
                    if (liststr[i])
                    {
                        his_no = startAddr + i;
                        his_msg = str + his_no.ToString();
                        lstAlarmNo.Add(his_no.ToString());
                    }
                }                
            }
            catch (Exception e)
            {
                log.Info("报警信息get失败，请检查:---" + e.Message);
            }
            return lstAlarmNo;
        }
        /// <summary>
        /// 批量读输入-INPUT STATUS
        /// </summary>
        /// <param name="address">起始地址</param>
        /// <param name="length">总长度</param>
        /// <returns>位数组</returns>
        private bool[] ReadInputsMore(ushort address, ushort length, ModbusTcpNet busTcpClient)
        {
            bool[] bArr = new bool[length];
            try
            {
                OperateResult<bool[]> result = busTcpClient.ReadDiscrete(address.ToString(), length);
                if (result.IsSuccess)
                {
                    bArr = result.Content;
                }
            }
            catch (Exception ex)
            {
                log.Error("ReadInputsMore读取异常：" + ex.Message);
            }
            return bArr;
        }
        private List<string> AlarmNoString(byte[] alarmsgByte, string PDataAddr)
        {
            List<string> lan = new List<string>();
            //报警
            if (alarmsgByte != null && alarmsgByte.Length > 0)
            {
                int addr_start = 0;
                string firstStr = "D";
                addr_start = int.Parse(PDataAddr.Substring(1));
                firstStr = PDataAddr.Substring(0, 1);
                //
                DateTime adate = DateTime.Now;
                byte rValue;
                string tmpStr = "";
                string sdata = "";
                for (int i = 0; i < alarmsgByte.Length; i++)
                {
                    rValue = alarmsgByte[i];
                    tmpStr = Convert.ToString(rValue, 2).PadLeft(8, '0');
                    sdata = ReverseB(tmpStr);
                    //log.Info("alarm_byte:" + sdata);
                    string _No = (addr_start + i).ToString();
                    for (int j = 0; j < sdata.Length; j++)
                    {
                        if (sdata[j] == '1')
                        {
                            string alarm_msg = "";
                            if (firstStr.Length == 1)
                            {
                                alarm_msg = firstStr + _No + "." + j.ToString();
                            }
                            else
                            {
                                alarm_msg = firstStr + "." + _No + "." + j.ToString();
                            }

                            lan.Add(alarm_msg);
                        }
                    }
                }
            }

            return lan;
        }
        /// <summary>
        /// 反转字符串
        /// </summary>
        public string ReverseB(string text)
        {
            char[] charArray = text.ToCharArray();
            Array.Reverse(charArray);
            return new string(charArray);
        }
        #endregion
    }
}
