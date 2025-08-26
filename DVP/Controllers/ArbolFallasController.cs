using DataAccess;
using DVP.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace DVP.Controllers
{
    public class ArbolFallasController : Controller
    {
        DataAccess.DVPEntities _dvpEntities = new DataAccess.DVPEntities();

        // GET: ArbolFallas
        public ActionResult Index()
        {
            var tokenEnSession = Session["token"]?.ToString();

            if (string.IsNullOrEmpty(tokenEnSession))
            {
                return RedirectToAction("Index", "Account");
            }

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

        [HttpPost]
        public JsonResult CreateSubEquipo(ArbolFallasViewModel.SubEquipo data)
        {
            try
            {
                if (!ModelState.IsValid)
                    return Json(new { success = false, message = "Datos inválidos." });

                if (data._equipoId == null)
                    return Json(new { success = false, message = "Equipo es obligatorio." });
                var nuevo = new SubEquipo
                {
                    Descripcion = data._descripcion,
                    EquipoID = data._equipoId
                };

                _dvpEntities.SubEquipo.Add(nuevo);
                _dvpEntities.SaveChanges();

                return Json(new { success = true, message = "Creado exitosamente." });

            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error al crear el tag: " + ex.Message });
            }
        }

        [HttpPost]
        public JsonResult CreateComponenteEquipo(ArbolFallasViewModel.ComponenteEquipo data)
        {
            try
            {
                if (!ModelState.IsValid)
                    return Json(new { success = false, message = "Datos inválidos." });

                if (data._subEquipoId == null)
                    return Json(new { success = false, message = "SubEquipo es obligatorio." });

                var nuevo = new ComponenteEquipo
                {
                    Descripcion = data._descripcion,
                    SubEquipoID = data._subEquipoId
                };

                _dvpEntities.ComponenteEquipo.Add(nuevo);
                _dvpEntities.SaveChanges();

                return Json(new { success = true, message = "ComponenteEquipo creado exitosamente." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error al crear el componente: " + ex.Message });
            }
        }

        [HttpPost]
        public JsonResult CreateClasificacion(ArbolFallasViewModel.Clasificacion data)
        {
            try
            {
                if (!ModelState.IsValid)
                    return Json(new { success = false, message = "Datos inválidos." });

                if (string.IsNullOrWhiteSpace(data._descripcion))
                    return Json(new { success = false, message = "Descripción es obligatoria." });

                var nuevo = new Clasificacion
                {
                    Descripcion = data._descripcion,
                    Ajeno = data._ajeno,
                    AfectaTMEF = data._afectaTMEF
                };

                _dvpEntities.Clasificacion.Add(nuevo);
                _dvpEntities.SaveChanges();

                return Json(new { success = true, message = "Clasificación creada exitosamente." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error al crear la clasificación: " + ex.Message });
            }
        }


        [HttpPost]
        public JsonResult CreateTipoFalla(ArbolFallasViewModel.TipoFalla data)
        {
            try
            {
                if (!ModelState.IsValid)
                    return Json(new { success = false, message = "Datos inválidos." });

                if (data._clasificacionId == null)
                    return Json(new { success = false, message = "Clasificación es obligatoria." });

                if (data._componenteEquipoId == null)
                    return Json(new { success = false, message = "ComponenteEquipo es obligatorio." });

                var nuevo = new TipoFalla
                {
                    Descripcion = data._descripcion,
                    ClasificacionID = data._clasificacionId,
                    ComponenteEquipoID = data._componenteEquipoId
                };

                _dvpEntities.TipoFalla.Add(nuevo);
                _dvpEntities.SaveChanges();

                return Json(new { success = true, message = "Tipo de falla creado exitosamente." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error al crear el tipo de falla: " + ex.Message });
            }
        }

        [HttpPost]
        public JsonResult UpdateSubEquipo(int SubEquipoID, string Descripcion)
        {
            var row = _dvpEntities.SubEquipo.FirstOrDefault(x => x.SubEquipoID == SubEquipoID);
            if (row == null) return Json(new { success = false, message = "No encontrado" });
            row.Descripcion = Descripcion;
            _dvpEntities.SaveChanges();
            return Json(new { success = true });
        }

        [HttpPost]
        public JsonResult UpdateComponente(int ComponenteEquipoID, string Descripcion)
        {
            var row = _dvpEntities.ComponenteEquipo.FirstOrDefault(x => x.ComponenteEquipoID == ComponenteEquipoID);
            if (row == null) return Json(new { success = false, message = "No encontrado" });
            row.Descripcion = Descripcion;
            _dvpEntities.SaveChanges();
            return Json(new { success = true });
        }

        [HttpPost]
        public JsonResult UpdateClasificacion(int ClasificacionID, string Descripcion)
        {
            var row = _dvpEntities.Clasificacion.FirstOrDefault(x => x.ClasificacionID == ClasificacionID);
            if (row == null) return Json(new { success = false, message = "No encontrado" });
            row.Descripcion = Descripcion;
            _dvpEntities.SaveChanges();
            return Json(new { success = true });
        }

        [HttpPost]
        public JsonResult UpdateTipoFalla(int TipoFallaID, string Descripcion)
        {
            var row = _dvpEntities.TipoFalla.FirstOrDefault(x => x.TipoFallaID == TipoFallaID);
            if (row == null) return Json(new { success = false, message = "No encontrado" });
            row.Descripcion = Descripcion;
            _dvpEntities.SaveChanges();
            return Json(new { success = true });
        }







    }
}