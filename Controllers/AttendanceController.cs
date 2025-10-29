using QLNS.Models;
using System;
using System.Linq;
using System.Web.Mvc;

public class AttendanceController : Controller
{
    private ql_nhanvienEntities db = new ql_nhanvienEntities();

    public ActionResult Index()
    {
        ViewBag.Menu = "att"; ViewBag.Title = "Chấm công (QR)";
        return View();
    }

    // api ghi công
    [HttpPost]
    public ActionResult Check(int empId, string type)
    {
        type = (type ?? "").ToUpper();
        if (type != "IN" && type != "OUT") return Json(new { ok = false, msg = "Type phải IN/OUT" });
        if (!db.nhanviens.Any(x => x.manv == empId)) return Json(new { ok = false, msg = "NV không tồn tại" });
        db.Attendances.Add(new Attendance { EmpId = empId, CheckType = type, CheckTime = DateTime.Now });
        db.SaveChanges();
        return Json(new { ok = true });
    }
}
