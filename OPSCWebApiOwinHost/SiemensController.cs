using HslCommunication;
using HslCommunication.Profinet.Siemens;
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
using System.Net.Sockets;
using System.Text;
using System.Web;
using System.Web.Http;

namespace JNRSWebApiOwinHost
{
    [RoutePrefix("api/Siemens")]
    public class SiemensController : ApiController
    {
        //初始化
        protected readonly ILog log = LogManager.GetLogger("JNRSLogger");
        static string[] storeAddr = ConfigurationManager.AppSettings["storeAddr"].Split(',');//机器人料库点位183个（本次10个）
        // static string[] storeAddr2 = ConfigurationManager.AppSettings["storeAddr2"].Split(',');//出库20个（本次2个）
        static string[] storeAddr3 = ConfigurationManager.AppSettings["storeAddr3"].Split(',');//缓存料道入库1个（本次1个）
        static string[] plcMonAddr = ConfigurationManager.AppSettings["plcMonAddr"].Split(',');
        static string[] addrSetFeedingToMachine = ConfigurationManager.AppSettings["addrSetFeedingToMachine"].Split(',');
        static string[] addrSetChangeToMachine = ConfigurationManager.AppSettings["addrSetChangeToMachine"].Split(',');
        static string[] addrSetUnloadToChannel = ConfigurationManager.AppSettings["addrSetUnloadToChannel"].Split(',');
        static string[] addrSetOutStock = ConfigurationManager.AppSettings["addrSetOutStock"].Split(',');
        static string[] addrRequestOutStock = ConfigurationManager.AppSettings["addrRequestOutStock"].Split(',');
        static string[] addrRequestInStock = ConfigurationManager.AppSettings["addrRequestInStock"].Split(',');
        //声明
        SiemensS7Net profinet = new SiemensS7Net(SiemensPLCS.S1500, storeAddr[0]);
        OperateResult connect = null;
        //连接PLC
        private bool _ServerIsConnected = false;
        public bool ServerIsConnected
        {
            get
            {
                if (!_ServerIsConnected)
                {
                    connect = profinet.ConnectServer();
                    if (connect.IsSuccess)
                        _ServerIsConnected = true;
                    else
                        _ServerIsConnected = false;
                }

                return _ServerIsConnected;
            }
        }
        void ShowAndRecordInfo(string msg)
        {
            Console.WriteLine(msg);
            log.Info(msg);
        }
        void ShowAndRecordError(string errorMsg)
        {
            Console.WriteLine(errorMsg);
            log.Error(errorMsg);
        }
        /// <summary>
        /// 立库所有库位扫描(XXX)
        /// </summary>
        /// <returns></returns>
        [Route("GetState")]
        [HttpGet]
        public IHttpActionResult GetState()
        {
            //http://127.0.0.1:9088/api/Siemens/GetState
            DateTime begintime = DateTime.Now;
            ShowAndRecordInfo("立库所有库位扫描 -> " + begintime.ToString("yyyy-MM-dd HH:mm:ss.fff"));

            List<StoreModel> lstStore = new List<StoreModel>();
            StoreModel _Store = null;
            try
            {
                if (ServerIsConnected)
                {
                    #region 机器人料库点位183个（本次10个）
                    ushort _data_num = ushort.Parse(storeAddr[3].ToString());//采集数据单元数量
                    ushort _data_len = ushort.Parse(storeAddr[4].ToString());//采集数据单元字节长度
                    int _data_len_jd = _data_num * _data_len;
                    //一次性获取所有数据
                    byte[] alarmsgByte = _ReadManyBytes(storeAddr[2], ushort.Parse(_data_len_jd.ToString()));
                    //拆分数据
                    for (int i = 0; i < alarmsgByte.Length / _data_len; i++)
                    {
                        _Store = new StoreModel();

                        _Store.location_no = ReadInt32Data(alarmsgByte, 0 + i * _data_len).ToString();
                        _Store.workpiece_type = ReadInt32Data(alarmsgByte, 4 + i * _data_len).ToString();
                        _Store.workpiece_status = ReadByteData(alarmsgByte, 9 + i * _data_len).ToString();
                        //_Store.mac_proc_info = ReadByteData(alarmsgByte, 10 + i * _data_len).ToString();

                        lstStore.Add(_Store);
                    }
                    #endregion

                    #region 出库20个（本次2个）
                    //ushort _data_num2 = ushort.Parse(storeAddr2[3].ToString());//采集数据单元数量
                    //ushort _data_len2 = ushort.Parse(storeAddr2[4].ToString());//采集数据单元字节长度
                    //int _data_len_jd2 = _data_num2 * _data_len2;
                    ////一次性获取所有数据
                    //byte[] alarmsgByte2 = _ReadManyBytes(storeAddr2[2], ushort.Parse(_data_len_jd2.ToString()));
                    ////拆分数据
                    //for (int i = 0; i < alarmsgByte2.Length / _data_len2; i++)
                    //{
                    //    _Store = new StoreModel();

                    //    _Store.location_no = ReadInt32Data(alarmsgByte2, 0 + i * _data_len2).ToString();
                    //    _Store.workpiece_type = ReadInt32Data(alarmsgByte2, 40 + i * _data_len2).ToString();
                    //    _Store.workpiece_status = ReadByteData(alarmsgByte2, 60 + i * _data_len2).ToString();
                    //    _Store.mac_proc_info = ReadByteData(alarmsgByte2, 66 + i * _data_len2).ToString();

                    //    lstStore.Add(_Store);
                    //}
                    #endregion
                }
                else
                {
                    ShowAndRecordError("PLC连接失败！" + profinet.IpAddress);
                }
                //
                return Json(lstStore);
            }
            catch (Exception ex)
            {
                ShowAndRecordError(ex.Message);
                return BadRequest("获取状态失败：" + ex.Message);
            }
        }
        /// <summary>
        /// PLC请求信号扫描
        /// </summary>
        /// <returns></returns>
        [Route("GetMonitorPLC")]
        [HttpGet]
        public IHttpActionResult GetMonitorPLC()
        {
            //http://127.0.0.1:9088/api/Siemens/GetMonitorPLC
            DateTime begintime = DateTime.Now;
            ShowAndRecordInfo("监听机床号与请求 -> " + begintime.ToString("yyyy-MM-dd HH:mm:ss.fff"));

            int plcMonData = 0;
            int plcMonData2 = 0;
            string rValue = "";
            try
            {
                if (ServerIsConnected)
                {
                    //采集
                    plcMonData = ReadByteDataNew(plcMonAddr[3]);//机床号 1~6
                    plcMonData2 = ReadByteDataNew(plcMonAddr[2]);//机床请求类型 1~4
                    rValue = plcMonData.ToString() + plcMonData2.ToString();
                    //
                    ShowAndRecordInfo("读取" + plcMonAddr[3] + "结果：" + plcMonData);
                    ShowAndRecordInfo("读取" + plcMonAddr[2] + "结果：" + plcMonData2);
                }
                else
                {
                    ShowAndRecordError("PLC连接失败！" + profinet.IpAddress);
                }
                //
                return Json(rValue);
            }
            catch (Exception ex)
            {
                ShowAndRecordError(ex.Message);
                return BadRequest("获取PLC请求信号失败：" + ex.Message);
            }
        }
        /// <summary>
        /// PLC请求信号扫描
        /// </summary>
        /// <returns></returns>
        [Route("GetMonitorPLCOutStock")]
        [HttpGet]
        public IHttpActionResult GetMonitorPLCOutStock()
        {
            //http://127.0.0.1:9088/api/Siemens/GetMonitorPLCOutStock
            DateTime begintime = DateTime.Now;
            ShowAndRecordInfo("监听出库请求-> " + begintime.ToString("yyyy-MM-dd HH:mm:ss.fff"));
            int plcMonData = 0;
            try
            {
                if (ServerIsConnected)
                {
                    //采集
                    plcMonData = ReadByteDataNew(plcMonAddr[6]);//PLC出库请求应答
                    //
                    ShowAndRecordInfo("读取" + plcMonAddr[6] + "结果：" + plcMonData);
                }
                else
                {
                    ShowAndRecordError("PLC连接失败！" + profinet.IpAddress);
                }
                //
                return Json(plcMonData);
            }
            catch (Exception ex)
            {
                ShowAndRecordError(ex.Message);
                return BadRequest("获取PLC请求信号失败：" + ex.Message);
            }
        }
        /// <summary>
        /// PLC出库完成信号
        /// </summary>
        /// <returns></returns>
        [Route("GetMonitorPLCOutStockComplete")]
        [HttpGet]
        public IHttpActionResult GetMonitorPLCOutStockComplete()
        {
            //http://127.0.0.1:9088/api/Siemens/GetMonitorPLCOutStockComplete
            DateTime begintime = DateTime.Now;
            ShowAndRecordInfo("监听出库完成信号 -> " + begintime.ToString("yyyy-MM-dd HH:mm:ss.fff"));
            int plcMonData = 0;
            try
            {
                if (ServerIsConnected)
                {
                    //采集
                    plcMonData = ReadByteDataNew(plcMonAddr[7]);//PLC出库请求应答
                    //
                    ShowAndRecordInfo("读取" + plcMonAddr[7] + "结果：" + plcMonData);
                }
                else
                {
                    ShowAndRecordError("PLC连接失败！" + profinet.IpAddress);
                }
                //
                return Json(plcMonData);
            }
            catch (Exception ex)
            {
                ShowAndRecordError(ex.Message);
                return BadRequest("获取PLC请求信号失败：" + ex.Message);
            }
        }
        /// <summary>
        /// 响应机床上料请求
        /// 机床上料 参数：库位号，程序号，工件种类 
        /// 操作：向对应地址写1，写库位号，程序号，工件种类
        /// </summary>
        /// <returns></returns>
        [Route("SetFeedingToMachine")]
        [HttpGet]
        public IHttpActionResult SetFeedingToMachine(string location_no, string program_number, string workpiece_type)
        {
            //http://127.0.0.1:9088/api/Siemens/SetFeedingToMachine
            DateTime begintime = DateTime.Now;
            ShowAndRecordInfo("响应机床上料请求 -> " + begintime.ToString("yyyy-MM-dd HH:mm:ss.fff"));
            bool rValue = false;
            try
            {
                if (ServerIsConnected)
                {
                    //写入
                    if (writeByte(plcMonAddr[1], 1)
                        && writeInt(addrSetFeedingToMachine[0], Convert.ToUInt16(location_no))
                        && writeInt(addrSetFeedingToMachine[2], Convert.ToUInt16(workpiece_type)))
                    {
                        rValue = true;
                        //
                        ShowAndRecordInfo("成功写入" + plcMonAddr[1] + "值：" + 1);
                        ShowAndRecordInfo("成功写入" + addrSetFeedingToMachine[0] + "值：" + location_no);
                        ShowAndRecordInfo("成功写入" + addrSetFeedingToMachine[2] + "值：" + workpiece_type);
                    }
                    else
                    {
                        ShowAndRecordError("响应机床上料请求失败");
                    }
                }
                else
                {
                    ShowAndRecordError("PLC连接失败！" + profinet.IpAddress);
                }
                //
                return Json(rValue);
            }
            catch (Exception ex)
            {
                ShowAndRecordError(ex.Message);
                return BadRequest("写入PLC机床上料数据失败：" + ex.Message);
            }
        }

