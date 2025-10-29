using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace QLNS.Models
{
    public class EmployeeRegisterVM
    {
        public string Hoten { get; set; }
        public System.DateTime? NgaySinh { get; set; }
        public string GioiTinh { get; set; }   // "Nam","Nữ","Khác"
        public string Sdt { get; set; }
        public string Diachi { get; set; }
        public string Email { get; set; }      // sẽ map vào nhanvien.email
        public int MaChucVu { get; set; }      // FK -> chucvu.machucvu

        // Tài khoản đăng nhập
        public string Username { get; set; }
        public string Password { get; set; }
    }
}
