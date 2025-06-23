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
using System.Web.Helpers;


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

                DateTime fechaCierreAutomatica = new DateTime(2024, 12, 31);

                bool fechaCerrada = _dvpEntities.CierreStatus
                    .Any(p => DbFunctions.TruncateTime(p.FechaReporte) == data._fechaEvento.Date);

                DateTime ultimoDiaMes = new DateTime(hoy.Year, hoy.Month, DateTime.DaysInMonth(hoy.Year, hoy.Month));
                DateTime inicioUltimosTresDias = ultimoDiaMes.AddDays(-2);
                bool estamosenUltimosTresDias = (ultimoDiaMes - hoy).TotalDays <= 2;
                bool estafechalaEventoEnUltimosTresDias = fechaEvento >= inicioUltimosTresDias && fechaEvento <= ultimoDiaMes;


                if (fechaCerrada == true || fechaEvento <= fechaCierreAutomatica)
                {
                    return Json(new
                    {
                        success = false,
                        message = $"La fecha {fechaEvento:dd/MM/yyyy} ya está cerrada y no se pueden registrar nuevos paros."
                    });
                }
                else
                {
                    if (!estamosenUltimosTresDias && estafechalaEventoEnUltimosTresDias)
                    {
                        return Json(new
                        {
                            success = false,
                            message = $"Solo se pueden registrar a futuro los tres ultimos días del mes para fines de proyección de cierre."
                        });
                    }

                    if (data._fechaEvento.Date >= DateTime.Now.Date && !estamosenUltimosTresDias)
                    {
                        return Json(new { success = false, message = "No se puede crear un evento posterior al día de ayer, a menos estemos en los 3 últimos días del mes para fines de proyección de cierre." });
                    }

                    // Verificar si ya existe un paro con los mismos datos
                    bool existeParo = _dvpEntities.Paros.Any(p =>
                        p.EquipoID == data._equipoId &&
                        p.FechaEvento == data._fechaEvento &&
                        p.TipoEventoID == data._tipoEventoId &&
                        p.ParoRelacionadoID == data._paroRelacionadoId
                    );

                    if (existeParo)
                    {
                        return Json(new { success = false, message = "Este paro ya existe en la base de datos." });
                    }

                    var resultado = ValidarQueNoEntreEnConflictoElParoACreaeConOtroPeriodo(data._fechaEvento) as JsonResult;
                    dynamic datosperiodo = resultado.Data;

                    if (datosperiodo.success == false)
                    {
                        return Json(new { success = false, message = datosperiodo.message });
                    }

                    var validacionultimoparo = VerificarSielUltimoParoDelEquipoTuvoArranque(data._equipoId,data._fechaEvento) as JsonResult;
                    dynamic datosvalidacion = validacionultimoparo.Data;

                    if (datosvalidacion.success == false)
                    {
                        return Json(new { success = false, message = datosvalidacion.message });
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
                //DateTime hoy = DateTime.Now.Date;
                DateTime hoy = new DateTime(2025, 6, 28);
                DateTime ultimoDiaMes = new DateTime(hoy.Year, hoy.Month, DateTime.DaysInMonth(hoy.Year, hoy.Month));
                DateTime inicioUltimosTresDias = ultimoDiaMes.AddDays(-2);
                bool estamosenUltimosTresDias = (ultimoDiaMes - hoy).TotalDays <= 2;
                bool estafechalaEventoEnUltimosTresDias = fechaEvento >= inicioUltimosTresDias && fechaEvento <= ultimoDiaMes;
                DateTime fechaCierreAutomatica = new DateTime(2024, 12, 31);



                var inactiveOrigen = _dvpEntities.Paros.FirstOrDefault(p => p.ParosID == data._paroId);
                var fechaLimite = inactiveOrigen.FechaEvento.Value.Date.AddDays(1);

                var fechaEventoDelParo = inactiveOrigen.FechaEvento.Value.Date;

                var ultimoParo = _dvpEntities.Paros
                            .Where(p => p.ParoRelacionadoID == inactiveOrigen.ParoRelacionadoID && p.StatusDelete == false)
                            .OrderByDescending(p => p.FechaEvento)
                            .FirstOrDefault();

                if (inactiveOrigen.Cerrado == true && ultimoParo.TipoEventoID != DAY_DELAY_EVENT || fechaEvento <= fechaCierreAutomatica)
                {
                    return Json(new
                    {
                        success = false,
                        message = $"La fecha {fechaEvento:dd/MM/yyyy} ya está cerrada y no se pueden registrar nuevos paros."
                    });
                }
                else
                {
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

                    if (!estamosenUltimosTresDias && estafechalaEventoEnUltimosTresDias)
                    {
                        return Json(new
                        {
                            success = false,
                            message = $"Solo se pueden registrar a futuro los tres ultimos días del mes para fines de proyección de cierre."
                        });
                    }

                    if (data._fechaEvento.Date >= DateTime.Now.Date && !estamosenUltimosTresDias)
                    {
                        
                        return Json(new { success = false, message = "No se puede crear un evento 1 o mas dias despues al día de ayer, necesitas un day delay." });
                    }
                                        
                    var resultado = ValidarSiElPeriodoACrearEstaDentroDeUnPeriodoDeParosYaCreado(data._fechaEvento, inactiveOrigen.ParosID) as JsonResult;
                    dynamic datosperiodo = resultado.Data;

                    if (datosperiodo.success == false)
                    {
                        return Json(new { success = false, message = datosperiodo.message });
                    }

                    var resultadoParosEntrePeriodo = ValidarSiElPeriodoACrearEstaFueraDeUnPeriodoDeParosYaCreadoPeroChocaConOtoEvento(inactiveOrigen.ParosID, data._fechaEvento) as JsonResult;
                    dynamic datosParosEntrePeriodo = resultadoParosEntrePeriodo.Data;

                    if (datosParosEntrePeriodo.success == false)
                    {
                        return Json(new { success = false, message = datosParosEntrePeriodo.message });
                    }

                    if (data._fechaEvento.Date >= inactiveOrigen.FechaEvento.Value.Date && (data._fechaEvento.Day  - ultimoParo.FechaEvento.Value.Day >= 1 && ultimoParo.TipoEventoID != DAY_DELAY_EVENT))
                    {

                        return Json(new { success = false, message = "Para crear este evento en esta fecha necesitas crear un day delay." });
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

                var inactiveOrigen = _dvpEntities.Paros.FirstOrDefault(p => p.ParosID == data._paroId);
                var fechaLimite = inactiveOrigen.FechaEvento.Value.Date.AddDays(1);

                DateTime fechaCierreAutomatica = new DateTime(2024, 12, 31);

                var fechaEventoDelParo = inactiveOrigen.FechaEvento.Value.Date;

                if (fechaEvento <= fechaCierreAutomatica)
                {
                    return Json(new
                    {
                        success = false,
                        message = $"La fecha {fechaEvento:dd/MM/yyyy} ya está cerrada y no se pueden registrar nuevos paros."
                    });
                }
                else
                {
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

                    if (!estamosenUltimosTresDias && estafechalaEventoEnUltimosTresDias)
                    {
                        return Json(new
                        {
                            success = false,
                            message = $"Solo se pueden registrar a futuro los tres ultimos días del mes para fines de proyección de cierre."
                        });
                    }
                    if (data._fechaEvento.Date >= DateTime.Now.Date && !estamosenUltimosTresDias)
                    {
                        return Json(new { success = false, message = "NNo se puede crear un evento 1 o mas dias despues del evento original, necesitas un day delay." });
                    }

                    var resultadoParosEntrePeriodo = ValidarSiElPeriodoACrearEstaFueraDeUnPeriodoDeParosYaCreadoPeroChocaConOtoEvento(inactiveOrigen.ParosID, data._fechaEvento) as JsonResult;
                    dynamic datosParosEntrePeriodo = resultadoParosEntrePeriodo?.Data;

                    if (datosParosEntrePeriodo == null || datosParosEntrePeriodo.success == false)
                    {
                        return Json(new { success = false, message = datosParosEntrePeriodo?.message ?? "Error al validar los paros dentro del periodo." });
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
                DateTime primerDiaProximoMes = ultimoDiaMes.AddDays(1);
                DateTime segundodiaProyeccion = inicioUltimosTresDias.AddDays(1);
                DateTime tercerdiaProyeccion = inicioUltimosTresDias.AddDays(2);

                bool estamosenUltimosCuatroDias = (primerDiaProximoMes - hoy).TotalDays <= 3;
                bool estafechalaEventoEnUltimosCuatroDias = data._fechaEvento >= inicioUltimosTresDias && data._fechaEvento <= primerDiaProximoMes;
                DateTime fechaEvento = data._fechaEvento.Date;

                var inactiveOrigen = _dvpEntities.Paros.FirstOrDefault(p => p.ParosID == data._paroId);

                DateTime fechaCierreAutomatica = new DateTime(2024, 12, 31);

                var fechaEventoDelParo = inactiveOrigen.FechaEvento.Value.Date;

                var ultimoParo = _dvpEntities.Paros
                            .Where(p => p.ParoRelacionadoID == inactiveOrigen.ParoRelacionadoID && p.StatusDelete == false)
                            .OrderByDescending(p => p.FechaEvento)
                            .FirstOrDefault();

                if (inactiveOrigen.Cerrado == true && (ultimoParo.TipoEventoID != DAY_DELAY_EVENT || ultimoParo.TipoEventoID != RECLASIFICATION_EVENT) || fechaEvento <= fechaCierreAutomatica)
                {
                    return Json(new
                    {
                        success = false,
                        message = $"La fecha {fechaEvento:dd/MM/yyyy} ya está cerrada y no se pueden registrar nuevos paros."
                    });
                }
                else
                {
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

                    if (!estamosenUltimosCuatroDias && estafechalaEventoEnUltimosCuatroDias)
                    {
                        return Json(new
                        {
                            success = false,
                            message = $"Solo se pueden registrar a futuro los tres ultimos días del mes para fines de proyección de cierre."
                        });
                    }
                    if (data._fechaEvento.Date > DateTime.Now && !estamosenUltimosCuatroDias)
                    {
                        return Json(new { success = false, message = "No se puede crear un day delay mañana porque es un paro a futuro." });
                    }

                    var resultado = ValidarSiElPeriodoACrearEstaDentroDeUnPeriodoDeParosYaCreado(data._fechaEvento, inactiveOrigen.ParosID) as JsonResult;
                    dynamic datosperiodo = resultado.Data;

                    if (datosperiodo.success == false)
                    {
                        return Json(new { success = false, message = datosperiodo.message });
                    }

                    var resultadoParosEntrePeriodo = ValidarSiElPeriodoACrearEstaFueraDeUnPeriodoDeParosYaCreadoPeroChocaConOtoEvento(inactiveOrigen.ParosID, data._fechaEvento) as JsonResult;
                    dynamic datosParosEntrePeriodo = resultadoParosEntrePeriodo.Data;

                    if (datosParosEntrePeriodo.success == false)
                    {
                        return Json(new { success = false, message = datosParosEntrePeriodo.message });
                    }

                    if (estafechalaEventoEnUltimosCuatroDias && estamosenUltimosCuatroDias)
                    {
                        var dayDelayPrimerdíaProyección = _dvpEntities.Paros
                            .Where(p => p.ParoRelacionadoID == inactiveOrigen.ParoRelacionadoID && p.StatusDelete == false && p.TipoEventoID == DAY_DELAY_EVENT && p.FechaEvento == inicioUltimosTresDias)
                            .OrderByDescending(p => p.FechaEvento)
                            .FirstOrDefault();

                        bool existeActiveEnPrimerDiaProyeccion = _dvpEntities.Paros.Any(p =>
                             p.ParoRelacionadoID == inactiveOrigen.ParoRelacionadoID &&
                             p.StatusDelete == false &&
                             p.TipoEventoID == ACTIVE_EVENT
                             );

                        var dayDelaySegundodíaProyección = _dvpEntities.Paros
                            .Where(p => p.ParoRelacionadoID == inactiveOrigen.ParoRelacionadoID && p.StatusDelete == false && p.TipoEventoID == DAY_DELAY_EVENT && p.FechaEvento == segundodiaProyeccion)
                            .OrderByDescending(p => p.FechaEvento)
                            .FirstOrDefault();

                        bool existeActiveEnSegundoDiaProyeccion = _dvpEntities.Paros.Any(p =>
                             p.ParoRelacionadoID == inactiveOrigen.ParoRelacionadoID &&
                             p.StatusDelete == false &&
                             p.TipoEventoID == ACTIVE_EVENT
                             );

                        var dayDelayTercerdíaProyección = _dvpEntities.Paros
                            .Where(p => p.ParoRelacionadoID == inactiveOrigen.ParoRelacionadoID && p.StatusDelete == false && p.TipoEventoID == DAY_DELAY_EVENT && p.FechaEvento == tercerdiaProyeccion)
                            .OrderByDescending(p => p.FechaEvento)
                            .FirstOrDefault();

                        bool existeActiveEnTercerDiaProyeccion = _dvpEntities.Paros.Any(p =>
                             p.ParoRelacionadoID == inactiveOrigen.ParoRelacionadoID &&
                             p.StatusDelete == false &&
                             p.TipoEventoID == ACTIVE_EVENT
                             );

                        var dayDelayPrimerdíaPróximoMesProyección = _dvpEntities.Paros
                            .Where(p => p.ParoRelacionadoID == inactiveOrigen.ParoRelacionadoID && p.StatusDelete == false && p.TipoEventoID == DAY_DELAY_EVENT && p.FechaEvento == primerDiaProximoMes)
                            .OrderByDescending(p => p.FechaEvento)
                            .FirstOrDefault();

                        bool existeActivePrimerdíaPróximoMesPDiaProyeccion = _dvpEntities.Paros.Any(p =>
                             p.ParoRelacionadoID == inactiveOrigen.ParoRelacionadoID &&
                             p.StatusDelete == false &&
                             p.TipoEventoID == ACTIVE_EVENT
                             );

                        //calcular si tiene evento active para cada uno

                        if (data._fechaEvento.Date == tercerdiaProyeccion.Date && dayDelaySegundodíaProyección == null && !existeActiveEnSegundoDiaProyeccion)
                        {
                            return Json(new { success = false, message = "No se puede crear un evento 1 o mas dias despues del evento original en la proyección, debido a que necesitas un day delay, de la fecha anterior." });
                        }
                        if (data._fechaEvento.Date == primerDiaProximoMes.Date && dayDelayTercerdíaProyección == null && !existeActiveEnTercerDiaProyeccion)
                        {
                            return Json(new { success = false, message = "No se puede crear un evento 1 o mas dias despues del evento original en la proyección, debido a que necesitas un day delay,de la fecha anterior." });
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
                DateTime fechaCierreAutomatica = new DateTime(2024, 12, 31);
                DateTime fechaEvento = data._fechaEvento.Date;
                DateTime hoy = DateTime.Now.Date;
                DateTime ultimoDiaMes = new DateTime(hoy.Year, hoy.Month, DateTime.DaysInMonth(hoy.Year, hoy.Month));
                DateTime inicioUltimosTresDias = ultimoDiaMes.AddDays(-2);
                bool estamosenUltimosTresDias = (ultimoDiaMes - hoy).TotalDays <= 2;
                bool estafechalaEventoEnUltimosTresDias = fechaEvento >= inicioUltimosTresDias && fechaEvento <= ultimoDiaMes;

                var fechaEventoDelParo = paroExistente.FechaEvento.Value.Date;


                if (paroExistente.Cerrado == true && paroExistente.FechaEvento != data._fechaEvento)
                {
                    return Json(new
                    {
                        success = false,
                        message = $"La fecha {paroExistente.FechaEvento:dd/MM/yyyy} ya está cerrada no se puede hacer modificaciones en la hora del evento"
                    });
                }

                if (paroExistente == null)
                {
                    return Json(new { success = false, message = "No se encontró el paro original." });
                }

                if (!paroExistente.FechaEvento.HasValue)
                {
                    return Json(new { success = false, message = "El evento no tiene una fecha de evento válida." });
                }

                var validacion = VerificarEventoAEditar(paroExistente.ParoRelacionadoID.Value, paroExistente.TipoEventoID.Value, data._fechaEvento,paroExistente.ParosID);

                if (!validacion.Success)
                {
                    return Json(new { success = false, message = validacion.Message });
                }

                if (!estamosenUltimosTresDias && estafechalaEventoEnUltimosTresDias)
                {
                    return Json(new
                    {
                        success = false,
                        message = $"Solo se pueden registrar a futuro los tres ultimos días del mes para fines de proyección de cierre."
                    });
                }

                if (data._fechaEvento.Date >= DateTime.Now.Date && !estamosenUltimosTresDias)
                {
                    return Json(new { success = false, message = "No se puede crear un evento 1 o mas dias despues del evento original, necesitas un day delay." });
                }
                var resultadoParosEntrePeriodo = ValidarSiElPeriodoACrearEstaFueraDeUnPeriodoDeParosYaCreadoPeroChocaConOtoEvento(paroExistente.ParosID, data._fechaEvento) as JsonResult;
                dynamic datosParosEntrePeriodo = resultadoParosEntrePeriodo?.Data;

                if (datosParosEntrePeriodo == null || datosParosEntrePeriodo.success == false)
                {
                    return Json(new { success = false, message = datosParosEntrePeriodo?.message ?? "Error al validar los paros dentro del periodo." });
                }

                // Actualizar campos
                paroExistente.EquipoID = data._equipoId;
                paroExistente.TipoEventoID = paroExistente.TipoEventoID;
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
                return Json(new { success = false, message = "Error inesperado: " + ex.Message });
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
                DateTime fechaCierreAutomatica = new DateTime(2024, 12, 31);

                // Verificar si la fecha del evento ya está cerrada en CierreStatus
                if (paroExistente.Cerrado == true)
                {
                    return Json(new
                    {
                        success = false,
                        message = $"La fecha {paroExistente.FechaEvento:dd/MM/yyyy} ya está cerrada no se puede hacer modificaciones en la hora del evento"
                    });
                }
                else
                {
                    if (paroExistente == null)
                    {
                        return Json(new { success = false, message = "No se encontró el paro para actualizar." });
                    }

                    paroExistente.StatusValidate = false;

                    _dvpEntities.SaveChanges();

                    return Json(new { success = true, paroId = paroExistente.ParosID });
                }

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
                DateTime fechaCierreAutomatica = new DateTime(2024, 12, 31);

                // Verificar si la fecha del evento ya está cerrada en CierreStatus
                if (paroExistente.Cerrado == true)
                {
                    return Json(new
                    {
                        success = false,
                        message = $"La fecha {paroExistente.FechaEvento:dd/MM/yyyy} ya está cerrada no se puede hacer modificaciones en la hora del evento"
                    });
                }
                else
                {
                   
                    if (paroExistente == null)
                    {
                        return Json(new { success = false, message = "No se encontró el paro para actualizar." });
                    }

                    var paroRelacionadoIDparoExistente = paroExistente.ParoRelacionadoID;

                    if (paroExistente.TipoEventoID == INACTIVE_EVENT)
                    {
                        var  parosaborrar = _dvpEntities.Paros.Where(p => p.ParoRelacionadoID == paroRelacionadoIDparoExistente).ToList();

                        foreach (var paroaborrar in parosaborrar)
                        {
                            paroaborrar.StatusDelete = true;
                            _dvpEntities.SaveChanges();
                        }
                    }


                    paroExistente.StatusDelete = true;
                    _dvpEntities.SaveChanges();

                    return Json(new { success = true, paroId = paroExistente.ParosID });
                }

            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public JsonResult GetDowntimesByDate(DateTime fecha)
        {
            // Buscar todos los paros cuya fecha coincida
            var parosEnFecha = _dvpEntities.Paros
                .Where(p => DbFunctions.TruncateTime(p.FechaEvento) == fecha.Date && p.StatusDelete == false)
                .ToList();

            // Obtener los ParoRelacionadoID únicos desde esos eventos (para agrupar por el paro INACTIVE origen)
            var paroRelacionadoIds = parosEnFecha
                .Where(p => p.ParoRelacionadoID.HasValue)
                .Select(p => p.ParoRelacionadoID.Value)
                .Distinct()
                .ToList();

            // Obtener todos los eventos asociados a esos ParoRelacionadoID
            var parosOrigen = new List<dynamic>();

            if (paroRelacionadoIds.Any())
            {
                parosOrigen = _dvpEntities.Paros
                    .Where(p => paroRelacionadoIds.Contains(p.ParoRelacionadoID.Value) && p.StatusDelete == false)
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
                        _paroRelacionadoId = choose.ParoRelacionadoID,
                        _cerrado = choose.Cerrado
                    })
                    .ToList<dynamic>();
            }

            // IDs ya mostrados (para evitar duplicados)
            var idsMostrados = parosOrigen.Select(p => (int)p._paroId).ToList();

            // Obtener todos los paros sin arranque que no se hayan mostrado aún
            var parosSinArranque = GetParosSinArranque()
                .Where(p => !idsMostrados.Contains((int)((dynamic)p)._paroId))
                .ToList();

            // Combinar siempre los paros encontrados con los sin arranque
            var resultado = parosOrigen
                .Cast<object>()
                .Concat(parosSinArranque)
                .OrderBy(p => ((DateTime)((dynamic)p)._fechaEvento))
                .ToList();

            return Json(resultado, JsonRequestBehavior.AllowGet);
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

            var eventos = _dvpEntities.Paros
                .Where(p => p.ParoRelacionadoID == paroRelacionadoId && p.StatusDelete == false)
                .ToList();

            var eventoInactive = eventos.FirstOrDefault(p => p.TipoEventoID == INACTIVE_EVENT);
            var eventoActive = eventos.FirstOrDefault(p => p.TipoEventoID == ACTIVE_EVENT);
            var eventoPendingActive = eventos
                  .Where(p => (p.TipoEventoID == DAY_DELAY_EVENT || p.TipoEventoID == RECLASIFICATION_EVENT))
                  .OrderByDescending(p => p.FechaEvento)
                  .FirstOrDefault();
            var fechaRestaEventoInactive = DateTime.Now;

            double? resultado = null;

            if (eventoInactive != null && eventoActive != null)
            {
                // Caso normal: existe INACTIVE y ACTIVE
                resultado = Math.Abs((eventoInactive.FechaEvento - eventoActive.FechaEvento)?.TotalHours ?? 0);
            }
            else
            {
                if (eventoActive == null && eventoPendingActive == null)
                {
                    resultado = Math.Abs((eventoInactive.FechaEvento - fechaRestaEventoInactive)?.TotalHours ?? 0);

                }
                else
                {
                    // Caso alterno: NO existe ACTIVE => restamos eventoInactive - paro
                    resultado = Math.Abs((eventoInactive.FechaEvento - eventoPendingActive.FechaEvento)?.TotalHours ?? 0);
                }
            }
            
            

            var downtimes = eventos
                .Select(choose => new
                {
                    _paroId = choose.ParosID,
                    _fechaCreacionParo = choose.FechaCreacion,
                    _fechaEvento = choose.FechaEvento,
                    _comment = choose.Comentario,
                    _equipoId = choose.EquipoID,
                    _equipoName = choose.Equipo?.Descripcion,
                    _componenteEquipoName = choose.ComponenteEquipo?.Descripcion,
                    _tipoFallaName = choose.TipoFalla?.Descripcion,
                    _clasificacionName = choose.Clasificacion?.Descripcion,
                    _statusValidate = choose.StatusValidate,
                    _statusDelete = choose.StatusDelete,
                    _tipoEventoId = choose.TipoEventoID,
                    _tipoEventoName = choose.TipoEvento?.Descripcion,
                    _paroRelacionadoId = choose.ParoRelacionadoID,
                    _cerrado = choose.Cerrado,
                    _diferenciaEnHoras = resultado
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

                DateTime fechaMasUnoParaBuscarDayDelay = fecha.Date.AddDays(1);

                var parosDelDia = _dvpEntities.Paros
                    .Where(p => DbFunctions.TruncateTime(p.FechaEvento) == fecha.Date
                                && p.StatusDelete == false
                                || (p.TipoEventoID == DAY_DELAY_EVENT || p.TipoEventoID == RECLASIFICATION_EVENT
                                    && DbFunctions.TruncateTime(p.FechaEvento) == fechaMasUnoParaBuscarDayDelay))
                    .ToList();

                var relacionadosIds = parosDelDia
                    .Where(p => p.ParoRelacionadoID.HasValue)
                    .Select(p => p.ParoRelacionadoID.Value)
                    .Distinct()
                    .ToList();

                var paros = _dvpEntities.Paros
                    .Where(p => p.ParoRelacionadoID.HasValue &&
                                relacionadosIds.Contains(p.ParoRelacionadoID.Value) &&
                                p.StatusDelete == false)
                    .ToList();


                var parosInactive = paros
                    .Where(p => p.TipoEventoID == INACTIVE_EVENT && p.StatusDelete == false)
                    .ToList();

                var parosACerrar = paros
                    .Where(p => p.ParoRelacionadoID.HasValue && relacionadosIds.Contains(p.ParoRelacionadoID.Value) && p.StatusDelete == false)
                    .ToList();

                var parosDelafecha = paros
                    .Where(p => p.StatusDelete == false && p.FechaEvento.Value.Date == fecha.Date)
                    .ToList();

                if (parosDelafecha.Count() <= 0)
                {
                    return Json(new { success = false, message = "No se puede cerrar este reporte debido a que no hay eventos inactive en la fecha seleccionada" });
                }

                if (fecha >= DateTime.Now.AddDays(1))
                {
                    return Json(new { success = false, message = "No se pueden cerrar paros en reportes futuros" });
                }

                if (!parosACerrar.Any())
                {
                    return Json(new { success = false, message = "No se encontraron paros para la fecha seleccionada." });
                }

                foreach (var paro in parosInactive)
                {
                    var paroRelacionadoID = paro.ParoRelacionadoID;

                    // Verificar si NO existe evento ACTIVE relacionado
                    var eventoActivo = parosACerrar.FirstOrDefault(p =>
                         p.ParoRelacionadoID == paroRelacionadoID &&
                         p.TipoEventoID == ACTIVE_EVENT &&
                         p.StatusDelete == false);

                    if (eventoActivo == null)
                    {
                        // Si no hay evento ACTIVE, verificar que al menos haya DAY_DELAY_EVENT en la fecha
                        var eventoDayDelay = parosACerrar.FirstOrDefault(p =>
                            p.ParoRelacionadoID == paroRelacionadoID &&
                            p.TipoEventoID == DAY_DELAY_EVENT &&
                            p.FechaEvento >= fecha.Date &&
                            p.StatusDelete == false);


                        if (eventoDayDelay == null)
                        {
                            return Json(new
                            {
                                success = false,
                                message = "No se puede cerrar el reporte debido a que hay paros sin day delay ni active event que debes documentar."
                            });
                        }
                    }
                }

                // Filtrar paros con datos incompletos y TipoEventoID diferente de 3
                var parosIncompletos = parosACerrar
                    .Where(p =>
                        (p.EquipoID == null &&
                         p.TipoEventoID == null &&
                         p.SubEquipoID == null &&
                         p.ComponenteEquipoID == null &&
                         p.ClasificacionID == null &&
                         p.TipoFallaID == null &&
                         p.StatusValidate == true &&
                         p.StatusDelete == false &&
                         p.TipoEventoID != ACTIVE_EVENT)

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
                        message = $"No se puede cerrar el reporte. Hay paros con información incompleta en los siguientes equipos o no estan validados o sin validar: {nombres}"
                    });
                }

                var ParosValidadosBydate = GetUltimoEventoParosACerrar(parosACerrar, fecha);
                dynamic datosParosValidadosBydate = ParosValidadosBydate.Data;

                if (datosParosValidadosBydate.success == false)
                {
                    return Json(new { success = false, message = "" });
                }

                // Cerrar paros válidos
                foreach (var paro in parosACerrar.Where(p => !p.Cerrado.HasValue || p.Cerrado == false))
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
                var paroActivo = _dvpEntities.Paros
                    .Where(p =>
                        p.StatusDelete == false &&
                        p.ParoRelacionadoID == paroRelacionadoId &&
                        p.TipoEventoID == ACTIVE_EVENT &&
                        DbFunctions.TruncateTime(p.FechaEvento) >= DbFunctions.TruncateTime(fechaEvento)
                    )
                    .OrderBy(p => p.FechaEvento)
                    .FirstOrDefault();

                if (paroActivo != null && paroActivo.FechaEvento < fechaEvento)
                {
                    return new DowntimeViewModel
                    {
                        Success = false,
                        Message = $"Ya existe un evento activo el {paroActivo.FechaEvento?.ToString("dd/MM/yyyy HH:mm")}, no se puede añadir una reclasificación debido a que ya arrancó el equipo"
                    };
                }
            }


            if (tipoEventoId == DAY_DELAY_EVENT)
            {
                var paroActivo = _dvpEntities.Paros
                    .Where(p =>
                        p.StatusDelete == false &&
                        p.ParoRelacionadoID == paroRelacionadoId &&
                        p.TipoEventoID == ACTIVE_EVENT &&
                        DbFunctions.TruncateTime(p.FechaEvento) <= DbFunctions.TruncateTime(fechaEvento)
                    )
                    .OrderBy(p => p.FechaEvento)
                    .FirstOrDefault();

                if (paroActivo != null && paroActivo.FechaEvento < fechaEvento)
                {
                    return new DowntimeViewModel
                    {
                        Success = false,
                        Message = $"Ya existe un evento activo el posterior, no se puede añadir un day delay debido a que ya arrancó el equipo"
                    };
                }
            }

            if (tipoEventoId == ACTIVE_EVENT)
            {
                var existenEventosPosterior = _dvpEntities.Paros.Any(p =>
                    p.StatusDelete == false &&
                    p.ParoRelacionadoID == paroRelacionadoId &&
                    (p.TipoEventoID == DAY_DELAY_EVENT || p.TipoEventoID == RECLASIFICATION_EVENT) &&
                    DbFunctions.TruncateTime(p.FechaEvento) > DbFunctions.TruncateTime(fechaEvento)
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
                var mismoEvento = _dvpEntities.Paros.FirstOrDefault(p =>
                                  p.StatusDelete == false &&
                                  p.TipoEventoID == tipoEventoId &&
                                  p.FechaEvento == fechaEvento
                                );

                if (mismoEvento != null)
                {
                    return new DowntimeViewModel { Success = false, Message = "Ya existe un evento del mismo tipo a la misma hora" };
                }
            }
            return new DowntimeViewModel { Success = true };
        }


        private DowntimeViewModel VerificarEventoAEditar(int paroRelacionadoId, int tipoEventoId, DateTime fechaEvento, int paroId)
        {


            if (tipoEventoId == RECLASIFICATION_EVENT)
            {
                var paroActivo = _dvpEntities.Paros
                    .Where(p =>
                        p.ParosID != paroId &&
                        p.StatusDelete == false &&
                        p.ParoRelacionadoID == paroRelacionadoId &&
                        p.TipoEventoID == ACTIVE_EVENT &&
                        DbFunctions.TruncateTime(p.FechaEvento) >= DbFunctions.TruncateTime(fechaEvento)
                    )
                    .OrderBy(p => p.FechaEvento)
                    .FirstOrDefault();

                if (paroActivo != null && paroActivo.FechaEvento < fechaEvento)
                {
                    return new DowntimeViewModel
                    {
                        Success = false,
                        Message = $"Ya existe un evento activo el {paroActivo.FechaEvento?.ToString("dd/MM/yyyy HH:mm")}, no se puede añadir una reclasificación debido a que ya arrancó el equipo"
                    };
                }
            }


            if (tipoEventoId == DAY_DELAY_EVENT)
            {
                var paroActivo = _dvpEntities.Paros
                    .Where(p =>
                        p.ParosID != paroId &&
                        p.StatusDelete == false &&
                        p.ParoRelacionadoID == paroRelacionadoId &&
                        p.TipoEventoID == ACTIVE_EVENT &&
                        DbFunctions.TruncateTime(p.FechaEvento) <= DbFunctions.TruncateTime(fechaEvento)
                    )
                    .OrderBy(p => p.FechaEvento)
                    .FirstOrDefault();

                if (paroActivo != null && paroActivo.FechaEvento < fechaEvento)
                {
                    return new DowntimeViewModel
                    {
                        Success = false,
                        Message = $"Ya existe un evento activo el posterior, no se puede añadir un day delay debido a que ya arrancó el equipo"
                    };
                }

                if (true)
                {

                }
            }

            if (tipoEventoId == ACTIVE_EVENT)
            {
                var existenEventosPosterior = _dvpEntities.Paros.Any(p =>
                    p.ParosID != paroId &&
                    p.StatusDelete == false &&
                    p.ParoRelacionadoID == paroRelacionadoId &&
                    (p.TipoEventoID == DAY_DELAY_EVENT || p.TipoEventoID == RECLASIFICATION_EVENT) &&
                    DbFunctions.TruncateTime(p.FechaEvento) > DbFunctions.TruncateTime(fechaEvento)
                );

                if (existenEventosPosterior)
                {
                    return new DowntimeViewModel { Success = false, Message = "No se puede crear este active en esta fecha porque hay eventos posteriores" };
                }
                else
                {
                    var existeEventoActive = _dvpEntities.Paros.Any(p =>
                    p.ParosID != paroId &&
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
                var mismoEvento = _dvpEntities.Paros.FirstOrDefault(p =>
                                  p.ParosID != paroId &&
                                  p.StatusDelete == false &&
                                  p.TipoEventoID == tipoEventoId &&
                                  p.FechaEvento == fechaEvento
                                );

                if (mismoEvento != null)
                {
                    return new DowntimeViewModel { Success = false, Message = "Ya existe un evento del mismo tipo a la misma hora" };
                }
            }
            return new DowntimeViewModel { Success = true };
        }


        [HttpGet]
        public JsonResult ValidarSiElPeriodoACrearEstaDentroDeUnPeriodoDeParosYaCreado(DateTime fecha, int paroId)
        {
            int mes = fecha.Month;
            int año = fecha.Year;

            // Obtener todos los paros del mismo mes y año
            var paros = _dvpEntities.Paros
                .Where(p => p.FechaEvento.HasValue &&
                            p.FechaEvento.Value.Month == mes &&
                            p.FechaEvento.Value.Year == año && p.StatusDelete == false)
                .ToList();

            // Filtrar los paros inactivos
            var parosInactive = paros
                .Where(p => p.TipoEventoID == INACTIVE_EVENT && p.StatusDelete == false)
                .ToList();

            foreach (var paroInactive in parosInactive)
            {
                var paroRelacionadoID = paroInactive.ParoRelacionadoID;

                // Buscar el evento ACTIVE relacionado
                var paroActivo = paros.FirstOrDefault(p =>
                    p.ParoRelacionadoID == paroRelacionadoID &&
                    p.TipoEventoID == ACTIVE_EVENT && p.StatusDelete == false);

                // Asignar fechas de inicio y fin
                DateTime inicio = paroInactive.FechaEvento.Value;
                DateTime fin;

                var ultimoParo = paros
                  .Where(p => p.ParoRelacionadoID == paroRelacionadoID && p.StatusDelete == false)
                  .OrderByDescending(p => p.FechaEvento)
                  .FirstOrDefault();

                int ultimoParoId = ultimoParo.ParosID;

                if (paroActivo != null)
                {
                    fin = paroActivo.FechaEvento.Value;
                }
                else
                {
                    // Si no hay paro ACTIVE, buscar el último evento con mismo ParoRelacionadoID
                    fin = ultimoParo.FechaEvento.Value;
                }

                // Validar si la fecha proporcionada cae en ese periodo
                if (fecha >= inicio && fecha <= fin && paroId != ultimoParoId)
                {
                    return Json(new
                    {
                        success = false,
                        message = "No se puede crear el evento. La fecha coincide con un periodo de paro existente."
                    }, JsonRequestBehavior.AllowGet);
                }

            }

            // Si no hay conflicto
            return Json(new
            {
                success = true
            }, JsonRequestBehavior.AllowGet);
        }


        
        [HttpGet]
        public JsonResult ValidarQueNoEntreEnConflictoElParoACreaeConOtroPeriodo(DateTime fecha)
        {
            int mes = fecha.Month;
            int año = fecha.Year;

            // Obtener todos los paros del mismo mes y año
            var paros = _dvpEntities.Paros
                .Where(p => p.FechaEvento.HasValue &&
                            p.FechaEvento.Value.Month == mes &&
                            p.FechaEvento.Value.Year == año && p.StatusDelete == false)
                .ToList();

            // Filtrar los paros inactivos
            var parosInactive = paros
                .Where(p => p.TipoEventoID == INACTIVE_EVENT && p.StatusDelete == false)
                .ToList();

            foreach (var paroInactive in parosInactive)
            {
                var paroRelacionadoID = paroInactive.ParoRelacionadoID;
                var paroIDinactive = paroInactive.ParosID;

                // Buscar el evento ACTIVE relacionado
                var paroActivo = paros.FirstOrDefault(p =>
                    p.ParoRelacionadoID == paroRelacionadoID &&
                    p.TipoEventoID == ACTIVE_EVENT && p.StatusDelete == false);

                // Asignar fechas de inicio y fin
                DateTime inicio = paroInactive.FechaEvento.Value;
                DateTime fin;

                if (paroActivo != null)
                {
                    fin = paroActivo.FechaEvento.Value;
                }
                else
                {
                    // Si no hay paro ACTIVE, buscar el último evento con mismo ParoRelacionadoID
                    fin = paros
                        .Where(p => p.ParoRelacionadoID == paroRelacionadoID && p.StatusDelete == false)
                        .OrderByDescending(p => p.FechaEvento)
                        .Select(p => p.FechaEvento.Value)
                        .FirstOrDefault();
                }

                // Validar si la fecha proporcionada cae en ese periodo
                if (fecha >= inicio && fecha <= fin)
                {
                    return Json(new
                    {
                        success = false,
                        message = "No se puede crear el evento. La fecha coincide con un periodo de paro existente."
                    }, JsonRequestBehavior.AllowGet);
                }

            }

            // Si no hay conflicto
            return Json(new
            {
                success = true
            }, JsonRequestBehavior.AllowGet);
        }


        [HttpGet]
        public JsonResult ValidarSiElPeriodoACrearEstaFueraDeUnPeriodoDeParosYaCreadoPeroChocaConOtoEvento(int paroId, DateTime fechaEvento)
        {

            var paroInactive = _dvpEntities.Paros.Where(p => p.ParosID == paroId && p.StatusDelete == false).FirstOrDefault();
            var fechaParoInactive = paroInactive.FechaEvento;
            var paroRelacionadoId = paroInactive.ParoRelacionadoID;

            var parosEnRango= _dvpEntities.Paros
                .Where(p =>
                    p.ParosID != paroId &&
                    p.StatusDelete == false &&
                    p.FechaEvento >= fechaParoInactive &&
                    p.FechaEvento <= fechaEvento)
                .Select(p => new
                {
                    p.ParosID,
                    p.FechaEvento,
                    p.TipoEventoID,
                    p.ParoRelacionadoID,
                    p.EquipoID
                })
                .ToList();
            if (parosEnRango.Count() == 0){ return Json(new { success = true }, JsonRequestBehavior.AllowGet); }
            else
            {
                var existeParoInactiveEnElPeriodo = parosEnRango.FirstOrDefault();

                if (existeParoInactiveEnElPeriodo.TipoEventoID == INACTIVE_EVENT)
                {
                    return Json(new
                    {
                        success = false,
                        message = "No se puede crear el evento en esa fecha debido a que entra en conflicto con otros paros.",
                        conflictos = parosEnRango,
                    }, JsonRequestBehavior.AllowGet);
                }
            }

            return Json(new { success = true }, JsonRequestBehavior.AllowGet);

        }


        public JsonResult VerificarSielUltimoParoDelEquipoTuvoArranque(int equipoId, DateTime fechaEvento)
        {

            var parosEquipo = _dvpEntities.Paros
                .Where(p =>
                 p.EquipoID == equipoId &&
                 DbFunctions.TruncateTime(p.FechaEvento) == fechaEvento.Date &&
                 p.StatusDelete == false)
                .Select(p => new
                {
                   p.ParosID,
                   p.FechaEvento,
                   p.Equipo.Descripcion,
                   p.TipoEventoID,
                   p.ParoRelacionadoID,
                   p.StatusDelete
                })
                .ToList();
            if (parosEquipo.Count() == 0) { return Json(new { success = true }, JsonRequestBehavior.AllowGet); }
            else
            {
                var ultimoParo = parosEquipo.Where(p => p.TipoEventoID == INACTIVE_EVENT && p.StatusDelete == false).LastOrDefault();

                if (ultimoParo != null)
                {
                    int paroRelacionadoUltimoParoId = ultimoParo.ParoRelacionadoID.Value;


                    // Buscar el evento ACTIVE relacionado
                    var paroActivo = parosEquipo.FirstOrDefault(p =>
                        p.ParoRelacionadoID == paroRelacionadoUltimoParoId &&
                        p.TipoEventoID == ACTIVE_EVENT && p.StatusDelete == false);

                    //Buscar ultimo evento del INACTIVE
                    var ultimoEventoDelInactive = parosEquipo
                        .Where(p => p.ParoRelacionadoID == paroRelacionadoUltimoParoId && p.StatusDelete == false)
                        .OrderByDescending(p => p.FechaEvento)
                        .Select(p => p.FechaEvento)
                        .FirstOrDefault();


                    if (paroActivo == null && fechaEvento >= ultimoEventoDelInactive.Value)
                    {
                        return Json(new
                        {
                            success = false,
                            message = $"No se puede crear un evento inactive debido a que el equipo esta apagado en esa fecha {fechaEvento.ToString("dd/MM/yyyy HH:mm")}, " +
                            $"debes terminar de documentar el paro en cuestión para iniciar otro paro."

                        }, JsonRequestBehavior.AllowGet);
                    }
                }
                
            }

            return Json(new { success = true }, JsonRequestBehavior.AllowGet);

        }



        [HttpPost]
        public JsonResult GetUltimoEventoParosACerrar(List<Paros> paros, DateTime fechaCierre)
        {
            if (paros == null || !paros.Any())
            {
                return Json(new { success = true });
            }

            var parosInactive = paros.Where(p => p.TipoEventoID == INACTIVE_EVENT && p.StatusDelete == false).ToList();

            foreach (var paro in parosInactive)
            {
                var paroRelacionadoID = paro.ParosID;

                // Buscar eventos relacionados con este paro
                var eventosRelacionados = paros
                    .Where(p => p.ParoRelacionadoID == paroRelacionadoID && p.StatusDelete == false)
                    .OrderByDescending(p => p.FechaEvento)
                    .ToList();

                if (eventosRelacionados.Any())
                {
                    var ultimoEvento = eventosRelacionados.Where(p => p.TipoEventoID != INACTIVE_EVENT).First();

                    double diferenciaDias = (fechaCierre.Date - ultimoEvento.FechaEvento.Value.Date).TotalDays;

                    // Si la diferencia es de 2 días o más, y el tipo de evento no es ACTIVE o DAY_DELAY
                    if (diferenciaDias >= 1 &&
                        ultimoEvento.TipoEventoID != ACTIVE_EVENT &&
                        ultimoEvento.TipoEventoID != DAY_DELAY_EVENT)
                    {
                        return Json(new
                        {
                            success = false,
                            message = "No se puede cerrar el reporte porque hay paros que necesitan un day delay para poder documentar."
                        });
                    }
                }
            }

            return Json(new { success = true });
        }



        [HttpGet]
        public JsonResult GetUltimaFechaCierre()
        {
            var cierre = _dvpEntities.CierreStatus
                .OrderByDescending(c => c.FechaReporte)
                .FirstOrDefault();

            DateTime fechaBase;
            if (cierre == null || cierre.FechaReporte == null)
            {
                fechaBase = DateTime.Now.AddDays(-1);
            }
            else
            {
                fechaBase = cierre.FechaReporte.Value.AddDays(1);
            }
            var resultado = new
            {
                FechaReporte = fechaBase.ToString("yyyy-MM-dd")
            };

            return Json(new { success = true, data = resultado }, JsonRequestBehavior.AllowGet);
        }



        private List<object> GetParosSinArranque()
        {
            var paros = _dvpEntities.Paros
                .Where(p => p.FechaEvento.HasValue && p.StatusDelete == false)
                .ToList();

            var parosInactive = paros
                .Where(p => p.TipoEventoID == INACTIVE_EVENT)
                .ToList();

            var parosSinArranque = new List<object>();

            foreach (var paroInactive in parosInactive)
            {
                var paroRelacionadoID = paroInactive.ParoRelacionadoID;

                var tieneArranque = paros.Any(p =>
                    p.ParoRelacionadoID == paroRelacionadoID &&
                    p.TipoEventoID == ACTIVE_EVENT);

                if (!tieneArranque)
                {
                    parosSinArranque.Add(new
                    {
                        _paroId = paroInactive.ParosID,
                        _fechaCreacionParo = paroInactive.FechaCreacion,
                        _fechaEvento = paroInactive.FechaEvento,
                        _comment = paroInactive.Comentario,
                        _equipoId = paroInactive.EquipoID,
                        _equipoName = paroInactive.Equipo.Descripcion,
                        _componenteEquipoName = paroInactive.ComponenteEquipo.Descripcion,
                        _tipoFallaName = paroInactive.TipoFalla.Descripcion,
                        _clasificacionName = paroInactive.Clasificacion.Descripcion,
                        _statusValidate = paroInactive.StatusValidate,
                        _statusDelete = paroInactive.StatusDelete,
                        _tipoEventoId = paroInactive.TipoEventoID,
                        _tipoEventoName = paroInactive.TipoEvento.Descripcion,
                        _paroRelacionadoId = paroRelacionadoID,
                        _cerrado = paroInactive.Cerrado
                    });
                }
            }

            return parosSinArranque;
        }




        public const int INACTIVE_EVENT = 1;
        public const int DAY_DELAY_EVENT = 2;
        public const int ACTIVE_EVENT = 3;
        public const int RECLASIFICATION_EVENT = 4;


    }
}