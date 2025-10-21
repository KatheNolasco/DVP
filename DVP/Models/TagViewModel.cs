using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DVP.Models
{
    public class TagViewModel
    {
        DataAccess.DVPEntities _dvpEntities = new DataAccess.DVPEntities();
        public int _tagEquipoId { get; set; }
        public string _descripcion { get; set; }
        public string _tagName { get; set; }
        public string _tagCode { get; set; }
        public bool _activo { get; set; }
        public int? _tipoOperacionId { get; set; }
        public string _tipoOperacionDescripcion { get; set; }
        public int? _equipoId { get; set; }
        public string _equipoDescripcion { get; set; }
        public int? _materialId { get; set; }
        public string _materialDescripcion { get; set; }
        public int? _materialProducidoId { get; set; }
        public string _materialProducidoDescripcion { get; set; }

    }
}