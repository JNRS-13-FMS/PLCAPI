using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JNRSWebApiOwinHost
{
    public class StoreModel
    {
        public string location_no { get; set; }//工件库数据-库编码NO.
        public string workpiece_type { get; set; }//工件数据-零件号 
        public string workpiece_status { get; set; }//工件数据-零件状态
        public string mac_proc_info { get; set; }//工件数据-机床加工信息

        //public float axis_x { get; set; }
        //public float axis_y { get; set; }
    }
}
