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
                        // Materiales válidos del BOM para ese equipo/tipo
                        var materialIds = matsQ.Distinct().ToList();

                       
                        if (materialIds.Count > 0)
                        {
                            query = query.Where(d => d.MaterialID.HasValue && materialIds.Contains(d.MaterialID.Value));
                        }
                    }
                }
            }

            // Proyección final
            var capturas = query
                .OrderByDescending(d => d.FechaReporte)
                .Select(d => new
                {
                    DataOperacionID = d.DataOperacionID,
                    EquipoID = d.EquipoID,
                    Equipo = d.Equipo != null ? d.Equipo.Descripcion : null,

                    
                    MaterialID = d.MaterialID,
                    Material = d.Material != null ? d.Material.Descripcion : null,

                    CantidadPIMS = d.CantidadPIMS,
                    CantidadValidada = d.CantidadValidada,
                    UnidadMedidaID = d.UnidadMedidaID,
                    UnidadMedida = d.UnidadMedida != null ? d.UnidadMedida.Descripcion : null,
                    TipoMovimientoSAPID = d.TipoMovimientoSAPID,
                    FechaReporte = d.FechaReporte,
                    StatusClose = d.StatusClose,
                    StatusValidate = d.StatusValidate,
                    OrdenProcesoSAP = d.OrdenProcesoSAP
                })
                .ToList();

            return Json(capturas, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult CrearCapturaHumedad(OperationDataToReportViewModel data)
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
        public JsonResult UpdateCapturaHumedad(OperationDataToReportViewModel data)
        {
            try
            {
                if (data == null)
                    return Json(new { success = false, message = "Payload vacio." });

                if (!ModelState.IsValid)
                    return Json(new { success = false, message = "Datos invalidos." });

                if (data._equipoId <= 0)
                    return Json(new { success = false, message = "Equipo es obligatorio." });

                decimal? cantidadHumedad = data._cantidadValidada;
                if (cantidadHumedad == null)
                    return Json(new { success = false, message = "Cantidad de humedad es obligatoria." });

                if (data._unidadMedidaId <= 0)
                    return Json(new { success = false, message = "Unidad de medida es obligatoria." });

              
                DataOperacion row = null;

                if (data._dataOperacionId > 0)
                {
                    row = _dvpEntities.DataOperacion
                            .FirstOrDefault(x => x.DataOperacionID == data._dataOperacionId && x.TipoOperacionID == HUMEDAD);
                }

                if (row == null)
                {
                    
                    var fechaBusqueda = (data._fechaReporte != default(DateTime)) ? data._fechaReporte : DateTime.Now;

                    row = _dvpEntities.DataOperacion.FirstOrDefault(x =>
                        x.EquipoID == data._equipoId &&
                        x.TipoOperacionID == HUMEDAD &&
                        DbFunctions.TruncateTime(x.FechaReporte) == DbFunctions.TruncateTime((DateTime?)fechaBusqueda)
                    );
                }

                if (row == null)
                    return Json(new { success = false, message = "No existe registro de humedad para actualizar." });

                
                var quiereCambiarFecha = (data._fechaReporte != default(DateTime));
                if (quiereCambiarFecha)
                {
                    var nuevaFecha = data._fechaReporte;

                    bool existeMismaFecha = _dvpEntities.DataOperacion.Any(x =>
                        x.DataOperacionID != row.DataOperacionID &&
                        x.EquipoID == data._equipoId &&
                        x.TipoOperacionID == HUMEDAD &&
                        DbFunctions.TruncateTime(x.FechaReporte) == DbFunctions.TruncateTime((DateTime?)nuevaFecha)
                    );

                    if (existeMismaFecha)
                        return Json(new { success = false, message = "Ya existe un registro de humedad para esa fecha." });

                    row.FechaReporte = nuevaFecha;
                }

               
                row.EquipoID = data._equipoId;                 
                row.MaterialID = data._materialId;            
                row.CantidadPIMS = data._cantidadPims;
                row.CantidadValidada = cantidadHumedad;
                row.UnidadMedidaID = data._unidadMedidaId;
                row.TipoMovimientoSAPID = data._tipoMovimientoSapId; 
                row.OrdenProcesoSAP = data._ordenProcesoSAP;

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
                    message = "Error al actualizar la humedad: " + ex.Message
                });
            }
        }



        public const int HUMEDAD = 11;
        public const int TIPO_MOV_SAP_NA = 3;
        public const int UNIDAD_MEDIDA_HUMEDAD = 7;


    }
}