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

            if (rol != "Desarrollador de Software" && rol != "Administrador de la información")
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
                    _activo = p.Activo,
                    _consumido = p.Consumido,
                    _plantaId = p.PlantaID,
                    _plantaDescripcion = p.Planta.Descripcion,
                    _unidadMedidaId = p.UnidadMedidaID,
                    _unidadDescripcion = p.UnidadMedida.Descripcion
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
                    _activo = p.Activo,
                    _consumido = p.Consumido,
                    _plantaId = p.PlantaID,
                    _plantaDescripcion = p.Planta.Descripcion,
                    _unidadMedidaId = p.UnidadMedidaID,
                    _unidadDescripcion = p.UnidadMedida.Descripcion
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
                data._descripcion = (data._descripcion ?? "").Trim();
                data._codSAPNuevo = (data._codSAPNuevo ?? "").Trim();
                data._codOldSAP = (data._codOldSAP ?? "").Trim();
                data._idStock = (data._idStock ?? "").Trim();


                if (!TryValidateModel(data))
                {
                    var errs = ModelState.ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                    );
                    return Json(new { success = false, message = "Datos inválidos.", errors = errs });
                }

                if (string.IsNullOrWhiteSpace(data._descripcion) ||
                    string.IsNullOrWhiteSpace(data._codSAPNuevo) ||
                    string.IsNullOrWhiteSpace(data._codOldSAP) ||
                    string.IsNullOrWhiteSpace(data._idStock) ||
                    !data._clasificacionMaterialID.HasValue ||
                    !data._plantaId.HasValue ||
                    !data._unidadMedidaId.HasValue)
                {
                    return Json(new { success = false, message = "Campos requeridos incompletos." });
                }

                bool existe = _dvpEntities.Material.Any(m =>
                    m.CodSAPNuevo == data._codSAPNuevo &&
                    m.Descripcion == data._descripcion &&
                    m.PlantaID == data._plantaId.Value &&
                    m.UnidadMedidaID == data._unidadMedidaId.Value
                );
                if (existe)
                {
                    return Json(new { success = false, message = "El material ya existe (CodSAPNuevo + Planta + Unidad)." });
                }

                var nuevo = new Material
                {
                    Descripcion = data._descripcion,
                    CodSAPNuevo = data._codSAPNuevo,
                    CodOldSAP = data._codOldSAP,
                    Producido = data._producido,
                    ClasificacionMaterialID = data._clasificacionMaterialID.Value,
                    Alterno = data._alterno,
                    AfectaInventario = data._afectaInventario,
                    IDStock = data._idStock,
                    Activo = true,
                    PlantaID = data._plantaId.Value,
                    UnidadMedidaID = data._unidadMedidaId.Value,
                    Consumido = data._consumido
                };

                _dvpEntities.Material.Add(nuevo);
                _dvpEntities.SaveChanges();

                return Json(new { success = true, message = "Material creado exitosamente." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error al crear el material: " + ex.Message });
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
                    objeto.PlantaID = data._plantaId;
                    objeto.UnidadMedidaID = data._unidadMedidaId;


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

        [HttpGet]
        public JsonResult GetMaterialProducidoOConsumido()
        {
            var list = _dvpEntities.Material
                                     .Where(s => s.Producido == true && s.AfectaInventario == true)
                                     .Select(s => new
                                     {
                                         MaterialID = s.MaterialID,
                                         Descripcion = s.Descripcion
                                     })
                                     .ToList();

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult GetMaterialProducido()
        {
            var list = _dvpEntities.Material
                                     .Where(s => s.Producido == true)
                                     .Select(s => new
                                     {
                                         MaterialID = s.MaterialID,
                                         Descripcion = s.Descripcion
                                     })
                                     .ToList();

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult GetMaterialConsumido()
        {
            var list = _dvpEntities.Material
                                     .Where(s => s.Consumido == true)
                                     .Select(s => new
                                     {
                                         MaterialID = s.MaterialID,
                                         Descripcion = s.Descripcion
                                     })
                                     .ToList();

            return Json(list, JsonRequestBehavior.AllowGet);
        }


        [HttpGet]
        public JsonResult GetMaterialCombustible()
        {
            var list = _dvpEntities.Material
                                     .Where(s => s.ClasificacionMaterial.Descripcion == "COMBUSTIBLE")
                                     .Select(s => new
                                     {
                                         MaterialID = s.MaterialID,
                                         Descripcion = s.Descripcion
                                     })
                                     .ToList();

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult GetMaterialAlterno()
        {
            var list = _dvpEntities.Material
                                     .Where(s => s.Alterno == true)
                                     .Select(s => new
                                     {
                                         MaterialID = s.MaterialID,
                                         Descripcion = s.Descripcion
                                     })
                                     .ToList();

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult GetMaterialesProducidosByEquipo(int equipoId)
        {
            try
            {
                var lista = _dvpEntities.BillOfMaterial
                    .Where(b => b.EquipoID == equipoId && b.MaterialProduccionID.HasValue)
                    .GroupBy(b => new {
                        MaterialID = b.MaterialProduccionID,
                        Descripcion = b.Material1.Descripcion
                    })
                    .Select(g => new
                    {
                        MaterialID = g.Key.MaterialID,
                        Descripcion = g.Key.Descripcion,
                    })
                    .ToList();

                return Json(new { success = true, data = lista }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error al obtener Materiales Producidos: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public JsonResult GetMaterialesConsumidosByEquipo(int equipoId)
        {
            try
            {
                var lista = _dvpEntities.BillOfMaterial
                    .Where(b => b.EquipoID == equipoId && b.MaterialConsumoID.HasValue)
                    .GroupBy(b => new {
                        MaterialID = b.MaterialConsumoID,
                        Descripcion = b.Material.Descripcion
                    })
                    .Select(g => new
                    {
                        MaterialID = g.Key.MaterialID,
                        Descripcion = g.Key.Descripcion,
                    })
                    .ToList();

                return Json(new { success = true, data = lista }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error al obtener Materiales Producidos: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }
    }
}