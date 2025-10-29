using QLNS.Models;
using QLNS.Models.ViewModel;
using System;
using System.Data.Entity.Validation;
using System.Linq;
using System.Text;
using System.Web.Mvc;

public class TasksController : Controller
{
    private readonly ql_nhanvienEntities db = new ql_nhanvienEntities();

    // Kanban
    public ActionResult Index()
    {
        ViewBag.Menu = "task";
        ViewBag.Title = "Công việc";

        var tasks = db.congviecs.Select(x => new TaskItemVM
        {
            macv = x.macv,
            tieude = x.tieude,
            trangthai = x.trangthai,
            mucdo_uutien = x.mucdo_uutien,
            tiendo = x.tiendo,                 // nếu nullable thì (byte)(x.tiendo ?? 0)
            thoigian_kt = x.thoigian_kt
        }).ToList();

        return View(tasks);
    }

    // ============ GIAO VIỆC DÀI HẠN ============
    [HttpPost]
    [ValidateAntiForgeryToken]
    public ActionResult CreateTask(
        string tieude,
        string mota,
        DateTime? bd,
        DateTime? kt,
        string mucdouutien,
        int[] nhanviens,
        int malv = 0 // 0 => tự sinh lịch
    )
    {
        try
        {
            if (string.IsNullOrWhiteSpace(tieude))
                return Json(new { ok = false, msg = "Thiếu tiêu đề." });

            var start = bd ?? DateTime.Now;
            var end = kt ?? start.AddDays(1);
            if (start > end)
                return Json(new { ok = false, msg = "Thời gian bắt đầu phải nhỏ hơn thời gian kết thúc." });

            // 1) Tạo lịch nếu chưa có
            int malvToUse = malv;
            if (malvToUse <= 0)
            {
                int? firstEmp = (nhanviens != null && nhanviens.Length > 0) ? (int?)nhanviens[0] : null;

                var lv = new lichlamviec
                {
                    manv = firstEmp,          // null = lịch chung
                    mada = null,
                    tieude = $"[Task] {tieude}".Trim(),
                    loai = "Dự án",           // bắt buộc thuộc tập {Cá nhân, Dự án, Phòng ban, Công ty}
                    ngaybatdau = start.Date,
                    ngayketthuc = end.Date,
                    ghichu = "Auto from CreateTask"
                };
                db.lichlamviecs.Add(lv);
                db.SaveChanges();
                malvToUse = lv.malv;
            }

            // 2) Tạo công việc
            var cv = new congviec
            {
                malv = malvToUse,
                tieude = tieude.Trim(),
                mota = mota,
                thoigian_bd = start,
                thoigian_kt = end,
                mucdo_uutien = string.IsNullOrWhiteSpace(mucdouutien) ? "Trung bình" : mucdouutien,
                trangthai = "Chưa làm",
                tiendo = 0,
                nguoigiao = Session["manv"] as int?,
                // >>> QUAN TRỌNG: set loaicv để thỏa NOT NULL
                loaicv = "DaiHan"
            };
            db.congviecs.Add(cv);
            db.SaveChanges();

            // 3) Phân công
            if (nhanviens != null && nhanviens.Length > 0)
            {
                foreach (var id in nhanviens.Distinct())
                    db.PhanCongs.Add(new PhanCong { macv = cv.macv, manv = id, ngaygiao = DateTime.Today });

                db.SaveChanges();
            }

            return Json(new { ok = true, msg = "Giao việc dài hạn thành công", macv = cv.macv });
        }
        catch (DbEntityValidationException ex)
        {
            return Json(new { ok = false, msg = "Lỗi dữ liệu công việc: " + CollectEfErrors(ex) });
        }
        catch (Exception ex)
        {
            return Json(new { ok = false, msg = "Lỗi: " + ex.Message });
        }
    }

