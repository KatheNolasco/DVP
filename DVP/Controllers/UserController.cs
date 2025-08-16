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
using System.Security.Cryptography;
using System.Data.Entity.Validation;




namespace DVP.Controllers
{
    public class UserController : Controller
    {

        DataAccess.DVPEntities _dvpEntities = new DataAccess.DVPEntities();

        public ActionResult Index()
        {
            var tokenEnSession = Session["token"]?.ToString();

            if (string.IsNullOrEmpty(tokenEnSession))
            {
                return RedirectToAction("Index", "Account");
            }

            // Validar que el token exista en la base de datos
            var usuario = _dvpEntities.Usuario.FirstOrDefault(u => u.Token == tokenEnSession);
            if (usuario == null)
            {
                return RedirectToAction("Index", "Account");
            }

            var rol = _dvpEntities.UsuarioRol
                                  .Where(r => r.UsuarioID == usuario.UsuarioID)
                                  .Select(r => r.Rol.Descripcion)
                                  .FirstOrDefault();

            var query = _dvpEntities.Usuario.AsQueryable();

            if (rol != "Desarrollador de Software" && rol != "Administrador de la información")
            {
                return RedirectToAction("Index", "Account");
            }

            return View();
        }

        public ActionResult ConfigurationUser()
        {

            var tokenEnSession = Session["token"]?.ToString();

            if (string.IsNullOrEmpty(tokenEnSession))
            {
                return RedirectToAction("Index", "Account");
            }

            // Validar que el token exista en la base de datos
            var usuario = _dvpEntities.Usuario.FirstOrDefault(u => u.Token == tokenEnSession);
            if (usuario == null)
            {
                return RedirectToAction("Index", "Account");
            }

            return View();
        }

