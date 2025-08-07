using DataAccess;
using DVP.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace DVP.Controllers
{
    public class MaterialController : Controller
    {
        DataAccess.DVPEntities _dvpEntities = new DataAccess.DVPEntities();

        // GET: Material

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

            if (rol != "Desarrollador de Software")
            {
                return RedirectToAction("Index", "Account");
            }

            return View();
        }

        [HttpGet]
        public JsonResult GetClasificacionMaterial()
        {

            var clasificaciones = _dvpEntities.ClasificacionMaterial
                                     .Select(s => new
                                     {
                                         ClasificacionMaterialID = s.ClasificacionMaterialID,
                                         Descripcion = s.Descripcion
                                     })
                                     .ToList();

            return Json(clasificaciones, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult GetMaterialId(int materialId)
        {
            var material = _dvpEntities.Material
                .Where(p => p.MaterialID == materialId)
                .Select(p => new
                {
                    _materialID = p.MaterialID,
                    _descripcion = p.Descripcion,
                    _codSAPNuevo = p.CodSAPNuevo,
                    _codOldSAP = p.CodOldSAP,
                    _producido = p.Producido,
                    _clasificacionMaterialID = p.ClasificacionMaterialID,
                    _clasificacionMaterialdescripcion = p.ClasificacionMaterial.Descripcion,
                    _alterno = p.Alterno,
                    _afectaInventario = p.AfectaInventario,
                    _idStock = p.IDStock,
                    _activo = p.Activo
                })
                .FirstOrDefault();

            if (material == null)
            {
                return Json(new { success = false, message = "No encontrado" }, JsonRequestBehavior.AllowGet);
            }

            return Json(new { success = true, data = material }, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult GetMateriales()
        {
            var materiales = _dvpEntities.Material
                .Select(p => new
                {
                    _materialID = p.MaterialID,
                    _descripcion = p.Descripcion,
                    _codSAPNuevo = p.CodSAPNuevo,
                    _codOldSAP = p.CodOldSAP,
                    _producido = p.Producido,
                    _clasificacionMaterialID = p.ClasificacionMaterialID,
                    _clasificacionMaterialdescripcion = p.ClasificacionMaterial.Descripcion,
                    _alterno = p.Alterno,
                    _afectaInventario = p.AfectaInventario,
                    _idStock = p.IDStock,
                    _activo = p.Activo
                })
                .ToList();

            if (materiales == null)
            {
                return Json(new { success = false, message = "No encontrado" }, JsonRequestBehavior.AllowGet);
            }

            return Json(new { success = true, data = materiales }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult Create(MaterialViewModel data)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var nuevo = new Material
                    {
                        Descripcion = data._descripcion,
                        CodSAPNuevo = data._codSAPNuevo,
                        CodOldSAP = data._codOldSAP,
                        Producido = data._producido,
                        ClasificacionMaterialID = data._clasificacionMaterialID,
                        Alterno = data._alterno,
                        AfectaInventario = data._afectaInventario,
                        IDStock = data._idStock,
                        Activo = data._activo
                    };

                    _dvpEntities.Material.Add(nuevo);
                    _dvpEntities.SaveChanges();

                    return Json(new { success = true, message = "Material creado exitosamente." });
                }

                return Json(new { success = false, message = "Datos inválidos." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error al crear el equipo: " + ex.Message });
            }
        }


        [HttpPost]
        public JsonResult Edit(MaterialViewModel data)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var objeto = _dvpEntities.Material.FirstOrDefault(e => e.MaterialID == data._materialID);

                    if (objeto == null)
                        return Json(new { success = false, message = "Material no encontrado." });

                    objeto.Descripcion = data._descripcion;
                    objeto.CodSAPNuevo = data._codSAPNuevo;
                    objeto.CodOldSAP = data._codOldSAP;
                    objeto.Producido = data._producido;
                    objeto.ClasificacionMaterialID = data._clasificacionMaterialID;
                    objeto.Alterno = data._alterno;
                    objeto.AfectaInventario = data._afectaInventario;
                    objeto.IDStock = data._idStock;
                    objeto.Activo = data._activo;

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
    }
}