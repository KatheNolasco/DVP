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
                                         Descripcion = s.Descripcion,
                                         AfectaInventario = s.AfectaInventario,
                                     })
                                     .ToList();

            return Json(tiposOperacion, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult CreateTag(TagViewModel data)
        {
            try
            {
                data._descripcion = (data._descripcion ?? "").Trim();
                data._tagName = (data._tagName ?? "").Trim();
                data._tagCode = (data._tagCode ?? "").Trim();

                if (!TryValidateModel(data))
                    return Json(new
                    {
                        success = false,
                        message = "Datos inválidos.",
                        errors = ModelState.ToDictionary(
                        kvp => kvp.Key, kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray())
                    });

                if (data._equipoId == null || data._tipoOperacionId == null)
                    return Json(new { success = false, message = "Equipo y Tipo de operación son obligatorios." });

                bool yaExiste;
                if (data._materialId.HasValue)
                {
                    int materialId = data._materialId.Value;
                    yaExiste = _dvpEntities.TagEquipo.Any(t =>
                        t.EquipoID == data._equipoId.Value &&
                        t.TipoOperacionID == data._tipoOperacionId.Value &&
                        t.MaterialID == materialId &&
                        t.MaterialProducidoID == data._materialProducidoId
                    );
                }
                else
                {
                    yaExiste = _dvpEntities.TagEquipo.Any(t =>
                        t.EquipoID == data._equipoId.Value &&
                        t.TipoOperacionID == data._tipoOperacionId.Value &&
                        t.MaterialID == null &&
                        t.MaterialProducidoID == data._materialProducidoId
                    );
                }

                if (yaExiste)
                    return Json(new { success = false, message = "Ya existe un tag con esa combinación (Equipo, Tipo, Material Consumido y Material Producido)." });

                var nuevo = new TagEquipo
                {
                    Descripcion = data._descripcion,
                    TagName = data._tagName,
                    TagCode = data._tagCode,
                    Activo = true,
                    TipoOperacionID = data._tipoOperacionId.Value,
                    EquipoID = data._equipoId.Value,
                    MaterialID = data._materialId,
                    MaterialProducidoID = data._materialProducidoId
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

                bool yaExiste = _dvpEntities.TagEquipo.Any(t =>
                         t.TagEquipoID != data._tagEquipoId && 
                         (
                             (t.EquipoID == data._equipoId &&
                              t.TagCode == data._tagCode &&
                              t.TagName == data._tagName &&
                              t.TipoOperacionID == data._tipoOperacionId &&
                              t.MaterialID == data._materialId &&
                              t.MaterialProducidoID == data._materialProducidoId)

                              ||

                               t.MaterialID == data._materialId &&
                               t.MaterialProducidoID == data._materialProducidoId &&
                               t.TipoOperacionID == data._tipoOperacionId 
                         )
                       );

                if (yaExiste)
                    return Json(new { success = false, message = "Ya existe otro tag con ese Tipo de operación para este equipo ó material." });

                existente.Descripcion = data._descripcion;
                existente.TagName = data._tagName;
                existente.TagCode = data._tagCode;
                existente.Activo = data._activo;
                existente.TipoOperacionID = data._tipoOperacionId;
                existente.EquipoID = data._equipoId;
                existente.MaterialID = data._materialId;
                existente.MaterialProducidoID = data._materialProducidoId;

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
                    _equipoDescripcion = p.Equipo.Descripcion,
                    _materialId = p.MaterialID,
                    _materialDescripcion = p.Material.Descripcion,
                    _materialProducidoId = p.MaterialProducidoID,
                    _materialPProducidoDescripcion = p.Material1.Descripcion
                })
                .FirstOrDefault();

            if (paro == null)
            {
                return Json(new { success = false, message = "No encontrado" }, JsonRequestBehavior.AllowGet);
            }

            return Json(new { success = true, data = paro }, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult GetTagsByPlantAndEquipment(string equipoId = null, string tipoOperacionId = null, string materialProducidoId = null)
        { 
            try
            {
                var tokenEnSession = Session["token"] as string;
                if (string.IsNullOrEmpty(tokenEnSession))
                    return Json(new { success = false, message = "Sesión no iniciada" }, JsonRequestBehavior.AllowGet);

                var usuario = _dvpEntities.Usuario
                    .AsNoTracking()
                    .FirstOrDefault(u => u.Token == tokenEnSession);

                if (usuario == null)
                    return Json(new { success = false, message = "Usuario no encontrado" }, JsonRequestBehavior.AllowGet);

                if (usuario.PlantaID == null)
                    return Json(new { success = false, message = "El usuario no tiene planta asignada" }, JsonRequestBehavior.AllowGet);

                int? targetEquipoId = string.IsNullOrEmpty(equipoId) ? null : (int?)int.Parse(equipoId);
                int? targetTipoOperacionId = string.IsNullOrEmpty(tipoOperacionId) ? null : (int?)int.Parse(tipoOperacionId);
                int? targetMaterialProducidoId = string.IsNullOrEmpty(materialProducidoId) ? null : (int?)int.Parse(materialProducidoId);


                var projectedTags = _dvpEntities.TagEquipo
                    .AsNoTracking()
                    .Where(t => t.Equipo != null && t.Equipo.PlantaID == usuario.PlantaID)
                    .Where(t => targetEquipoId == null || t.EquipoID == targetEquipoId)
                    .Where(t => targetTipoOperacionId == null || t.TipoOperacionID == targetTipoOperacionId)
                    .Where(t => targetMaterialProducidoId == null || t.MaterialProducidoID == targetMaterialProducidoId)

                    .Select(p => new
                    {
                        _tagEquipoId = p.TagEquipoID,
                        _descripcion = p.Descripcion,
                        _tagName = p.TagName,
                        _tagCode = p.TagCode,
                        _activo = p.Activo,
                        _tipoOperacionId = p.TipoOperacionID,
                        _tipoOperacionDescripcion = p.TipoOperacion != null ? p.TipoOperacion.Descripcion : null,
                        _equipoId = p.EquipoID,
                        _equipoDescripcion = p.Equipo != null ? p.Equipo.Descripcion : null,
                        _materialDescripcion = p.Material != null ? p.Material.Descripcion : null,
                        _materialProducidoDescripcion = p.Material1 != null ? p.Material1.Descripcion : null,
                        _materialProducidoId = p.MaterialProducidoID
                    });


                var tags = projectedTags
                    .Distinct()
                    .OrderBy(x => x._equipoDescripcion)
                    .ThenBy(x => x._descripcion)
                    .ToList();

                if (tags.Count == 0)
                    return Json(new { success = false, message = "No se encontraron tags para los filtros aplicados." }, JsonRequestBehavior.AllowGet);

                return Json(new { success = true, data = tags }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error al obtener tags: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public JsonResult EliminarTag(int id)
        {
            try
            {
                var tag = _dvpEntities.TagEquipo
                    .FirstOrDefault(t => t.TagEquipoID == id);

                if (tag == null)
                    return Json(new { ok = false, msg = "Tag no encontrado" }, JsonRequestBehavior.AllowGet);

                _dvpEntities.TagEquipo.Remove(tag);
                _dvpEntities.SaveChanges();

                return Json(new { ok = true }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { ok = false, msg = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }


    }


}