        /// <summary>
        /// 响应机床换料请求
        /// 机床换料 参数：库位号，程序号，工件种类 
        /// 操作：向对应地址写1，写库位号，程序号，工件种类
        /// </summary>
        /// <returns></returns>
        [Route("SetChangeToMachine")]
        [HttpGet]
        public IHttpActionResult SetChangeToMachine(string location_no, string location_no_old, string program_number, string workpiece_type)
        {
            //http://127.0.0.1:9088/api/Siemens/SetChangeToMachine
            DateTime begintime = DateTime.Now;
            ShowAndRecordInfo("响应机床换料请求 -> " + begintime.ToString("yyyy-MM-dd HH:mm:ss.fff"));
            bool rValue = false;
            try
            {
                if (ServerIsConnected)
                {
                    //采集
                    if (writeByte(plcMonAddr[1], 1)
                        && writeInt(addrSetChangeToMachine[0], Convert.ToUInt16(location_no))
                        && writeInt(addrSetChangeToMachine[1], Convert.ToUInt16(location_no_old))
                        && writeInt(addrSetChangeToMachine[3], Convert.ToUInt16(workpiece_type)))
                    {
                        rValue = true;
                        //
                        ShowAndRecordInfo("成功写入" + plcMonAddr[1] + "值：" + 1);
                        ShowAndRecordInfo("成功写入" + addrSetChangeToMachine[0] + "值：" + location_no);
                        ShowAndRecordInfo("成功写入" + addrSetChangeToMachine[1] + "值：" + location_no_old);
                        ShowAndRecordInfo("成功写入" + addrSetChangeToMachine[3] + "值：" + workpiece_type);
                    }
                    else
                    {
                        ShowAndRecordError("响应机床换料请求失败");
                    }
                }
                else
                {
                    ShowAndRecordError("PLC连接失败！" + profinet.IpAddress);
                }
                //
                return Json(rValue);
            }
            catch (Exception ex)
            {
                ShowAndRecordError(ex.Message);
                return BadRequest("写入PLC机床上料数据失败：" + ex.Message);
            }
        }

