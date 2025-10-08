using DataAccess;
using DVP.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;


namespace DVP.Controllers
{
    public class OperationDataToReportController : Controller
    {
        DataAccess.DVPEntities _dvpEntities = new DataAccess.DVPEntities();

        



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


            if (rol != "Desarrollador de Software" && rol != "Administrador de la información")
            {
                return RedirectToAction("Index", "Account");
            }

            return View();


        }

        public ActionResult Energy()
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


            if (rol != "Desarrollador de Software" && rol != "Administrador de la información")
            {
                return RedirectToAction("Index", "Account");
            }

            return View();


        }

        public ActionResult Operation()
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


            if (rol != "Desarrollador de Software" && rol != "Administrador de la información")
            {
                return RedirectToAction("Index", "Account");
            }

            return View();


        }

        public ActionResult CapturaHumedad()
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


            if (rol != "Desarrollador de Software" && rol != "Administrador de la información")
            {
                return RedirectToAction("Index", "Account");
            }

            return View();


        }

        [HttpGet]
        public JsonResult GetCapturasHumedad(DateTime? _fecha, int? _equipoId = null, string _tipoMaterial = null)
        {
            var day = (_fecha ?? DateTime.Today).Date;
            var next = day.AddDays(1);

            var query = _dvpEntities.DataOperacion
                .Where(d => d.TipoOperacionID == HUMEDAD
                         && d.FechaReporte >= day
                         && d.FechaReporte < next);

            if (_equipoId.HasValue && _equipoId.Value > 0)
            {
                int eqId = _equipoId.Value;
                query = query.Where(d => d.EquipoID == eqId);

                if (!string.IsNullOrWhiteSpace(_tipoMaterial))
                {
                    var tipo = _tipoMaterial.Trim().ToLowerInvariant();
                    IQueryable<int> matsQ = null;

                    if (tipo == "produccion" || tipo == "producción")
                    {
                        matsQ = _dvpEntities.BillOfMaterial
                            .Where(b => b.EquipoID == eqId && b.MaterialProduccionID != null)
                            .Select(b => b.MaterialProduccionID.Value);
                    }
                    else if (tipo == "consumo")
                    {
                        matsQ = _dvpEntities.BillOfMaterial
                            .Where(b => b.EquipoID == eqId && b.MaterialConsumoID != null)
                            .Select(b => b.MaterialConsumoID.Value);
                    }

                    if (matsQ != null)
                    {
                        var materialIds = matsQ.Distinct().ToList();
                        if (materialIds.Count > 0)
                        {
                            query = query.Where(d => d.MaterialID.HasValue && materialIds.Contains(d.MaterialID.Value));
                        }
                    }
                }
            }

            var capturas = query
                .OrderByDescending(d => d.FechaReporte)
                .Select(d => new
                {
                    DataOperacionID = d.DataOperacionID,
                    EquipoID = d.EquipoID,
                    Equipo = d.Equipo != null ? d.Equipo.Descripcion : null,

                    MaterialID = d.MaterialID,
                    Material = d.Material != null ? d.Material.Descripcion : null,

                    // devuelvo ambas variantes por tolerancia de casing
                    CantidadPIMS = d.CantidadPIMS,
                    CantidadPims = d.CantidadPIMS,

                    CantidadValidada = d.CantidadValidada,
                    UnidadMedidaID = d.UnidadMedidaID,
                    UnidadMedida = d.UnidadMedida != null ? d.UnidadMedida.Descripcion : null,
                    TipoMovimientoSAPID = d.TipoMovimientoSAPID,
                    TipoMovimientoSAP = d.TipoMovimientoSAP != null ? d.TipoMovimientoSAP.Descripcion : null,
                    FechaReporte = d.FechaReporte,
                    StatusClose = d.StatusClose,
                    StatusValidate = d.StatusValidate,
                    OrdenProcesoSAP = d.OrdenProcesoSAP
                })
                .ToList();

            return Json(capturas, JsonRequestBehavior.AllowGet);
        }


        [HttpPost]
        public JsonResult CrearCapturaHumedadByBOM(OperationDataToReportViewModel data)
        {
            try
            {
                if (data == null)
                    return Json(new { success = false, message = "Payload vacio." });

                if (!ModelState.IsValid)
                    return Json(new { success = false, message = "Datos invalidos." });


                if (data._fechaReporte.Date <= DateTime.Now.AddDays(-2))
                    return Json(new { success = false, message = "No se puede registrar humedades 2 días anteriores o más del día actual." });

                if (data._equipoId <= 0)
                    return Json(new { success = false, message = "Equipo es obligatorio." });

                decimal? cantidadHumedad = data._cantidadValidada;
                if (cantidadHumedad == null)
                    return Json(new { success = false, message = "Cantidad de humedad es obligatoria." });

                DateTime fechaReporte = data._fechaReporte; 

                var existe = _dvpEntities.DataOperacion.Any(x =>
                    x.MaterialID == data._materialId &&
                    x.TipoOperacionID == HUMEDAD &&
                    DbFunctions.TruncateTime(x.FechaReporte) == fechaReporte
                );


                if (existe)
                    return Json(new { success = false, message = "Ya existe un registro de humedad para ese minuto." });

                var nuevo = new DataOperacion
                {
                    EquipoID = data._equipoId,
                    TipoOperacionID = HUMEDAD,
                    MaterialID = data._materialId,
                    CantidadPIMS = data._cantidadPims,
                    CantidadValidada = cantidadHumedad,
                    UnidadMedidaID = UNIDAD_MEDIDA_HUMEDAD,
                    TipoMovimientoSAPID = TIPO_MOV_SAP_NA,
                    FechaReporte = fechaReporte,
                    StatusClose = false,
                    StatusValidate = true
                };

                _dvpEntities.DataOperacion.Add(nuevo);
                _dvpEntities.SaveChanges();

                return Json(new
                {
                    success = true,
                    id = nuevo.DataOperacionID,
                    message = "Humedad registrada correctamente."
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = "Error al registrar la humedad: " + ex.Message
                });
            }
        }


        [HttpPost]
        public JsonResult CrearCapturaHumedadByMaterialProduccion(OperationDataToReportViewModel data)
        {
            try
            {
                if (data == null)
                    return Json(new { success = false, message = "Payload vacio." });

                if (!ModelState.IsValid)
                    return Json(new { success = false, message = "Datos invalidos." });

                if (data._fechaReporte.Date <= DateTime.Now.AddDays(-2))
                    return Json(new { success = false, message = "No se puede registrar humedades 2 dias anteriores o mas del dia actual." });

                if (data._materialId <= 0)
                    return Json(new { success = false, message = "Material es obligatorio." });

                decimal? cantidadHumedad = data._cantidadValidada;
                if (cantidadHumedad == null)
                    return Json(new { success = false, message = "Cantidad de humedad es obligatoria." });

                DateTime fechaReporte = data._fechaReporte;

                // obtener equipos por material produccion (activos y distinct)
                var equipos = _dvpEntities.BillOfMaterial
                    .Where(b => b.MaterialProduccionID == data._materialId && b.Active == true)
                    .Select(b => b.EquipoID)
                    .Distinct()
                    .ToList();

                if (equipos == null || equipos.Count == 0)
                    return Json(new { success = false, message = "No hay equipos asociados a ese material de produccion." });

                var inserted = 0;
                var skipped = 0;
                var details = new List<object>();

                foreach (var eqId in equipos)
                {
                    if (eqId <= 0) { skipped++; continue; }

                    var existe = _dvpEntities.DataOperacion.Any(x =>
                        x.EquipoID == eqId &&
                        x.MaterialID == data._materialId &&
                        x.TipoOperacionID == HUMEDAD &&
                        DbFunctions.TruncateTime(x.FechaReporte) == DbFunctions.TruncateTime((DateTime?)fechaReporte)
                    );

                    if (existe)
                    {
                        return Json(new { success = false, message = "Ya existe humedad para este dia en ese material." });
                    }

                    var nuevo = new DataOperacion
                    {
                        EquipoID = eqId,
                        TipoOperacionID = HUMEDAD,
                        MaterialID = data._materialId,
                        CantidadPIMS = data._cantidadPims,
                        CantidadValidada = cantidadHumedad,
                        UnidadMedidaID = UNIDAD_MEDIDA_HUMEDAD,
                        TipoMovimientoSAPID = TIPO_MOV_SAP_NA,
                        FechaReporte = fechaReporte,
                        StatusClose = false,
                        StatusValidate = true
                    };

                    _dvpEntities.DataOperacion.Add(nuevo);
                    inserted++;
                    details.Add(new { equipoId = eqId, status = "insertado", id = (int?)null }); // id real tras SaveChanges
                }

                _dvpEntities.SaveChanges();

                // opcional: actualizar ids en detalles insertados (si lo deseas, requeriria rastrear entidades)
                var summary = new { inserted, skipped, total = inserted + skipped, details };

                return Json(new
                {
                    success = true,
                    message = "Proceso completado para material de produccion.",
                    summary
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error al registrar la humedad: " + ex.Message });
            }
        }



        [HttpPost]
        public JsonResult CrearCapturaHumedadByMaterialConsumo(OperationDataToReportViewModel data)
        {
            try
            {
                if (data == null)
                    return Json(new { success = false, message = "Payload vacio." });

                if (!ModelState.IsValid)
                    return Json(new { success = false, message = "Datos invalidos." });

                if (data._fechaReporte.Date <= DateTime.Now.AddDays(-2))
                    return Json(new { success = false, message = "No se puede registrar humedades 2 dias anteriores o mas del dia actual." });

                if (data._materialId <= 0)
                    return Json(new { success = false, message = "Material es obligatorio." });

                decimal? cantidadHumedad = data._cantidadValidada;
                if (cantidadHumedad == null)
                    return Json(new { success = false, message = "Cantidad de humedad es obligatoria." });

                DateTime fechaReporte = data._fechaReporte;

                // obtener equipos por material consumo (activos y distinct)
                var equipos = _dvpEntities.BillOfMaterial
                    .Where(b => b.MaterialConsumoID == data._materialId && b.Active == true)
                    .Select(b => b.EquipoID)
                    .Distinct()
                    .ToList();

                if (equipos == null || equipos.Count == 0)
                    return Json(new { success = false, message = "No hay equipos asociados a ese material de consumo." });

                var inserted = 0;
                var skipped = 0;
                var details = new List<object>();

                foreach (var eqId in equipos)
                {
                    if (eqId <= 0) { skipped++; continue; }

                    var existe = _dvpEntities.DataOperacion.Any(x =>
                        x.EquipoID == eqId &&
                        x.MaterialID == data._materialId &&
                        x.TipoOperacionID == HUMEDAD &&
                        DbFunctions.TruncateTime(x.FechaReporte) == DbFunctions.TruncateTime((DateTime?)fechaReporte)
                    );

                    if (existe)
                    {
                        return Json(new { success = false, message = "Ya existe humedad para este dia en ese material." });
                    }

                    var nuevo = new DataOperacion
                    {
                        EquipoID = eqId,
                        TipoOperacionID = HUMEDAD,
                        MaterialID = data._materialId,
                        CantidadPIMS = data._cantidadPims,
                        CantidadValidada = cantidadHumedad,
                        UnidadMedidaID = UNIDAD_MEDIDA_HUMEDAD,
                        TipoMovimientoSAPID = TIPO_MOV_SAP_NA,
                        FechaReporte = fechaReporte,
                        StatusClose = false,
                        StatusValidate = true
                    };

                    _dvpEntities.DataOperacion.Add(nuevo);
                    inserted++;
                    details.Add(new { equipoId = eqId, status = "insertado", id = (int?)null });
                }

                _dvpEntities.SaveChanges();

                var summary = new { inserted, skipped, total = inserted + skipped, details };

                return Json(new
                {
                    success = true,
                    message = "Proceso completado para material de consumo.",
                    summary
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error al registrar la humedad: " + ex.Message });
            }
        }


        [HttpPost]
        public JsonResult UpdateCapturaHumedad(OperationDataToReportViewModel data)
        {
            try
            {
                if (data == null)
                    return Json(new { success = false, message = "Payload vacío." });

                if (!ModelState.IsValid)
                    return Json(new { success = false, message = "Datos inválidos." });

                decimal? cantidadHumedad = data._cantidadValidada;
                if (cantidadHumedad == null)
                    return Json(new { success = false, message = "Cantidad de humedad es obligatoria." });

                DataOperacion row = null;
                if (data._dataOperacionId > 0)
                {
                    row = _dvpEntities.DataOperacion
                                    .FirstOrDefault(x => x.DataOperacionID == data._dataOperacionId && x.TipoOperacionID == HUMEDAD);
                }

                if (row == null)
                {
                    return Json(new { success = false, message = "No existe registro de humedad para actualizar (ID no encontrado)." });
                }

                // **CORRECCIÓN CLAVE:** Solo ejecuta la validación de 2 días si la fecha es válida.
                // Si data._fechaReporte es la fecha default, significa que el front la envió mal.
                if (row.FechaReporte.HasValue && data._fechaReporte != default(DateTime))
                {
                    if (data._fechaReporte.Date <= row.FechaReporte.Value.Date.AddDays(-1))
                    {
                        return Json(new { success = false, message = "No se puede actualizar humedades 2 días anteriores o más del día actual." });
                    }
                }

                row.CantidadPIMS = data._cantidadPims;
                row.CantidadValidada = cantidadHumedad;

                _dvpEntities.SaveChanges();

                return Json(new
                {
                    success = true,
                    id = row.DataOperacionID,
                    message = "Humedad actualizada correctamente."
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = "Error al actualizar la humedad: This function can only be invoked from LINQ to Entities."
                });
            }
        }


        [HttpPost]
        public JsonResult AddEnergy(OperationDataToReportViewModel data)
        {
            try
            {
                if (data == null)
                    return Json(new { success = false, message = "Payload vacio." });

                if (!ModelState.IsValid)
                    return Json(new { success = false, message = "Datos invalidos." });


                if (data._fechaReporte.Date <= DateTime.Now.AddDays(-3))
                    return Json(new { success = false, message = "No se puede registrar 2 días anteriores o más del día actual." });

                if (data._equipoId <= 0)
                    return Json(new { success = false, message = "Equipo es obligatorio." });

                decimal? cantidadEnergia = data._cantidadValidada;
                if (cantidadEnergia == null)
                    return Json(new { success = false, message = "Cantidad es obligatoria." });

                DateTime fechaReporte = data._fechaReporte;

                var materialId = _dvpEntities.Material
                                             .Where(m => m.Descripcion.Contains("KWH"))
                                             .Select(m => m.MaterialID)
                                             .FirstOrDefault();


                var existe = _dvpEntities.DataOperacion.Any(x =>
                    x.MaterialID == materialId &&
                    x.TipoOperacionID == HUMEDAD &&
                    DbFunctions.TruncateTime(x.FechaReporte) == fechaReporte
                );


                if (existe)
                    return Json(new { success = false, message = "Ya existe un registro de energía para este equipo en esta fecha." });

                var nuevo = new DataOperacion
                {
                    EquipoID = data._equipoId,
                    TipoOperacionID = HUMEDAD,
                    MaterialID = data._materialId,
                    CantidadPIMS = data._cantidadPims,
                    CantidadValidada = cantidadEnergia,
                    UnidadMedidaID = UNIDAD_MEDIDA_KWH,
                    TipoMovimientoSAPID = TIPO_MOV_SAP_NA,
                    FechaReporte = fechaReporte,
                    StatusClose = false,
                    StatusValidate = true
                };

                _dvpEntities.DataOperacion.Add(nuevo);
                _dvpEntities.SaveChanges();

                return Json(new
                {
                    success = true,
                    id = nuevo.DataOperacionID,
                    message = "Humedad registrada correctamente."
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = "Error al registrar la humedad: " + ex.Message
                });
            }
        }



        public const int HUMEDAD = 11;
        public const int TIPO_MOV_SAP_NA = 3;
        public const int UNIDAD_MEDIDA_HUMEDAD = 7;
        public const int UNIDAD_MEDIDA_KWH = 2;



    }
}