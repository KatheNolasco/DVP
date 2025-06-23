using DVP.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace DVP.Controllers
{
    public class InventoryController : Controller
    {
        // GET: Inventory
        public ActionResult Index()
        {
            InventoryViewModelcs viewModel = new InventoryViewModelcs
            {
                Equipos = new InventoryViewModelcs().GetEquipos().ToList()
            };

            return View(viewModel);
        }
    }
}