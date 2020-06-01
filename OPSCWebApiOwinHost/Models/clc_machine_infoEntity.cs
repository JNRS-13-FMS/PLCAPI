using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JNRSWebApiOwinHost.Models
{
    public class clc_machine_infoEntity
    {
        public object _id { get; set; }
        #region  实体成员
        /// <summary>
        /// group_id
        /// </summary>
        /// <returns></returns>
        [Column("GROUP_ID")]
        public int? group_id { get; set; }
        /// <summary>
        /// group_name
        /// </summary>
        /// <returns></returns>
        [Column("GROUP_NAME")]
        public string group_name { get; set; }
        /// <summary>
        /// machine_id
        /// </summary>
        /// <returns></returns>
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("MACHINE_ID")]
        public int machine_id { get; set; }
        /// <summary>
        /// machine_name
        /// </summary>
        /// <returns></returns>
        [Column("MACHINE_NAME")]
        public string machine_name { get; set; }
        /// <summary>
        /// machine_number
        /// </summary>
        /// <returns></returns>
        [Column("MACHINE_NUMBER")]
        public string machine_number { get; set; }
        /// <summary>
        /// machine_series
        /// </summary>
        /// <returns></returns>
        [Column("machine_series")]
        public string machine_series { get; set; }
        /// <summary>
        /// comm_protocol
        /// </summary>
        /// <returns></returns>
        [Column("COMM_PROTOCOL")]
        public string comm_protocol { get; set; }
        /// <summary>
        /// comm_interface
        /// </summary>
        /// <returns></returns>
        [Column("COMM_INTERFACE")]
        public string comm_interface { get; set; }
        /// <summary>
        /// rank_num
        /// </summary>
        /// <returns></returns>
        [Column("RANK_NUM")]
        public int? rank_num { get; set; }
        /// <summary>
        /// cate_kind
        /// </summary>
        /// <returns></returns>
        [Column("CATE_KIND")]
        public string cate_kind { get; set; }
        /// <summary>
        /// sets_no
        /// </summary>
        /// <returns></returns>
        [Column("SETS_NO")]
        public string sets_no { get; set; }
        /// <summary>
        /// is_run_state
        /// </summary>
        /// <returns></returns>
        [Column("IS_RUN_STATE")]
        public string is_run_state { get; set; }
        /// <summary>
        /// is_prod
        /// </summary>
        /// <returns></returns>
        [Column("IS_PROD")]
        public string is_prod { get; set; }
        /// <summary>
        /// is_run_param
        /// </summary>
        /// <returns></returns>
        [Column("IS_RUN_PARAM")]
        public string is_run_param { get; set; }
        /// <summary>
        /// is_alarm
        /// </summary>
        /// <returns></returns>
        [Column("IS_ALARM")]
        public string is_alarm { get; set; }
        /// <summary>
        /// is_program
        /// </summary>
        /// <returns></returns>
        [Column("IS_PROGRAM")]
        public string is_program { get; set; }
        /// <summary>
        /// is_barcode
        /// </summary>
        /// <returns></returns>
        [Column("IS_BARCODE")]
        public string is_barcode { get; set; }
        /// <summary>
        /// mis_visual
        /// </summary>
        /// <returns></returns>
        [Column("MIS_VISUAL")]
        public string mis_visual { get; set; }
        /// <summary>
        /// station_cnt
        /// </summary>
        /// <returns></returns>
        [Column("STATION_CNT")]
        public string station_cnt { get; set; }
        /// <summary>
        /// rank_sets
        /// </summary>
        /// <returns></returns>
        [Column("RANK_SETS")]
        public string rank_sets { get; set; }
        /// <summary>
        /// is_main
        /// </summary>
        /// <returns></returns>
        [Column("IS_MAIN")]
        public string is_main { get; set; }
        /// <summary>
        /// enabled
        /// </summary>
        /// <returns></returns>
        [Column("enabled")]
        public int enabled { get; set; }
        /// <summary>
        /// memo
        /// </summary>
        /// <returns></returns>
        [Column("memo")]
        public string memo { get; set; }
        /// <summary>
        /// machine_ip
        /// </summary>
        /// <returns></returns>
        [Column("machine_ip")]
        public string machine_ip { get; set; }
        /// <summary>
        /// machine_port
        /// </summary>
        /// <returns></returns>
        [Column("machine_port")]
        public int machine_port { get; set; }
        /// <summary>
        /// listen_port
        /// </summary>
        /// <returns></returns>
        [Column("listen_port")]
        public int listen_port { get; set; }
        /// <summary>
        /// hardware_id
        /// </summary>
        /// <returns></returns>
        [Column("hardware_id")]
        public string hardware_id { get; set; }
        #endregion

    }
}
