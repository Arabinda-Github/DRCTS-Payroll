using Microsoft.AspNetCore.Mvc;

namespace HR_Payroll.Web.Controllers
{
    public class MasterController : Controller
    {
        public IActionResult AssignDepartmentMember()
        {
            return View();
        }
       
    }
}
