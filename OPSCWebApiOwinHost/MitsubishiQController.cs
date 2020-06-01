using HslCommunication;
using HslCommunication.Profinet.Melsec;
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
    [RoutePrefix("api/MitQ")]
    public class MitsubishiQController : ApiController
    {
        static JsonConfigHelper _JsonConfigHelperMitQ = new JsonConfigHelper(@"IpConfig/fcMitQ.json");
        string _IPConfigMitQ = _JsonConfigHelperMitQ["IPConfig"];
        protected readonly ILog log = LogManager.GetLogger("JNRSLogger");
        MelsecMcNet profinet = null;

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
                    profinet = new MelsecMcNet();
                    profinet.IpAddress = item.ip;
                    profinet.Port = item.port;

                    OperateResult connect = profinet.ConnectServer();
                    if (connect.IsSuccess)
                    {
                        log.Info("PLC连接成功！" + profinet.IpAddress);
                        Console.WriteLine("PLC连接成功！" + profinet.IpAddress);
                        //采集
                        foreach (ParaConfig pc in item.ParaConfig)
                        {
                            switch (pc.fields_name)
                            {
                                case "run_state":
                                case "product_category":
                                    pc.data_setup = new string[] { ReadUInt16DataNew(pc.data_addr).ToString() };
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
                                    pc.data_setup = new string[] { ReadInt32DataNew(pc.data_addr).ToString() };
                                    break;
                                case "alarm_no":
                                    byte[] alarmsgByte = _ReadManyBytes(pc.data_addr, ushort.Parse(pc.data_len.ToString()));
                                    pc.data_setup = AlarmNoString(alarmsgByte, pc.data_addr).ToArray();
                                    break;
                                default: break;
                            }
                            lstParaConfig.Add(pc);
                        }

                        //断开
                        profinet.ConnectClose();
                        //log.Info("PLC断开连接成功！");
                    }
                    else
                    {
                        log.Error("PLC连接失败！" + profinet.IpAddress);
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
        public int GetStateFromThreeColor(int threeColor)
        {
            //0 red;1 yellow;2 green (0,1,2表示点位)
            int rValue = 4;
            if (threeColor >= 4)
                rValue = 1;
            else if (threeColor == 1 || threeColor == 3)
                rValue = 2;
            else if (threeColor == 2)
                rValue = 3;
            return rValue;
        }
        #region 2020-2-12 根据地址从PLC获取数据
        /// <summary>
        /// 根据地址读取bit数据
        /// </summary>
        private bool readBoolData(string address)
        {
            try
            {
                OperateResult<bool> result = profinet.ReadBool(address);
                if (result.IsSuccess)
                {
                    return result.Content;
                }
                else
                {
                    log.Error(result.ToMessageShowString());
                    return false;
                }
            }
            catch (Exception ex)
            {
                log.Error("readBoolData读取失败：" + ex.Message);
                return false;
            }
        }
        /// <summary>
        /// 根据地址读取byte数据
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        private byte ReadByteDataNew(string address)
        {
            try
            {
                ushort len = 1;
                OperateResult<byte[]> result = profinet.Read(address, len);
                if (result.IsSuccess)
                {
                    return result.Content[0];
                }
                else
                {
                    log.Error("ReadByteDataNew 读取失败：" + result.ToMessageShowString());
                    return new byte();
                }
            }
            catch (Exception ex)
            {
                log.Error("ReadByteDataNew 读取异常：" + ex.Message);
                return new byte();
            }
        }
        /// <summary>
        /// 根据地址读取ushort数据
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        private ushort ReadUInt16DataNew(string address)
        {
            try
            {
                OperateResult<ushort> result = profinet.ReadUInt16(address);
                if (result.IsSuccess)
                {
                    return result.Content;
                }
                else
                {
                    log.Error("ReadByteDataNew 读取失败：" + result.ToMessageShowString());
                    return 0;
                }
            }
            catch (Exception ex)
            {
                log.Error("ReadByteDataNew 读取异常：" + ex.Message);
                return 0;
            }
        }
        /// <summary>
        /// 根据地址读取int数据
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        private int ReadInt32DataNew(string address)
        {
            try
            {
                OperateResult<int> result = profinet.ReadInt32(address);
                if (result.IsSuccess)
                {
                    return result.Content;
                }
                else
                {
                    log.Error("ReadByteDataNew 读取失败：" + result.ToMessageShowString());
                    return 0;
                }
            }
            catch (Exception ex)
            {
                log.Error("ReadByteDataNew 读取异常：" + ex.Message);
                return 0;
            }
        }
        //批量获取字节
        public static ushort PLC_MAX_Length = 999;//一次性取值最大长度
        public static ushort PLC_Byte_Length = 2;//欧姆龙按照Word存储，所以此处为2

        /// <summary>
        /// 分批读取字节
        /// </summary>
        /// <param name="address">起始地址</param>
        /// <param name="length">总长度</param>
        /// <returns>字节数组</returns>
        private byte[] _ReadManyBytes(string address, ushort length)
        {
            string PLC_DM_StartAddress = address;
            ushort PLC_DM_AddressLength = length;
            byte[] manybytes = null;
            if (PLC_DM_AddressLength <= PLC_MAX_Length)//PLC Byte读取最大长度
            {
                manybytes = ReadManyBytes(PLC_DM_StartAddress, PLC_DM_AddressLength);
            }
            else
            {
                manybytes = new byte[PLC_DM_AddressLength * PLC_Byte_Length];
                byte[] bytes_dm_tmp = null;
                string area = PLC_DM_StartAddress[0].ToString();
                int _PLC_DM_StartAddress = Convert.ToUInt16(PLC_DM_StartAddress.Substring(1));
                int k = PLC_DM_AddressLength / PLC_MAX_Length;//读取循环次数
                ushort l = (ushort)(PLC_DM_AddressLength % PLC_MAX_Length);//最后一次读取长度

                for (int i = 0; i <= k; i++)
                {
                    System.Threading.Thread.Sleep(50);//停顿50毫秒
                    if (i < k)
                    {
                        bytes_dm_tmp = null;
                        bytes_dm_tmp = ReadManyBytes(area + (_PLC_DM_StartAddress + i * PLC_MAX_Length), PLC_MAX_Length);
                        bytes_dm_tmp.CopyTo(manybytes, i * PLC_MAX_Length * PLC_Byte_Length);
                    }
                    else
                    {
                        bytes_dm_tmp = ReadManyBytes(area + (_PLC_DM_StartAddress + i * PLC_MAX_Length), l);
                        bytes_dm_tmp.CopyTo(manybytes, i * PLC_MAX_Length * PLC_Byte_Length);
                    }
                }
            }

            //log.Info("请求的长度(Word)：" + PLC_DM_AddressLength.ToString());
            //log.Info("返回的长度(Byte)：" + manybytes.Length.ToString());

            string strBuf = "";
            for (int i = 0; i < manybytes.Length; i++)
            {
                strBuf += manybytes[i].ToString("X2") + " ";
            }
            //log.Info("接收的数据如下:\n" + strBuf);

            return manybytes;
        }
        private byte[] ReadManyBytes(string address, ushort length)
        {
            byte[] bArr = new byte[length];
            try
            {
                OperateResult<byte[]> result = profinet.Read(address, length);
                if (result.IsSuccess)
                {
                    //log.Info("ReadManyBytes读取结果：" + result.Content.Length);
                    bArr = result.Content;
                }
                else
                {
                    log.Error("ReadManyBytes读取失败：" + result.ToMessageShowString());
                }
            }
            catch (Exception ex)
            {
                log.Error("ReadManyBytes读取异常：" + ex.Message);
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
