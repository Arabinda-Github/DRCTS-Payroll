using Microsoft.AspNetCore.Mvc;

namespace HR_Payroll.Web.Controllers
{
    public class DashboardController : Controller
    {
        public IActionResult AdminDashboard()
        {
            return View();
        }
    }
}
