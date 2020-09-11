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
using System.Text;
using System.Web;
using System.Web.Http;

namespace JNRSWebApiOwinHost
{
    [RoutePrefix("api/Siemens")]
    public class SiemensController : ApiController
    {
        protected readonly ILog log = LogManager.GetLogger("JNRSLogger");
        string[] storeAddr = ConfigurationManager.AppSettings["storeAddr"].Split(',');//机器人料库点位183个（本次10个）
        // string[] storeAddr2 = ConfigurationManager.AppSettings["storeAddr2"].Split(',');//出库20个（本次2个）
        string[] storeAddr3 = ConfigurationManager.AppSettings["storeAddr3"].Split(',');//缓存料道入库1个（本次1个）
        string[] plcMonAddr = ConfigurationManager.AppSettings["plcMonAddr"].Split(',');
        string[] addrSetFeedingToMachine = ConfigurationManager.AppSettings["addrSetFeedingToMachine"].Split(',');
        string[] addrSetChangeToMachine = ConfigurationManager.AppSettings["addrSetChangeToMachine"].Split(',');
        string[] addrSetUnloadToChannel = ConfigurationManager.AppSettings["addrSetUnloadToChannel"].Split(',');
        string[] addrSetOutStock = ConfigurationManager.AppSettings["addrSetOutStock"].Split(',');
        string[] addrRequestOutStock = ConfigurationManager.AppSettings["addrRequestOutStock"].Split(',');
        string[] addrRequestInStock = ConfigurationManager.AppSettings["addrRequestInStock"].Split(',');
        SiemensS7Net profinet = null;

