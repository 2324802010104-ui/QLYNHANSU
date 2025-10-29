using System;
using System.Linq;
using System.Web.Mvc;
using QLNS.Models;
using QLNS.Helpers; // nếu bạn dùng PasswordHasher

public class EmployeesController : Controller
{
    private readonly ql_nhanvienEntities db = new ql_nhanvienEntities();

    // tiện: check login + quyền
    private bool IsLoggedIn() => Session["username"] != null;
    private bool IsManagerOrAdmin()
    {
        var role = Session["role"] as string;           // "Admin" | "Quản lý" | "Nhân viên"
        return role == "Admin" || role == "Quản lý";
    }

    public ActionResult Index(string q = null)
    {
        if (!IsLoggedIn()) return RedirectToAction("Login", "Auth");

        ViewBag.Menu = "emp";
        ViewBag.Title = "Nhân sự";

        var list = db.nhanviens.AsQueryable();
        if (!string.IsNullOrEmpty(q))
            list = list.Where(x => x.hoten.Contains(q) || x.sdt.Contains(q) || x.email.Contains(q));

        return View(list.OrderBy(x => x.hoten).ToList());
    }

    public ActionResult Details(int id)
    {
        if (!IsLoggedIn()) return RedirectToAction("Login", "Auth");

        var nv = db.nhanviens.Find(id);
        if (nv == null) return HttpNotFound();

        ViewBag.Menu = "emp";
        ViewBag.Title = "Hồ sơ nhân viên";
        return View(nv);
    }

    // ========== ĐĂNG KÝ NHÂN VIÊN ==========

    [HttpGet]
    public ActionResult Create()
    {
        if (!IsLoggedIn()) return RedirectToAction("Login", "Auth");
        if (!IsManagerOrAdmin()) return RedirectToAction("Index", "Dashboard");

        ViewBag.Menu = "empCreate";
        ViewBag.Title = "Đăng ký nhân viên";
        ViewBag.MaChucVu = new SelectList(db.chucvus.ToList(), "machucvu", "tenchucvu");

        return View(new EmployeeRegisterVM());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public ActionResult Create(EmployeeRegisterVM m)
    {
        if (!IsLoggedIn()) return RedirectToAction("Login", "Auth");
        if (!IsManagerOrAdmin()) return RedirectToAction("Index", "Dashboard");

        ViewBag.Menu = "empCreate";
        ViewBag.Title = "Đăng ký nhân viên";

        if (!ModelState.IsValid)
        {
            ViewBag.MaChucVu = new SelectList(db.chucvus.ToList(), "machucvu", "tenchucvu", m.MaChucVu);
            return View(m);
        }

        // Check trùng
        if (db.dangnhaps.Any(x => x.tendangnhap == m.Username))
            ModelState.AddModelError("Username", "Tên đăng nhập đã tồn tại");
        if (db.nhanviens.Any(x => x.email == m.Email))
            ModelState.AddModelError("Email", "Email đã tồn tại");

        if (!ModelState.IsValid)
        {
            ViewBag.MaChucVu = new SelectList(db.chucvus.ToList(), "machucvu", "tenchucvu", m.MaChucVu);
            return View(m);
        }

        // 1) Tạo tài khoản đăng nhập
        var dn = new dangnhap
        {
            tendangnhap = m.Username,
            matkhauHash = PasswordHasher.Sha256(m.Password), // hoặc m.Password khi test
            email = m.Email,
            tennguoidung = m.Hoten,
            sdt = m.Sdt,
            diachi = m.Diachi,
            mavt = 3 // 1=Admin, 2=Quản lý, 3=Nhân viên
        };
        db.dangnhaps.Add(dn);
        db.SaveChanges();

        // 2) Tạo hồ sơ nhân viên
        var nv = new nhanvien
        {
            madangnhap = dn.madangnhap,
            hoten = m.Hoten,
            ngaysinh = m.NgaySinh,
            gioitinh = m.GioiTinh,
            sdt = m.Sdt,
            email = m.Email,
            diachi = m.Diachi,
            tenchucvu = m.MaChucVu,
            trangthai = "Đang làm"
        };
        db.nhanviens.Add(nv);
        db.SaveChanges();

        TempData["msg"] = "Tạo nhân viên thành công";
        return RedirectToAction("Index");
    }
}
