using DataAccess;
using DVP.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Runtime.InteropServices;
using System.Web;
using System.Web.Mvc;

namespace DVP.Controllers
{
    public class EquipoController : Controller
    {
        DataAccess.DVPEntities _dvpEntities = new DataAccess.DVPEntities();

        // GET: Equipo
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
        public JsonResult GetProceso()
        {
           
            var procesos = _dvpEntities.Proceso
                                     .Select(s => new
                                     {
                                         ProcesoID = s.ProcesoID,
                                         Descripcion = s.Descripcion
                                     })
                                     .ToList();

            return Json(procesos, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult GetPlanta()
        {

            var plantas = _dvpEntities.Planta
                                     .Select(s => new
                                     {
                                         PlantaID = s.PlantaID,
                                         Descripcion = s.Descripcion
                                     })
                                     .ToList();

            return Json(plantas, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult GetUnidadOperativa()
        {

            var unidadOperativa = _dvpEntities.UnidadOperativa
                                     .Select(s => new
                                     {
                                         UnidadOperativaID = s.UnidadOperativaID,
                                         Descripcion = s.Descripcion
                                     })
                                     .ToList();

            return Json(unidadOperativa, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult GetPais()
        {

            var paises = _dvpEntities.Pais
                                     .Select(s => new
                                     {
                                         PaisID = s.PaisID,
                                         Descripcion = s.Descripcion
                                     })
                                     .ToList();

            return Json(paises, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult GetEquipoId(int equipoId)
        {
            var paro = _dvpEntities.Equipo
                .Where(p => p.EquipoID == equipoId)
                .Select(p => new
                {
                    _equipoId = p.EquipoID,
                    _descripcion = p.Descripcion,
                    _condigoDetalle = p.CondigoDetalle,
                    _condigoSAP = p.CondigoSAP,
                    _procesoID = p.ProcesoID,
                    _procesoDescripcion = p.Proceso.Descripcion,
                    _plantaID = p.PlantaID,
                    _unidadOperativaID = p.UnidadOperativaID,
                    _paisID = p.PaisID,
                    _buscarParo = p.BuscarParo,
                    _active = p.Active,
                    _enviarASAP = p.EnviarASAP
                })
                .FirstOrDefault();

            if (paro == null)
            {
                return Json(new { success = false, message = "No encontrado" }, JsonRequestBehavior.AllowGet);
            }

            return Json(new { success = true, data = paro }, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult GetEquipos()
        {
            var equipos = _dvpEntities.Equipo
                .Select(p => new
                {
                    _equipoId = p.EquipoID,
                    _descripcion = p.Descripcion,
                    _condigoDetalle = p.CondigoDetalle,
                    _condigoSAP = p.CondigoSAP,
                    _procesoId = p.ProcesoID,
                    _procesoDescripcion = p.Proceso.Descripcion,
                    _plantaId = p.PlantaID,
                    _plantaDescripcion = p.Planta.Descripcion,
                    _unidadOperativaId = p.UnidadOperativaID,
                    _paisId = p.PaisID,
                    _buscarParo = p.BuscarParo,
                    _active = p.Active,
                    _enviarASAP = p.EnviarASAP,
                    _fechaCreacion = p.FechaCreación
                })
                .ToList();

            if (equipos == null)
            {
                return Json(new { success = false, message = "No encontrado" }, JsonRequestBehavior.AllowGet);
            }

            return Json(new { success = true, data = equipos }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult CreateEquipment(EquipoViewModel data)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var nuevoEquipo = new Equipo
                    {
                        Descripcion = data._descripcion,
                        CondigoDetalle = data._condigoDetalle,
                        CondigoSAP = data._condigoSAP,
                        ProcesoID = data._procesoId,
                        PlantaID = data._plantaId,
                        UnidadOperativaID = data._unidadOperativaId,
                        PaisID = data._paisId,
                        BuscarParo = data._buscarParo,
                        Active = data._active,
                        EnviarASAP = data._enviarASAP,
                        FechaCreación = DateTime.Now,
                    };

                    _dvpEntities.Equipo.Add(nuevoEquipo);
                    _dvpEntities.SaveChanges();

                    return Json(new { success = true, message = "Creado exitosamente." });
                }

                return Json(new { success = false, message = "Datos inválidos." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error al crear el equipo: " + ex.Message });
            }
        }


        [HttpPost]
        public JsonResult EditEquipment(EquipoViewModel data)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var equipoExistente = _dvpEntities.Equipo.FirstOrDefault(e => e.EquipoID == data._equipoId);

                    if (equipoExistente == null)
                        return Json(new { success = false, message = "No encontrado." });

                    equipoExistente.Descripcion = data._descripcion;
                    equipoExistente.CondigoDetalle = data._condigoDetalle;
                    equipoExistente.CondigoSAP = data._condigoSAP;
                    equipoExistente.ProcesoID = data._procesoId;
                    equipoExistente.PlantaID = data._plantaId;
                    equipoExistente.UnidadOperativaID = data._unidadOperativaId;
                    equipoExistente.PaisID = data._plantaId;
                    equipoExistente.BuscarParo = data._buscarParo;
                    equipoExistente.Active = data._active;
                    equipoExistente.EnviarASAP = data._enviarASAP;

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
        public JsonResult GetEquiposporPaislogguedUser()
        {

            var tokenEnSession = Session["token"]?.ToString();

            if (string.IsNullOrEmpty(tokenEnSession))
            {
                return Json(new { success = false, message = "Sesión no iniciada" }, JsonRequestBehavior.AllowGet);
            }

            var usuario = _dvpEntities.Usuario.FirstOrDefault(u => u.Token == tokenEnSession);

            if (usuario == null)
            {
                return Json(new { success = false, message = "Usuario no encontrado" }, JsonRequestBehavior.AllowGet);
            }
            var equipos= _dvpEntities.Equipo
                                     .Select(s => new
                                     {
                                         EquipoID = s.EquipoID,
                                         Descripcion = s.Descripcion,
                                         PaisID = s.PaisID
                                     }).Where(p => p.PaisID == usuario.PaisID)
                                     .ToList();

            return Json(equipos, JsonRequestBehavior.AllowGet);
        }


    }
}