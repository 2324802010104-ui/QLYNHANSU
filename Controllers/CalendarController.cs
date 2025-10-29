using QLNS.Models;
using System.Linq;
using System.Web.Mvc;

public class CalendarController : Controller
{
    private ql_nhanvienEntities db = new ql_nhanvienEntities();

    public ActionResult Index()
    {
        ViewBag.Menu = "cal"; ViewBag.Title = "Lịch làm việc";
        return View();
    }

    public ActionResult Events(int? manv)
    {
        var q = db.lichlamviecs.AsQueryable();
        if (manv.HasValue) q = q.Where(x => x.manv == manv.Value);
        var list = q.Select(x => new {
            id = x.malv,
            title = x.tieude,
            start = x.ngaybatdau,
            end = x.ngayketthuc
        }).ToList();
        return Json(list, JsonRequestBehavior.AllowGet);
    }

    public ActionResult EmployeeOptions()
    {
        var list = db.nhanviens.Select(n => new { n.manv, n.hoten }).ToList();
        return Json(list, JsonRequestBehavior.AllowGet);
    }
}
