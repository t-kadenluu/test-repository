// Placeholder controller files
using Microsoft.AspNetCore.Mvc;

namespace AdminPortal.Controllers;

public class DashboardController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}

public class UsersController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
    
    public IActionResult Details(int id)
    {
        return View();
    }
}

public class OrdersController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}

public class ReportsController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}
