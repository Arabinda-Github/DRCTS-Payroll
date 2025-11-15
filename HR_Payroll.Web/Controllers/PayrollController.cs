using Microsoft.AspNetCore.Mvc;

namespace HR_Payroll.Web.Controllers
{
    public class PayrollController : Controller
    {
        public IActionResult ApplyLeave()
        {
            return View();
        }
    }
}
