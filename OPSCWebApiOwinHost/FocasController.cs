using JNRSWebApiOwinHost.Models;
using log4net;
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
    [RoutePrefix("api/Focas")]
    public class FocasController : ApiController
    {
        private NBear.Data.Gateway gate = new NBear.Data.Gateway("JNRSAPI");
        protected readonly ILog Loger = LogManager.GetLogger("JNRSLogger");
        HelperBiz _helper = new HelperBiz();
        
        /// <summary>
        /// 通过machine_id获取ip，port，并连接设备
        /// </summary>
        private ushort GetFlibhndl(int machine_id,out string msg)
        {
            msg = "";
            ushort _Flibhndl = 0;
            IMongoQuery query1 = Query.EQ("machine_id", machine_id);
            clc_machine_infoEntity mi = _DoMongo.GetModel<clc_machine_infoEntity>("clc_machine_info", query1);
            //
            try
            {
                if (mi == null)
                {
                    msg = "没找到id为" + machine_id.ToString() + "的设备";
                    return _Flibhndl;
                }

                short ret; // 返回值
                // 获取库句柄 ( Ethernet )
                ret = Focas1.cnc_allclibhndl3(mi.machine_ip, ushort.Parse(mi.machine_port.ToString()), 10, out _Flibhndl);
                if (ret != Focas1.EW_OK)
                {
                    msg = "发生异常，请检查！";
                    return _Flibhndl;
                }
                return _Flibhndl;
            }
            catch (Exception e)
            {
                msg = e.Message + "\n" + e.StackTrace;
                Loger.Info(msg);
                return _Flibhndl;
            }
        }

        /// <summary>
        /// 根据machine_id获取CNC中的程序列表
        /// </summary>
        [Route("GetCNCFileName")]
        [HttpGet]
        public IHttpActionResult GetCNCFileName(int machine_id)
        {
            //显示传入值
            Dictionary<string, string> paras = new Dictionary<string, string>();
            paras.Add("machine_id", machine_id.ToString());
            InterfaceIn("GetCNCFileName", paras);
            //记录日志
            string msgvm = "GetCNCFileName:machine_id->" + machine_id;
            Loger.Info(msgvm);
            //主体
            try
            {
                //设备连接
                string msg = "";
                ushort Flibhndl = GetFlibhndl(machine_id, out msg);
                Console.WriteLine("Flibhndl:"+ Flibhndl);
                if (Flibhndl == 0)
                {
                    Loger.Info("设备连接失败！");
                    return BadRequest(msg);
                }
                else
                {
                    Console.WriteLine(machine_id.ToString() + "设备连接成功！");
                    Loger.Info(machine_id.ToString() + "设备连接成功！");
                }
                //
                string path = "//CNC_MEM/USER/PATH1/";//指定文件夹
                List<string> filenames = new List<string>();
                List<string> filefolders = new List<string>();
                int dir_num = 0; int file_num = 0;
                //string pronames = "";
                //调用
                GetFileNum(Flibhndl, path, out dir_num, out file_num);
                if (file_num > 0)
                {
                    GetFilePaths(Flibhndl, path, dir_num + file_num, out filenames, out filefolders);
                    Console.WriteLine("GetFilePaths OK");
                    Loger.Info("GetFilePaths OK");
                    //
                    IMongoQuery query1 = Query.EQ("machine_id", machine_id);
                    _DoMongo.Remove("clc_files", query1);
                    //
                    clc_filesEntity _clc_files;
                    foreach (var item in filenames)
                    {
                        Loger.Info("item:" + item);
                        //存储至MongoDB的clc_files数据集
                        _clc_files = new clc_filesEntity();
                        _clc_files.machine_id = machine_id;
                        _clc_files.program_name = item;
                        _clc_files.download = 0;
                        //
                        //List<clc_filesEntity_query> list1 = _DoCNCFiles.GetCncFilesIsExist(machine_id, item);
                        //if (list1.Count == 0)
                        //{
                            Loger.Info("存储至clc_files数据集:" + item);
                            _DoMongo.putStandardCNCFiles(_clc_files, DateTime.Now);
                            Loger.Info("存储成功:" + item);
                        //}

                        //存储至SQL的clc_files表中
                        //DataTable dt = gate.ExecuteStoredProcedure("DNC_InsertPrgName",
                        //    new string[] { "machine_id", "program_name" },
                        //    new object[] { machine_id, item }).Tables[0];

                        //string msg0 = dt.Rows[0][0].ToString();
                        //if (!string.IsNullOrEmpty(msg))
                        //{
                        //    Loger.Info("msg:" + msg);
                        //}
                    }
                }
                else
                    Console.WriteLine("此路径下没有文件");

                return Ok();
            }
            catch (Exception e)
            {
                return BadRequest(e.Message + e.StackTrace);
            }
        }
        /// <summary>
        /// 根据路径获取文件名和文件夹名的数量
        /// </summary>
        public void GetFileNum(ushort Flibhndl,string pathname, out int dir_num, out int file_num)
        {
            short ret; // 返回值
            #region step1:cnc_rdpdf_subdirn           
            Console.WriteLine("pathname:" + pathname);
            Focas1.ODBPDFNFIL pdf_nfil2 = new Focas1.ODBPDFNFIL();
            ret = Focas1.cnc_rdpdf_subdirn(Flibhndl, pathname, pdf_nfil2);
            Loger.Info("读文件夹数量ret:" + ret);
            Loger.Info("dir_num:" + pdf_nfil2.dir_num);
            Loger.Info("file_num:" + pdf_nfil2.file_num);
            Console.WriteLine("读文件夹数量ret:" + ret);
            Console.WriteLine("dir_num:" + pdf_nfil2.dir_num);
            Console.WriteLine("file_num:" + pdf_nfil2.file_num);
            dir_num = pdf_nfil2.dir_num;
            file_num = pdf_nfil2.file_num;           
            #endregion
        }
        /// <summary>
        /// 根据路径获取文件名和文件夹名列表
        /// </summary>
        /// <param name="pathname">路径</param>
        /// <param name="num">路径下文件和文件夹总数量</param>
        /// <param name="filenames">文件名列表</param>
        public void GetFilePaths(ushort Flibhndl,string pathname, int num, out List<string> filenames, out List<string> filefolders)
        {
            try
            {
                short ret; // 返回值
                //data_kind=1 文件；data_kind=0 文件夹
                filenames = new List<string>();
                filefolders = new List<string>();
                #region cnc_rdpdf_alldir

                short num_prog = 100;
                Focas1.IDBPDFADIR pdf_adir_in;
                Focas1.ODBPDFADIR pdf_adir_out;
                for (int i = 0; i < num; i++)
                {
                    pdf_adir_in = new Focas1.IDBPDFADIR();
                    pdf_adir_out = new Focas1.ODBPDFADIR();

                    pdf_adir_in.path = pathname;
                    pdf_adir_in.size_kind = 3;
                    pdf_adir_in.req_num = Convert.ToInt16(i);//通过改变req_num可以读到不同文件名

                    ret = Focas1.cnc_rdpdf_alldir(Flibhndl, ref num_prog, pdf_adir_in, pdf_adir_out);
                    Loger.Info("调用cnc_rdpdf_alldir:" + ret.ToString());
                    //Console.WriteLine("读文件夹下文件信息ret:" + ret);
                    //Console.WriteLine("y-m-d:" + pdf_adir_out.year + "-" + pdf_adir_out.mon + "-" + pdf_adir_out.day);
                    //Console.WriteLine("h-m-s:" + pdf_adir_out.hour + ":" + pdf_adir_out.min + ":" + pdf_adir_out.sec);
                    //Console.WriteLine("data_kind:" + pdf_adir_out.data_kind);
                    //Console.WriteLine("d_f:" + pdf_adir_out.d_f);
                    if (pdf_adir_out.data_kind == 1)
                    {
                        filenames.Add(pdf_adir_out.d_f);
                    }
                    else
                    if (pdf_adir_out.data_kind == 0)
                    {
                        filefolders.Add(pdf_adir_out.d_f);
                    }
                }
                #endregion
            }
            catch (Exception ex)
            {
                Console.WriteLine("GetFilePaths---" + ex.Message + "\n" + ex.StackTrace);
                filenames = null;
                filefolders = null;
            }
        }
        /// <summary>
        /// 下载程序(作废)  
        /// </summary>
        [Route("DownLoadProgram")]
        [HttpGet]
        public IHttpActionResult DownLoadProgram(int machine_id, string prgname)
        {
            //显示传入值
            Dictionary<string, string> paras = new Dictionary<string, string>();
            paras.Add("machine_id", machine_id.ToString());
            paras.Add("prgname", prgname);
            InterfaceIn("DownLoadProgram", paras);
            //记录日志
            string msgvm = "DownLoadProgram:machine_id->" + machine_id+ " prgname->" + prgname;
            Loger.Info(msgvm);

            short ret; // 返回值
            try
            {
                //设备连接
                string msg = "";
                ushort Flibhndl = GetFlibhndl(machine_id, out msg);
                Console.WriteLine("Flibhndl:" + Flibhndl);
                if (Flibhndl == 0)
                {
                    return BadRequest(msg);
                }
                else
                    Console.WriteLine(machine_id.ToString() + "设备连接成功！");
                //
                #region cnc_upload4
                string str = "";
                byte[] buf = new byte[1280]; // String of CNC program
                int len;
                ret = Focas1.cnc_upstart4(Flibhndl, 0, prgname);// "//CNC_MEM/USER/PATH1/prgname"
                if (ret != Focas1.EW_OK)
                {
                    Loger.Info("cnc_upstart4:" + ret);
                    return BadRequest("cnc_upstart4 fail!");
                }
                do
                {
                    len = 1280;
                    do
                    {
                        ret = Focas1.cnc_upload4(Flibhndl, ref len, buf);
                    } while (ret == 10);

                    if (ret == Focas1.EW_OK)
                    {
                        for (int idx = 0; idx < len; idx++)
                            str += Convert.ToString(Convert.ToChar(buf[idx]));
                    }
                    if (buf[len - 1] == '%')
                    {
                        break;
                    }
                } while (ret == Focas1.EW_OK);
                ret = Focas1.cnc_upend4(Flibhndl);
                Loger.Info("CNC_Program:" + str);
                string con_fanucFile = ConfigurationManager.AppSettings["fanucFile"].ToString();
                File.WriteAllText(con_fanucFile + prgname, str);
                #endregion
                return Ok(str);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message + e.StackTrace);
            }
        }

        /// <summary>
        /// 下载程序  
        /// </summary>
        [Route("DownLoadProgram2")]
        [HttpGet]
        public HttpResponseMessage DownLoadProgram2(int machine_id, string prgname)
        {
            //显示传入值
            Dictionary<string, string> paras = new Dictionary<string, string>();
            paras.Add("machine_id", machine_id.ToString());
            paras.Add("prgname", prgname);
            InterfaceIn("DownLoadProgram2", paras);
            //记录日志
            string msgvm = "DownLoadProgram:machine_id->" + machine_id + " prgname->" + prgname;
            Loger.Info(msgvm);

            short ret; // 返回值
            try
            {
                //设备连接
                string msg = "";
                ushort Flibhndl = GetFlibhndl(machine_id, out msg);
                Console.WriteLine("Flibhndl:" + Flibhndl);
                if (Flibhndl == 0)
                {
                    //return BadRequest(msg);
                    return ReturnJson(msg);
                }
                else
                    Console.WriteLine(machine_id.ToString() + "设备连接成功！");
                //
                #region cnc_upload4
                string str = "";
                byte[] buf = new byte[1280]; // String of CNC program
                int len;
                ret = Focas1.cnc_upstart4(Flibhndl, 0, prgname);// "//CNC_MEM/USER/PATH1/prgname"
                if (ret != Focas1.EW_OK)
                {
                    Loger.Info("cnc_upstart4:" + ret);
                    //return BadRequest("cnc_upstart4 fail!");
                    return ReturnJson("cnc_upstart4:" + ret);
                }
                do
                {
                    len = 1280;
                    do
                    {
                        ret = Focas1.cnc_upload4(Flibhndl, ref len, buf);
                    } while (ret == 10);

                    if (ret == Focas1.EW_OK)
                    {
                        for (int idx = 0; idx < len; idx++)
                            str += Convert.ToString(Convert.ToChar(buf[idx]));
                    }
                    if (buf[len - 1] == '%')
                    {
                        break;
                    }
                } while (ret == Focas1.EW_OK);
                ret = Focas1.cnc_upend4(Flibhndl);
                Loger.Info("CNC_Program:" + str);
                string con_fanucFile = ConfigurationManager.AppSettings["fanucFile"].ToString();
                File.WriteAllText(con_fanucFile + prgname, str);
                #endregion
                //
                //string fileName = "O1107";//客户端保存的文件名
                //string filePath = "F:\\SIEMENS\\O1107";//路径

                //以字符流的形式下载文件
                //FileStream fs = new FileStream(filePath, FileMode.Open);
                //byte[] bytes = new byte[(int)fs.Length];
                //fs.Read(bytes, 0, bytes.Length);
                //fs.Close();
                //HttpResponseMessage Response = Request.CreateResponse(HttpStatusCode.OK);
                //Response.ContentType = "application/octet-stream";
                ////通知浏览器下载文件而不是打开
                //Response.AddHeader("Content-Disposition", "attachment;  filename=" + HttpUtility.UrlEncode(fileName, System.Text.Encoding.UTF8));
                //Response.BinaryWrite(bytes);
                //Response.Flush();
                //Response.End();

                //以字符流的形式下载文件
                //FileStream fs = new FileStream(filePath, FileMode.Open);
                //byte[] bytes = new byte[(int)fs.Length];
                //fs.Read(bytes, 0, bytes.Length);
                //fs.Close();
                //Response.ContentType = "application/octet-stream";
                ////通知浏览器下载文件而不是打开
                //Response.AddHeader("Content-Disposition", "attachment; filename=" + HttpUtility.UrlEncode(fileName, System.Text.Encoding.UTF8));
                //Response.BinaryWrite(bytes);
                //Response.Flush();
                //Response.End();
                //
                //System.Net.Http.Headers.TransferCodingHeaderValue
                MediaTypeHeaderValue _mediaType = MediaTypeHeaderValue.Parse("application/octet-stream");//指定文件类型
                ContentDispositionHeaderValue _disposition = ContentDispositionHeaderValue.Parse("attachment;filename=" + HttpUtility.UrlEncode(prgname));//指定文件名称（编码中文）
                HttpResponseMessage fullResponse = Request.CreateResponse(HttpStatusCode.OK);
                FileStream fileStream = new FileStream(con_fanucFile + prgname, FileMode.Open);
                fullResponse.Content = new StreamContent(fileStream);
                fullResponse.Content.Headers.ContentType = _mediaType;
                fullResponse.Content.Headers.ContentDisposition = _disposition;
                //fileStream.Close();
                return fullResponse;
            }
            catch (Exception ex)
            {
                Loger.Error(ex);
                throw new HttpResponseException(HttpStatusCode.NotFound);
            }
        }
        /// <summary>
        /// 删除程序  
        /// </summary>
        [Route("DeleteProgram")]
        [HttpGet]
        public IHttpActionResult DeleteProgram(int machine_id, string prgname)
        {
            //显示传入值
            Dictionary<string, string> paras = new Dictionary<string, string>();
            paras.Add("machine_id", machine_id.ToString());
            paras.Add("prgname", prgname);
            InterfaceIn("DeleteProgram", paras);
            //记录日志
            string msgvm = "DeleteProgram:machine_id->" + machine_id + " prgname->" + prgname;
            Loger.Info(msgvm);

            short ret; // 返回值
            try
            {
                //设备连接
                string msg = "";
                ushort Flibhndl = GetFlibhndl(machine_id, out msg);
                Console.WriteLine("Flibhndl:" + Flibhndl);
                if (Flibhndl == 0)
                {
                    return BadRequest(msg);
                }
                else
                    Console.WriteLine(machine_id.ToString() + "设备连接成功！");
                //
                #region cnc_pdf_del
                ret = Focas1.cnc_pdf_del(Flibhndl, "//CNC_MEM/USER/PATH1/" + prgname);
                Loger.Info("删除CNC文件ret:" + ret);
                if (ret == Focas1.EW_OK)
                {
                    _DoCNCFiles.RemoveCncFilesOne(machine_id, prgname);
                    Loger.Info("删除程序" + prgname + "完成！");
                    return Ok();
                }
                else
                    return BadRequest("删除CNC程序" + prgname + "失败");
                #endregion
            }
            catch (Exception e)
            {
                return BadRequest(e.Message + e.StackTrace);
            }
        }
        /// <summary>
        /// 上传程序
        /// </summary>
        [Route("UpLoadProgram")]
        [HttpGet]
        public IHttpActionResult UpLoadProgram(int machine_id, string prgname)
        {
            //显示传入值
            Dictionary<string, string> paras = new Dictionary<string, string>();
            paras.Add("machine_id", machine_id.ToString());
            paras.Add("prgname", prgname);
            InterfaceIn("UpLoadProgram", paras);
            //记录日志
            string msgvm = "UpLoadProgram:machine_id->" + machine_id + " prgname->" + prgname;
            Loger.Info(msgvm);

            short ret; // 返回值
            try
            {
                //设备连接
                string msg = "";
                ushort Flibhndl = GetFlibhndl(machine_id, out msg);
                Console.WriteLine("Flibhndl:" + Flibhndl);
                if (Flibhndl == 0)
                {
                    return BadRequest(msg);
                }
                else
                    Console.WriteLine(machine_id.ToString() + "设备连接成功！");
                //
                //读取程序内容
                Loger.Info("prgname:" + prgname);
                string con_fanucFile = ConfigurationManager.AppSettings["fanucFile"].ToString();
                //string content = System.IO.File.ReadAllText(@"C:\Users\Public\TestFolder\WriteText.txt");
                string content = File.ReadAllText(con_fanucFile + machine_id.ToString() + "\\" + prgname);
                Loger.Info("content:\n" + content);
                //
                if (string.IsNullOrEmpty(content))
                {
                    msg = "程序" + prgname + "内容为空，不能上传！";
                    return BadRequest(msg);
                }
                //上传前先删除程序
                #region cnc_pdf_del
                ret = Focas1.cnc_pdf_del(Flibhndl, "//CNC_MEM/USER/PATH1/" + prgname);
                Loger.Info("删除CNC文件ret:" + ret);
                #endregion

                #region cnc_download4
                //string prg = "\nO1101\nM3 S1200\nG0 Z0\nG0 X0 Y0\nG1 F500 X120. Y-30.\nM30\n%";
                string prgText = content;
                int len = 0;
                int n = 0;

                ret = Focas1.cnc_dwnstart4(Flibhndl, 0, "//CNC_MEM/USER/PATH1/");
                if (ret != Focas1.EW_OK)
                {
                    Loger.Info("cnc_dwnstart4:" + ret);
                    return BadRequest("cnc_dwnstart4 fail!"); 
                }

                len = prgText.Length;
                while (len > 0)
                {
                    n = len;
                    ret = Focas1.cnc_download4(Flibhndl, ref n, prgText);
                    if (ret == (short)Focas1.focas_ret.EW_BUFFER)
                    {
                        continue;
                    }
                    if (ret == Focas1.EW_OK)
                    {
                        prgText += n;
                        len -= n;
                    }
                    if (ret != Focas1.EW_OK)
                    {
                        break;
                    }
                }
                ret = Focas1.cnc_dwnend4(Flibhndl);
                Loger.Info("cnc_dwnend4:" + ret);
                #endregion
                return Ok();
            }
            catch (Exception e)
            {
                return BadRequest(e.Message + e.StackTrace);
            }
        }
        /// <summary>
        /// 上传程序2
        /// </summary>
        [Route("UpLoadProgram2")]
        [HttpGet]
        public IHttpActionResult UpLoadProgram2(string id)
        {
            //显示传入值
            Dictionary<string, string> paras = new Dictionary<string, string>();
            paras.Add("id", id);
            InterfaceIn("UpLoadProgram2", paras);
            //记录日志
            string msgvm = "UpLoadProgram2:id->" + id;
            Loger.Info(msgvm);

            short ret; // 返回值
            try
            {
                string msg = "";
                //获取对象
                IMongoQuery query1 = Query.EQ("_id", id);
                clc_dncEntity_query dnc = _DoMongo.GetModel<clc_dncEntity_query>("clc_dnc", query1);
                if (dnc == null)
                {
                    msg = "id为" + id.ToString() + "的数据不存在";
                    return BadRequest(msg);
                }
                //属性值
                int machine_id = dnc.machine_id;
                string prgname = dnc.program_name;
                string path = dnc.upload_address;
                //设备连接
                ushort Flibhndl = GetFlibhndl(machine_id, out msg);
                Console.WriteLine("Flibhndl:" + Flibhndl);
                if (Flibhndl == 0)
                {
                    return BadRequest(msg);
                }
                else
                    Console.WriteLine(machine_id.ToString() + "设备连接成功！");
                //
                //读取程序内容
                Loger.Info("prgname:" + prgname);
                //string content = System.IO.File.ReadAllText(@"C:\Users\Public\TestFolder\WriteText.txt");
                //string con_fanucFile = ConfigurationManager.AppSettings["fanucFile"].ToString();
                //string content = File.ReadAllText(con_fanucFile + prgname);
                string content = File.ReadAllText(path);
                Loger.Info("content:\n" + content);
                if (string.IsNullOrEmpty(content))
                {
                    msg = "程序" + prgname + "内容为空，不能上传！";
                    return BadRequest(msg);
                }
                //上传前先删除程序
                #region cnc_pdf_del
                ret = Focas1.cnc_pdf_del(Flibhndl, "//CNC_MEM/USER/PATH1/" + prgname);
                Loger.Info("删除CNC文件ret:" + ret);
                #endregion

                //上传程序
                #region cnc_download4
                //string prg = "\nO1101\nM3 S1200\nG0 Z0\nG0 X0 Y0\nG1 F500 X120. Y-30.\nM30\n%";
                string prgText = content;
                int len = 0;
                int n = 0;

                ret = Focas1.cnc_dwnstart4(Flibhndl, 0, "//CNC_MEM/USER/PATH1/");
                if (ret != Focas1.EW_OK)
                {
                    Loger.Info("cnc_dwnstart4:" + ret);
                    return BadRequest("cnc_dwnstart4 fail!");
                }

                len = prgText.Length;
                while (len > 0)
                {
                    n = len;
                    ret = Focas1.cnc_download4(Flibhndl, ref n, prgText);
                    if (ret == (short)Focas1.focas_ret.EW_BUFFER)
                    {
                        continue;
                    }
                    if (ret == Focas1.EW_OK)
                    {
                        prgText += n;
                        len -= n;
                    }
                    if (ret != Focas1.EW_OK)
                    {
                        break;
                    }
                }
                ret = Focas1.cnc_dwnend4(Flibhndl);
                Loger.Info("cnc_dwnend4:" + ret);
                #endregion
                return Ok();
            }
            catch (Exception e)
            {
                return BadRequest(e.Message + e.StackTrace);
            }
        }

        [Route("InsertMongoDB")]
        [HttpGet]
        public IHttpActionResult InsertMongoDB(int machine_id)
        {
            List<string> filenames = new List<string>();
            filenames.Add("O1102");
            clc_filesEntity _clc_files;
            foreach (var item in filenames)
            {
                Loger.Info("item:" + item);
                //存储至MongoDB的clc_files数据集
                _clc_files = new clc_filesEntity();
                _clc_files.machine_id = machine_id;
                _clc_files.program_name = item;
                _clc_files.download = 0;
                //
                List<clc_filesEntity_query> list1 = _DoCNCFiles.GetCncFilesIsExist(machine_id, item);
                if (list1.Count == 0)
                {
                    _DoMongo.putStandardCNCFiles(_clc_files, DateTime.Now);
                }
            }
            return Ok();
        }
        [Route("Test")]
        [HttpGet]
        //测试
        public IHttpActionResult TestAPI(string a, string b)
        {
            //http://127.0.0.1:9086/api/Focas/Test?barcode=1&dao=2
            Console.WriteLine(a + "-" + b);
            return Ok();
        }

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
        public HttpResponseMessage ReturnJson(string msg)
        {
            JsonObject json = new JsonObject();
            json["Message"] = msg;
            Loger.Error(json);
            InterfaceOut(msg);
            return Request.CreateResponse(HttpStatusCode.BadRequest, json);
        }
    }
}