        /// <summary>
        /// 响应机床卸料请求
        /// 机床卸料 参数：库位号 
        /// 操作：向对应地址写1，写库位号
        /// </summary>
        /// <returns></returns>
        [Route("SetUnloadToChannel")]
        [HttpGet]
        public IHttpActionResult SetUnloadToChannel(string location_no)
        {
            //http://127.0.0.1:9088/api/Siemens/SetUnloadToChannel
            DateTime begintime = DateTime.Now;
            ShowAndRecordInfo("响应机床卸料请求 -> " + begintime.ToString("yyyy-MM-dd HH:mm:ss.fff"));
            bool rValue = false;
            try
            {
                if (ServerIsConnected)
                {
                    //采集
                    if (writeByte(plcMonAddr[1], 1)
                        && writeInt(addrSetUnloadToChannel[0], Convert.ToUInt16(location_no)))
                    {
                        rValue = true;
                        //
                        ShowAndRecordInfo("成功写入" + plcMonAddr[1] + "值：" + 1);
                        ShowAndRecordInfo("成功写入" + addrSetUnloadToChannel[0] + "值：" + location_no);
                    }
                    else
                    {
                        ShowAndRecordError("响应机床卸料请求失败");
                    }
                }
                else
                {
                    ShowAndRecordError("PLC连接失败！" + profinet.IpAddress);
                }
                //
                return Json(rValue);
            }
            catch (Exception ex)
            {
                ShowAndRecordError(ex.Message);
                return BadRequest("写入PLC机床卸料数据失败：" + ex.Message);
            }
        }
        /// <summary>
        /// 响应出库请求 总控控制命令标志位
        /// 出库至缓存料道 参数：库位号 
        /// 操作：向对应地址写1，写库位号
        /// </summary>
        /// <returns></returns>
        [Route("SetOutStock")]
        [HttpGet]
        public IHttpActionResult SetOutStock(string location_no)
        {
            //http://127.0.0.1:9088/api/Siemens/SetOutStock
            DateTime begintime = DateTime.Now;
            ShowAndRecordInfo("响应出库请求 -> " + begintime.ToString("yyyy-MM-dd HH:mm:ss.fff"));
            bool rValue = false;
            try
            {
                if (ServerIsConnected)
                {
                    //采集
                    if (writeByte(plcMonAddr[5], 1)
                        && writeInt(addrSetOutStock[0], Convert.ToUInt16(location_no)))
                    {
                        rValue = true;
                        //
                        ShowAndRecordInfo("成功写入" + plcMonAddr[5] + "值：" + 1);
                        ShowAndRecordInfo("成功写入" + addrSetOutStock[0] + "值：" + location_no);
                    }
                    else
                    {
                        ShowAndRecordError("响应出库请求失败");
                    }
                }
                else
                {
                    ShowAndRecordError("PLC连接失败！" + profinet.IpAddress);
                }
                //
                return Json(rValue);
            }
            catch (Exception ex)
            {
                ShowAndRecordError(ex.Message);
                return BadRequest("写入PLC出库数据失败：" + ex.Message);
            }
        }
        /// <summary>
        /// 出库请求复位
        /// </summary>
        /// <returns></returns>
        [Route("RequestOutStock")]
        [HttpGet]
        public IHttpActionResult RequestOutStock()
        {
            //http://127.0.0.1:9088/api/Siemens/RequestOutStock
            DateTime begintime = DateTime.Now;
            ShowAndRecordInfo("出库请求复位 -> " + begintime.ToString("yyyy-MM-dd HH:mm:ss.fff"));
            bool rValue = false;
            try
            {
                if (ServerIsConnected)
                {
                    //采集
                    int plcControl = ReadByteDataNew(plcMonAddr[6]);
                    //
                    ShowAndRecordInfo("读取" + plcMonAddr[6] + "结果：" + plcControl);
                    if (plcControl == 0)
                    {
                        rValue = writeByte(addrRequestOutStock[0], 0);
                        if (rValue)
                            ShowAndRecordInfo("成功写入" + addrRequestOutStock[0] + "值：" + 0);
                        else
                        {
                            ShowAndRecordError("出库请求复位失败");
                        }
                    }
                    else
                    {
                        ShowAndRecordInfo(plcMonAddr[6] + "出库请求复位无需复位");
                    }
                }
                else
                {
                    ShowAndRecordError("PLC连接失败！" + profinet.IpAddress);
                }
                //
                return Json(rValue);
            }
            catch (Exception ex)
            {
                ShowAndRecordError(ex.Message);
                return BadRequest("写入PLC出库请求数据失败：" + ex.Message);
            }
        }

