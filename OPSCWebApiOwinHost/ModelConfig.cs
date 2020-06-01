using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JNRSWebApiOwinHost
{
    public class ModelConfig
    {
        public List<IPConfig> IPConfig { get; set; }
    }

    public class IPConfig
    {
        public string ip { get; set; }//ip地址
        public int port { get; set; }//端口
        public string series { get; set; }
        public int slot { get; set; }
        public List<ParaConfig> ParaConfig { get; set; }//参数对象
    }
    public class ParaConfig
    {              
        public string machine_id { get; set; }//设备ID
        public string machine_name { get; set; }//设备名称
        public string machine_number { get; set; }//设备编号
        public string is_main { get; set; }//是否主设备
        public string fields_name { get; set; }//参数名
        public string data_addr { get; set; }//采集地址
        public string data_num { get; set; }//采集数量
        public string data_len { get; set; }//采集长度
        public string[] data_setup { get; set; }//采集值
    }
    //public class ParaConfig
    //{
    //    public string ip { get; set; }
    //    public int port { get; set; }
    //    public string series { get; set; }
    //    public int slot { get; set; }

    //    #region state
    //    public string run_state1 { get; set; }
    //    public string run_state2 { get; set; }
    //    public string run_state3 { get; set; }
    //    public string run_state4 { get; set; }
    //    public string run_state5 { get; set; }
    //    public string run_state6 { get; set; }
    //    public string run_state7 { get; set; }
    //    public string run_state8 { get; set; }
    //    public string run_state9 { get; set; }
    //    public string run_state10 { get; set; }
    //    public string run_state11 { get; set; }
    //    public string run_state12 { get; set; }
    //    public string run_state13 { get; set; }
    //    public string run_state14 { get; set; }
    //    public string run_state15 { get; set; }
    //    public string run_state16 { get; set; }
    //    public string run_state17 { get; set; }
    //    public string run_state18 { get; set; }
    //    public string run_state19 { get; set; }
    //    public string run_state20 { get; set; }
    //    #endregion

    //    #region prod
    //    public string plan_prod_num_day { get; set; }//目标产量/日
    //    public string slcount_day { get; set; }//入线产量/日
    //    public string xlcount_day { get; set; }//出线产量/日
    //    public string plan_prod_num_shift { get; set; }//目标产量/班次
    //    public string slcount_shift { get; set; }//入线产量/班次
    //    public string xlcount_shift { get; set; }//出线产量/班次
    //    public string npass_prod_num_day { get; set; }//不合格产量/日
    //    public string npass_prod_num_shift { get; set; }//不合格产量/班次
    //    public string achieving_rate_day { get; set; }//达成率/日
    //    public string achieving_rate_shift { get; set; }//达成率/班次
    //    public string pass_prod_rate_day { get; set; }//合格率/日
    //    public string pass_prod_rate_shift { get; set; }//合格率/班次
    //    public string npass_prod_rate_day { get; set; }//不合格率/日
    //    public string npass_prod_rate_shift { get; set; }//不合格率/班次
    //    #endregion

    //    public string alarm_no { get; set; }
    //    public string act_axis_0 { get; set; }//A轴坐标
    //    public string act_axis_1 { get; set; }//B轴坐标
    //    public string act_axis_2 { get; set; }//C轴坐标
    //    public string act_axis_3 { get; set; }//D轴坐标
    //    public string act_axis_4 { get; set; }//E轴坐标
    //    public string product_category { get; set; }//产品种类
    //    public string spindle_speed { get; set; }//速度
    //    public string spindle_override { get; set; }//倍率
    //}
}
