using DataAccess;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DVP.Models
{
    public class EquipoViewModel
    {

        DataAccess.DVPEntities _dvpEntities = new DataAccess.DVPEntities();

        public int _equipoId { get; set; }
        public string _descripcion { get; set; }
        public string _condigoDetalle { get; set; }
        public string _condigoSAP { get; set; }
        public int? _procesoId { get; set; }
        public string _procesoDescripcion { get; set; }
        public int? _plantaId { get; set; }
        public string _plantaDescripcion { get; set; }
        public int? _unidadOperativaId { get; set; }
        public string _unidadOperativaDescripcion { get; set; }
        public int? _paisId { get; set; }
        public string _paisDescripcion { get; set; }
        public bool _buscarParo { get; set; }
        public bool _active { get; set; }
        public bool _enviarASAP { get; set; }
        public DateTime _fechaCreacion { get; set; }
        public int _personasAsignadas { get; set; }



    }
}