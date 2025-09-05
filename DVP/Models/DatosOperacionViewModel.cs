using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace DVP.Models
{
    public class DatosOperacionViewModel
    {
        DataAccess.DVPEntities _dvpEntities = new DataAccess.DVPEntities();

        [Table("TipoOperacion")]
        public class TipoOperacion
        {
            [Key]
            [Column("TipoOperacionID")]
            public int? _tipoOperacionId { get; set; }

            [Column("Descripcion")]
            public string _descripcion { get; set; }

            [Column("AfectaInventario")]
            public bool _afectaInventario { get; set; }
        }

        [Table("TipoMovimientoSAP")]
        public class TipoMovimientoSAP
        {
            [Key]
            [Column("TipoMovimientoSAPID")]
            public int? _tipoMovimientoSapId { get; set; }

            [Column("Descripcion")]
            public string _descripcion { get; set; }
        }

        [Table("UnidadMedida")]
        public class UnidadMedida
        {
            [Key]
            [Column("UnidadMedidaID")]
            public int? _unidadMedidaID { get; set; }

            [Column("Descripcion")]
            public string _descripcion { get; set; }
        }
    }
}
