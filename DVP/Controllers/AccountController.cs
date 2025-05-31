using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Threading.Tasks;
using System.Data.Entity;
using DataAccess;
//using System.DirectoryServices.AccountManagement;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;


namespace DVP.Controllers
{
    public class AccountController : Controller
    {
        private readonly DVPEntities _dVPEntities = new DVPEntities();


        // GET: Account
        public ActionResult Index()
        {
            return View();
        }
    }
}