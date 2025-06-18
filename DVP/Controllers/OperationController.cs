using DVP.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace DVP.Controllers
{
    public class OperationController : Controller
    {
        // GET: Operation
        public ActionResult Index()
        {
            OperationViewModel viewModel = new OperationViewModel
            {
                Equipos = new OperationViewModel().GetEquipos().ToList()
            };

            return View(viewModel);
        }
    }
}