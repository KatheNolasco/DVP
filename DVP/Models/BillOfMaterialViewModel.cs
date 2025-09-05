using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DVP.Models
{
    public class BillOfMaterialViewModel
    {
        public int? _billOfMaterialID { get; set; }
        public int? _materialProduccionID { get; set; }
        public string _materialProduccionDescripcion { get; set; }
        public int? _tipoOperacionID { get; set; }
        public string _tipoOperacionDescripcion { get; set; }
        public int? _tipoMovimientoSAPID { get; set; }
        public string _tipoMovimientoSAPDescripcion { get; set; }
        public decimal? _factorConsumo { get; set; }
        public int? _equipoID { get; set; }
        public string _equipoDescripcion { get; set; }
        public bool _consumoSeco { get; set; }
        public bool _consumoHumedo { get; set; }
        public DateTime _fechaBOM { get; set; }
        public int? _materialConsumoID { get; set; }
        public string _materialConsumoDescripcion { get; set; }


    }
}