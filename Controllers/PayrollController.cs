using QLNS.Models;
using System.Linq;
using System.Web.Mvc;

public class PayrollController : Controller
{
    private ql_nhanvienEntities db = new ql_nhanvienEntities();

    public ActionResult Index(int? thang, int? nam)
    {
        ViewBag.Menu = "pay"; ViewBag.Title = "Lương";
        int y = nam ?? System.DateTime.Today.Year;
        int m = thang ?? System.DateTime.Today.Month;
        var q = from p in db.chitietluongs
                join n in db.nhanviens on p.manv equals n.manv
                where p.thang == m && p.nam == y
                orderby n.hoten
                select new PayVM
                {
                    Manv = n.manv,
                    Hoten = n.hoten,
                    Thang = p.thang,
                    Nam = p.nam,
                    LuongCoBan = p.luongcoban,
                    PhuCap = p.phucap ?? 0,
                    Thuong = p.thuong ?? 0,
                    Phat = p.phat ?? 0,
                    Tong = p.tongluong ?? 0
                };
        ViewBag.Thang = m; ViewBag.Nam = y;
        return View(q.ToList());
    }
}
public class PayVM
{
    public int Manv; public string Hoten; public int Thang; public int Nam;
    public decimal LuongCoBan, PhuCap, Thuong, Phat, Tong;
}
