using DataAccess;
using DVP.Models;
using Microsoft.Ajax.Utilities;
using System;
using System.Collections.Generic;
using System.Data.Entity.Validation;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace DVP.Controllers
{
    public class DatosOperacionController : Controller
    {
        DataAccess.DVPEntities _dvpEntities = new DataAccess.DVPEntities();


        // GET: DataOperacion
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
        public JsonResult CreateTipoOperacion(DatosOperacionViewModel.TipoOperacion data)
        {
            try
            {
                if (!ModelState.IsValid)
                    return Json(new { success = false, message = "Datos invalidos." });

                var desc = (data._descripcion ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(desc))
                    return Json(new { success = false, message = "Descripcion es obligatoria." });

                // Normalizar para comparar duplicados (case-insensitive)
                var existe = _dvpEntities.TipoOperacion
                    .Any(t => t.Descripcion.ToLower() == desc.ToLower());
                if (existe)
                    return Json(new { success = false, message = "Ya existe un registro con esa descripcion." });

                var nuevo = new TipoOperacion
                {
                    Descripcion = desc,
                    AfectaInventario = data._afectaInventario
                };

                _dvpEntities.TipoOperacion.Add(nuevo);
                _dvpEntities.SaveChanges();

                return Json(new { success = true, message = "Creado exitosamente." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error al crear TipoOperacion: " + ex.Message });
            }
        }


        [HttpPost]
        public JsonResult CreateTipoMovimientoSAP(DatosOperacionViewModel.TipoMovimientoSAP data)
        {
            try
            {
                if (!ModelState.IsValid)
                    return Json(new { success = false, message = "Datos invalidos." });

                var desc = (data._descripcion ?? string.Empty).Trim();
                var descMov = (data._descripcionMovimiento ?? string.Empty).Trim();

                if (string.IsNullOrWhiteSpace(desc))
                    return Json(new { success = false, message = "Descripcion es obligatoria." });

                var nuevo = new TipoMovimientoSAP
                {
                    Descripcion = desc,
                    DescripcionMovimiento = descMov
                };

                _dvpEntities.TipoMovimientoSAP.Add(nuevo);
                _dvpEntities.SaveChanges();

                return Json(new { success = true, message = "Creado exitosamente." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error al crear TipoMovimientoSAP: " + ex.Message });
            }
        }

        [HttpPost]
        public JsonResult CreateUnidadMedida(DatosOperacionViewModel.UnidadMedida data)
        {
            try
            {
                if (!ModelState.IsValid)
                    return Json(new { success = false, message = "Datos invalidos." });

                var desc = (data._descripcion ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(desc))
                    return Json(new { success = false, message = "Descripcion es obligatoria." });

                var nuevo = new UnidadMedida
                {
                    Descripcion = desc,
                };

                _dvpEntities.UnidadMedida.Add(nuevo);
                _dvpEntities.SaveChanges();

                return Json(new { success = true, message = "Creado exitosamente." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error al crear TipoOperacion: " + ex.Message });
            }
        }

        [HttpPost]
        public JsonResult UpdateTipoOperacion(int TipoOperacionID, string Descripcion, bool? AfectaInventario)
        {
            try
            {
                var row = _dvpEntities.TipoOperacion.FirstOrDefault(x => x.TipoOperacionID == TipoOperacionID);
                if (row == null)
                    return Json(new { success = false, message = "No encontrado" });

                var desc = (Descripcion ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(desc))
                    return Json(new { success = false, message = "Descripcion es obligatoria." });

                // Evitar duplicados con otros registros
                var existeOtro = _dvpEntities.TipoOperacion
                    .Any(t => t.TipoOperacionID != TipoOperacionID && t.Descripcion.ToLower() == desc.ToLower());
                if (existeOtro)
                    return Json(new { success = false, message = "Ya existe otro registro con esa descripcion." });

                row.Descripcion = desc;
                if (AfectaInventario.HasValue)
                    row.AfectaInventario = AfectaInventario.Value;

                _dvpEntities.SaveChanges();
                return Json(new { success = true, message = "Actualizado exitosamente." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error al actualizar TipoOperacion: " + ex.Message });
            }
        }

        [HttpPost]
        public JsonResult UpdateTipoMovimientoSAP(int TipoMovimientoSAPID, string Descripcion, string DescripcionMovimiento)
        {
            var row = _dvpEntities.TipoMovimientoSAP.FirstOrDefault(x => x.TipoMovimientoSAPID == TipoMovimientoSAPID);
            if (row == null) return Json(new { success = false, message = "No encontrado" });

            row.Descripcion = Descripcion;
            row.DescripcionMovimiento = DescripcionMovimiento;

            _dvpEntities.SaveChanges();
            return Json(new { success = true });
        }

        [HttpPost]
        public JsonResult UpdateUnidadMedida(int UnidadMedidaID, string Descripcion)
        {
            var row = _dvpEntities.UnidadMedida.FirstOrDefault(x => x.UnidadMedidaID == UnidadMedidaID);
            if (row == null) return Json(new { success = false, message = "No encontrado" });

            row.Descripcion = Descripcion;

            _dvpEntities.SaveChanges();
            return Json(new { success = true });
        }

        [HttpGet]
        public JsonResult GetTipoMovimientoSAP()
        {
            var tipos = _dvpEntities.TipoMovimientoSAP
                                    .Select(s => new
                                    {
                                        TipoMovimientoSAPID = s.TipoMovimientoSAPID,
                                        Descripcion = s.Descripcion,
                                        DescripcionMovimiento = s.DescripcionMovimiento
                                    })
                                    .ToList();

            return Json(tipos, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult GetUnidadMedida()
        {
            var tipos = _dvpEntities.UnidadMedida
                                    .Select(s => new
                                    {
                                        UnidadMedidaID = s.UnidadMedidaID,
                                        Descripcion = s.Descripcion
                                    })
                                    .ToList();

            return Json(tipos, JsonRequestBehavior.AllowGet);
        }




    }
}