        /// <summary>
        /// 机器人控制命令标志位复位后，总控控制命令标志位同步复位
        /// </summary>
        /// <returns></returns>
        [Route("ResetRequestInfo")]
        [HttpGet]
        public IHttpActionResult ResetRequestInfo()
        {
            //http://127.0.0.1:9088/api/Siemens/ResetRequestInfo
            DateTime begintime = DateTime.Now;
            ShowAndRecordInfo("总控控制命令标志位同步复位 -> " + begintime.ToString("yyyy-MM-dd HH:mm:ss.fff"));
            bool rValue = false;
            try
            {
                if (ServerIsConnected)
                {
                    //采集
                    rValue = writeByte(plcMonAddr[1], 0);
                    if (rValue)
                        ShowAndRecordInfo("成功写入" + plcMonAddr[1] + "值：" + 0);
                    else
                    {
                        ShowAndRecordError("总控控制命令标志位同步复位失败");
                    }
                }
                else
                {
                    ShowAndRecordError("PLC连接失败！" + profinet.IpAddress);
                }
                //
                return Json(rValue);
            }
            catch (Exception ex)
            {
                ShowAndRecordError(ex.Message);
                return BadRequest("总控控制命令标志位置零：" + ex.Message);
            }
        }
        /// <summary>
        /// 出库完成信号复位后，总控出库请求复位信号同步复位
        /// </summary>
        /// <returns></returns>
        [Route("ResetRequestInfoOutStock")]
        [HttpGet]
        public IHttpActionResult ResetRequestInfoOutStock()
        {
            //http://127.0.0.1:9088/api/Siemens/ResetRequestInfoOutStock
            DateTime begintime = DateTime.Now;
            ShowAndRecordInfo("总控出库请求复位信号同步复位 -> " + begintime.ToString("yyyy-MM-dd HH:mm:ss.fff"));
            bool rValue = false;
            try
            {
                if (ServerIsConnected)
                {
                    //采集
                    int plcControl = ReadByteDataNew(plcMonAddr[7]);
                    if (plcControl == 1)
                    {
                        rValue = writeByte(plcMonAddr[8], 1);
                        if (rValue)
                            ShowAndRecordInfo("成功写入" + plcMonAddr[8] + "值：" + 1);
                        else
                        {
                            ShowAndRecordError("总控出库请求复位信号同步复位失败");
                        }
                    }
                    else
                    {
                        ShowAndRecordInfo(plcMonAddr[6] + "总控出库请求复位信号无需复位");
                    }
                }
                else
                {
                    ShowAndRecordError("PLC连接失败！" + profinet.IpAddress);
                }
                //
                return Json(rValue);
            }
            catch (Exception ex)
            {
                ShowAndRecordError(ex.Message);
                return BadRequest("总控控制命令标志位置零：" + ex.Message);
            }
        }
        /// <summary>
        /// 读取RFID内容-缓存料道上料口
        /// </summary>
        /// <returns></returns>
        [Route("GetRFID")]
        [HttpGet]
        public IHttpActionResult GetRFID()
        {
            //http://127.0.0.1:9088/api/Siemens/GetRFID
            ShowAndRecordInfo("获取RFID内容 -> " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));

            List<StoreModel> lstStore = new List<StoreModel>();
            StoreModel _Store = null;
            try
            {
                if (ServerIsConnected)
                {
                    #region 缓存料道入库1个（本次1个）
                    ushort _data_num = ushort.Parse(storeAddr3[3].ToString());//采集数据单元数量
                    ushort _data_len = ushort.Parse(storeAddr3[4].ToString());//采集数据单元字节长度
                    int _data_len_jd = _data_num * _data_len;
                    //一次性获取所有数据
                    byte[] alarmsgByte = _ReadManyBytes(storeAddr3[2], ushort.Parse(_data_len_jd.ToString()));
                    //拆分数据
                    _Store = new StoreModel();

                    _Store.location_no = Convert.ToInt32(ReadStringData(alarmsgByte, 0, 4)).ToString();
                    _Store.workpiece_type = Convert.ToInt32(ReadStringData(alarmsgByte, 4, 4)).ToString();
                    _Store.workpiece_status = Convert.ToInt32(ReadStringData(alarmsgByte, 9, 1)).ToString();
                    ShowAndRecordInfo("获取location_no结果：" + _Store.location_no);
                    ShowAndRecordInfo("获取workpiece_type结果：" + _Store.workpiece_type);
                    ShowAndRecordInfo("获取workpiece_status结果：" + _Store.workpiece_status);

                    lstStore.Add(_Store);
                    #endregion
                }
                else
                {
                    ShowAndRecordError("PLC连接失败！" + profinet.IpAddress);
                }
                //
                return Json(lstStore);
            }
            catch (Exception ex)
            {
                ShowAndRecordError(ex.Message);
                return BadRequest("获取状态失败：" + ex.Message);
            }
        }
        /// <summary>
        /// 监听入库请求信号
        /// </summary>
        /// <returns></returns>
        [Route("GetMonitorInStock")]
        [HttpGet]
        public IHttpActionResult GetMonitorInStock()
        {
            //http://127.0.0.1:9088/api/Siemens/GetMonitorInStock
            DateTime begintime = DateTime.Now;
            ShowAndRecordInfo("监听入库请求信号 -> " + begintime.ToString("yyyy-MM-dd HH:mm:ss.fff"));
            int plcMonData = 0;
            try
            {
                if (ServerIsConnected)
                {
                    //采集
                    plcMonData = ReadByteDataNew(addrRequestInStock[0]);//PLC入库请求应答
                    //
                    ShowAndRecordInfo("读取" + addrRequestInStock[0] + "结果：" + plcMonData);
                }
                else
                {
                    ShowAndRecordError("PLC连接失败！" + profinet.IpAddress);
                }
                //
                return Json(plcMonData);
            }
            catch (Exception ex)
            {
                ShowAndRecordError(ex.Message);
                return BadRequest("获取PLC请求信号失败：" + ex.Message);
            }
        }
        /// <summary>
        /// 监听单工件入库完成信号
        /// </summary>
        /// <returns></returns>
        [Route("GetMonitorInStockComplete")]
        [HttpGet]
        public IHttpActionResult GetMonitorInStockComplete()
        {
            //http://127.0.0.1:9088/api/Siemens/GetMonitorInStockComplete
            DateTime begintime = DateTime.Now;
            ShowAndRecordInfo("监听单工件入库完成信号 -> " + begintime.ToString("yyyy-MM-dd HH:mm:ss.fff"));
            int plcMonData = 0;
            try
            {
                if (ServerIsConnected)
                {
                    //采集
                    plcMonData = ReadByteDataNew(addrRequestInStock[2]);//PLC入库完成信号
                    //
                    ShowAndRecordInfo("读取" + addrRequestInStock[2] + "结果：" + plcMonData);
                }
                else
                {
                    ShowAndRecordError("PLC连接失败！" + profinet.IpAddress);
                }
                //
                return Json(plcMonData);
            }
            catch (Exception ex)
            {
                ShowAndRecordError(ex.Message);
                return BadRequest("获取PLC请求信号失败：" + ex.Message);
            }
        }
        /// <summary>
        /// 启动机器人执行入库动作
        /// </summary>
        /// <returns></returns>
        [Route("SetRFIDReadComplete")]
        [HttpGet]
        public IHttpActionResult SetRFIDReadComplete()
        {
            //http://127.0.0.1:9088/api/Siemens/SetRFIDReadComplete
            DateTime begintime = DateTime.Now;
            ShowAndRecordInfo("启动机器人执行入库动作 -> " + begintime.ToString("yyyy-MM-dd HH:mm:ss.fff"));
            bool rValue = false;
            try
            {
                if (ServerIsConnected)
                {
                    //采集
                    rValue = writeByte(addrRequestInStock[1], 1);
                    if (rValue)
                        ShowAndRecordInfo("成功写入" + addrRequestInStock[1] + "值：" + 1);
                    else
                    {
                        ShowAndRecordError("启动机器人执行入库动作失败");
                    }
                }
                else
                {
                    ShowAndRecordError("PLC连接失败！" + profinet.IpAddress);
                }
                //
                return Json(rValue);
            }
            catch (Exception ex)
            {
                ShowAndRecordError(ex.Message);
                return BadRequest("写入PLC出库数据失败：" + ex.Message);
            }
        }
        /// <summary>
        /// 复位启动机器人执行入库动作信号
        /// </summary>
        /// <returns></returns>
        [Route("ResetRFIDReadComplete")]
        [HttpGet]
        public IHttpActionResult ResetRFIDReadComplete()
        {
            //http://127.0.0.1:9088/api/Siemens/ResetRFIDReadComplete
            DateTime begintime = DateTime.Now;
            ShowAndRecordInfo("复位启动机器人执行入库动作信号 -> " + begintime.ToString("yyyy-MM-dd HH:mm:ss.fff"));
            bool rValue = false;
            try
            {
                if (ServerIsConnected)
                {
                    //采集
                    int plcControl = ReadByteDataNew(addrRequestInStock[2]);
                    if (plcControl == 1)
                    {
                        rValue = writeByte(addrRequestInStock[1], 0);
                        if (rValue)
                            ShowAndRecordInfo("成功写入" + addrRequestInStock[1] + "值：" + 0);
                        else
                        {
                            ShowAndRecordError("复位启动机器人执行入库动作信号失败");
                        }
                    }
                    else
                    {
                        ShowAndRecordInfo("无需复位启动机器人执行入库动作信号");
                    }
                }
                else
                {
                    ShowAndRecordError("PLC连接失败！" + profinet.IpAddress);
                }
                //
                return Json(rValue);
            }
            catch (Exception ex)
            {
                ShowAndRecordError(ex.Message);
                return BadRequest("复位启动机器人执行入库动作信号失败：" + ex.Message);
            }
        }

