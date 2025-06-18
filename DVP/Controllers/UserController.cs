using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using DataAccess;
using DVP.Models;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Web.WebPages;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Text;
using System.Web.UI.WebControls.WebParts;
using System.Data.Entity;

namespace DVP.Controllers
{
    public class UserController : Controller
    {
        public ActionResult Index()
        {
            UserViewModel viewModel = new UserViewModel
            {
                Usuarios = new UserViewModel().GetUsuarios().ToList()
            };

            return View(viewModel);

        }
    }
}