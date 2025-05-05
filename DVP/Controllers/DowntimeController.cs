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
                    _fechaEvento = p.FechaEvento
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
                var existeParo = _dvpEntities.Paros.Any(p =>
                    p.EquipoID == data._equipoId &&
                    p.FechaEvento == data._fechaEvento &&
                    p.TipoEventoID == data._tipoEventoId
                );

                if (existeParo)
                {
                    return Json(new { success = false, message = "Este paro ya existe en la base de datos." });
                }

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
                };

                _dvpEntities.Paros.Add(nuevoParo);
                _dvpEntities.SaveChanges(); 

                nuevoParo.ParoRelacionadoID = nuevoParo.ParosID;
                _dvpEntities.SaveChanges(); 

                return Json(new { success = true, paroId = nuevoParo.ParosID });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult UpdateDowntime(DowntimeViewModel data)
        {
            if (data == null)
            {
                return Json(new { success = false, message = "Datos inválidos." });
            }

            try
            {
                var paroExistente = _dvpEntities.Paros.FirstOrDefault(p =>
                    p.EquipoID == data._equipoId &&
                    p.FechaEvento == data._fechaEvento &&
                    p.TipoEventoID == data._tipoEventoId
                );

                if (paroExistente == null)
                {
                    return Json(new { success = false, message = "No se encontró el paro para actualizar." });
                }

                paroExistente.SubEquipoID = data._subEquipoId;
                paroExistente.ComponenteEquipoID = data._componenteEquipoId;
                paroExistente.ClasificacionID = data._clasificacionId;
                paroExistente.TipoFallaID = data._tipoFallaId;
                paroExistente.Comentario = data._comment;


                _dvpEntities.SaveChanges();

                return Json(new { success = true, paroId = paroExistente.ParosID });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }







    }
}