        [HttpGet]
        public JsonResult GetuserbyId(int userId)
        {
            var user = _dvpEntities.Usuario
                .Where(u => u.UsuarioID == userId)
                .Select(u => new
                {
                    _usuarioId = u.UsuarioID,
                    _descripcion = u.Descripcion,
                    _email = u.Email,
                    _nombre = u.Nombre,
                    _aDUsername = u.ADUsername,
                    _numeroEmpleado = u.NumeroEmpleado,
                    _plantaId = u.PlantaID,
                    _plantaDescripcion = u.Planta.Descripcion,
                    _unidadOperativaId = u.UnidadOperativaID,
                    _paisId = u.PaisID,
                    _gerenciaId = u.GerenciaID,
                    _active = u.Active,
                    _userIdProgreso = u.UserIdProgreso,
                    _rolId = u.UsuarioRol.Select(r => r.RolID).FirstOrDefault()
                })
                .FirstOrDefault();

            if (user == null)
            {
                return Json(new { success = false, message = "No encontrado" }, JsonRequestBehavior.AllowGet);
            }

            return Json(new { success = true, data = user }, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult GetRol()
        {

            var roles = _dvpEntities.Rol
                                     .Select(s => new
                                     {
                                         RolID = s.RolID,
                                         Descripcion = s.Descripcion
                                     })
                                     .ToList();

            return Json(roles, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult GetGerencia()
        {

            var gerencias = _dvpEntities.Gerencia
                                     .Select(s => new
                                     {
                                         GerenciaID = s.GerenciaID,
                                         Descripcion = s.Descripcion
                                     })
                                     .ToList();

            return Json(gerencias, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult Create(DVP.Models.UserViewModel model)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    bool yaExiste = _dvpEntities.Usuario.Any(u =>
                        u.UserIdProgreso == model._userIdProgreso ||
                        u.Email == model._email ||
                        u.ADUsername == model._aDUsername ||
                        u.Nombre == model._nombre ||
                        u.NumeroEmpleado == model._numeroEmpleado);

                    if (yaExiste)
                    {
                        return Json(new
                        {
                            success = false,
                            message = "Ya existe un usuario que tiene alguna de las siguientes informaciones ya guardadas: IDSAP, Email, Nombre, Número Empleado o ADUsername. Favor revisar."
                        });
                    }

                    string salt = GenerarSalt();
                    string hashConSalt = HashearContraseña(model._contraseñaHash, salt);

                    var nuevoUsuario = new Usuario
                    {
                        Descripcion = model._descripcion,
                        Email = model._email,
                        Nombre = model._nombre,
                        ContraseñaHash = hashConSalt,
                        Salt = salt,
                        ADUsername = model._aDUsername,
                        EsAD = model._esAD,
                        NumeroEmpleado = model._numeroEmpleado,
                        PlantaID = model._plantaId,
                        UnidadOperativaID = model._unidadOperativaId,
                        PaisID = model._paisId,
                        GerenciaID = model._gerenciaId,
                        Active = model._active,
                        UserIdProgreso = model._userIdProgreso,
                    };

                    _dvpEntities.Usuario.Add(nuevoUsuario);
                    _dvpEntities.SaveChanges();

                    var rolAsignado = new UsuarioRol
                    {
                        UsuarioID = nuevoUsuario.UsuarioID,
                        RolID = model._rolId.Value,
                    };

                    _dvpEntities.UsuarioRol.Add(rolAsignado);
                    _dvpEntities.SaveChanges();

                    return Json(new { success = true, message = "Creado exitosamente." });
                }

                return Json(new { success = false, message = "Datos inválidos." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error al crear el usuario: " + ex.Message });
            }
        }

        [HttpPost]
        public JsonResult Edit(DVP.Models.UserViewModel data)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var user = _dvpEntities.Usuario.FirstOrDefault(e => e.UsuarioID == data._usuarioId);

                    if (user == null)
                        return Json(new { success = false, message = "No encontrado." });

                    user.Descripcion = data._descripcion;
                    user.Email = data._email;
                    user.Nombre = data._nombre;
                    user.ADUsername = data._aDUsername;
                    user.EsAD = data._esAD;
                    user.NumeroEmpleado = data._numeroEmpleado;
                    user.PlantaID = data._plantaId;
                    user.UnidadOperativaID = data._unidadOperativaId;
                    user.PaisID = data._paisId;
                    user.GerenciaID = data._gerenciaId;
                    user.Active = data._active;
                    user.UserIdProgreso = data._userIdProgreso;
                    _dvpEntities.SaveChanges();

                    var roluser = _dvpEntities.UsuarioRol.FirstOrDefault(e => e.UsuarioID == data._usuarioId);
                    roluser.RolID = data._rolId.Value;
                    _dvpEntities.SaveChanges();

                    return Json(new { success = true, message = "Actualizado exitosamente." });
                }

                return Json(new { success = false, message = "Datos inválidos para editar." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error al editar el equipo: " + ex.Message });
            }
        }

        private string HashearContraseña(string contraseña, string salt)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                // Combina contraseña + salt
                string combinado = contraseña + salt;
                byte[] bytes = Encoding.UTF8.GetBytes(combinado);
                byte[] hashBytes = sha256.ComputeHash(bytes);

                StringBuilder sb = new StringBuilder();
                foreach (byte b in hashBytes)
                    sb.Append(b.ToString("x2"));

                return sb.ToString();
            }
        }

        private string GenerarSalt(int longitud = 32)
        {
            using (var rng = new RNGCryptoServiceProvider())
            {
                byte[] saltBytes = new byte[longitud];
                rng.GetBytes(saltBytes);
                return Convert.ToBase64String(saltBytes);
            }
        }

        [HttpGet]
        public JsonResult GetUsuarios()
        {
            var tokenEnSession = Session["token"]?.ToString();

            if (string.IsNullOrEmpty(tokenEnSession))
            {
                return Json(new { success = false, message = "No tienes acceso" }, JsonRequestBehavior.AllowGet);
            }

            var usuario = _dvpEntities.Usuario.FirstOrDefault(u => u.Token == tokenEnSession);
            if (usuario == null)
            {
                return Json(new { success = false, message = "Usuario no válido." }, JsonRequestBehavior.AllowGet);
            }

            var rol = _dvpEntities.UsuarioRol
                                  .Where(r => r.UsuarioID == usuario.UsuarioID)
                                  .Select(r => r.Rol.Descripcion)
                                  .FirstOrDefault();

            var paisId = usuario.PaisID ?? 0;
            var plantaId = usuario.PlantaID ?? 0;

            var query = _dvpEntities.Usuario.AsQueryable();

            if (rol != "Desarrollador de Software")
            {
                query = query.Where(u => u.PaisID == paisId && u.PlantaID == plantaId);
            }

            var usuarios = query
                .Select(u => new
                {
                    _usuarioId = u.UsuarioID,
                    _descripcion = u.Descripcion,
                    _email = u.Email,
                    _nombre = u.Nombre,
                    _aDUsername = u.ADUsername,
                    _numeroEmpleado = u.NumeroEmpleado,
                    _plantaId = u.PlantaID,
                    _plantaDescripcion = u.Planta.Descripcion,
                    _unidadOperativaId = u.UnidadOperativaID,
                    _paisId = u.PaisID,
                    _gerenciaId = u.GerenciaID,
                    _active = u.Active,
                    _userIdProgreso = u.UserIdProgreso
                })
                .ToList();

            if (usuarios == null || usuarios.Count == 0)
            {
                return Json(new { success = false, message = "No se encontraron usuarios." }, JsonRequestBehavior.AllowGet);
            }

            return Json(new { success = true, data = usuarios }, JsonRequestBehavior.AllowGet);
        }


        [HttpPost]
        public JsonResult GuardarElementoConfiguracion(DVP.Models.UserViewModel model)
        {
            try
            {
                // Validación mínima de entrada
                var desc = (model._descripcion ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(desc))
                    return Json(new { success = false, message = "La descripción es obligatoria." });

                switch (model._tipo)
                {
                    case "Planta":
                        var planta = new Planta
                        {
                            Descripcion = desc,
                            CodigoSAPProgreso = model._codigoSAPProgreso, 
                            CodigoSAP = "N/A",
                            Active = true
                        };
                        _dvpEntities.Planta.Add(planta);
                        break;

                    case "UnidadOperativa":
                        var unidad = new UnidadOperativa
                        {
                            Descripcion = desc,
                            Active = true
                        };
                        _dvpEntities.UnidadOperativa.Add(unidad);
                        break;

                    case "Pais":
                        var pais = new Pais
                        {
                            Descripcion = desc,
                            CodigoSAPNuevo = model._codigoSAPProgreso,
                            CodigoSAP = "N/A",
                            Active = true
                        };
                        _dvpEntities.Pais.Add(pais);
                        break;

                    case "Gerencia":
                        var gerencia = new Gerencia
                        {
                            Descripcion = desc,
                            Active = true
                        };
                        _dvpEntities.Gerencia.Add(gerencia);
                        break;

                    default:
                        return Json(new { success = false, message = "Tipo desconocido." });
                }

                // (opcional) inspeccionar validaciones antes de guardar
                var pre = _dvpEntities.GetValidationErrors();
                if (pre.Any())
                {
                    var detallesPre = string.Join("; ",
                        pre.SelectMany(v => v.ValidationErrors)
                           .Select(e => $"{e.PropertyName}: {e.ErrorMessage}")
                    );
                    return Json(new { success = false, message = "Validación falló: " + detallesPre });
                }

                _dvpEntities.SaveChanges();
                return Json(new { success = true, message = "Guardado correctamente." });
            }
            catch (DbEntityValidationException vex)
            {
                var detalles = string.Join("; ",
                    vex.EntityValidationErrors.SelectMany(x => x.ValidationErrors)
                       .Select(e => $"{e.PropertyName}: {e.ErrorMessage}")
                );
                var entidades = string.Join("; ",
                    vex.EntityValidationErrors.Select(x => $"{x.Entry.Entity.GetType().Name} ({x.Entry.State})")
                );
                return Json(new { success = false, message = "Validación falló: " + detalles, entities = entidades });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }


        [HttpGet]
        public JsonResult GetListadoConfiguracion(string _tipo)
        {
            try
            {
                List<object> listado = new List<object>();

                switch (_tipo)
                {
                    case "Planta":
                        listado = _dvpEntities.Planta
                            .Select(p => new { _descripcion = p.Descripcion, _codigoSAPProgreso = p.CodigoSAPProgreso, _active = p.Active })
                            .ToList<object>();
                        break;

                    case "UnidadOperativa":
                        listado = _dvpEntities.UnidadOperativa
                            .Select(u => new { _descripcion = u.Descripcion, _codigoSAPProgreso = "", _active = u.Active })
                            .ToList<object>();
                        break;

                    case "Pais":
                        listado = _dvpEntities.Pais
                            .Select(p => new { _descripcion = p.Descripcion, _codigoSAPProgreso = p.CodigoSAPNuevo, _active = p.Active })
                            .ToList<object>();
                        break;

                    case "Gerencia":
                        listado = _dvpEntities.Gerencia
                            .Select(g => new { _descripcion = g.Descripcion, _codigoSAPProgreso = "", _active = g.Active })
                            .ToList<object>();
                        break;

                    case "Rol":
                        listado = _dvpEntities.Rol
                            .Select(r => new { _descripcion = r.Descripcion, _codigoSAPProgreso = "", _active = r.Active })
                            .ToList<object>();
                        break;

                    default:
                        return Json(new { success = false, message = "Tipo no válido" }, JsonRequestBehavior.AllowGet);
                }

                return Json(new { success = true, items = listado }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }



        [HttpPost]
        public JsonResult ActualizarElementoConfiguracion(DVP.Models.UserViewModel model)
        {
            try
            {
                switch (model._tipo)
                {
                    case "Planta":
                        var planta = _dvpEntities.Planta.FirstOrDefault(p => p.Descripcion == model._descripcion);
                        if (planta == null)
                            return Json(new { success = false, message = "Planta no encontrada" });

                        planta.Descripcion = model._descripcion;
                        planta.CodigoSAPProgreso = model._codigoSAPProgreso;
                        planta.Active = model._active;
                        break;

                    case "UnidadOperativa":
                        var unidad = _dvpEntities.UnidadOperativa.FirstOrDefault(u => u.Descripcion == model._descripcion);
                        if (unidad == null)
                            return Json(new { success = false, message = "Unidad Operativa no encontrada" });

                        unidad.Descripcion = model._descripcion;
                        unidad.Active = model._active;
                        break;

                    case "Pais":
                        var pais = _dvpEntities.Pais.FirstOrDefault(p => p.Descripcion == model._descripcion);
                        if (pais == null)
                            return Json(new { success = false, message = "País no encontrado" });

                        pais.Descripcion = model._descripcion;
                        pais.Active = model._active;
                        break;

                    case "Gerencia":
                        var gerencia = _dvpEntities.Gerencia.FirstOrDefault(g => g.Descripcion == model._descripcion);
                        if (gerencia == null)
                            return Json(new { success = false, message = "Gerencia no encontrada" });

                        gerencia.Descripcion = model._descripcion;
                        gerencia.Active = model._active;
                        break;

                    default:
                        return Json(new { success = false, message = "Tipo inválido" });
                }

                _dvpEntities.SaveChanges();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }


        private bool ExistsAsignacion(int usuarioId, int plantaId)
        {
            return _dvpEntities.PlantaAsignada
                .Any(p => p.UsuarioID == usuarioId && p.PlantaID == plantaId);
        }

        [HttpGet]
        public JsonResult GetPlantasAsignadas(int userId)
        {
            try
            {
                if (userId <= 0)
                    return Json(new object[0], JsonRequestBehavior.AllowGet);

                var plantas = _dvpEntities.PlantaAsignada
                    .Where(pa => pa.UsuarioID == userId)
                    .Select(pa => new { pa.PlantaID, pa.Planta.Descripcion })
                    .ToList();

                return Json(plantas, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                Response.StatusCode = 500;
                return Json(new { error = "Error al obtener plantas asignadas", detail = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }


        [HttpPost]
        public JsonResult AsignarPlanta(DVP.Models.UserViewModel.PlantaAsignada req)
        {
            try
            {
                if (req == null || req._usuarioId <= 0)
                    return Json(new { success = false, message = "Datos inválidos." });

                var usuarioId = req._usuarioId;
                var seleccionadas = (req._plantaIds ?? new List<int>()).Distinct().ToList();

                // Obtener actuales en BD
                var actuales = _dvpEntities.PlantaAsignada
                    .Where(p => p.UsuarioID == usuarioId)
                    .Select(p => p.PlantaID)
                    .ToList();

                // Calcular diferencias
                var aAgregar = seleccionadas.Except(actuales).ToList();
                var aEliminar = actuales.Except(seleccionadas).ToList();

                // Agregar nuevas
                var ahora = DateTime.Now;
                foreach (var pid in aAgregar)
                {
                    _dvpEntities.PlantaAsignada.Add(new PlantaAsignada
                    {
                        UsuarioID = usuarioId.Value,
                        PlantaID = pid,
                        FechaAsignacion = ahora
                    });
                }

                // Eliminar las que ya no están marcadas
                if (aEliminar.Any())
                {
                    var rowsEliminar = _dvpEntities.PlantaAsignada
                        .Where(p => p.UsuarioID == usuarioId && aEliminar.Contains(p.PlantaID))
                        .ToList();

                    _dvpEntities.PlantaAsignada.RemoveRange(rowsEliminar);
                }

                _dvpEntities.SaveChanges();

                return Json(new
                {
                    success = true,
                    message = "Asignaciones sincronizadas.",
                    added = aAgregar,
                    removed = aEliminar
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error al asignar plantas: " + ex.Message });
            }
        }

    }



}