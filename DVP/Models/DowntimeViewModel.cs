using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using DataAccess;


namespace DVP.Models
{
    public class DowntimeViewModel
    {
        DataAccess.DVPEntities _dvpEntities = new DataAccess.DVPEntities();

        public int _paroId { get; set; }

        public int _equipoId { get; set; }
        public string _codigoSAP { get; set; }
        public string _nombreEquipo { get; set; }

        public string _descripcion { get; set; }
        public int _procesoId { get; set; }
        public int _procesoDescripcion{ get; set; }
        public string _comment { get; set; }

        public int _plamtaId { get; set; }
        public int _unidadOperativaId { get; set; }
        public string _unidadOperativadescrpipcion { get; set; }
        public int _paisId { get; set; }
        public int? _paroRelacionadoId { get; set; }
        public int _paisDescripcion { get; set; }
        public int _tagId { get; set; }
        public bool _active { get; set; }
        public string _descripcionEquipo { get; set; }
        public DateTime _fechaCreacionParo { get; set; }
        public DateTime _fechaInicio { get; set; }
        public DateTime _fechaEvento { get; set; }
        public DateTime _fechaModificacion { get; set; }
        public TimeSpan _horaEvento { get; set; }
        public TimeSpan _periodoTotal { get; set; }
        public int _cantidadParosId { get; set; }



        public string _equipoName { get; set; }
        public string _subequipoName { get; set; }
        public string _componenteEquipoName { get; set; }
        public string _clasificacionName { get; set; }
        public string _tipoFallaName { get; set; }
        public string _tipoEventoName { get; set; }
        public string _usuarioCreadorName { get; set; }


        public int _validacionReporteId { get; set; }





        public const int INACTIVE_EVENT = 1;
        public const int DAY_DELAY_EVENT = 2;
        public const int ACTIVE_EVENT = 3;
        public const int RECLASIFICATION_EVENT = 4;



        public int _subEquipoId { get; set; }
        
        public int _componenteEquipoId { get; set; }

        public int _clasificacionId { get; set; }
        public int _tipoFallaId { get; set; }
        public int _tipoEventoId { get; set; }

        public bool? _statusValidate { get; set; }
        public bool? _statusDelete { get; set; }
        public int _usuarioCreadorID { get; set; }

        public IEnumerable<Equipo> Equipos { get; set; }
        public IEnumerable<TipoEvento> TipoEventos { get; set; }

        public bool? _cerrado { get; set; }
        public DateTime _fechaCierre { get; set; }
        public DateTime _fechaReporte { get; set; }


        public bool Success { get; set; }
        public string Message { get; set; }



        public IEnumerable<Equipo> GetEquipos()
        {
            IEnumerable<Equipo> listaEquipos = _dvpEntities.Equipo.ToList();
            return listaEquipos;
        }

        public IEnumerable<TipoEvento> GetTipoEvento()
        {
            IEnumerable<TipoEvento> listatipoevento = _dvpEntities.TipoEvento.ToList();
            return listatipoevento;
        }

        public IEnumerable<Clasificacion> GetClasificacionFalla()
        {
            IEnumerable<Clasificacion> listaclasificacion = _dvpEntities.Clasificacion.ToList();
            return listaclasificacion;
        }


        public List<DowntimeViewModel> GetdowntimeListByDate(DateTime fecha)
        {
            var model = _dvpEntities.Paros
                .Where(p => p.FechaEvento.HasValue && p.FechaEvento.Value.Date == fecha.Date)
                .Select(choose => new DowntimeViewModel()
                {
                    _paroId = choose.ParosID,
                    _fechaCreacionParo = choose.FechaCreacion.Value,
                    _fechaEvento = choose.FechaEvento.Value,
                    _comment = choose.Comentario,
                    _equipoId = choose.EquipoID.Value,
                    _equipoName = choose.Equipo.Descripcion,
                    _subequipoName = choose.SubEquipo.Descripcion,
                    _componenteEquipoName = choose.ComponenteEquipo.Descripcion,
                    _tipoFallaName = choose.TipoFalla.Descripcion,
                    _clasificacionName = choose.Clasificacion.Descripcion,
                    _statusValidate = choose.StatusValidate.Value,
                    _statusDelete = choose.StatusDelete.Value,
                    _tipoEventoName = choose.TipoEvento.Descripcion,
                    _usuarioCreadorName = choose.Usuario.Nombre
                })
                .ToList();

            return model;
        }




    }
}