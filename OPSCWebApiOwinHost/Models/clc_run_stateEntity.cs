using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JNRSWebApiOwinHost.Models
{
    public class clc_run_stateEntity
    {
        #region  实体成员
        /// <summary>
        /// time_stamp
        /// </summary>
        /// <returns></returns>
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("TIME_STAMP")]
        public string time_stamp { get; set; }
        /// <summary>
        /// time_second
        /// </summary>
        /// <returns></returns>
        [Column("TIME_SECOND")]
        public string time_second { get; set; }
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
        public int? machine_id { get; set; }
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
        /// run_state
        /// </summary>
        /// <returns></returns>
        [Column("RUN_STATE")]
        public int? run_state { get; set; }
        /// <summary>
        /// read_time
        /// </summary>
        /// <returns></returns>
        [Column("READ_TIME")]
        public DateTime? read_time { get; set; }
        #endregion
    }
}
