using DataAccess;
using DVP.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity.Infrastructure;
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
                if (data == null)
                    return Json(new { success = false, message = "Payload invalido." });

                // normalizaciones
                var prod = (data._materialProduccionIDs ?? new List<int>()).Where(x => x > 0).Distinct().ToList();
                var cons = (data._materialConsumoIDs ?? new List<int>()).Where(x => x > 0).Distinct().ToList();
                var equipos = (data._equipoIDs ?? new List<int>()).Where(x => x > 0).Distinct().ToList();
                
                // retrocompatibilidad: si no viene _equipoIDs, usar _equipoID
                if (!equipos.Any())
                {
                    if (data._equipoID == null || data._equipoID <= 0)
                        return Json(new { success = false, message = "Equipo es requerido." });
                    equipos.Add(data._equipoID.Value);
                }

                // validaciones
                if (prod.Count != 1)
                    return Json(new { success = false, message = "Debe seleccionar exactamente un (1) material de produccion." });

                if (cons.Count == 0)
                    return Json(new { success = false, message = "Debe indicar al menos un material de consumo." });

                if (cons.Intersect(prod).Any())
                    return Json(new { success = false, message = "Un material no puede ser simultaneamente de produccion y de consumo." });

                if (data._consumoSeco && data._consumoHumedo)
                    return Json(new { success = false, message = "Consumo Seco y Consumo Humedo no pueden estar ambos activos." });

                if (data._produccionSeca && data._produccionHumeda)
                    return Json(new { success = false, message = "Producción Seca y Producción Humeda no pueden estar ambos activos." });

                var materialProduccionID = prod[0];
                var factor = data._factorConsumo ?? 0m;
                var tipoOperacionProduccion = 2;
                var tipoMovimientoSAP = 1;

                var nuevasFilas = new List<BillOfMaterial>();

                foreach (var equipoID in equipos)
                {
                    foreach (var materialConsumoID in cons)
                    {
                        var fila = new BillOfMaterial
                        {
                            MaterialProduccionID = materialProduccionID,
                            MaterialConsumoID = materialConsumoID,
                            TipoOperacionID = tipoOperacionProduccion,
                            TipoMovimientoSAPID = tipoMovimientoSAP,    
                            EquipoID = equipoID,
                            FactorConsumo = factor,
                            ConsumoSeco = data._consumoSeco,
                            ConsumoHumedo = data._consumoHumedo,
                            FechaBOM = DateTime.Now, 
                            Active = true,
                            ProduccionSeca = data._produccionSeca,
                            ProduccionHumeda = data._produccionHumeda
                        };

                        if (!ExisteDuplicado(fila))
                            nuevasFilas.Add(fila);
                    }
                }

                if (nuevasFilas.Count == 0)
                    return Json(new { success = false, message = "No se generaron filas nuevas (posibles duplicados)." });

                _dvpEntities.BillOfMaterial.AddRange(nuevasFilas);
                _dvpEntities.SaveChanges();

                return Json(new
                {
                    success = true,
                    created = nuevasFilas.Count,
                    message = $"Creado exitosamente ({nuevasFilas.Count}) fila(s)."
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error al crear BOM: " + ex.Message });
            }
        }


        private bool ExisteDuplicado(BillOfMaterial m)
        {
            return _dvpEntities.BillOfMaterial.Any(x =>
                x.MaterialProduccionID == m.MaterialProduccionID &&
                x.MaterialConsumoID == m.MaterialConsumoID &&
                (x.TipoOperacionID ?? 0) == (m.TipoOperacionID ?? 0) &&
                (x.TipoMovimientoSAPID ?? 0) == (m.TipoMovimientoSAPID ?? 0) &&
                x.EquipoID == m.EquipoID &&
                x.ConsumoSeco == m.ConsumoSeco &&
                x.ConsumoHumedo == m.ConsumoHumedo &&
                x.ProduccionSeca == m.ProduccionSeca &&
                x.ProduccionHumeda == m.ProduccionHumeda
            );
        }

        [HttpPost]
        public JsonResult EditBOM(BillOfMaterialViewModel data)
        {
            try
            {
                if (data == null)
                    return Json(new { success = false, message = "Payload invalido." });

                if (data._billOfMaterialID <= 0)
                    return Json(new { success = false, message = "ID invalido." });

                if (data._equipoID <= 0)
                    return Json(new { success = false, message = "Equipo es requerido." });

                // no permitir el mismo material como produccion y consumo
                if (data._materialProduccionID == data._materialConsumoID)
                    return Json(new { success = false, message = "Un material no puede ser simultaneamente de produccion y consumo." });

                // buscar fila
                var row = _dvpEntities.BillOfMaterial.FirstOrDefault(x => x.BillOfMaterialID == data._billOfMaterialID);
                if (row == null)
                    return Json(new { success = false, message = "BOM no encontrado." });

                // regla fija
                var tipoOperacionProduccion = 2;
                var tipoMovimientoSAP = 1;
                var nuevoFactor = data._factorConsumo ?? 0m;

                // validar duplicado con la nueva combinacion (excluyendose a si mismo)
                if (ExisteDuplicadoEdit(
                    excludeId: row.BillOfMaterialID,
                    materialProduccionID: data._materialProduccionID.Value,
                    materialConsumoID: data._materialConsumoID.Value,
                    tipoOperacionID: tipoOperacionProduccion,
                    tipoMovimientoSAPID: tipoMovimientoSAP,
                    equipoID: data._equipoID.Value,
                    consumoSeco: data._consumoSeco,
                    consumoHumedo: data._consumoHumedo,
                    produccionSeca: data._produccionSeca,
                    produccionHumeda: data._produccionHumeda))
                {
                    return Json(new { success = false, message = "Ya existe una BOM con la misma combinacion." });
                }

                // actualizar
                row.MaterialProduccionID = data._materialProduccionID;
                row.MaterialConsumoID = data._materialConsumoID;  
                row.EquipoID = data._equipoID;
                row.FactorConsumo = nuevoFactor;
                row.ConsumoSeco = data._consumoSeco;
                row.ConsumoHumedo = data._consumoHumedo;
                row.Active = data._active;
                row.ProduccionSeca = data._produccionSeca;
                row.ProduccionHumeda = data._produccionHumeda;
                _dvpEntities.SaveChanges();

                return Json(new { success = true, message = "Actualizado exitosamente." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error al actualizar BOM: " + ex.Message });
            }
        }

        private bool ExisteDuplicadoEdit(
            int excludeId,
            int materialProduccionID,
            int materialConsumoID,
            int? tipoOperacionID,
            int? tipoMovimientoSAPID,
            int equipoID,
            bool consumoSeco,
            bool consumoHumedo,
            bool produccionSeca,
            bool produccionHumeda)
        {
            return _dvpEntities.BillOfMaterial.Any(x =>
                x.BillOfMaterialID != excludeId &&
                x.MaterialProduccionID == materialProduccionID &&
                x.MaterialConsumoID == materialConsumoID &&
                (x.TipoOperacionID ?? 0) == (tipoOperacionID ?? 0) &&
                (x.TipoMovimientoSAPID ?? 0) == (tipoMovimientoSAPID ?? 0) &&
                x.EquipoID == equipoID &&
                x.ConsumoSeco == consumoSeco &&
                x.ConsumoHumedo == consumoHumedo &&
                x.ProduccionSeca == produccionSeca &&
                x.ProduccionHumeda == produccionHumeda
            );
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
                    _materialConsumoDescripcion = b.Material.Descripcion,
                    _produccionSeca = b.ProduccionSeca,
                    _produccionHumeda = b.ProduccionHumeda
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
            try
            {
                var boms = _dvpEntities.BillOfMaterial
                    .Select(b => new
                    {
                        _billOfMaterialID = b.BillOfMaterialID,

                        _materialProduccionID = b.MaterialProduccionID,
                        _materialProduccionDescripcion = _dvpEntities.Material
                            .Where(m => m.MaterialID == b.MaterialProduccionID)
                            .Select(m => m.Descripcion)
                            .FirstOrDefault(),

                        _tipoOperacionID = b.TipoOperacionID,
                        _tipoOperacionDescripcion = b.TipoOperacion.Descripcion,
                        _tipoMovimientoSAPID = b.TipoMovimientoSAPID,
                        _tipoMovimientoSAPDescripcion = b.TipoMovimientoSAP.Descripcion,

                        _factorConsumo = b.FactorConsumo,
                        _equipoID = b.EquipoID,
                        _equipoDescripcion = b.Equipo.Descripcion,

                        _consumoSeco = b.ConsumoSeco,
                        _consumoHumedo = b.ConsumoHumedo,
                        _produccionSeca = b.ProduccionSeca,
                        _produccionHumedo = b.ProduccionHumeda,
                        _fechaBOM = b.FechaBOM,
                        _fechaBOMIso = b.FechaBOM, 

                        _materialConsumoID = b.MaterialConsumoID,
                        _materialConsumoDescripcion = _dvpEntities.Material
                            .Where(m => m.MaterialID == b.MaterialConsumoID)
                            .Select(m => m.Descripcion)
                            .FirstOrDefault()
                    })
                    .ToList();

                if (boms.Count == 0)
                    return Json(new { success = false, message = "No encontrado" }, JsonRequestBehavior.AllowGet);

                return Json(new { success = true, data = boms }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public JsonResult GetBOMsByMaterial(int materialProduccionId)
        {
            try
            {
                var lista = _dvpEntities.BillOfMaterial
                    .Where(b => b.MaterialProduccionID == materialProduccionId)
                    .Select(b => new
                    {
                        _billOfMaterialID = b.BillOfMaterialID,
                        _materialProduccionID = b.MaterialProduccionID,
                        _materialProduccionDescripcion = b.Material1.Descripcion,
                        _tipoOperacionID = b.TipoOperacionID,
                        _tipoOperacionDescripcion = b.TipoOperacion.Descripcion,
                        _tipoMovimientoSAPID = b.TipoMovimientoSAPID,
                        _tipoMovimientoSAPDescripcion = b.TipoMovimientoSAP.Descripcion,
                        _factorConsumo = b.FactorConsumo,
                        _equipoID = b.EquipoID,
                        _equipoDescripcion = b.Equipo.Descripcion,
                        _consumoSeco = b.ConsumoSeco,
                        _consumoHumedo = b.ConsumoHumedo,
                        _produccionSeca = b.ProduccionSeca,
                        _produccionHumeda = b.ProduccionHumeda,
                        _fechaBOM = b.FechaBOM,
                        _materialConsumoID = b.MaterialConsumoID,
                        _materialConsumoDescripcion = b.Material.Descripcion,
                        _active = b.Active
                    })
                    .ToList();

                return Json(new { success = true, data = lista }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }


        [HttpGet]
        public JsonResult GetBOMsConsumoByEquipo(int equipoId)
        {
            try
            {
                var lista = _dvpEntities.BillOfMaterial
                    .Where(b => b.EquipoID == equipoId)
                    .Select(b => new
                    {
                        _billOfMaterialID = b.BillOfMaterialID,
                        _tipoOperacionID = b.TipoOperacionID,
                        _tipoOperacionDescripcion = b.TipoOperacion.Descripcion,
                        _tipoMovimientoSAPID = b.TipoMovimientoSAPID,
                        _tipoMovimientoSAPDescripcion = b.TipoMovimientoSAP.Descripcion,
                        _factorConsumo = b.FactorConsumo,
                        _equipoID = b.EquipoID,
                        _equipoDescripcion = b.Equipo.Descripcion,
                        _consumoSeco = b.ConsumoSeco,
                        _consumoHumedo = b.ConsumoHumedo,
                        _produccionSeca = b.ProduccionSeca,
                        _produccionHumeda = b.ProduccionHumeda,
                        _fechaBOM = b.FechaBOM,
                        _materialConsumoID = b.MaterialConsumoID,
                        _materialConsumoDescripcion = b.Material.Descripcion,
                        _active = b.Active
                    })
                    .ToList();

                return Json(new { success = true, data = lista }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }


        [HttpGet]
        public JsonResult GetBOMsProduccionByEquipo(int equipoId)
        {
            try
            {
                var lista = _dvpEntities.BillOfMaterial
                    .Where(b => b.EquipoID == equipoId)
                    .Select(b => new
                    {
                        _billOfMaterialID = b.BillOfMaterialID,
                        _materialProduccionID = b.MaterialProduccionID,
                        _materialProduccionDescripcion = b.Material1.Descripcion,
                        _tipoOperacionID = b.TipoOperacionID,
                        _tipoOperacionDescripcion = b.TipoOperacion.Descripcion,
                        _tipoMovimientoSAPID = b.TipoMovimientoSAPID,
                        _tipoMovimientoSAPDescripcion = b.TipoMovimientoSAP.Descripcion,
                        _factorConsumo = b.FactorConsumo,
                        _equipoID = b.EquipoID,
                        _equipoDescripcion = b.Equipo.Descripcion,
                        _consumoSeco = b.ConsumoSeco,
                        _consumoHumedo = b.ConsumoHumedo,
                        _produccionSeca = b.ProduccionSeca,
                        _produccionHumeda = b.ProduccionHumeda,
                        _fechaBOM = b.FechaBOM,
                        _active = b.Active
                    })
                    .ToList();

                return Json(new { success = true, data = lista }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }


        [HttpPost]
        public JsonResult DeleteBOM(int id)
        {
            try
            {
                var entity = _dvpEntities.BillOfMaterial.FirstOrDefault(x => x.BillOfMaterialID == id);
                if (entity == null)
                {
                    return Json(new { success = false, message = "Registro no encontrado." });
                }

                _dvpEntities.BillOfMaterial.Remove(entity);
                _dvpEntities.SaveChanges();
                return Json(new { success = true, message = "Eliminado correctamente." });

            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = "Error al eliminar: " + ex.Message
                });
            }

        }
    }
}