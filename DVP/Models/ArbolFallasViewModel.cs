using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace DVP.Models
{
    public class ArbolFallasViewModel
    {
        DataAccess.DVPEntities _dvpEntities = new DataAccess.DVPEntities();


        [Table("SubEquipo")]
        public class SubEquipo
        {
            [Key]
            [Column("SubEquipoID")]
            public int? _subEquipoId { get; set; }

            [Column("Descripcion")]
            public string _descripcion { get; set; }

            [Column("EquipoID")]
            public int? _equipoId { get; set; }

            [Column("CodigoDet")]
            public string _codigoDet { get; set; }
        }

        [Table("ComponenteEquipo")]
        public class ComponenteEquipo
        {
            [Key]
            [Column("ComponenteEquipoID")]
            public int? _componenteEquipoId { get; set; }

            [Column("Descripcion")]
            public string _descripcion { get; set; }

            [Column("SubEquipoID")]
            public int? _subEquipoId { get; set; }

            [Column("CodigoDet")]
            public string _codigoDet { get; set; }
        }

        [Table("Clasificacion")]
        public class Clasificacion
        {
            [Key]
            [Column("ClasificacionID")]
            public int? _clasificacionId { get; set; }

            [Column("Descripcion")]
            public string _descripcion { get; set; }

            [Column("Ajeno")]
            public bool _ajeno { get; set; }

            [Column("AfectaTMEF")]
            public bool _afectaTMEF { get; set; }

            [Column("CodigoDet")]
            public string _codigoDet { get; set; }

            [Column("TipoParoID")]
            public int? _tipoParoId { get; set; }
        }

        [Table("TipoFalla")]
        public class TipoFalla
        {
            [Key]
            [Column("TipoFallaID")]
            public int? _tipoFallaId { get; set; }

            [Column("Descripcion")]
            public string _descripcion { get; set; }

            [Column("ClasificacionID")]
            public int? _clasificacionId { get; set; }

            [Column("ComponenteEquipoID")]
            public int? _componenteEquipoId { get; set; }

            [Column("CodigoDet")]
            public string _codigoDet { get; set; }
        }

        [Table("TipoParo")]
        public class TipoParo
        {
            [Key]
            [Column("TipoParoID")]
            public int? _tipoFallaId { get; set; }

            [Column("Descripcion")]
            public string _descripcion { get; set; }
        }
    }
}