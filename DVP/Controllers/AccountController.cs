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
using System.Security.Cryptography;


namespace DVP.Controllers
{
    public class AccountController : Controller
    {
        DataAccess.DVPEntities _dvpEntities = new DataAccess.DVPEntities();

        // GET: Account
        public ActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public JsonResult LogIn(string _userIdProgreso, string _contraseñaHash)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_userIdProgreso) || string.IsNullOrWhiteSpace(_contraseñaHash))
                {
                    return Json(new { success = false, message = "Usuario y contraseña son requeridos." });
                }

                var usuario = _dvpEntities.Usuario
                    .FirstOrDefault(u => u.UserIdProgreso == _userIdProgreso);

                if (usuario == null)
                {
                    return Json(new { success = false, message = "Usuario o contraseña incorrectos." });
                }

                string hashIngresado = HashearContraseña(_contraseñaHash, usuario.Salt);

                if (hashIngresado != usuario.ContraseñaHash)
                {
                    return Json(new { success = false, message = "Usuario o contraseña incorrectos." });
                }

                string tokenJwt = GenerarJwtToken(usuario.UsuarioID, usuario.UserIdProgreso, usuario.Nombre);
                usuario.Token = tokenJwt;
                usuario.UltimoLogin = DateTime.Now;
                _dvpEntities.SaveChanges();

                var rol = _dvpEntities.UsuarioRol
                              .Where(r => r.UsuarioID == usuario.UsuarioID)
                              .Select(r => r.Rol.Descripcion) 
                              .FirstOrDefault();

                Session["rol"] = rol;
                Session["NombreUsuario"] = usuario.Nombre;
                Session["token"] = tokenJwt;

                return Json(new
                {
                    success = true,
                    token = tokenJwt,
                    nombre = usuario.Nombre
                });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "Error interno al iniciar sesión." });
            }
        }



        private string HashearContraseña(string contraseña, string salt)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                string combinado = contraseña + salt;
                byte[] bytes = Encoding.UTF8.GetBytes(combinado);
                byte[] hashBytes = sha256.ComputeHash(bytes);

                StringBuilder sb = new StringBuilder();
                foreach (byte b in hashBytes)
                    sb.Append(b.ToString("x2"));

                return sb.ToString();
            }
        }




        private string GenerarJwtToken(int usuarioId, string email, string nombre)
        {
            var claveSecreta = ConfigurationManager.AppSettings["JwtSecretKey"];
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(claveSecreta));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, usuarioId.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, email),
                new Claim(JwtRegisteredClaimNames.Name, nombre),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(
                issuer: "tuapp.com",
                audience: "tuapp.com",
                claims: claims,
                expires: DateTime.UtcNow.AddHours(2),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }


        public ActionResult LogOut()
        {
            var token = Session["token"]?.ToString();
            if (!string.IsNullOrEmpty(token))
            {
                var usuario = _dvpEntities.Usuario.FirstOrDefault(u => u.Token == token);
                if (usuario != null)
                {
                    usuario.Token = null;
                    _dvpEntities.SaveChanges();
                }
            }

            Session.Clear();
            return RedirectToAction("Index", "Account");
        }


    }
}