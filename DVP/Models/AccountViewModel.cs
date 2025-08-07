using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DVP.Models
{
    public class AccountViewModel
    {
        public int _usuarioId { get; set; }
        public string _descripcion { get; set; }
        public string _email { get; set; }
        public string _nombre { get; set; }
        public string _contraseñaHash { get; set; }
        public string _aDUsername { get; set; }
        public bool _esAD { get; set; }
        public DateTime? _fechaMigracionAD { get; set; }
        public string _token { get; set; }
        public DateTime? _ultimoLogin { get; set; }
        public string _numeroEmpleado { get; set; }
        public int? _plantaId { get; set; }
        public int? _unidadOperativaId { get; set; }
        public int? _paisId { get; set; }
        public int? _gerenciaId { get; set; }
        public bool _active { get; set; }
        public string _userIdProgreso { get; set; }

        public int? _rolId { get; set; }
    }
}