        /// <summary>
        /// 读取请求信号
        /// </summary>
        /// <returns></returns>
        [Route("ReadByte4Req")]
        [HttpGet]
        public IHttpActionResult ReadByte4Req(string addr)
        {
            //http://127.0.0.1:9088/api/Siemens/ReadByte4Req
            DateTime begintime = DateTime.Now;
            ShowAndRecordInfo("读取信号 -> " + begintime.ToString("yyyy-MM-dd HH:mm:ss.fff"));
            int plcMonData = 0;
            try
            {
                if (ServerIsConnected)
                {
                    //采集
                    plcMonData = ReadByteDataNew(addr);//PLC入库请求应答
                    //
                    ShowAndRecordInfo("读取" + addr + "结果：" + plcMonData);
                }
                else
                {
                    ShowAndRecordError("PLC连接失败！" + profinet.IpAddress);
                }
                //
                return Json(plcMonData);
            }
            catch (Exception ex)
            {
                ShowAndRecordError(ex.Message);
                return BadRequest(addr + "->ReadByte4Req失败：" + ex.Message);
            }
        }
        /// <summary>
        /// 写入请求信号
        /// </summary>
        /// <returns></returns>
        [Route("WriteByte4Req")]
        [HttpGet]
        public IHttpActionResult WriteByte4Req(string addr, string keyValue)
        {
            //http://127.0.0.1:9088/api/Siemens/WriteByte4Req
            DateTime begintime = DateTime.Now;
            ShowAndRecordInfo("写入信号 -> " + begintime.ToString("yyyy-MM-dd HH:mm:ss.fff"));
            bool rValue = false;
            try
            {
                if (ServerIsConnected)
                {
                    //采集
                    rValue = writeByte(addr, Convert.ToByte(keyValue));
                    if (rValue)
                        ShowAndRecordInfo("成功写入" + addr + "值：" + keyValue);
                    else
                    {
                        ShowAndRecordError("写入PLC失败");
                    }
                }
                else
                {
                    ShowAndRecordError("PLC连接失败！" + profinet.IpAddress);
                }
                //
                return Json(rValue);
            }
            catch (Exception ex)
            {
                ShowAndRecordError(ex.Message);
                return BadRequest(addr + keyValue.ToString() + "->WriteByte4Req失败：" + ex.Message);
            }
        }

