using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JNRSWebApiOwinHost.Models
{
    public class clc_filesEntity
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
        /// download
        /// </summary>
        /// <returns></returns>
        [Column("download")]
        public int download { get; set; }
    }
}
