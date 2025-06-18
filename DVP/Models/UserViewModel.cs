using DataAccess;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DVP.Models
{
    public class UserViewModel
    {
        DataAccess.DVPEntities _dvpEntities = new DataAccess.DVPEntities();

        public int _usuarioID { get; set; }
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
        public int? _plantaID { get; set; }
        public int? _unidadOperativaID { get; set; }
        public int? _paisID { get; set; }
        public int? _gerenciaID { get; set; }
        public bool _active { get; set; }


        public IEnumerable<Usuario> Usuarios { get; set; }


        public IEnumerable<Usuario> GetUsuarios()
        {
            IEnumerable<Usuario> listaUsuarios = _dvpEntities.Usuario.ToList();
            return listaUsuarios;
        }


    }
}