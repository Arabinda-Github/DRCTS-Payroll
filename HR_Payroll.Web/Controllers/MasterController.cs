using Microsoft.AspNetCore.Mvc;

namespace HR_Payroll.Web.Controllers
{
    public class MasterController : Controller
    {
        public IActionResult AssignDepartmentMember()
        {
            return View();
        }
        public IActionResult AssignManager()
        {
            return View();
        }
        public IActionResult AssignTeamLeader()
        {
            return View();
        }
        public IActionResult AssignEmployee()
        {
            return View();
        }
    }
}