        /// <summary>
        /// 判断“工件数据-夹具UII标签”与“工件数据-夹具UMD数据标签”库位一致性
        /// </summary>
        /// <returns></returns>
        [Route("JudgeUTag")]
        [HttpGet]
        public IHttpActionResult JudgeUTag()
        {
            //http://127.0.0.1:9088/api/Siemens/JudgeUTag
            DateTime begintime = DateTime.Now;
            ShowAndRecordInfo("判断工件库位一致性 -> " + begintime.ToString("yyyy-MM-dd HH:mm:ss.fff"));
            int plcMonData = 0;
            try
            {
                if (ServerIsConnected)
                {
                    #region 缓存料道入库1个（本次1个）
                    int _data_len = 32;
                    string addr_uii = "DB200.98";
                    string addr_umd = "DB200.130";
                    //一次性获取所有数据
                    byte[] bytesUii = _ReadManyBytes(addr_uii, ushort.Parse(_data_len.ToString()));
                    byte[] bytesUmd = _ReadManyBytes(addr_umd, ushort.Parse(_data_len.ToString()));
                    //拆分数据
                    if (bytesUii[20] == bytesUmd[0] && bytesUii[21] == bytesUmd[1] && bytesUii[22] == bytesUmd[2] && bytesUii[23] == bytesUmd[3])
                    {
                        plcMonData = 1;
                        ShowAndRecordInfo("工件数据库位一致");
                    }
                    else
                    {
                        ShowAndRecordInfo("库位不一致！！！");
                    }
                    #endregion
                }
                else
                {
                    ShowAndRecordError("PLC连接失败！" + profinet.IpAddress);
                }
                //
                return Json(plcMonData);
            }
            catch (Exception ex)
            {
                ShowAndRecordError(ex.Message);
                return BadRequest("获取状态失败：" + ex.Message);
            }
        }


        [Route("JudgeFeed")]
        [HttpGet]
        public IHttpActionResult JudgeFeed()
        {
            //http://127.0.0.1:9088/api/Siemens/JudgeFeed
            DateTime begintime = DateTime.Now;
            Console.WriteLine("判断工件库位类型状态一致性 -> " + begintime.ToString("yyyy-MM-dd HH:mm:ss.fff"));
            log.Info("判断工件库位类型状态一致性 -> " + begintime.ToString("yyyy-MM-dd HH:mm:ss.fff"));
            int plcMonData = 0;
            List<StoreModel> lstStore = new List<StoreModel>();
            StoreModel _m = null;
            try
            {
                //profinet = new SiemensS7Net(SiemensPLCS.S1500);
                //profinet.IpAddress = storeAddr[0];
                //profinet.Port = Convert.ToInt32(storeAddr[1]);
                //profinet.Slot = 0;
                //profinet.Rack = 0;

                //OperateResult connect = profinet.ConnectServer();
                //if (connect.IsSuccess)

                if (ServerIsConnected)
                {
                    #region 机床上料
                    int _data_len = 140;
                    string addr_uii = "DB200.0";
                    //一次性获取所有数据
                    byte[] bytesUii = _ReadManyBytes(addr_uii, ushort.Parse(_data_len.ToString()));

                    _m = new StoreModel();
                    _m.location_no = Convert.ToInt32(ReadStringData(bytesUii, 0, 4)).ToString();
                    _m.workpiece_type = Convert.ToInt32(ReadStringData(bytesUii, 40, 4)).ToString();
                    _m.workpiece_status = Convert.ToInt32(ReadStringData(bytesUii, 60, 1)).ToString();
                    //拆分数据
                    if (   bytesUii[0]  == bytesUii[130] 
                        && bytesUii[1]  == bytesUii[131] 
                        && bytesUii[2]  == bytesUii[132] 
                        && bytesUii[3]  == bytesUii[133]
                        && bytesUii[40] == bytesUii[134]
                        && bytesUii[41] == bytesUii[135]
                        && bytesUii[42] == bytesUii[136]
                        && bytesUii[43] == bytesUii[137]
                        && bytesUii[60] == bytesUii[139])
                    {
                        plcMonData = 1;
                        Console.WriteLine("工件数据库位类型状态一致");
                        log.Info("工件数据库位类型状态一致");
                    }
                    else
                    {
                        Console.WriteLine("库位类型状态不一致！！！");
                        log.Error("库位类型状态不一致！！！");
                    }
                    _m.result = plcMonData.ToString();
                    lstStore.Add(_m);
                    #endregion
                }
                else
                {
                    Console.WriteLine("PLC连接失败！" + profinet.IpAddress);
                    log.Error("PLC连接失败！" + profinet.IpAddress);
                    _m.result = plcMonData.ToString();
                    lstStore.Add(_m);
                }
                //
                return Json(lstStore);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                log.Error(ex.Message);
                return BadRequest("获取状态失败：" + ex.Message);
            }
        }

        /// <summary>
        /// 读取下料缓存工位信号
        /// </summary>
        /// <returns></returns>
        [Route("ReadBufferStation")]
        [HttpGet]
        public IHttpActionResult ReadBufferStation()
        {
            //http://127.0.0.1:9088/api/Siemens/ReadBufferStation
            DateTime begintime = DateTime.Now;
            ShowAndRecordInfo("读取下料缓存工位信号 -> " + begintime.ToString("yyyy-MM-dd HH:mm:ss.fff"));
            
            string _PDataAddr = "DB200.230";//起始点
            int _PDataLen = 3;//长度
            List<string> listbs = new List<string>();
            byte[] bArr = new byte[_PDataLen];
            string sdata = "";
            try
            {
                if (ServerIsConnected)
                {
                    //采集
                    OperateResult<byte[]> result = profinet.Read(_PDataAddr, Convert.ToUInt16(_PDataLen));
                    if (result.IsSuccess)
                    {
                        bArr = result.Content;
                        int cLen = 8;
                        for (int i = 0; i < _PDataLen; i++)
                        {
                            sdata = Convert.ToString(bArr[i], 2).PadLeft(8, '0');
                            log.Info("bArr[" + i.ToString() + "]:" + sdata);
                            if (i == 2)
                                cLen = 4;
                            else
                                cLen = 8;
                            for (int j = 0; j < cLen; j++)
                            {
                                listbs.Add(sdata.Substring(7 - j, 1));//字符串倒序排列了，所以用7减去位数
                            }
                        }
                    }
                }
                else
                {
                    ShowAndRecordError("PLC连接失败！" + profinet.IpAddress);
                }
                //
                return Json(listbs);
            }
            catch (Exception ex)
            {
                ShowAndRecordError(ex.Message);
                return BadRequest(_PDataAddr + "->ReadByte4Req失败：" + ex.Message);
            }           
            
        }
        /// <summary>
        /// byte读取(1 byte)
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public int ReadByteData(byte[] bytes_dm, int len_D)
        {
            int data = 0;//需要返回的数据
            string sdata = "";
            try
            {
                if (bytes_dm.Length == 0)
                    return data;
                for (int i = len_D; i < len_D + 1; i++)
                {
                    sdata += Convert.ToChar(bytes_dm[i]);
                }
                data = Convert.ToInt32(sdata);//取低位
                log.Info("ReadByteData读取结果：" + data);
                return data;
            }
            catch (Exception ex)
            {
                ShowAndRecordError("ReadByteData读取失败：" + ex.Message);
                return 0;
            }
        }

