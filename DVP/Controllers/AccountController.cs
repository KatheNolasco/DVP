using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using DataAccess;
using System.Configuration;


namespace DVP.Controllers
{
    public class AccountController : Controller
    {

        private readonly DVPEntities _context = new DVPEntities();

        // GET: Account
        public ActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public JsonResult Login(string email, string contraseña)
        {
            var usuario = _context.Usuario.FirstOrDefault(u => u.Email == email && u.ContraseñaHash == contraseña);

            if (usuario == null)
            {
                return Json(new { success = false, message = "Credenciales inválidas" });
            }

            // Leer clave secreta desde Web.config
            var claveSecreta = ConfigurationManager.AppSettings["JwtSecretKey"];
            var clave = Encoding.ASCII.GetBytes(claveSecreta);

            var tokenHandler = new JwtSecurityTokenHandler();
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                new Claim(ClaimTypes.Name, usuario.Nombre),
                new Claim(ClaimTypes.Email, usuario.Email),
                new Claim("UsuarioID", usuario.UsuarioID.ToString())
            }),
                Expires = DateTime.UtcNow.AddHours(4),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(clave), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(token);

            // Guarda el token y actualiza el último login
            usuario.Token = tokenString;
            usuario.UltimoLogin = DateTime.Now;
            _context.SaveChanges();

            return Json(new { success = true, token = tokenString });
        }
    }
}