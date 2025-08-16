using DataAccess;
using DVP.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace DVP.Controllers
{
    public class TagController : Controller
    {
        DataAccess.DVPEntities _dvpEntities = new DataAccess.DVPEntities();
        // GET: Tag
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

            var query = _dvpEntities.Usuario.AsQueryable();

            if (rol != "Desarrollador de Software" && rol != "Administrador de la información")
            {
                return RedirectToAction("Index", "Account");
            }

            return View();
        }

        [HttpGet]
        public JsonResult GetTipoOperacion()
        {

            var tiposOperacion = _dvpEntities.TipoOperacion
                                     .Select(s => new
                                     {
                                         TipoOperacionID = s.TipoOperacionID,
                                         Descripcion = s.Descripcion
                                     })
                                     .ToList();

            return Json(tiposOperacion, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult CreateTag(TagViewModel data)
        {
            try
            {
                if (!ModelState.IsValid)
                    return Json(new { success = false, message = "Datos inválidos." });

                if (data._equipoId == null || data._tipoOperacionId == null)
                    return Json(new { success = false, message = "Equipo y Tipo de operación son obligatorios." });

                // DUP CHECK: ya existe un Tag para ese Equipo y ese TipoOperacion
                bool yaExiste = _dvpEntities.TagEquipo.Any(t =>
                    t.EquipoID == data._equipoId &&
                    t.TipoOperacionID == data._tipoOperacionId
                );

                if (yaExiste)
                    return Json(new { success = false, message = "Ya existe un tag con ese Tipo de operación para este equipo." });

                var nuevo = new TagEquipo
                {
                    Descripcion = data._descripcion,
                    TagName = data._tagName,
                    TagCode = data._tagCode,
                    Activo = data._activo,
                    TipoOperacionID = data._tipoOperacionId,
                    EquipoID = data._equipoId
                };

                _dvpEntities.TagEquipo.Add(nuevo);
                _dvpEntities.SaveChanges();

                return Json(new { success = true, message = "Creado exitosamente." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error al crear el tag: " + ex.Message });
            }
        }

        [HttpPost]
        public JsonResult EditTag(TagViewModel data)
        {
            try
            {
                if (!ModelState.IsValid)
                    return Json(new { success = false, message = "Datos inválidos para editar." });

                if (data._equipoId == null || data._tipoOperacionId == null)
                    return Json(new { success = false, message = "Equipo y Tipo de operación son obligatorios." });

                var existente = _dvpEntities.TagEquipo.FirstOrDefault(e => e.TagEquipoID == data._tagEquipoId);
                if (existente == null)
                    return Json(new { success = false, message = "No encontrado." });

                // DUP CHECK: excluye el registro que se esta editando
                bool yaExisteOtro = _dvpEntities.TagEquipo.Any(t =>
                    t.EquipoID == data._equipoId &&
                    t.TipoOperacionID == data._tipoOperacionId &&
                    t.TagEquipoID != data._tagEquipoId
                );

                if (yaExisteOtro)
                    return Json(new { success = false, message = "Ya existe otro tag con ese Tipo de operación para este equipo." });

                existente.Descripcion = data._descripcion;
                existente.TagName = data._tagName;
                existente.TagCode = data._tagCode;
                existente.Activo = data._activo;
                existente.TipoOperacionID = data._tipoOperacionId;
                existente.EquipoID = data._equipoId;

                _dvpEntities.SaveChanges();

                return Json(new { success = true, message = "Actualizado exitosamente." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error al editar el tag: " + ex.Message });
            }
        }


        [HttpGet]
        public JsonResult GetTagId(int tagId)
        {
            var paro = _dvpEntities.TagEquipo
                .Where(p => p.TagEquipoID == tagId)
                .Select(p => new
                {
                    _tagEquipoId = p.TagEquipoID,
                    _descripcion = p.Descripcion,
                    _tagName = p.TagName,
                    _tagCode = p.TagCode,
                    _activo = p.Activo,
                    _tipoOperacionId = p.TipoOperacionID,
                    _tipoOperacionDescripcion = p.TipoOperacion.Descripcion,
                    _equipoId = p.EquipoID,
                    _equipoDescripcion = p.Equipo.Descripcion
                })
                .FirstOrDefault();

            if (paro == null)
            {
                return Json(new { success = false, message = "No encontrado" }, JsonRequestBehavior.AllowGet);
            }

            return Json(new { success = true, data = paro }, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult GetTags()
        {
            var equipos = _dvpEntities.TagEquipo
                .Select(p => new
                {
                    _tagEquipoId = p.TagEquipoID,
                    _descripcion = p.Descripcion,
                    _tagName = p.TagName,
                    _tagCode = p.TagCode,
                    _activo = p.Activo,
                    _tipoOperacionId = p.TipoOperacionID,
                    _tipoOperacionDescripcion = p.TipoOperacion.Descripcion,
                    _equipoId = p.EquipoID,
                    _equipoDescripcion = p.Equipo.Descripcion
                })
                .ToList();

            if (equipos == null)
            {
                return Json(new { success = false, message = "No encontrado" }, JsonRequestBehavior.AllowGet);
            }

            return Json(new { success = true, data = equipos }, JsonRequestBehavior.AllowGet);
        }
    }
}