        #region 相关代码
        public NetworkStream stream;
        TcpClient tcpclient;
        StreamWriter sw;
        StreamReader sr;
        public bool createPLConn(int plcPort, string plcIP)
        {
            log.Info("plcPort:" + plcPort + " plcIP:" + plcIP);
            try
            {
                tcpclient = new TcpClient(plcIP, plcPort);  // 连接服务器

                if (tcpclient.Connected)
                {
                    stream = tcpclient.GetStream();   // 获取网络数据流对象
                    log.Info("设备连接成功！");
                    return true;
                }
                else
                {
                    log.Error("设备连接失败！");
                    return false;
                }
            }
            catch (Exception ex)
            {
                log.Error(ex.Message);
                log.Error("Driver Error\n\nCould not initalize adapter - TCP/IP    \nConnection aborted");
                return false;
            }
        }
        /// <summary>
        /// 断开PLC连接
        /// </summary>
        public bool disPLConn(TcpClient tcpclient)
        {
            // 断开连接
            tcpclient.Close();
            log.Info("设备断开连接成功！");
            return true;
        }
        /// <summary>
        /// 发送指令并接收结果
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public string SendAndRevData(string address)
        {
            string data = "";//需要返回的数据
            try
            {
                bool con = true;
                sw = new StreamWriter(stream);
                sr = new StreamReader(stream, Encoding.GetEncoding("gb2312"));
                string readResult = "";
                while (con)
                {
                    try
                    {
                        stream.ReadTimeout = 10;//Set ReadEcho Timeout
                        //Send 
                        sw.Write(address + "\n");
                        sw.Flush();

                        while (true)
                        {
                            char c = (char)sr.Read();
                            //科德机床
                            if (c == 10)//Char("10) … 换行
                            {
                                //log.Info("遇到换行符，本次读取结束");
                                break;
                            }
                            else
                            {
                                readResult += c;
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        log.Error(e.Message);
                    }

                    data = readResult;
                    //log.Info("SendAndRevData:" + data);
                    con = false;
                }
                return data;
            }
            catch (Exception ex)
            {
                log.Error("SendAndRevData读取失败：" + ex.Message);
                return "";
            }
        }
        /// <summary>
        /// 向机床写入程序号
        /// </summary>
        /// <param name="addr"></param>
        /// <param name="keyValue"></param>
        /// <returns></returns>
        [Route("WriteProgramNo2CNC")]
        [HttpGet]
        public IHttpActionResult WriteProgramNo2CNC(string plcIP, string workpiece_type)
        {
            //http://127.0.0.1:9088/api/Siemens/WriteProgramNo2CNC
            DateTime begintime = DateTime.Now;
            ShowAndRecordInfo("向机床写入程序号 -> " + begintime.ToString("yyyy-MM-dd HH:mm:ss.fff"));
            bool rValue = false;
            try
            {
                bool connect = createPLConn(62944, plcIP);
                if (connect)
                {
                    string addr = "<set><req>yes</req><st>plc</st><var>G_Num</var><val>" + workpiece_type + "</val></set>";
                    //写入程序号
                    ShowAndRecordInfo("写入:" + addr);
                    string callback = SendAndRevData(addr);
                    ShowAndRecordInfo("写入结果：" + callback);
                    if (callback == "<set><var>G_Num</var></set>")
                        rValue = true;
                    //
                    disPLConn(tcpclient);
                }
                else
                {
                    ShowAndRecordError("设备连接失败！" + profinet.IpAddress);
                }
                //
                return Json(rValue);
            }
            catch (Exception ex)
            {
                ShowAndRecordError(ex.Message);
                return BadRequest(plcIP +" & "+ workpiece_type + "->WriteProgramNo2CNC异常：" + ex.Message);
            }
        }

        #endregion

        /// <summary>
        /// short读取(2 byte)
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public int ReadInt16Data(byte[] bytes_dm, int len_D)
        {
            int data = 0;//需要返回的数据
            byte[] tmpBuffer = new byte[2];
            string sdata = "";
            try
            {
                if (bytes_dm.Length == 0)
                    return data;

                tmpBuffer[0] = bytes_dm[len_D + 0];
                tmpBuffer[1] = bytes_dm[len_D + 1];

                sdata += Convert.ToString(tmpBuffer[0], 2).PadLeft(8, '0');
                sdata += Convert.ToString(tmpBuffer[1], 2).PadLeft(8, '0');

                data = Convert.ToInt32(sdata, 2); //二进制字符串转十进制数
                log.Info("ReadInt16Data读取结果：" + data);
                return data;
            }
            catch (Exception ex)
            {
                ShowAndRecordError("ReadInt16Data读取失败：" + ex.Message);
                return 0;
            }
        }
        /// <summary>
        /// int读取(4 byte)
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public int ReadInt32Data(byte[] bytes_dm, int len_D)
        {
            int data = 0;//需要返回的数据
            byte[] tmpBuffer = new byte[4];
            string sdata = "";
            try
            {
                if (bytes_dm.Length == 0)
                    return data;

                for (int i = len_D; i < len_D + 4; i++)
                {
                    sdata += Convert.ToChar(bytes_dm[i]);
                }


                data = Convert.ToInt32(sdata); //字符串转十进制数
                log.Info("ReadInt32Data读取结果：" + data);
                return data;
            }
            catch (Exception ex)
            {
                ShowAndRecordError("ReadInt32Data读取失败：" + ex.Message);
                return 0;
            }
        }


        /// <summary>
        /// float读取(4 byte)
        /// </summary>
        /// <param name="bytes_dm">byte数组</param>
        /// <param name="len_D">开始索引</param>
        /// <returns></returns>
        public float ReadFloatData(byte[] bytes_dm, int len_D)
        {
            //byte[] bsArr1 = new byte[4] { 0xE3, 0x38, 0x8E, 0x3F };
            //float fValue1 = BitConverter.ToSingle(bsArr1, 0);

            float data = 0.0f;//需要返回的数据
            byte[] tmpBuffer = new byte[4];
            try
            {
                if (bytes_dm.Length == 0)
                    return data;

                tmpBuffer[0] = bytes_dm[len_D + 3];
                tmpBuffer[1] = bytes_dm[len_D + 2];
                tmpBuffer[2] = bytes_dm[len_D + 1];
                tmpBuffer[3] = bytes_dm[len_D + 0];

                data = BitConverter.ToSingle(tmpBuffer, 0);
                log.Info("ReadFloatData读取结果：" + data);
                return data;
            }
            catch (Exception ex)
            {
                ShowAndRecordError("ReadFloatData读取失败：" + ex.Message);
                return 0;
            }
        }
        public string ReadStringData(byte[] bytes_dm, int beginAddr, int len_D)
        {
            string data = "";//需要返回的数据
            byte[] tmpBuffer = new byte[len_D];
            try
            {
                if (bytes_dm.Length == 0)
                    return data;

                for (int i = 0; i < len_D; i++)
                {
                    tmpBuffer[i] = bytes_dm[beginAddr + i];
                }

                for (int i = 0; i < len_D; i++)
                {
                    data += Convert.ToChar(tmpBuffer[i]);
                }

                //log.Info("ReadStringData读取结果：" + data);
                return data;
            }
            catch (Exception ex)
            {
                ShowAndRecordError("ReadStringData读取失败：" + ex.Message);
                return "";
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
            //Console.WriteLine("threeColor:" + threeColor);
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
                    ShowAndRecordError(result.ToMessageShowString());
                    return false;
                }
            }
            catch (Exception ex)
            {
                ShowAndRecordError("readBoolData读取失败：" + ex.Message);
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
                OperateResult<byte> result = profinet.ReadByte(address);
                if (result.IsSuccess)
                {
                    return result.Content;
                }
                else
                {
                    ShowAndRecordError("ReadByteDataNew 读取失败：" + result.ToMessageShowString());
                    return new byte();
                }
            }
            catch (Exception ex)
            {
                ShowAndRecordError("ReadByteDataNew 读取异常：" + ex.Message);
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
                    ShowAndRecordError("ReadByteDataNew 读取失败：" + result.ToMessageShowString());
                    return 0;
                }
            }
            catch (Exception ex)
            {
                ShowAndRecordError("ReadByteDataNew 读取异常：" + ex.Message);
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
                    ShowAndRecordError("ReadByteDataNew 读取失败：" + result.ToMessageShowString());
                    return 0;
                }
            }
            catch (Exception ex)
            {
                ShowAndRecordError("ReadByteDataNew 读取异常：" + ex.Message);
                return 0;
            }
        }
        /// <summary>
        /// 向PLC写string数据
        /// </summary>
        public bool writeString(string address, string value)
        {
            try
            {
                OperateResult result = profinet.Write(address, value);
                if (result.IsSuccess)
                {
                    return true;
                }
                else
                {
                    ShowAndRecordError(result.ToMessageShowString());
                    return false;
                }
            }
            catch (Exception ex)
            {
                ShowAndRecordError("Write Error\n\nCould not write to PLC" + ex.Message);
                return false;
            }
        }

        /// <summary>
        /// 向PLC写byte数据
        /// </summary>
        public bool writeByte(string address, byte value)
        {
            try
            {
                OperateResult result = profinet.Write(address, value);
                if (result.IsSuccess)
                {
                    return true;
                }
                else
                {
                    ShowAndRecordError(address + "写入" + value + "失败 " + result.ToMessageShowString());
                    return false;
                }
            }
            catch (Exception ex)
            {
                ShowAndRecordError("Write Error\n\nCould not write to PLC" + ex.Message);
                return false;
            }
        }
        /// <summary>
        /// 向PLC写int数据
        /// </summary>
        public bool writeInt(string address, int value)
        {
            try
            {
                OperateResult result = profinet.Write(address, value);
                if (result.IsSuccess)
                {
                    return true;
                }
                else
                {
                    ShowAndRecordError(address + "写入" + value + "失败 " + result.ToMessageShowString());
                    return false;
                }
            }
            catch (Exception ex)
            {
                ShowAndRecordError("Write Error\n\nCould not write to PLC" + ex.Message);
                return false;
            }
        }
        //批量获取字节
        public static ushort PLC_MAX_Length = 512;//一次性取值最大长度
        public static ushort PLC_Byte_Length = 1;//西门子按照Byte存储，所以此处为1

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
            if (PLC_DM_AddressLength <= PLC_MAX_Length)//西门子PLC Byte读取最大长度
            {
                manybytes = ReadManyBytes(PLC_DM_StartAddress, PLC_DM_AddressLength);
            }
            else
            {
                manybytes = new byte[PLC_DM_AddressLength * PLC_Byte_Length];
                byte[] bytes_dm_tmp = null;
                string area = PLC_DM_StartAddress.Split('.')[0].ToString();//DB块
                int _PLC_DM_StartAddress = Convert.ToUInt16(PLC_DM_StartAddress.Split('.')[1]);
                int k = PLC_DM_AddressLength / PLC_MAX_Length;//读取循环次数
                ushort l = (ushort)(PLC_DM_AddressLength % PLC_MAX_Length);//最后一次读取长度

                for (int i = 0; i <= k; i++)
                {
                    System.Threading.Thread.Sleep(50);//停顿50毫秒
                    if (i < k)
                    {
                        bytes_dm_tmp = null;
                        bytes_dm_tmp = ReadManyBytes(area + "." + (_PLC_DM_StartAddress + i * PLC_MAX_Length), PLC_MAX_Length);
                        bytes_dm_tmp.CopyTo(manybytes, i * PLC_MAX_Length * PLC_Byte_Length);
                    }
                    else
                    {
                        bytes_dm_tmp = ReadManyBytes(area + "." + (_PLC_DM_StartAddress + i * PLC_MAX_Length), l);
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
                    ShowAndRecordError("ReadManyBytes读取失败：" + result.ToMessageShowString());
                }
            }
            catch (Exception ex)
            {
                ShowAndRecordError("ReadManyBytes读取异常：" + ex.Message);
            }
            return bArr;
        }

        #endregion
    }
}
