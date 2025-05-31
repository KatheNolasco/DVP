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

namespace DVP.Controllers
{
    public class DowntimeController : Controller
    {
        DataAccess.DVPEntities _dvpEntities = new DataAccess.DVPEntities();

        // GET: Downtime
        public ActionResult Index()
        {
            DowntimeViewModel viewModel = new DowntimeViewModel
            {
                Equipos = new DowntimeViewModel().GetEquipos().ToList()
            };

            return View(viewModel);

        }

        [HttpGet]
        public JsonResult GetSubEquipos(int _equipoId)
        {
            if (_equipoId <= 0)
            {
                return Json(new { error = "ID de equipo no válido." }, JsonRequestBehavior.AllowGet);
            }

            var subEquipos = _dvpEntities.SubEquipo
                                     .Where(s => s.EquipoID == _equipoId)
                                     .Select(s => new
                                     {
                                         SubEquipoID = s.SubEquipoID,
                                         Descripcion = s.Descripcion
                                     })
                                     .ToList();

            return Json(subEquipos, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult GetComponenteEquipo(int _subEquipoId)
        {
            if (_subEquipoId <= 0)
            {
                return Json(new { error = "ID de equipo no válido." }, JsonRequestBehavior.AllowGet);
            }

            var subEquipos = _dvpEntities.ComponenteEquipo
                                     .Where(s => s.SubEquipoID == _subEquipoId)
                                     .Select(s => new
                                     {
                                         ComponenteEquipoID = s.ComponenteEquipoID,
                                         Descripcion = s.Descripcion
                                     })
                                     .ToList();

            return Json(subEquipos, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult GetTiposFalla(int _clasificacionId, int _componenteEquipoId)
        {
            if (_clasificacionId <= 0 && _componenteEquipoId <= 0)
            {
                return Json(new { error = "ID de equipo no válido." }, JsonRequestBehavior.AllowGet);
            }

            var subEquipos = _dvpEntities.TipoFalla
                                     .Where(s => s.ClasificacionID == _clasificacionId && s.ComponenteEquipoID == _componenteEquipoId)
                                     .Select(s => new
                                     {
                                         TipoFallaID = s.TipoFallaID,
                                         Descripcion = s.Descripcion
                                     })
                                     .ToList();

            return Json(subEquipos, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult GetParoById(int paroId)
        {
            var paro = _dvpEntities.Paros
                .Where(p => p.ParosID == paroId)
                .Select(p => new
                {
                    _paroId = p.ParosID,
                    _equipoId = p.EquipoID,
                    _tipoEventoId = p.TipoEventoID,
                    _subEquipoId = p.SubEquipoID,
                    _componenteEquipoId = p.ComponenteEquipoID,
                    _tipoFallaId = p.TipoFallaID,
                    _clasificacionId = p.ClasificacionID,
                    _comment = p.Comentario,
                    _fechaEvento = p.FechaEvento,
                    _statusValidate = p.StatusValidate,
                    _statusDelete = p.StatusDelete,
                })
                .FirstOrDefault();

            if (paro == null)
            {
                return Json(new { success = false, message = "Paro no encontrado" }, JsonRequestBehavior.AllowGet);
            }

            return Json(new { success = true, data = paro }, JsonRequestBehavior.AllowGet);
        }


        [HttpPost]
        public JsonResult CreateDowntime(DowntimeViewModel data)
        {
            if (data == null)
            {
                return Json(new { success = false, message = "Datos inválidos." });
            }

            try
            {
                DateTime fechaEvento = data._fechaEvento.Date;
                DateTime hoy = DateTime.Now.Date;
                DateTime fechaCierreAutomatica = new DateTime(2025, 5, 27);


                // Verificar si la fecha del evento ya está cerrada en CierreStatus
                bool fechaCerrada = _dvpEntities.CierreStatus.Any(p =>
                    p.FechaReporte == fechaEvento
                );

                DateTime ultimoDiaMes = new DateTime(hoy.Year, hoy.Month, DateTime.DaysInMonth(hoy.Year, hoy.Month));
                DateTime inicioUltimosTresDias = ultimoDiaMes.AddDays(-2);
                bool estamosenUltimosTresDias = (ultimoDiaMes - hoy).TotalDays <= 2;
                bool estafechalaEventoEnUltimosTresDias = fechaEvento >= inicioUltimosTresDias && fechaEvento <= ultimoDiaMes;

                if (fechaCerrada == true || fechaEvento < fechaCierreAutomatica)
                {
                    return Json(new
                    {
                        success = false,
                        message = $"La fecha {fechaEvento:dd/MM/yyyy} ya está cerrada y no se pueden registrar nuevos paros."
                    });
                }
                else
                {
                    if (estamosenUltimosTresDias && !estafechalaEventoEnUltimosTresDias || !estamosenUltimosTresDias && estafechalaEventoEnUltimosTresDias)
                    {
                        return Json(new
                        {
                            success = false,
                            message = $"Solo se pueden registrar a futuro los tres ultimos días del mes para fines de proyección de cierre."
                        });
                    }
                    else
                    {
                        // Verificar si ya existe un paro con los mismos datos
                        bool existeParo = _dvpEntities.Paros.Any(p =>
                            p.EquipoID == data._equipoId &&
                            p.FechaEvento == data._fechaEvento &&
                            p.TipoEventoID == data._tipoEventoId
                        );

                        if (existeParo)
                        {
                            return Json(new { success = false, message = "Este paro ya existe en la base de datos." });
                        }

                        // Crear nuevo paro
                        var nuevoParo = new Paros
                        {
                            EquipoID = data._equipoId,
                            TipoEventoID = data._tipoEventoId,
                            SubEquipoID = data._subEquipoId,
                            ComponenteEquipoID = data._componenteEquipoId,
                            ClasificacionID = data._clasificacionId,
                            TipoFallaID = data._tipoFallaId,
                            Comentario = data._comment,
                            FechaEvento = data._fechaEvento,
                            FechaCreacion = DateTime.Now,
                            StatusValidate = true,
                            StatusDelete = false,
                        };

                        _dvpEntities.Paros.Add(nuevoParo);
                        _dvpEntities.SaveChanges();

                        // Asignar ParoRelacionadoID
                        nuevoParo.ParoRelacionadoID = nuevoParo.ParosID;
                        _dvpEntities.SaveChanges();

                        return Json(new { success = true, paroId = nuevoParo.ParosID });
                    }
                }

            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult AddActiveEvent(DowntimeViewModel data)
        {
            if (data == null || data._paroId <= 0 || data._fechaEvento == null)
            {
                return Json(new { success = false, message = "Datos inválidos o ID no proporcionado." });
            }

            try
            {
                DateTime fechaEvento = data._fechaEvento.Date;
                DateTime hoy = DateTime.Now.Date;
                DateTime ultimoDiaMes = new DateTime(hoy.Year, hoy.Month, DateTime.DaysInMonth(hoy.Year, hoy.Month));
                DateTime inicioUltimosTresDias = ultimoDiaMes.AddDays(-2);
                bool estamosenUltimosTresDias = (ultimoDiaMes - hoy).TotalDays <= 2;
                bool estafechalaEventoEnUltimosTresDias = fechaEvento >= inicioUltimosTresDias && fechaEvento <= ultimoDiaMes;
                DateTime fechaCierreAutomatica = new DateTime(2025, 5, 27);


                // Verificar si la fecha del evento ya está cerrada en CierreStatus
                bool fechaCerrada = _dvpEntities.CierreStatus.Any(p =>
                    p.FechaReporte == fechaEvento
                );

                var inactiveOrigen = _dvpEntities.Paros.FirstOrDefault(p => p.ParosID == data._paroId);

                if (inactiveOrigen == null)
                {
                    return Json(new { success = false, message = "No se encontró el paro original." });
                }

                if (inactiveOrigen.TipoEventoID != INACTIVE_EVENT)
                {
                    return Json(new { success = false, message = "Solo se puede crear un evento activo si el paro original es de tipo Inactive" });
                }

                // Validación: la fecha nueva no debe ser menor que la del paro original
                if (data._fechaEvento < inactiveOrigen.FechaEvento)
                {
                    return Json(new { success = false, message = "La fecha y hora no puede ser mas antigua a la del evento original." });
                }

                var validacion = VerificarEvento(inactiveOrigen.ParosID, ACTIVE_EVENT, data._fechaEvento);

                if (!validacion.Success)
                {
                    return Json(new { success = false, message = validacion.Message });
                }

                if (fechaCerrada == true || fechaEvento < fechaCierreAutomatica)
                {

                }
                else
                {
                    if (estamosenUltimosTresDias && !estafechalaEventoEnUltimosTresDias || !estamosenUltimosTresDias && estafechalaEventoEnUltimosTresDias)
                    {
                        return Json(new
                        {
                            success = false,
                            message = $"Solo se pueden registrar a futuro los tres ultimos días del mes para fines de proyección de cierre."
                        });
                    }
                }
                

                // Crear el nuevo evento
                var nuevoParo = new Paros
                {
                    EquipoID = inactiveOrigen.EquipoID,
                    TipoEventoID = ACTIVE_EVENT,
                    Comentario = data._comment,
                    FechaEvento = data._fechaEvento,
                    FechaCreacion = DateTime.Now,
                    StatusValidate = true,
                    StatusDelete = false,
                    ParoRelacionadoID = inactiveOrigen.ParosID
                };

                _dvpEntities.Paros.Add(nuevoParo);
                _dvpEntities.SaveChanges();

                return Json(new { success = true, paroId = nuevoParo.ParosID });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }


        [HttpPost]
        public JsonResult AddReclasificationEvent(DowntimeViewModel data)
        {
            if (data == null || data._paroId <= 0)
            {
                return Json(new { success = false, message = "Datos inválidos o ID no proporcionado." });
            }

            try
            {
                DateTime fechaEvento = data._fechaEvento.Date;
                DateTime hoy = DateTime.Now.Date;
                DateTime ultimoDiaMes = new DateTime(hoy.Year, hoy.Month, DateTime.DaysInMonth(hoy.Year, hoy.Month));
                DateTime inicioUltimosTresDias = ultimoDiaMes.AddDays(-2);
                bool estamosenUltimosTresDias = (ultimoDiaMes - hoy).TotalDays <= 2;
                bool estafechalaEventoEnUltimosTresDias = fechaEvento >= inicioUltimosTresDias && fechaEvento <= ultimoDiaMes;
                DateTime fechaCierreAutomatica = new DateTime(2025, 5, 27);


                // Verificar si la fecha del evento ya está cerrada en CierreStatus
                bool fechaCerrada = _dvpEntities.CierreStatus.Any(p =>
                    p.FechaReporte == fechaEvento
                );

                var inactiveOrigen = _dvpEntities.Paros.FirstOrDefault(p => p.ParosID == data._paroId);

                if (inactiveOrigen == null)
                {
                    return Json(new { success = false, message = "No se encontró el paro original." });
                }

                if (inactiveOrigen.TipoEventoID != INACTIVE_EVENT)
                {
                    return Json(new { success = false, message = "Se debe agregar una reclasificación al evento principal inactive" });
                }

                if (data._fechaEvento < inactiveOrigen.FechaEvento)
                {
                    return Json(new { success = false, message = "La fecha y hora no puede ser más antigua a la del evento original." });
                }

                var validacion = VerificarEvento(inactiveOrigen.ParosID, RECLASIFICATION_EVENT, data._fechaEvento);

                if (!validacion.Success)
                {
                    return Json(new { success = false, message = validacion.Message });
                }

                if (fechaCerrada == true || fechaEvento < fechaCierreAutomatica)
                {
                    return Json(new
                    {
                        success = false,
                        message = $"La fecha {fechaEvento:dd/MM/yyyy} ya está cerrada y no se pueden registrar nuevos paros."
                    });
                }
                else
                {
                    if (estamosenUltimosTresDias && !estafechalaEventoEnUltimosTresDias || !estamosenUltimosTresDias && estafechalaEventoEnUltimosTresDias)
                    {
                        return Json(new
                        {
                            success = false,
                            message = $"Solo se pueden registrar a futuro los tres ultimos días del mes para fines de proyección de cierre."
                        });
                    }
                }

                

                var nuevoParo = new Paros
                {
                    EquipoID = data._equipoId,
                    TipoEventoID = RECLASIFICATION_EVENT,
                    SubEquipoID = data._subEquipoId,
                    ComponenteEquipoID = data._componenteEquipoId,
                    ClasificacionID = data._clasificacionId,
                    TipoFallaID = data._tipoFallaId,
                    Comentario = data._comment,
                    FechaEvento = data._fechaEvento,
                    FechaCreacion = DateTime.Now,
                    StatusValidate = true,
                    StatusDelete = false,
                    ParoRelacionadoID = inactiveOrigen.ParosID,
                };

                _dvpEntities.Paros.Add(nuevoParo);
                _dvpEntities.SaveChanges();

                return Json(new { success = true, paroId = inactiveOrigen.ParosID });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }



        [HttpPost]
        public JsonResult AddDayDelayEvent(DowntimeViewModel data)
        {
            if (data == null || data._paroId <= 0)
            {
                return Json(new { success = false, message = "Datos inválidos o ID no proporcionado." });
            }

            try
            {
                DateTime hoy = DateTime.Now.Date;
                DateTime ultimoDiaMes = new DateTime(hoy.Year, hoy.Month, DateTime.DaysInMonth(hoy.Year, hoy.Month));
                DateTime inicioUltimosTresDias = ultimoDiaMes.AddDays(-2);
                bool estamosenUltimosTresDias = (ultimoDiaMes - hoy).TotalDays <= 2;
                bool estafechalaEventoEnUltimosTresDias = data._fechaEvento >= inicioUltimosTresDias && data._fechaEvento <= ultimoDiaMes;

                DateTime fechaEvento = data._fechaEvento.Date;
                DateTime fechaCierreAutomatica = new DateTime(2025, 5, 27);


                // Verificar si la fecha del evento ya está cerrada en CierreStatus
                bool fechaCerrada = _dvpEntities.CierreStatus.Any(p =>
                    p.FechaReporte == fechaEvento
                );

                var inactiveOrigen = _dvpEntities.Paros.FirstOrDefault(p => p.ParosID == data._paroId);
                if (inactiveOrigen == null)
                {
                    return Json(new { success = false, message = "No se encontró el paro original." });
                }

                if (inactiveOrigen.TipoEventoID != INACTIVE_EVENT)
                {
                    return Json(new { success = false, message = "Se debe agregar una day delay al evento principal inactive " });
                }

                // Validación: la fecha nueva no debe ser menor que la del paro original
                if (data._fechaEvento < inactiveOrigen.FechaEvento)
                {
                    return Json(new { success = false, message = "La fecha y hora no puede ser mas antigua a la del evento original." });
                }

                var validacion = VerificarEvento(inactiveOrigen.ParosID, DAY_DELAY_EVENT, data._fechaEvento);

                if (!validacion.Success)
                {
                    return Json(new { success = false, message = validacion.Message });
                }

                if (fechaCerrada == true || fechaEvento < fechaCierreAutomatica)
                {

                }
                else
                {
                    if (!estamosenUltimosTresDias && estafechalaEventoEnUltimosTresDias || data._fechaEvento.Date > hoy)
                    {
                        return Json(new
                        {
                            success = false,
                            message = $"Solo se pueden registrar a futuro los tres ultimos días del mes para fines de proyección de cierre."
                        });
                    }
                }

                

                // Crear el nuevo evento
                var nuevoParo = new Paros
                {
                    EquipoID = data._equipoId,
                    TipoEventoID = DAY_DELAY_EVENT,
                    SubEquipoID = data._subEquipoId,
                    ComponenteEquipoID = data._componenteEquipoId,
                    ClasificacionID = data._clasificacionId,
                    TipoFallaID = data._tipoFallaId,
                    Comentario = data._comment,
                    FechaEvento = data._fechaEvento,
                    FechaCreacion = DateTime.Now,
                    StatusValidate = true,
                    StatusDelete = false,
                    ParoRelacionadoID = inactiveOrigen.ParosID,
                };

                _dvpEntities.Paros.Add(nuevoParo);
                _dvpEntities.SaveChanges();

                return Json(new { success = true, paroId = inactiveOrigen.ParosID });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult UpdateDowntime(DowntimeViewModel data)
        {
            if (data == null || data._paroId <= 0)
            {
                return Json(new { success = false, message = "Datos inválidos o ID no proporcionado." });
            }

            try
            {
                var paroExistente = _dvpEntities.Paros.FirstOrDefault(p => p.ParosID == data._paroId);
                if (paroExistente == null)
                {
                    return Json(new { success = false, message = "No se encontró el paro original." });
                }

                // Validación: la fecha nueva no debe ser menor que la del paro original
                if (data._fechaEvento < paroExistente.FechaEvento || data._tipoEventoId == INACTIVE_EVENT)
                {
                    return Json(new { success = false, message = "La fecha y hora no puede ser mas antigua a la del evento original." });
                }


                // Actualizar campos
                paroExistente.EquipoID = data._equipoId;
                paroExistente.TipoEventoID = data._tipoEventoId;
                paroExistente.SubEquipoID = data._subEquipoId > 0 ? data._subEquipoId : (int?)null;
                paroExistente.ComponenteEquipoID = data._componenteEquipoId > 0 ? data._componenteEquipoId : (int?)null;
                paroExistente.ClasificacionID = data._clasificacionId > 0 ? data._clasificacionId : (int?)null;
                paroExistente.TipoFallaID = data._tipoFallaId > 0 ? data._tipoFallaId : (int?)null;
                paroExistente.Comentario = data._comment;
                paroExistente.FechaEvento = data._fechaEvento;
                paroExistente.FechaModificacion = DateTime.Now;
                paroExistente.StatusValidate = true;

                _dvpEntities.SaveChanges();

                return Json(new { success = true, paroId = paroExistente.ParosID });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult UpdatePending(DowntimeViewModel data)
        {
            if (data == null || data._paroId <= 0)
            {
                return Json(new { success = false, message = "Datos inválidos o ID no proporcionado." });
            }

            try
            {
                var paroExistente = _dvpEntities.Paros.FirstOrDefault(p => p.ParosID == data._paroId);

                if (paroExistente == null)
                {
                    return Json(new { success = false, message = "No se encontró el paro para actualizar." });
                }

                paroExistente.StatusValidate = false;

                _dvpEntities.SaveChanges();

                return Json(new { success = true, paroId = paroExistente.ParosID });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult DeleteDowntime(DowntimeViewModel data)
        {
            if (data == null || data._paroId <= 0)
            {
                return Json(new { success = false, message = "Datos inválidos o ID no proporcionado." });
            }

            try
            {
                var paroExistente = _dvpEntities.Paros.FirstOrDefault(p => p.ParosID == data._paroId);

                if (paroExistente == null)
                {
                    return Json(new { success = false, message = "No se encontró el paro para actualizar." });
                }

                paroExistente.StatusDelete = true;

                _dvpEntities.SaveChanges();

                return Json(new { success = true, paroId = paroExistente.ParosID });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public JsonResult GetDowntimesByDate(DateTime fecha)
        {
            var downtimes = _dvpEntities.Paros
                 .Where(p => DbFunctions.TruncateTime(p.FechaEvento) == fecha.Date && p.TipoEventoID == INACTIVE_EVENT)
                 .Select(choose => new
                 {
                     _paroId = choose.ParosID,
                     _fechaCreacionParo = choose.FechaCreacion,
                     _fechaEvento = choose.FechaEvento,
                     _comment = choose.Comentario,
                     _equipoId = choose.EquipoID,
                     _equipoName = choose.Equipo.Descripcion,
                     _componenteEquipoName = choose.ComponenteEquipo.Descripcion,
                     _tipoFallaName = choose.TipoFalla.Descripcion,
                     _clasificacionName = choose.Clasificacion.Descripcion,
                     _statusValidate = choose.StatusValidate,
                     _statusDelete = choose.StatusDelete,
                     _tipoEventoId = choose.TipoEventoID,
                     _tipoEventoName = choose.TipoEvento.Descripcion,
                     _paroRelacionadoId = choose.ParoRelacionadoID
                 })
                 .ToList();

            return Json(downtimes, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult GetDowntimesByInactive(int paroId)
        {
            var paro = _dvpEntities.Paros.FirstOrDefault(p => p.ParosID == paroId);

            if (paro == null || paro.ParoRelacionadoID == null)
            {
                return Json(new List<object>(), JsonRequestBehavior.AllowGet); 
            }

            var paroRelacionadoId = paro.ParoRelacionadoID;

            var downtimes = _dvpEntities.Paros
                .Where(p => p.ParoRelacionadoID == paroRelacionadoId)
                .Select(choose => new
                {
                    _paroId = choose.ParosID,
                    _fechaCreacionParo = choose.FechaCreacion,
                    _fechaEvento = choose.FechaEvento,
                    _comment = choose.Comentario,
                    _equipoId = choose.EquipoID,
                    _equipoName = choose.Equipo.Descripcion,
                    _componenteEquipoName = choose.ComponenteEquipo.Descripcion,
                    _tipoFallaName = choose.TipoFalla.Descripcion,
                    _clasificacionName = choose.Clasificacion.Descripcion,
                    _statusValidate = choose.StatusValidate,
                    _statusDelete = choose.StatusDelete,
                    _tipoEventoId = choose.TipoEventoID,
                    _tipoEventoName = choose.TipoEvento.Descripcion,
                    _paroRelacionadoId = choose.ParoRelacionadoID
                })
                .ToList();

            return Json(downtimes, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult CerrarReporte(DateTime fecha)
        {
            try
            {
                if (fecha == default(DateTime))
                {
                    return Json(new { success = false, message = "Fecha inválida" });
                }

                var paros = _dvpEntities.Paros
                    .Include(p => p.Equipo)
                    .Where(p => DbFunctions.TruncateTime(p.FechaEvento) == fecha.Date)
                    .ToList();

                if (!paros.Any())
                {
                    return Json(new { success = false, message = "No se encontraron paros para la fecha seleccionada." });
                }

                if (paros.All(p => p.Cerrado == true))
                {
                    return Json(new { success = false, message = "Ya los paros fueron cerrados correctamente." });
                }

                // Filtrar paros con datos incompletos y TipoEventoID diferente de 3
                var parosIncompletos = paros
                    .Where(p =>
                        (p.EquipoID == null ||
                         p.TipoEventoID == null ||
                         p.SubEquipoID == null ||
                         p.ComponenteEquipoID == null ||
                         p.ClasificacionID == null ||
                         p.TipoFallaID == null ||
                         p.StatusValidate == false ||
                         string.IsNullOrWhiteSpace(p.Comentario))
                        && p.TipoEventoID != ACTIVE_EVENT
                    )
                    .ToList();

                if (parosIncompletos.Any())
                {
                    var equiposIncompletos = parosIncompletos
                        .Select(p => p.Equipo != null ? p.Equipo.Descripcion : $"EquipoID {p.EquipoID}")
                        .Distinct()
                        .ToList();

                    string nombres = string.Join(", ", equiposIncompletos);

                    return Json(new
                    {
                        success = false,
                        message = $"No se puede cerrar el reporte. Hay paros con información incompleta en los siguientes equipos o no estan validados: {nombres}"
                    });
                }

                // Cerrar paros válidos
                foreach (var paro in paros.Where(p => !p.Cerrado.HasValue || p.Cerrado == false))
                {
                    paro.Cerrado = true;
                    paro.FechaCierre = DateTime.Now;
                }

                _dvpEntities.SaveChanges();

                // Registrar el cierre
                var cierreGenerado = new CierreStatus
                {
                    FechaCierre = DateTime.Now,
                    FechaReporte = fecha,
                    Cerrado = true
                };

                _dvpEntities.CierreStatus.Add(cierreGenerado);
                _dvpEntities.SaveChanges();

                return Json(new { success = true, totalCerrados = paros.Count });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Ocurrió un error al cerrar los paros. Detalles: " + ex.Message });
            }
        }



        private DowntimeViewModel VerificarEvento(int paroRelacionadoId, int tipoEventoId, DateTime fechaEvento)
        {


            if (tipoEventoId == RECLASIFICATION_EVENT)
            {
                var existeArranque = _dvpEntities.Paros.Any(p =>
                    p.StatusDelete == false &&
                    p.ParoRelacionadoID == paroRelacionadoId &&
                    p.TipoEventoID == ACTIVE_EVENT &&
                    p.FechaEvento > fechaEvento
                );

                if (existeArranque)
                {
                    return new DowntimeViewModel { Success = false, Message = "Ya existe un evento activo, no se puede añadir una reclasificación porque ya arrancó el equipo" };
                }
            }

            if (tipoEventoId == DAY_DELAY_EVENT)
            {
                var existePosterior = _dvpEntities.Paros.Any(p =>
                    p.StatusDelete == false &&
                    p.ParoRelacionadoID == paroRelacionadoId &&
                    p.TipoEventoID == ACTIVE_EVENT &&
                    p.FechaEvento > fechaEvento
                );

                if (existePosterior)
                {
                    return new DowntimeViewModel { Success = false, Message = "No se puede crear este day delay event porque existe un evento active" };
                }
            }

            if (tipoEventoId == ACTIVE_EVENT)
            {
                var existenEventosPosterior = _dvpEntities.Paros.Any(p =>
                    p.StatusDelete == false &&
                    p.ParoRelacionadoID == paroRelacionadoId &&
                    (p.TipoEventoID == DAY_DELAY_EVENT || p.TipoEventoID == RECLASIFICATION_EVENT) &&
                    p.FechaEvento > fechaEvento
                );

                if (existenEventosPosterior)
                {
                    return new DowntimeViewModel { Success = false, Message = "No se puede crear este active en esta fecha porque hay eventos posteriores" };
                }
                else
                {
                    var existeEventoActive = _dvpEntities.Paros.Any(p =>
                    p.StatusDelete == false &&
                    p.ParoRelacionadoID == paroRelacionadoId &&
                    p.TipoEventoID == ACTIVE_EVENT 
                   );

                    if (existeEventoActive)
                    {
                        return new DowntimeViewModel { Success = false, Message = "Ya existe un evento active para este paro" };
                    }
                }

                
            }

            if (tipoEventoId == RECLASIFICATION_EVENT || tipoEventoId == DAY_DELAY_EVENT || tipoEventoId == INACTIVE_EVENT || tipoEventoId == ACTIVE_EVENT)
            {
                var existeMismoEvento = _dvpEntities.Paros.Any(p =>
                    p.StatusDelete == false &&
                    p.ParoRelacionadoID == paroRelacionadoId &&
                    p.TipoEventoID == tipoEventoId &&
                    p.FechaEvento == fechaEvento
                );

                if (existeMismoEvento)
                {
                    return new DowntimeViewModel { Success = false, Message = "Ya existe un evento del mismo tipo" };
                }
            }
            return new DowntimeViewModel { Success = true };
        }










        public const int INACTIVE_EVENT = 1;
        public const int DAY_DELAY_EVENT = 2;
        public const int ACTIVE_EVENT = 3;
        public const int RECLASIFICATION_EVENT = 4;

    }
}