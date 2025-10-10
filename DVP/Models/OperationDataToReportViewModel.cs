using DataAccess;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;


namespace DVP.Models
{
    public class OperationDataToReportViewModel
    {
        DataAccess.DVPEntities _dvpEntities = new DataAccess.DVPEntities();

        public int _dataOperacionId { get; set; }
        public int _equipoId { get; set; }
        public string _equipoDescripcion { get; set; }
        public int _tipoOperacionId { get; set; }
        public string _tipoOperacionDescripcion { get; set; }
        public int _materialId { get; set; }
        public string _materialDescripcion { get; set; }
        public decimal _cantidadPims { get; set; }
        public decimal? _cantidadValidada { get; set; }
        public int _unidadMedidaId { get; set; }
        public string _unidadMedidaDescripcion { get; set; }
        public int? _tipoMovimientoSapId { get; set; }
        public string _tipoMovimientoDescripcion { get; set; }
        public DateTime _fechaReporte { get; set; }
        public string _ordenProcesoSAP { get; set; }
        public List<int> _equipoIds { get; set; }
        public List<int> _materialIds { get; set; }
        public bool _statusClose { get; set; }
        public bool _statusValidate { get; set; }


    }
}