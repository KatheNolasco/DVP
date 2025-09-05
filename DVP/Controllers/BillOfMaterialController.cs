using DataAccess;
using DVP.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace DVP.Controllers
{
    public class BillOfMaterialController : Controller
    {
        DataAccess.DVPEntities _dvpEntities = new DataAccess.DVPEntities();

        // GET: BillOfMaterial
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

        [HttpPost]
        public JsonResult CreateBOM(BillOfMaterialViewModel data)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var nuevo = new BillOfMaterial
                    {
                     
                        MaterialProduccionID = data._materialProduccionID,
                        TipoOperacionID = data._tipoOperacionID,
                        TipoMovimientoSAPID = data._tipoMovimientoSAPID,
                        FactorConsumo = data._factorConsumo,
                        EquipoID = data._equipoID,
                        ConsumoSeco = data._consumoSeco,
                        ConsumoHumedo = data._consumoHumedo,
                        FechaBOM = DateTime.Now,
                        MaterialConsumoID = data._materialConsumoID
                    };

                    _dvpEntities.BillOfMaterial.Add(nuevo);
                    _dvpEntities.SaveChanges();

                    return Json(new { success = true, message = "BOM creado exitosamente." });
                }

                return Json(new { success = false, message = "Datos invalidos." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error al crear el BOM: " + ex.Message });
            }
        }

        [HttpPost]
        public JsonResult EditBOM(BillOfMaterialViewModel data)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var objeto = _dvpEntities.BillOfMaterial
                        .FirstOrDefault(b => b.BillOfMaterialID == data._billOfMaterialID);

                    if (objeto == null)
                        return Json(new { success = false, message = "BOM no encontrado." });

                    objeto.MaterialProduccionID = data._materialProduccionID;
                    objeto.TipoOperacionID = data._tipoOperacionID;
                    objeto.TipoMovimientoSAPID = data._tipoMovimientoSAPID;
                    objeto.FactorConsumo = data._factorConsumo;
                    objeto.EquipoID = data._equipoID;
                    objeto.ConsumoSeco = data._consumoSeco;
                    objeto.ConsumoHumedo = data._consumoHumedo;
                    objeto.FechaBOM = data._fechaBOM;        
                    objeto.MaterialConsumoID = data._materialConsumoID;

                    _dvpEntities.SaveChanges();

                    return Json(new { success = true, message = "Actualizado exitosamente." });
                }

                return Json(new { success = false, message = "Datos invalidos para editar." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error al editar el BOM: " + ex.Message });
            }
        }

        [HttpGet]
        public JsonResult GetBOMId(int billOfMaterialId)
        {
            var bom = _dvpEntities.BillOfMaterial
                .Where(b => b.BillOfMaterialID == billOfMaterialId)
                .Select(b => new
                {
                    _billOfMaterialID = b.BillOfMaterialID,
                    _materialProduccionID = b.MaterialProduccionID,
                    _materialProduccionDescripcion = b.Material.Descripcion,
                    _tipoOperacionID = b.TipoOperacionID,
                    _tipoOperacionDescripcion = b.TipoOperacion.Descripcion,
                    _tipoMovimientoSAPID = b.TipoMovimientoSAPID,
                    _tipoMovimientoSAPDescripcion = b.TipoMovimientoSAP.Descripcion,
                    _factorConsumo = b.FactorConsumo,
                    _equipoID = b.EquipoID,
                    _equipoDescripcion = b.Equipo.Descripcion,
                    _consumoSeco = b.ConsumoSeco,
                    _consumoHumedo = b.ConsumoHumedo,
                    _fechaBOM = b.FechaBOM,
                    _materialConsumoID = b.MaterialConsumoID,
                    _materialConsumoDescripcion = b.Material.Descripcion
                })
                .FirstOrDefault();

            if (bom == null)
            {
                return Json(new { success = false, message = "No encontrado" }, JsonRequestBehavior.AllowGet);
            }

            return Json(new { success = true, data = bom }, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult GetBOMs()
        {
            var boms = _dvpEntities.BillOfMaterial
                .Select(b => new
                {
                    _billOfMaterialID = b.BillOfMaterialID,
                    _materialProduccionID = b.MaterialProduccionID,
                    _materialProduccionDescripcion = b.Material.Descripcion,
                    _tipoOperacionID = b.TipoOperacionID,
                    _tipoOperacionDescripcion = b.TipoOperacion.Descripcion,
                    _tipoMovimientoSAPID = b.TipoMovimientoSAPID,
                    _tipoMovimientoSAPDescripcion = b.TipoMovimientoSAP.Descripcion,
                    _factorConsumo = b.FactorConsumo,
                    _equipoID = b.EquipoID,
                    _equipoDescripcion = b.Equipo.Descripcion,
                    _consumoSeco = b.ConsumoSeco,
                    _consumoHumedo = b.ConsumoHumedo,
                    _fechaBOM = b.FechaBOM,
                    _materialConsumoID = b.MaterialConsumoID,
                    _materialConsumoDescripcion = b.Material.Descripcion
                })
                .ToList();

            if (boms == null || !boms.Any())
            {
                return Json(new { success = false, message = "No encontrado" }, JsonRequestBehavior.AllowGet);
            }

            return Json(new { success = true, data = boms }, JsonRequestBehavior.AllowGet);
        }



    }
}