        /// <summary>
        /// 立库所有库位扫描
        /// </summary>
        /// <returns></returns>
        [Route("GetState")]
        [HttpGet]
        public IHttpActionResult GetState()
        {
            //http://127.0.0.1:9088/api/Siemens/GetState
            DateTime begintime = DateTime.Now;
            Console.WriteLine("Siemens/GetState开始调用-> " + begintime.ToString("yyyy-MM-dd HH:mm:ss.fff"));
            log.Info("Siemens/GetState开始调用-> " + begintime.ToString("yyyy-MM-dd HH:mm:ss.fff"));

            List<StoreModel> lstStore = new List<StoreModel>();
            StoreModel _Store = null;
            try
            {
                //连接
                profinet = new SiemensS7Net(SiemensPLCS.S1500);
                profinet.IpAddress = storeAddr[0];
                profinet.Port = int.Parse(storeAddr[1]);
                profinet.Rack = byte.Parse("0");
                profinet.Slot = byte.Parse("0");

                OperateResult connect = profinet.ConnectServer();
                if (connect.IsSuccess)
                {
                    log.Info("PLC连接成功！" + profinet.IpAddress);
                    Console.WriteLine("PLC连接成功！" + profinet.IpAddress);

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
                        _Store.workpiece_type = ReadInt32Data(alarmsgByte, 40 + i * _data_len).ToString();
                        _Store.workpiece_status = ReadByteData(alarmsgByte, 60 + i * _data_len).ToString();
                        _Store.mac_proc_info = ReadByteData(alarmsgByte, 66 + i * _data_len).ToString();
                        //_Store.axis_x = ReadFloatData(alarmsgByte, 2 + i * _data_len);
                        //_Store.axis_y = ReadFloatData(alarmsgByte, 6 + i * _data_len);

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
                    //    //_Store.axis_x = ReadFloatData(alarmsgByte, 2 + i * _data_len);
                    //    //_Store.axis_y = ReadFloatData(alarmsgByte, 6 + i * _data_len);

                    //    lstStore.Add(_Store);
                    //}
                    #endregion

                    //断开
                    profinet.ConnectClose();
                }
                else
                {
                    log.Error("PLC连接失败！" + profinet.IpAddress);
                }

                DateTime endtime = DateTime.Now;
                string tick = ExecDateDiff(begintime, endtime);
                Console.WriteLine("耗时" + tick + "毫秒\n");
                log.Info("耗时" + tick + "毫秒\n");
                //
                return Json(lstStore);
            }
            catch (Exception ex)
            {
                log.Error(ex.Message);
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
            Console.WriteLine("Siemens/GetMonitorPLC开始调用-> " + begintime.ToString("yyyy-MM-dd HH:mm:ss.fff"));
            log.Info("Siemens/GetMonitorPLC开始调用-> " + begintime.ToString("yyyy-MM-dd HH:mm:ss.fff"));
            int plcMonData = 0;
            int plcMonData2 = 0;
            string rValue = "";
            try
            {
                //连接
                profinet = new SiemensS7Net(SiemensPLCS.S1500);
                profinet.IpAddress = storeAddr[0];
                profinet.Port = int.Parse(storeAddr[1]);
                profinet.Rack = byte.Parse("0");
                profinet.Slot = byte.Parse("0");

                OperateResult connect = profinet.ConnectServer();
                if (connect.IsSuccess)
                {
                    log.Info("PLC连接成功！" + profinet.IpAddress);
                    Console.WriteLine("PLC连接成功！" + profinet.IpAddress);
                    //采集
                    plcMonData = ReadByteDataNew(plcMonAddr[3]);//机床号 1~6
                    plcMonData2 = ReadByteDataNew(plcMonAddr[2]);//机床请求类型 1~4
                    rValue = plcMonData.ToString() + plcMonData2.ToString();
                    //断开
                    profinet.ConnectClose();
                }
                else
                {
                    log.Error("PLC连接失败！" + profinet.IpAddress);
                }

                DateTime endtime = DateTime.Now;
                string tick = ExecDateDiff(begintime, endtime);
                Console.WriteLine("耗时" + tick + "毫秒\n");
                log.Info("耗时" + tick + "毫秒\n");
                //
                return Json(rValue);
            }
            catch (Exception ex)
            {
                log.Error(ex.Message);
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
            Console.WriteLine("Siemens/GetMonitorPLCOutStock开始调用-> " + begintime.ToString("yyyy-MM-dd HH:mm:ss.fff"));
            log.Info("Siemens/GetMonitorPLCOutStock开始调用-> " + begintime.ToString("yyyy-MM-dd HH:mm:ss.fff"));
            int plcMonData = 0;
            try
            {
                //连接
                profinet = new SiemensS7Net(SiemensPLCS.S1500);
                profinet.IpAddress = storeAddr[0];
                profinet.Port = int.Parse(storeAddr[1]);
                profinet.Rack = byte.Parse("0");
                profinet.Slot = byte.Parse("0");

                OperateResult connect = profinet.ConnectServer();
                if (connect.IsSuccess)
                {
                    log.Info("PLC连接成功！" + profinet.IpAddress);
                    Console.WriteLine("PLC连接成功！" + profinet.IpAddress);
                    //采集
                    plcMonData = ReadByteDataNew(plcMonAddr[6]);//PLC出库请求应答
                    //断开
                    profinet.ConnectClose();
                }
                else
                {
                    log.Error("PLC连接失败！" + profinet.IpAddress);
                }

                DateTime endtime = DateTime.Now;
                string tick = ExecDateDiff(begintime, endtime);
                Console.WriteLine("耗时" + tick + "毫秒\n");
                log.Info("耗时" + tick + "毫秒\n");
                //
                return Json(plcMonData);
            }
            catch (Exception ex)
            {
                log.Error(ex.Message);
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
            Console.WriteLine("Siemens/GetMonitorPLCOutStock开始调用-> " + begintime.ToString("yyyy-MM-dd HH:mm:ss.fff"));
            log.Info("Siemens/GetMonitorPLCOutStock开始调用-> " + begintime.ToString("yyyy-MM-dd HH:mm:ss.fff"));
            int plcMonData = 0;
            try
            {
                //连接
                profinet = new SiemensS7Net(SiemensPLCS.S1500);
                profinet.IpAddress = storeAddr[0];
                profinet.Port = int.Parse(storeAddr[1]);
                profinet.Rack = byte.Parse("0");
                profinet.Slot = byte.Parse("0");

                OperateResult connect = profinet.ConnectServer();
                if (connect.IsSuccess)
                {
                    log.Info("PLC连接成功！" + profinet.IpAddress);
                    Console.WriteLine("PLC连接成功！" + profinet.IpAddress);
                    //采集
                    plcMonData = ReadByteDataNew(plcMonAddr[7]);//PLC出库请求应答
                    //断开
                    profinet.ConnectClose();
                }
                else
                {
                    log.Error("PLC连接失败！" + profinet.IpAddress);
                }

                DateTime endtime = DateTime.Now;
                string tick = ExecDateDiff(begintime, endtime);
                Console.WriteLine("耗时" + tick + "毫秒\n");
                log.Info("耗时" + tick + "毫秒\n");
                //
                return Json(plcMonData);
            }
            catch (Exception ex)
            {
                log.Error(ex.Message);
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
            Console.WriteLine("Siemens/SetFeedingToMachine开始调用-> " + begintime.ToString("yyyy-MM-dd HH:mm:ss.fff"));
            log.Info("Siemens/SetFeedingToMachine开始调用-> " + begintime.ToString("yyyy-MM-dd HH:mm:ss.fff"));
            bool rValue = false;
            try
            {
                //连接
                profinet = new SiemensS7Net(SiemensPLCS.S1500);
                profinet.IpAddress = storeAddr[0];
                profinet.Port = int.Parse(storeAddr[1]);
                profinet.Rack = byte.Parse("0");
                profinet.Slot = byte.Parse("0");

                OperateResult connect = profinet.ConnectServer();
                if (connect.IsSuccess)
                {
                    log.Info("PLC连接成功！" + profinet.IpAddress);
                    Console.WriteLine("PLC连接成功！" + profinet.IpAddress);
                    //采集
                    rValue = writeByte(plcMonAddr[1], 1);
                    rValue = writeInt(addrSetFeedingToMachine[0], Convert.ToUInt16(location_no));
                    //rValue = writeString(addrSetFeedingToMachine[1], program_number);
                    rValue = writeInt(addrSetFeedingToMachine[2], Convert.ToUInt16(workpiece_type));// writeString(addrSetFeedingToMachine[2], workpiece_type);

                    //断开
                    profinet.ConnectClose();
                }
                else
                {
                    log.Error("PLC连接失败！" + profinet.IpAddress);
                }

                DateTime endtime = DateTime.Now;
                string tick = ExecDateDiff(begintime, endtime);
                Console.WriteLine("耗时" + tick + "毫秒\n");
                log.Info("耗时" + tick + "毫秒\n");
                //
                return Json(rValue);
            }
            catch (Exception ex)
            {
                log.Error(ex.Message);
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
            Console.WriteLine("Siemens/SetChangeToMachine开始调用-> " + begintime.ToString("yyyy-MM-dd HH:mm:ss.fff"));
            log.Info("Siemens/SetChangeToMachine开始调用-> " + begintime.ToString("yyyy-MM-dd HH:mm:ss.fff"));
            bool rValue = false;
            try
            {
                //连接
                profinet = new SiemensS7Net(SiemensPLCS.S1500);
                profinet.IpAddress = storeAddr[0];
                profinet.Port = int.Parse(storeAddr[1]);
                profinet.Rack = byte.Parse("0");
                profinet.Slot = byte.Parse("0");

                OperateResult connect = profinet.ConnectServer();
                if (connect.IsSuccess)
                {
                    log.Info("PLC连接成功！" + profinet.IpAddress);
                    Console.WriteLine("PLC连接成功！" + profinet.IpAddress);
                    //采集
                    rValue = writeByte(plcMonAddr[1], 1);
                    rValue = writeInt(addrSetChangeToMachine[0], Convert.ToUInt16(location_no)); //writeString(addrSetChangeToMachine[0], location_no);
                    rValue = writeInt(addrSetChangeToMachine[1], Convert.ToUInt16(location_no_old)); //writeString(addrSetChangeToMachine[1], location_no_old);
                    //rValue = writeString(addrSetChangeToMachine[2], program_number);
                    rValue = writeInt(addrSetChangeToMachine[3], Convert.ToUInt16(workpiece_type)); //writeString(addrSetChangeToMachine[3], workpiece_type);

                    //断开
                    profinet.ConnectClose();
                }
                else
                {
                    log.Error("PLC连接失败！" + profinet.IpAddress);
                }

                DateTime endtime = DateTime.Now;
                string tick = ExecDateDiff(begintime, endtime);
                Console.WriteLine("耗时" + tick + "毫秒\n");
                log.Info("耗时" + tick + "毫秒\n");
                //
                return Json(rValue);
            }
            catch (Exception ex)
            {
                log.Error(ex.Message);
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
            Console.WriteLine("Siemens/SetUnloadToChannel开始调用-> " + begintime.ToString("yyyy-MM-dd HH:mm:ss.fff"));
            log.Info("Siemens/SetUnloadToChannel开始调用-> " + begintime.ToString("yyyy-MM-dd HH:mm:ss.fff"));
            bool rValue = false;
            try
            {
                //连接
                profinet = new SiemensS7Net(SiemensPLCS.S1500);
                profinet.IpAddress = storeAddr[0];
                profinet.Port = int.Parse(storeAddr[1]);
                profinet.Rack = byte.Parse("0");
                profinet.Slot = byte.Parse("0");

                OperateResult connect = profinet.ConnectServer();
                if (connect.IsSuccess)
                {
                    log.Info("PLC连接成功！" + profinet.IpAddress);
                    Console.WriteLine("PLC连接成功！" + profinet.IpAddress);
                    //采集
                    rValue = writeByte(plcMonAddr[1], 1);
                    rValue = writeInt(addrSetUnloadToChannel[0], Convert.ToUInt16(location_no)); //writeString(addrSetUnloadToChannel[1], location_no);

                    //断开
                    profinet.ConnectClose();
                }
                else
                {
                    log.Error("PLC连接失败！" + profinet.IpAddress);
                }

                DateTime endtime = DateTime.Now;
                string tick = ExecDateDiff(begintime, endtime);
                Console.WriteLine("耗时" + tick + "毫秒\n");
                log.Info("耗时" + tick + "毫秒\n");
                //
                return Json(rValue);
            }
            catch (Exception ex)
            {
                log.Error(ex.Message);
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
            Console.WriteLine("Siemens/SetOutStock开始调用-> " + begintime.ToString("yyyy-MM-dd HH:mm:ss.fff"));
            log.Info("Siemens/SetOutStock开始调用-> " + begintime.ToString("yyyy-MM-dd HH:mm:ss.fff"));
            bool rValue = false;
            try
            {
                //连接
                profinet = new SiemensS7Net(SiemensPLCS.S1500);
                profinet.IpAddress = storeAddr[0];
                profinet.Port = int.Parse(storeAddr[1]);
                profinet.Rack = byte.Parse("0");
                profinet.Slot = byte.Parse("0");

                OperateResult connect = profinet.ConnectServer();
                if (connect.IsSuccess)
                {
                    log.Info("PLC连接成功！" + profinet.IpAddress);
                    Console.WriteLine("PLC连接成功！" + profinet.IpAddress);
                    //采集
                    rValue = writeByte(plcMonAddr[5], 1);
                    rValue = writeInt(addrSetOutStock[0], Convert.ToUInt16(location_no)); //writeString(addrSetOutStock[1], location_no);

                    //断开
                    profinet.ConnectClose();
                }
                else
                {
                    log.Error("PLC连接失败！" + profinet.IpAddress);
                }

                DateTime endtime = DateTime.Now;
                string tick = ExecDateDiff(begintime, endtime);
                Console.WriteLine("耗时" + tick + "毫秒\n");
                log.Info("耗时" + tick + "毫秒\n");
                //
                return Json(rValue);
            }
            catch (Exception ex)
            {
                log.Error(ex.Message);
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
            Console.WriteLine("Siemens/RequestOutStock开始调用-> " + begintime.ToString("yyyy-MM-dd HH:mm:ss.fff"));
            log.Info("Siemens/RequestOutStock开始调用-> " + begintime.ToString("yyyy-MM-dd HH:mm:ss.fff"));
            bool rValue = false;
            try
            {
                //连接
                profinet = new SiemensS7Net(SiemensPLCS.S1500);
                profinet.IpAddress = storeAddr[0];
                profinet.Port = int.Parse(storeAddr[1]);
                profinet.Rack = byte.Parse("0");
                profinet.Slot = byte.Parse("0");

                OperateResult connect = profinet.ConnectServer();
                if (connect.IsSuccess)
                {
                    log.Info("PLC连接成功！" + profinet.IpAddress);
                    Console.WriteLine("PLC连接成功！" + profinet.IpAddress);
                    //采集
                    int plcControl = ReadByteDataNew(plcMonAddr[6]);
                    if (plcControl == 0)
                        rValue = writeByte(addrRequestOutStock[0], 0);
                    else
                        rValue = true;

                    //断开
                    profinet.ConnectClose();
                }
                else
                {
                    log.Error("PLC连接失败！" + profinet.IpAddress);
                }

                DateTime endtime = DateTime.Now;
                string tick = ExecDateDiff(begintime, endtime);
                Console.WriteLine("耗时" + tick + "毫秒\n");
                log.Info("耗时" + tick + "毫秒\n");
                //
                //lstStore[0].workpiece_type = "";
                return Json(rValue);
            }
            catch (Exception ex)
            {
                log.Error(ex.Message);
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
            Console.WriteLine("Siemens/ResetRequestInfo开始调用-> " + begintime.ToString("yyyy-MM-dd HH:mm:ss.fff"));
            log.Info("Siemens/ResetRequestInfo开始调用-> " + begintime.ToString("yyyy-MM-dd HH:mm:ss.fff"));
            bool rValue = false;
            try
            {
                //连接
                profinet = new SiemensS7Net(SiemensPLCS.S1500);
                profinet.IpAddress = storeAddr[0];
                profinet.Port = int.Parse(storeAddr[1]);
                profinet.Rack = byte.Parse("0");
                profinet.Slot = byte.Parse("0");

                OperateResult connect = profinet.ConnectServer();
                if (connect.IsSuccess)
                {
                    log.Info("PLC连接成功！" + profinet.IpAddress);
                    Console.WriteLine("PLC连接成功！" + profinet.IpAddress);
                    //采集
                    //int plcControl = ReadByteDataNew(plcMonAddr[0]);
                    rValue = writeByte(plcMonAddr[1], 0);
                    //if (plcControl == 0)
                    //    rValue = writeByte(plcMonAddr[1], 0);
                    //else
                    //    rValue = true;

                    //断开
                    profinet.ConnectClose();
                }
                else
                {
                    log.Error("PLC连接失败！" + profinet.IpAddress);
                }

                DateTime endtime = DateTime.Now;
                string tick = ExecDateDiff(begintime, endtime);
                Console.WriteLine("耗时" + tick + "毫秒\n");
                log.Info("耗时" + tick + "毫秒\n");
                //
                //lstStore[0].workpiece_type = "";
                return Json(rValue);
            }
            catch (Exception ex)
            {
                log.Error(ex.Message);
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
            Console.WriteLine("Siemens/ResetRequestInfo开始调用-> " + begintime.ToString("yyyy-MM-dd HH:mm:ss.fff"));
            log.Info("Siemens/ResetRequestInfo开始调用-> " + begintime.ToString("yyyy-MM-dd HH:mm:ss.fff"));
            bool rValue = false;
            try
            {
                //连接
                profinet = new SiemensS7Net(SiemensPLCS.S1500);
                profinet.IpAddress = storeAddr[0];
                profinet.Port = int.Parse(storeAddr[1]);
                profinet.Rack = byte.Parse("0");
                profinet.Slot = byte.Parse("0");

                OperateResult connect = profinet.ConnectServer();
                if (connect.IsSuccess)
                {
                    log.Info("PLC连接成功！" + profinet.IpAddress);
                    Console.WriteLine("PLC连接成功！" + profinet.IpAddress);
                    //采集
                    int plcControl = ReadByteDataNew(plcMonAddr[7]);
                    //rValue = writeByte(plcMonAddr[8], 0);
                    if (plcControl == 1)
                    {
                        rValue = writeByte(plcMonAddr[8], 1);
                    }

                    //else
                    //    rValue = true;

                    //断开
                    profinet.ConnectClose();
                }
                else
                {
                    log.Error("PLC连接失败！" + profinet.IpAddress);
                }

                DateTime endtime = DateTime.Now;
                string tick = ExecDateDiff(begintime, endtime);
                Console.WriteLine("耗时" + tick + "毫秒\n");
                log.Info("耗时" + tick + "毫秒\n");
                //
                //lstStore[0].workpiece_type = "";
                return Json(rValue);
            }
            catch (Exception ex)
            {
                log.Error(ex.Message);
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
            DateTime begintime = DateTime.Now;
            Console.WriteLine("Siemens/GetRFID开始调用-> " + begintime.ToString("yyyy-MM-dd HH:mm:ss.fff"));
            log.Info("Siemens/GetRFID开始调用-> " + begintime.ToString("yyyy-MM-dd HH:mm:ss.fff"));

            List<StoreModel> lstStore = new List<StoreModel>();
            StoreModel _Store = null;
            try
            {
                //连接
                profinet = new SiemensS7Net(SiemensPLCS.S1500);
                profinet.IpAddress = storeAddr[0];
                profinet.Port = int.Parse(storeAddr[1]);
                profinet.Rack = byte.Parse("0");
                profinet.Slot = byte.Parse("0");

                OperateResult connect = profinet.ConnectServer();
                if (connect.IsSuccess)
                {
                    log.Info("PLC连接成功！" + profinet.IpAddress);
                    Console.WriteLine("PLC连接成功！" + profinet.IpAddress);

                    #region 缓存料道入库1个（本次1个）
                    ushort _data_num = ushort.Parse(storeAddr3[3].ToString());//采集数据单元数量
                    ushort _data_len = ushort.Parse(storeAddr3[4].ToString());//采集数据单元字节长度
                    int _data_len_jd = _data_num * _data_len;
                    //一次性获取所有数据
                    byte[] alarmsgByte = _ReadManyBytes(storeAddr[2], ushort.Parse(_data_len_jd.ToString()));
                    //拆分数据
                    for (int i = 0; i < alarmsgByte.Length / _data_len; i++)
                    {
                        _Store = new StoreModel();

                        _Store.location_no = ReadInt32Data(alarmsgByte, 0 + i * _data_len).ToString();
                        _Store.workpiece_type = ReadInt32Data(alarmsgByte, 40 + i * _data_len).ToString();
                        _Store.workpiece_status = ReadByteData(alarmsgByte, 60 + i * _data_len).ToString();
                        //_Store.mac_proc_info = ReadByteData(alarmsgByte, 66 + i * _data_len).ToString();
                        //_Store.axis_x = ReadFloatData(alarmsgByte, 2 + i * _data_len);
                        //_Store.axis_y = ReadFloatData(alarmsgByte, 6 + i * _data_len);

                        lstStore.Add(_Store);
                    }
                    #endregion

                    //断开
                    profinet.ConnectClose();
                }
                else
                {
                    log.Error("PLC连接失败！" + profinet.IpAddress);
                }

                DateTime endtime = DateTime.Now;
                string tick = ExecDateDiff(begintime, endtime);
                Console.WriteLine("耗时" + tick + "毫秒\n");
                log.Info("耗时" + tick + "毫秒\n");
                //
                return Json(lstStore);
            }
            catch (Exception ex)
            {
                log.Error(ex.Message);
                return BadRequest("获取状态失败：" + ex.Message);
            }
        }
        /// <summary>
        /// 监听入库请求信号()
        /// </summary>
        /// <returns></returns>
        [Route("GetMonitorInStock")]
        [HttpGet]
        public IHttpActionResult GetMonitorInStock()
        {
            //http://127.0.0.1:9088/api/Siemens/GetMonitorInStock
            DateTime begintime = DateTime.Now;
            Console.WriteLine("Siemens/GetMonitorInStock开始调用-> " + begintime.ToString("yyyy-MM-dd HH:mm:ss.fff"));
            log.Info("Siemens/GetMonitorInStock开始调用-> " + begintime.ToString("yyyy-MM-dd HH:mm:ss.fff"));
            int plcMonData = 0;
            try
            {
                //连接
                profinet = new SiemensS7Net(SiemensPLCS.S1500);
                profinet.IpAddress = storeAddr[0];
                profinet.Port = int.Parse(storeAddr[1]);
                profinet.Rack = byte.Parse("0");
                profinet.Slot = byte.Parse("0");

                OperateResult connect = profinet.ConnectServer();
                if (connect.IsSuccess)
                {
                    log.Info("PLC连接成功！" + profinet.IpAddress);
                    Console.WriteLine("PLC连接成功！" + profinet.IpAddress);
                    //采集
                    plcMonData = ReadByteDataNew(addrRequestInStock[0]);//PLC入库请求应答
                    //断开
                    profinet.ConnectClose();
                }
                else
                {
                    log.Error("PLC连接失败！" + profinet.IpAddress);
                }

                DateTime endtime = DateTime.Now;
                string tick = ExecDateDiff(begintime, endtime);
                Console.WriteLine("耗时" + tick + "毫秒\n");
                log.Info("耗时" + tick + "毫秒\n");
                //
                return Json(plcMonData);
            }
            catch (Exception ex)
            {
                log.Error(ex.Message);
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
            Console.WriteLine("Siemens/GetMonitorInStockComplete开始调用-> " + begintime.ToString("yyyy-MM-dd HH:mm:ss.fff"));
            log.Info("Siemens/GetMonitorInStockComplete开始调用-> " + begintime.ToString("yyyy-MM-dd HH:mm:ss.fff"));
            int plcMonData = 0;
            try
            {
                //连接
                profinet = new SiemensS7Net(SiemensPLCS.S1500);
                profinet.IpAddress = storeAddr[0];
                profinet.Port = int.Parse(storeAddr[1]);
                profinet.Rack = byte.Parse("0");
                profinet.Slot = byte.Parse("0");

                OperateResult connect = profinet.ConnectServer();
                if (connect.IsSuccess)
                {
                    log.Info("PLC连接成功！" + profinet.IpAddress);
                    Console.WriteLine("PLC连接成功！" + profinet.IpAddress);
                    //采集
                    plcMonData = ReadByteDataNew(addrRequestInStock[2]);//PLC入库完成信号
                    //断开
                    profinet.ConnectClose();
                }
                else
                {
                    log.Error("PLC连接失败！" + profinet.IpAddress);
                }

                DateTime endtime = DateTime.Now;
                string tick = ExecDateDiff(begintime, endtime);
                Console.WriteLine("耗时" + tick + "毫秒\n");
                log.Info("耗时" + tick + "毫秒\n");
                //
                return Json(plcMonData);
            }
            catch (Exception ex)
            {
                log.Error(ex.Message);
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
            Console.WriteLine("Siemens/SetRFIDReadComplete开始调用-> " + begintime.ToString("yyyy-MM-dd HH:mm:ss.fff"));
            log.Info("Siemens/SetRFIDReadComplete开始调用-> " + begintime.ToString("yyyy-MM-dd HH:mm:ss.fff"));
            bool rValue = false;
            try
            {
                //连接
                profinet = new SiemensS7Net(SiemensPLCS.S1500);
                profinet.IpAddress = storeAddr[0];
                profinet.Port = int.Parse(storeAddr[1]);
                profinet.Rack = byte.Parse("0");
                profinet.Slot = byte.Parse("0");

                OperateResult connect = profinet.ConnectServer();
                if (connect.IsSuccess)
                {
                    log.Info("PLC连接成功！" + profinet.IpAddress);
                    Console.WriteLine("PLC连接成功！" + profinet.IpAddress);
                    //采集
                    rValue = writeByte(addrRequestInStock[1], 1);

                    //断开
                    profinet.ConnectClose();
                }
                else
                {
                    log.Error("PLC连接失败！" + profinet.IpAddress);
                }

                DateTime endtime = DateTime.Now;
                string tick = ExecDateDiff(begintime, endtime);
                Console.WriteLine("耗时" + tick + "毫秒\n");
                log.Info("耗时" + tick + "毫秒\n");
                //
                return Json(rValue);
            }
            catch (Exception ex)
            {
                log.Error(ex.Message);
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
            Console.WriteLine("Siemens/ResetRFIDReadComplete开始调用-> " + begintime.ToString("yyyy-MM-dd HH:mm:ss.fff"));
            log.Info("Siemens/ResetRFIDReadComplete开始调用-> " + begintime.ToString("yyyy-MM-dd HH:mm:ss.fff"));
            bool rValue = false;
            try
            {
                //连接
                profinet = new SiemensS7Net(SiemensPLCS.S1500);
                profinet.IpAddress = storeAddr[0];
                profinet.Port = int.Parse(storeAddr[1]);
                profinet.Rack = byte.Parse("0");
                profinet.Slot = byte.Parse("0");

                OperateResult connect = profinet.ConnectServer();
                if (connect.IsSuccess)
                {
                    log.Info("PLC连接成功！" + profinet.IpAddress);
                    Console.WriteLine("PLC连接成功！" + profinet.IpAddress);
                    //采集
                    int plcControl = ReadByteDataNew(addrRequestInStock[2]);
                    if (plcControl == 1)
                        rValue = writeByte(addrRequestInStock[1], 0);
                    else
                        rValue = true;

                    //断开
                    profinet.ConnectClose();
                }
                else
                {
                    log.Error("PLC连接失败！" + profinet.IpAddress);
                }

                DateTime endtime = DateTime.Now;
                string tick = ExecDateDiff(begintime, endtime);
                Console.WriteLine("耗时" + tick + "毫秒\n");
                log.Info("耗时" + tick + "毫秒\n");
                //
                return Json(rValue);
            }
            catch (Exception ex)
            {
                log.Error(ex.Message);
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
            Console.WriteLine("Siemens/ReadByte4Req开始调用-> " + begintime.ToString("yyyy-MM-dd HH:mm:ss.fff"));
            log.Info("Siemens/ReadByte4Req开始调用-> " + begintime.ToString("yyyy-MM-dd HH:mm:ss.fff"));
            int plcMonData = 0;
            try
            {
                //连接
                profinet = new SiemensS7Net(SiemensPLCS.S1500);
                profinet.IpAddress = storeAddr[0];
                profinet.Port = int.Parse(storeAddr[1]);
                profinet.Rack = byte.Parse("0");
                profinet.Slot = byte.Parse("0");

                OperateResult connect = profinet.ConnectServer();
                if (connect.IsSuccess)
                {
                    log.Info("PLC连接成功！" + profinet.IpAddress);
                    Console.WriteLine("PLC连接成功！" + profinet.IpAddress);
                    //采集
                    plcMonData = ReadByteDataNew(addr);//PLC入库请求应答
                    //断开
                    profinet.ConnectClose();
                }
                else
                {
                    log.Error("PLC连接失败！" + profinet.IpAddress);
                }

                DateTime endtime = DateTime.Now;
                string tick = ExecDateDiff(begintime, endtime);
                Console.WriteLine("耗时" + tick + "毫秒\n");
                log.Info("耗时" + tick + "毫秒\n");
                //
                return Json(plcMonData);
            }
            catch (Exception ex)
            {
                log.Error(ex.Message);
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
            Console.WriteLine("Siemens/WriteByte4Req开始调用-> " + begintime.ToString("yyyy-MM-dd HH:mm:ss.fff"));
            log.Info("Siemens/WriteByte4Req开始调用-> " + begintime.ToString("yyyy-MM-dd HH:mm:ss.fff"));
            bool rValue = false;
            try
            {
                //连接
                profinet = new SiemensS7Net(SiemensPLCS.S1500);
                profinet.IpAddress = storeAddr[0];
                profinet.Port = int.Parse(storeAddr[1]);
                profinet.Rack = byte.Parse("0");
                profinet.Slot = byte.Parse("0");

                OperateResult connect = profinet.ConnectServer();
                if (connect.IsSuccess)
                {
                    log.Info("PLC连接成功！" + profinet.IpAddress);
                    Console.WriteLine("PLC连接成功！" + profinet.IpAddress);
                    //采集
                    rValue = writeByte(addr, Convert.ToByte(keyValue));

                    //断开
                    profinet.ConnectClose();
                }
                else
                {
                    log.Error("PLC连接失败！" + profinet.IpAddress);
                }

                DateTime endtime = DateTime.Now;
                string tick = ExecDateDiff(begintime, endtime);
                Console.WriteLine("耗时" + tick + "毫秒\n");
                log.Info("耗时" + tick + "毫秒\n");
                //
                return Json(rValue);
            }
            catch (Exception ex)
            {
                log.Error(ex.Message);
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
            Console.WriteLine("Siemens/JudgeUTag开始调用-> " + begintime.ToString("yyyy-MM-dd HH:mm:ss.fff"));
            log.Info("Siemens/JudgeUTag开始调用-> " + begintime.ToString("yyyy-MM-dd HH:mm:ss.fff"));
            int plcMonData = 0;
            try
            {
                //连接
                profinet = new SiemensS7Net(SiemensPLCS.S1500);
                profinet.IpAddress = storeAddr[0];
                profinet.Port = int.Parse(storeAddr[1]);
                profinet.Rack = byte.Parse("0");
                profinet.Slot = byte.Parse("0");

                OperateResult connect = profinet.ConnectServer();
                if (connect.IsSuccess)
                {
                    log.Info("PLC连接成功！" + profinet.IpAddress);
                    Console.WriteLine("PLC连接成功！" + profinet.IpAddress);

                    #region 缓存料道入库1个（本次1个）
                    int _data_len = 32;
                    string addr_uii = "DB200.98";
                    string addr_umd = "DB200.130";
                    //一次性获取所有数据
                    byte[] bytesUii = _ReadManyBytes(addr_uii, ushort.Parse(_data_len.ToString()));
                    byte[] bytesUmd = _ReadManyBytes(addr_umd, ushort.Parse(_data_len.ToString()));
                    //拆分数据
                    if (bytesUii[0]== bytesUmd[0] && bytesUii[1] == bytesUmd[1]&& bytesUii[2] == bytesUmd[2]&& bytesUii[3] == bytesUmd[3])
                    {
                        plcMonData = 1;
                    }
                    #endregion

                    //断开
                    profinet.ConnectClose();
                }
                else
                {
                    log.Error("PLC连接失败！" + profinet.IpAddress);
                }

                DateTime endtime = DateTime.Now;
                string tick = ExecDateDiff(begintime, endtime);
                Console.WriteLine("耗时" + tick + "毫秒\n");
                log.Info("耗时" + tick + "毫秒\n");
                //
                return Json(plcMonData);
            }
            catch (Exception ex)
            {
                log.Error(ex.Message);
                return BadRequest("获取状态失败：" + ex.Message);
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
            try
            {
                if (bytes_dm.Length == 0)
                    return data;

                data = bytes_dm[len_D];//取低位
                log.Info("ReadByteData读取结果：" + data);
                return data;
            }
            catch (Exception ex)
            {
                log.Error("ReadByteData读取失败：" + ex.Message);
                return 0;
            }
        }
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
                log.Error("ReadInt16Data读取失败：" + ex.Message);
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

                tmpBuffer[0] = bytes_dm[len_D + 0];
                tmpBuffer[1] = bytes_dm[len_D + 1];
                tmpBuffer[2] = bytes_dm[len_D + 2];
                tmpBuffer[3] = bytes_dm[len_D + 3];

                sdata += Convert.ToString(tmpBuffer[0], 2).PadLeft(8, '0');
                sdata += Convert.ToString(tmpBuffer[1], 2).PadLeft(8, '0');
                sdata += Convert.ToString(tmpBuffer[2], 2).PadLeft(8, '0');
                sdata += Convert.ToString(tmpBuffer[3], 2).PadLeft(8, '0');

                data = Convert.ToInt32(sdata, 2); //二进制字符串转十进制数
                log.Info("ReadInt32Data读取结果：" + data);
                return data;
            }
            catch (Exception ex)
            {
                log.Error("ReadInt32Data读取失败：" + ex.Message);
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
                log.Error("ReadFloatData读取失败：" + ex.Message);
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
                log.Error("ReadStringData读取失败：" + ex.Message);
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
                OperateResult<byte> result = profinet.ReadByte(address);
                if (result.IsSuccess)
                {
                    //Console.WriteLine("result.Content:" + result.Content);
                    return result.Content;
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
                    log.Error(result.ToMessageShowString());
                    return false;
                }
            }
            catch (Exception ex)
            {
                log.Error("Write Error\n\nCould not write to PLC" + ex.Message);
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
                    log.Error(result.ToMessageShowString());
                    return false;
                }
            }
            catch (Exception ex)
            {
                log.Error("Write Error\n\nCould not write to PLC" + ex.Message);
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
                    log.Error(result.ToMessageShowString());
                    return false;
                }
            }
            catch (Exception ex)
            {
                log.Error("Write Error\n\nCould not write to PLC" + ex.Message);
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
                string firstStr = "M";
                if (PDataAddr.Substring(0, 2) == "DB")
                {
                    addr_start = int.Parse(PDataAddr.Split('.')[1]);
                    firstStr = PDataAddr.Split('.')[0];
                }
                else
                {
                    addr_start = int.Parse(PDataAddr.Substring(1));
                    firstStr = PDataAddr.Substring(0, 1);
                }
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
        /// <summary>
        /// 控制台传入值
        /// </summary>
        /// <param name="interfaceName">接口名称</param>
        /// <param name="paras">传入值</param>
        private void InterfaceIn(string interfaceName, Dictionary<string, string> paras)
        {
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.WriteLine("**************************************************");
            Console.WriteLine(("* " + interfaceName).PadRight(30, ' ') + DateTime.Now.ToString() + " *");
            Console.WriteLine("**************************************************");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("传入值");
            foreach (var para in paras)
            {
                Console.WriteLine("{0}:{1}", para.Key, para.Value);
            }
        }
        /// <summary>
        /// 控制台返回值
        /// </summary>
        /// <param name="result">返回值</param>
        private void InterfaceOut(string result)
        {
            Console.WriteLine("返回值");
            Console.WriteLine(result);
        }
        /// <summary>
        /// 返回Json格式消息
        /// </summary>
        /// <param name="msg">字符串</param>
        /// <returns></returns>
        public HttpResponseMessage ReturnErrorJson(string msg)
        {
            JsonObject json = new JsonObject();
            json["Message"] = msg;
            json["Flag"] = "0";
            log.Error(json);
            InterfaceOut("Fail." + msg);
            return Request.CreateResponse(HttpStatusCode.BadRequest, json);
        }
        /// <summary>
        /// 返回Json格式消息
        /// </summary>
        /// <param name="msg">字符串</param>
        /// <returns></returns>
        public HttpResponseMessage ReturnJson(string flag, string data, string msg)
        {
            JsonObject json = new JsonObject();
            json["Flag"] = flag;
            json["Message"] = msg;
            json["Data"] = data;
            log.Info(json);
            if (flag == "1")
            {
                InterfaceOut("Success.");
                return Request.CreateResponse(HttpStatusCode.OK, json);
            }
            else
            {
                InterfaceOut("Fail." + msg);
                return Request.CreateResponse(HttpStatusCode.BadRequest, json);
            }
        }




    }
}
