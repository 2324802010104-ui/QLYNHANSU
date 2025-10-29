using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace QLNS.Models.ViewModel
{
    public class TaskItemVM
    {
        public int macv { get; set; }
        public string tieude { get; set; }
        public string trangthai { get; set; }
        public string mucdo_uutien { get; set; }
        public byte tiendo { get; set; }          // đổi theo schema của bạn
        public System.DateTime? thoigian_kt { get; set; }
    }
}