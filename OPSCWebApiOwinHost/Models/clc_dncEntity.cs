using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JNRSWebApiOwinHost.Models
{
    public class clc_dncEntity
    {
        /// <summary>
        /// machine_id
        /// </summary>
        /// <returns></returns>
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("machine_id")]
        public int machine_id { get; set; }
        /// <summary>
        /// program_name
        /// </summary>
        /// <returns></returns>
        [Column("program_name")]
        public string program_name { get; set; }
        /// <summary>
        /// upload_time
        /// </summary>
        /// <returns></returns>
        [Column("upload_time")]
        public DateTime upload_time { get; set; }
        /// <summary>
        /// upload_user
        /// </summary>
        /// <returns></returns>
        [Column("upload_user")]
        public string upload_user { get; set; }
        /// <summary>
        /// upload_address
        /// </summary>
        /// <returns></returns>
        [Column("upload_address")]
        public string upload_address { get; set; }
        /// <summary>
        /// upload
        /// </summary>
        /// <returns></returns>
        [Column("upload")]
        public int upload { get; set; }
        /// <summary>
        /// delete
        /// </summary>
        /// <returns></returns>
        [Column("delete")]
        public int delete { get; set; }

    }
}