    // ============ GIAO VIỆC CHIA CA (PART-TIME) ============
    [HttpPost]
    [ValidateAntiForgeryToken]
    public ActionResult CreateShiftTask(
        string tieude,
        string mota,
        DateTime fromDate, DateTime toDate,
        TimeSpan gioBd, TimeSpan gioKt,
        int[] daysOfWeek, // 0=CN..6=Th7
        int[] nhanviens
    )
    {
        try
        {
            if (fromDate.Date > toDate.Date)
                return Json(new { ok = false, msg = "Khoảng ngày không hợp lệ." });
            if (gioBd >= gioKt)
                return Json(new { ok = false, msg = "Giờ bắt đầu phải < giờ kết thúc." });
            if (nhanviens == null || nhanviens.Length == 0)
                return Json(new { ok = false, msg = "Chọn ít nhất 1 nhân viên." });

            // 1) Lịch master
            var lv = new lichlamviec
            {
                manv = null,
                mada = null,
                tieude = "[Ca] " + (string.IsNullOrWhiteSpace(tieude) ? "Ca làm việc" : tieude.Trim()),
                loai = "Dự án",
                ngaybatdau = fromDate.Date,
                ngayketthuc = toDate.Date,
                ghichu = "Auto from CreateShiftTask"
            };
            db.lichlamviecs.Add(lv);
            db.SaveChanges();

            // 2) Task cha
            var parent = new congviec
            {
                malv = lv.malv,
                tieude = string.IsNullOrWhiteSpace(tieude) ? "Ca làm việc" : tieude.Trim(),
                mota = mota,
                thoigian_bd = fromDate.Date + gioBd,
                thoigian_kt = toDate.Date + gioKt,
                mucdo_uutien = "Trung bình",
                trangthai = "Chưa làm",
                tiendo = 0,
                nguoigiao = Session["manv"] as int?,
                // >>> QUAN TRỌNG: set loaicv để thỏa NOT NULL
                loaicv = "Ca"
            };
            db.congviecs.Add(parent);
            db.SaveChanges();

            // 3) Rải ca vào bảng lichca
            int count = 0;
            var dowSet = (daysOfWeek ?? new int[0]).ToHashSet();
            for (var d = fromDate.Date; d <= toDate.Date; d = d.AddDays(1))
            {
                if (dowSet.Count > 0 && !dowSet.Contains((int)d.DayOfWeek)) continue;

                foreach (var manv in nhanviens.Distinct())
                {
                    bool exist = db.lichcas.Any(x => x.manv == manv && x.ngay == d && x.giobd == gioBd && x.giokt == gioKt);
                    if (exist) continue;

                    db.lichcas.Add(new lichca
                    {
                        manv = manv,
                        ngay = d,
                        giobd = gioBd,
                        giokt = gioKt,
                        macv = parent.macv
                    });
                    count++;
                }
            }
            db.SaveChanges();

            // 4) Phân công vào task cha
            foreach (var id in nhanviens.Distinct())
                db.PhanCongs.Add(new PhanCong { macv = parent.macv, manv = id, ngaygiao = DateTime.Today });
            db.SaveChanges();

            return Json(new { ok = true, msg = $"Đã tạo {count} ca làm.", macv = parent.macv });
        }
        catch (DbEntityValidationException ex)
        {
            return Json(new { ok = false, msg = "Lỗi dữ liệu công việc: " + CollectEfErrors(ex) });
        }
        catch (Exception ex)
        {
            return Json(new { ok = false, msg = "Lỗi: " + ex.Message });
        }
    }

    // Cập nhật trạng thái / tiến độ
    [HttpPost]
    public ActionResult UpdateStatus(int macv, string trangthai, byte? tiendo)
    {
        var cv = db.congviecs.Find(macv);
        if (cv == null) return HttpNotFound();

        if (!string.IsNullOrEmpty(trangthai)) cv.trangthai = trangthai;
        if (tiendo.HasValue) cv.tiendo = tiendo.Value;

        db.SaveChanges();
        return Json(new { ok = true });
    }

    // Dropdown
    public ActionResult EmployeeOptions()
    {
        var list = db.nhanviens
                     .Where(n => n.trangthai == "Đang làm")
                     .Select(n => new { n.manv, n.hoten })
                     .ToList();
        return Json(list, JsonRequestBehavior.AllowGet);
    }

    public ActionResult LichOptions()
    {
        var list = db.lichlamviecs
                     .Select(l => new { l.malv, l.tieude })
                     .ToList();
        return Json(list, JsonRequestBehavior.AllowGet);
    }

    // gom message validate EF cho dễ đọc
    private static string CollectEfErrors(DbEntityValidationException ex)
    {
        var sb = new StringBuilder();
        foreach (var eve in ex.EntityValidationErrors)
            foreach (var ve in eve.ValidationErrors)
                sb.Append($"{ve.PropertyName}: {ve.ErrorMessage}; ");
        return sb.ToString();
    }
}
