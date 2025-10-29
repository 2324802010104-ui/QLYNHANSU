using QLNS.Helpers;
using QLNS.Models;
using System.Linq;
using System.Web.Mvc;

public class AuthController : Controller
{
    private ql_nhanvienEntities db = new ql_nhanvienEntities();

    [HttpGet]
    public ActionResult Login() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public ActionResult Login(string username, string password)
    {
        string hash = PasswordHasher.Sha256(password);
        var u = db.dangnhaps.FirstOrDefault(x => x.tendangnhap == username && x.matkhauHash == hash);
        if (u == null) { ViewBag.Err = "Sai tài khoản hoặc mật khẩu"; return View(); }

        // map sang nhân viên (1-1)
        var nv = db.nhanviens.FirstOrDefault(x => x.madangnhap == u.madangnhap);
        Session["madangnhap"] = u.madangnhap;
        Session["username"] = u.tennguoidung;
        Session["role"] = u.vaitro.tenvaitro;
        Session["manv"] = nv?.manv;

        return RedirectToAction("Index", "Dashboard");
    }

    public ActionResult Logout()
    {
        Session.Clear();
        return RedirectToAction("Login");
    }
}
