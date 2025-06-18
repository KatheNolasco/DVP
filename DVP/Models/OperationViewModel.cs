using DataAccess;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DVP.Models
{
    public class OperationViewModel
    {
        DataAccess.DVPEntities _dvpEntities = new DataAccess.DVPEntities();

        public IEnumerable<Equipo> GetEquipos()
        {
            IEnumerable<Equipo> listaEquipos = _dvpEntities.Equipo.ToList();
            return listaEquipos;
        }

        public IEnumerable<Equipo> Equipos { get; set; }

    }
}