using DataAccess;
using DVP.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity;
using System.Globalization;
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
                    StatusValidate = false
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


        [HttpGet]
        public JsonResult GetEnergy(DateTime? _fecha, int? _equipoId = null)
        {
            // Obtener la fecha de inicio del día
            var day = (_fecha ?? DateTime.Today).Date;

            // 1. Obtener TODOS los equipos que el usuario logueado puede ver
            var todosLosEquipos = _dvpEntities.Equipo
                .Where(e => e.PlantaID == 1) // Asume que tienes este filtro de seguridad
                .Select(e => new { EquipoID = e.EquipoID, Descripcion = e.Descripcion })
                .ToList();

            // 2. Obtener la data de energía (KWH) para el día
            var dataEnergia = _dvpEntities.DataOperacion
                .Where(d => d.TipoOperacionID == KWH_OPERATION
                         && DbFunctions.TruncateTime(d.FechaReporte) == day)
                .ToList();

            // 3. Aplicar el filtro de equipo si fue seleccionado
            if (_equipoId.HasValue && _equipoId.Value > 0)
            {
                int eqId = _equipoId.Value;
                todosLosEquipos = todosLosEquipos.Where(e => e.EquipoID == eqId).ToList();
            }

            // 4. Hacer el LEFT JOIN (Entity Framework LINQ Query)
            var capturas = todosLosEquipos
                .GroupJoin(
                    dataEnergia,
                    equipo => equipo.EquipoID,
                    data => data.EquipoID,
                    (equipo, dataGroup) => new
                    {
                        EquipoID = equipo.EquipoID,
                        Equipo = equipo.Descripcion,
                        // Si hay datos, toma el primero. Si no, usa NULL o el valor predeterminado (0).
                        Data = dataGroup.FirstOrDefault()
                    }
                )
                .OrderBy(x => x.Equipo)
                .Select(x => new
                {
                    DataOperacionID = x.Data?.DataOperacionID,
                    EquipoID = x.EquipoID,
                    Equipo = x.Equipo,

                    // Si hay Data, toma el material, si no, pon "KWH" (o busca el material ID de KWH)
                    Material = x.Data?.Material?.Descripcion ?? "KWH",

                    // Si hay Data, toma la Cantidad. Si no, pon 0.0
                    CantidadPIMS = x.Data?.CantidadPIMS ?? 0.0m,
                    CantidadPims = x.Data?.CantidadPIMS ?? 0.0m,
                    CantidadValidada = x.Data?.CantidadValidada ?? 0.0m,

                    UnidadMedidaID = x.Data?.UnidadMedidaID,
                    UnidadMedida = x.Data?.UnidadMedida?.Descripcion ?? "KWH",
                    TipoMovimientoSAPID = x.Data?.TipoMovimientoSAPID,
                    TipoMovimientoSAP = x.Data?.TipoMovimientoSAP?.Descripcion,
                    TipoMovimientoSAPDescripcion = x.Data?.TipoMovimientoSAP?.Descripcion,

                    // La fecha de reporte será la fecha filtrada (day), o la fecha real si existe
                    FechaReporte = x.Data?.FechaReporte ?? day,
                    StatusClose = x.Data?.StatusClose ?? false,
                    StatusValidate = x.Data?.StatusValidate ?? false,
                    OrdenProcesoSAP = x.Data?.OrdenProcesoSAP
                })
                .ToList();

            return Json(capturas, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult UpdateEnergy(OperationDataToReportViewModel data)
        {
            try
            {
                if (data._statusClose == true)
                    return Json(new { success = false, message = "No se pueden modificar campos ya cerrados." });

                if (data._fechaReporte.Date >= DateTime.Now.Date)
                    return Json(new { success = false, message = "No se puede registrar en día mayor o igual del dia actual." });

                // 1. Validaciones Iniciales
                if (data == null || !data._cantidadValidada.HasValue)
                    return Json(new { success = false, message = "Cantidad es obligatoria." });

                // Determinar el MaterialID para KWH
                var materialIdKwh = _dvpEntities.Material
                                                 .Where(m => m.Descripcion.Contains("KWH"))
                                                 .Select(m => m.MaterialID)
                                                 .FirstOrDefault();

                if (materialIdKwh <= 0)
                    return Json(new { success = false, message = "Error: No se pudo determinar el Material ID para KWH." });


                bool existeDatos = _dvpEntities.DataOperacion
                                   .Any(e => e.FechaReporte == data._fechaReporte.Date &&
                                   e.StatusClose == true &&
                                   e.TipoOperacionID == KWH_OPERATION);

                if (existeDatos)
                    return Json(new { success = false, message = $"No se puede agregar data para esta fecha {data._fechaReporte.ToShortDateString()} debido a que el reporte esta cerrado" });

                // ----------------------------------------------------------------------------------
                // === LÓGICA DE ACTUALIZACIÓN (Si DataOperacionID > 0) ===
                // ----------------------------------------------------------------------------------
                if (data._dataOperacionId > 0)
                {
                    var registroAActualizar = _dvpEntities.DataOperacion
                                                           .FirstOrDefault(d => d.DataOperacionID == data._dataOperacionId);

                    if (registroAActualizar == null)
                    {
                        return Json(new { success = false, message = "Registro de energía no encontrado para actualizar." });
                    }

                    // Validaciones y Actualización
                    if (registroAActualizar.FechaReporte.Value.Date <= DateTime.Now.Date.AddDays(-3))
                        return Json(new { success = false, message = "No se puede modificar un registro antiguo." });

                    if (registroAActualizar.MaterialID != materialIdKwh)
                        return Json(new { success = false, message = "El registro no corresponde a KWH." });

                    registroAActualizar.CantidadValidada = data._cantidadValidada.Value;
                    registroAActualizar.StatusValidate = true;
                    _dvpEntities.SaveChanges();

                    return Json(new { success = true, id = registroAActualizar.DataOperacionID, message = "Energía (KWH) actualizada." });
                }


                // ----------------------------------------------------------------------------------
                // === LÓGICA DE CREACIÓN (Si DataOperacionID es 0 o nulo) ===
                // ----------------------------------------------------------------------------------


                // Caso 2b: No existe, se crea
                var nuevo = new DataOperacion
                {
                    EquipoID = data._equipoId,
                    TipoOperacionID = KWH_OPERATION,
                    MaterialID = materialIdKwh,
                    CantidadPIMS = data._cantidadPims,
                    CantidadValidada = data._cantidadValidada,
                    UnidadMedidaID = UNIDAD_MEDIDA_KWH,
                    TipoMovimientoSAPID = PROD_MOV_SAP_ID,
                    FechaReporte = data._fechaReporte,
                    StatusClose = false,
                    StatusValidate = true
                };

                _dvpEntities.DataOperacion.Add(nuevo);
                _dvpEntities.SaveChanges();

                return Json(new { success = true, id = nuevo.DataOperacionID, message = "Nuevo registro de Energía (KWH) creado." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error en la operación de Upsert de energía: " + ex.Message });
            }
        }


        [HttpPost]
        public JsonResult CerrarReporteEnergia(OperationDataToReportViewModel data)
        {
            try
            {
                if (data._fechaReporte == default(DateTime))
                {
                    return Json(new { success = false, message = "Debe proporcionar una fecha de reporte válida." });
                }

                var fechaReporte = data._fechaReporte.Date; 

                var query = _dvpEntities.DataOperacion
                       .Where(e => e.FechaReporte == fechaReporte &&
                       e.StatusClose == false && 
                       e.StatusValidate == true && 
                       e.TipoOperacionID == KWH_OPERATION);

                var energylist = query.ToList();

                if (!energylist.Any())
                {
                    return Json(new { success = false, message = $"No se encontraron reportes de energía abiertos y validados para la fecha {data._fechaReporte.ToShortDateString()}." });
                }

                foreach (var energy in energylist)
                {
                    energy.StatusClose = true;
                }

                _dvpEntities.SaveChanges();

                return Json(new { success = true, message = $"Se cerraron exitosamente {energylist.Count} reportes validados." });

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return Json(new { success = false, message = "Error en la operación de cierre de reporte: " + ex.Message });
            }
        }


        [HttpGet]
        public JsonResult GetOperation(DateTime? _fecha, int? _equipoId = null)
        {
            var day = (_fecha ?? DateTime.Today).Date;

            // ===== 1) Equipos =====
            var queryEquipos = _dvpEntities.Equipo.Where(e => e.PlantaID == 1);
            if (_equipoId.HasValue && _equipoId.Value > 0)
                queryEquipos = queryEquipos.Where(e => e.EquipoID == _equipoId.Value);

            var equiposSeleccionados = queryEquipos
                .Select(e => new { e.EquipoID, EquipoDescripcion = e.Descripcion })
                .ToList();

            var equipoIdsSeleccionados = equiposSeleccionados.Select(e => e.EquipoID).ToList();

            // ===== 2) BOM =====
            var queryBillOfMaterial = _dvpEntities.BillOfMaterial
                .Where(b => equipoIdsSeleccionados.Contains(b.EquipoID.Value));

            // Produccion
            var produccionHeaders = queryBillOfMaterial
                .Where(b => b.MaterialProduccionID.HasValue)
                .GroupBy(b => new { b.EquipoID, b.MaterialProduccionID, b.Material1.Descripcion })
                .Select(g => new
                {
                    EquipoID = g.Key.EquipoID.Value,
                    MaterialID = g.Key.MaterialProduccionID.Value,
                    MaterialDescripcion = g.Key.Descripcion,
                    TipoMaterial = "PRODUCCION",
                    MaterialProduccionPadreID = (int?)null
                });

            // Consumo con padre (CONSUMO)
            var consumoDetails = queryBillOfMaterial
                .Where(b => b.MaterialConsumoID.HasValue && b.MaterialProduccionID.HasValue)
                .Select(b => new
                {
                    EquipoID = b.EquipoID.Value,
                    MaterialID = b.MaterialConsumoID.Value,
                    MaterialDescripcion = b.Material.Descripcion,
                    TipoMaterial = "CONSUMO",
                    MaterialProduccionPadreID = b.MaterialProduccionID
                });

            // Consumo para equipo (CONSUMO_EQUIPO)
            var consumoParaEquipo = queryBillOfMaterial
                .Where(b => b.MaterialConsumoID.HasValue && !b.MaterialProduccionID.HasValue)
                .Select(b => new
                {
                    EquipoID = b.EquipoID.Value,
                    MaterialID = b.MaterialConsumoID.Value,
                    MaterialDescripcion = b.Material.Descripcion,
                    TipoMaterial = "CONSUMO_EQUIPO",
                    MaterialProduccionPadreID = (int?)null
                });

            // ===== 3) Union =====
            var combinacionesMaterialesUnicas = produccionHeaders
                .Union(consumoDetails)
                .Union(consumoParaEquipo)
                .ToList();

            // ===== 4) Equipo x Material =====
            var combinacionesRequeridas = (
                from equipo in equiposSeleccionados
                join material in combinacionesMaterialesUnicas on equipo.EquipoID equals material.EquipoID
                select new
                {
                    equipo.EquipoID,
                    EquipoDescripcion = equipo.EquipoDescripcion,
                    material.MaterialID,
                    material.MaterialDescripcion,
                    material.TipoMaterial,
                    material.MaterialProduccionPadreID
                }
            ).ToList();

            var materialIdsRequeridos = combinacionesRequeridas.Select(c => c.MaterialID).ToList();

            // ===== 5) DataOperacion principal =====
            var datosOperacionReales = _dvpEntities.DataOperacion
                .Include("Material.ClasificacionMaterial")
                .Include("UnidadMedida")
                .Include("TipoMovimientoSAP")
                .Where(d => DbFunctions.TruncateTime(d.FechaReporte) == day)
                .Where(d => d.EquipoID.HasValue && equipoIdsSeleccionados.Contains(d.EquipoID.Value))
                .Where(d => d.MaterialID.HasValue && materialIdsRequeridos.Contains(d.MaterialID.Value))
                .ToList();

            // ===== 6) Humedad (TipoOperacionID = HUMEDAD) =====
            int humedadId = _dvpEntities.TipoOperacion
                .Where(t => t.Descripcion.ToUpper() == "HUMEDAD")
                .Select(t => t.TipoOperacionID)
                .FirstOrDefault();

            var datosHumedad = _dvpEntities.DataOperacion
                .Where(d => DbFunctions.TruncateTime(d.FechaReporte) == day)
                .Where(d => d.EquipoID.HasValue && equipoIdsSeleccionados.Contains(d.EquipoID.Value))
                .Where(d => d.MaterialID.HasValue && materialIdsRequeridos.Contains(d.MaterialID.Value))
                .Where(d => d.TipoOperacionID == humedadId)
                .Select(d => new
                {
                    d.EquipoID,
                    d.MaterialID,
                    d.MaterialProducidoID,
                    Humedad = d.CantidadValidada ?? d.CantidadPIMS ?? 0.0m
                })
                .ToList();

            // ===== 7) JOIN combinando con MaterialProducidoID =====
            var resultados = combinacionesRequeridas
                .Select(comb =>
                {
                    var data = datosOperacionReales.FirstOrDefault(d =>
                        d.EquipoID == comb.EquipoID &&
                        d.MaterialID == comb.MaterialID &&
                        (
                            (comb.MaterialProduccionPadreID != null && d.MaterialProducidoID == comb.MaterialProduccionPadreID)
                            ||
                            (comb.MaterialProduccionPadreID == null)
                        ));

                    var hum = datosHumedad.FirstOrDefault(h =>
                        h.EquipoID == comb.EquipoID &&
                        h.MaterialID == comb.MaterialID &&
                        (
                            (comb.MaterialProduccionPadreID != null && h.MaterialProducidoID == comb.MaterialProduccionPadreID)
                            ||
                            (comb.MaterialProduccionPadreID == null)
                        ));

                    return new
                    {
                        EquipoID = comb.EquipoID,
                        Equipo = comb.EquipoDescripcion,
                        MaterialID = comb.MaterialID,
                        Material = comb.MaterialDescripcion,
                        TipoMaterial = comb.TipoMaterial,
                        MaterialProduccionPadreID = comb.MaterialProduccionPadreID,
                        DataOperacionID = data?.DataOperacionID,
                        CantidadPIMS = data?.CantidadPIMS ?? 0.0m,
                        CantidadValidada = data?.CantidadValidada ?? 0.0m,
                        UnidadMedida = data?.UnidadMedida?.Descripcion ?? "N/A",
                        TipoMovimientoSAPID = data?.TipoMovimientoSAPID,
                        TipoMovimientoSAP = data?.TipoMovimientoSAP?.Descripcion,
                        FechaReporte = data?.FechaReporte ?? day,
                        StatusClose = data?.StatusClose ?? false,
                        StatusValidate = data?.StatusValidate ?? false,
                        OrdenProcesoSAP = data?.OrdenProcesoSAP,
                        Clasificacion = data?.Material?.ClasificacionMaterial?.Descripcion ?? "N/A",
                        Humedad = hum?.Humedad ?? 0.0m
                    };
                })
                // ===== 8) Ordenamiento =====
                .OrderBy(x => x.Equipo)
                .ThenBy(x =>
                    x.TipoMaterial == "PRODUCCION" ? 1 :
                    x.TipoMaterial == "CONSUMO" ? 2 :
                    x.TipoMaterial == "CONSUMO_EQUIPO" ? 3 : 4
                )
                .ThenBy(x =>
                    x.Clasificacion == "MATERIAL" ? 1 :
                    x.Clasificacion == "COMBUSTIBLE" ? 2 :
                    x.Clasificacion == "HORAS" ? 3 :
                    x.Clasificacion == "KWH" ? 4 :
                    x.Clasificacion == "SACOS" ? 5 : 6
                )
                .ThenBy(x => x.Material)
                .ToList();

            return Json(resultados, JsonRequestBehavior.AllowGet);
        }

        public static string RunPythonScriptKWH(string fecha = null)
        {
            var exePath = ConfigurationManager.AppSettings["PythonExePathKWHdo"];
            var scriptPath = ConfigurationManager.AppSettings["PythonScriptPathKWHdo"];

            if (string.IsNullOrEmpty(fecha))
                fecha = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");

            var start = new System.Diagnostics.ProcessStartInfo
            {
                FileName = exePath,
                Arguments = $"\"{scriptPath}\" {fecha}",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                StandardOutputEncoding = System.Text.Encoding.UTF8,
                StandardErrorEncoding = System.Text.Encoding.UTF8,
                WorkingDirectory = System.IO.Path.GetDirectoryName(scriptPath)
            };

            using (var process = System.Diagnostics.Process.Start(start))
            {
                string stdout = process.StandardOutput.ReadToEnd();
                string stderr = process.StandardError.ReadToEnd();
                process.WaitForExit();

                if (process.ExitCode != 0)
                    stdout += $"\nEXIT CODE: {process.ExitCode}";

                if (!string.IsNullOrEmpty(stderr))
                    stdout += "\nERROR: " + stderr;

                return stdout;
            }
        }


        [HttpGet]
        public JsonResult EjecutarScriptKWH(string fecha = null)
        {
            try
            {
                // validar fecha si viene
                if (!string.IsNullOrWhiteSpace(fecha))
                {
                    if (!DateTime.TryParseExact(
                            fecha,
                            "yyyy-MM-dd",
                            CultureInfo.InvariantCulture,
                            DateTimeStyles.None,
                            out var fechaDt))
                    {
                        return Json(new
                        {
                            success = false,
                            message = "Formato de fecha invalido. Use yyyy-MM-dd."
                        }, JsonRequestBehavior.AllowGet);
                    }

                    // misma logica que EjecutarScriptProd: bloquear hoy y futuro
                    if (fechaDt.Date > DateTime.Today.AddDays(-1))
                    {
                        return Json(new
                        {
                            success = false,
                            message = "No se puede ejecutar el script para fechas posteriores al dia de ayer."
                        }, JsonRequestBehavior.AllowGet);
                    }
                }

                // si no viene, usar hoy en el formato esperado
                var fechaArg = string.IsNullOrWhiteSpace(fecha)
                    ? DateTime.Today.ToString("yyyy-MM-dd")
                    : fecha;

                string resultado = RunPythonScriptKWH(fechaArg);

                return Json(new { success = true, output = resultado }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                // devolver el error que envia el catch
                Response.StatusCode = 500;
                return Json(new
                {
                    success = false,
                    error = ex.Message,
                    message = ex.Message,
                    detail = ex.ToString()
                }, JsonRequestBehavior.AllowGet);
            }
        }



        public static string RunPythonScriptProd(string fecha = null)
        {
            var exePath = ConfigurationManager.AppSettings["PythonExePathProdDO"];
            var scriptPath = ConfigurationManager.AppSettings["PythonScriptPathProdDO"];

            if (string.IsNullOrEmpty(fecha))
                fecha = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");

            var start = new System.Diagnostics.ProcessStartInfo
            {
                FileName = exePath,
                Arguments = $"\"{scriptPath}\" {fecha}",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                StandardOutputEncoding = System.Text.Encoding.UTF8,
                StandardErrorEncoding = System.Text.Encoding.UTF8,
                WorkingDirectory = System.IO.Path.GetDirectoryName(scriptPath)
            };

            using (var process = System.Diagnostics.Process.Start(start))
            {
                string stdout = process.StandardOutput.ReadToEnd();
                string stderr = process.StandardError.ReadToEnd();
                process.WaitForExit();

                if (process.ExitCode != 0)
                    stdout += $"\nEXIT CODE: {process.ExitCode}";

                if (!string.IsNullOrEmpty(stderr))
                    stdout += "\nERROR: " + stderr;

                return stdout;
            }
        }


        [HttpGet]
        public JsonResult EjecutarScriptProd(string fecha = null)
        {
            try
            {
                // validar fecha si viene
                if (!string.IsNullOrWhiteSpace(fecha))
                {
                    if (!DateTime.TryParseExact(fecha, "yyyy-MM-dd",
                                                CultureInfo.InvariantCulture,
                                                DateTimeStyles.None,
                                                out var fechaDt))
                    {
                        return Json(new { success = false, message = "Formato de fecha invalido. Use yyyy-MM-dd." },
                                    JsonRequestBehavior.AllowGet);
                    }

                    if (fechaDt.Date > DateTime.Today.AddDays(-1))
                    {
                        return Json(new { success = false, message = "No se puede ejecutar el script para fechas posteriores al dia de ayer." },
                                    JsonRequestBehavior.AllowGet);
                    }
                }

                // si no viene, usar hoy en el formato esperado
                var fechaArg = string.IsNullOrWhiteSpace(fecha)
                    ? DateTime.Today.ToString("yyyy-MM-dd")
                    : fecha;

                string resultado = RunPythonScriptProd(fechaArg);

                return Json(new { success = true, output = resultado }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                
                Response.StatusCode = 500;

                return Json(new
                {
                    success = false,
                    error = ex.Message,      
                    message = ex.Message,    
                    detail = ex.ToString()   
                }, JsonRequestBehavior.AllowGet);
            }
        }



        public const int HUMEDAD = 11;
        public const int KWH_OPERATION = 4;
        public const int TIPO_MOV_SAP_NA = 3;
        public const int UNIDAD_MEDIDA_HUMEDAD = 7;
        public const int UNIDAD_MEDIDA_KWH = 2;
        public const int PROD_MOV_SAP_ID = 1;
        public const int CONS_MOV_SAP_ID = 2;





    }
}