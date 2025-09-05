using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using DataAccess;

namespace DVP.Models
{
    public class MaterialViewModel
    {
        DataAccess.DVPEntities _dvpEntities = new DataAccess.DVPEntities();

        public int _materialID { get; set; }
        public string _descripcion { get; set; }
        public string _codSAPNuevo { get; set; }
        public string _codOldSAP { get; set; }
        public bool? _producido { get; set; }
        public int? _clasificacionMaterialID { get; set; }
        public string _clasificacionMaterialdescripcion { get; set; }
        public bool? _alterno { get; set; }
        public bool? _afectaInventario { get; set; }
        public string _idStock { get; set; }
        public bool? _activo { get; set; }
        public int? _plantaId { get; set; }
        public string _plantaDescripcion { get; set; }
        public int? _unidadMedidaId { get; set; }
        public string _unidadMedidaDescripcion { get; set; }





    }
}