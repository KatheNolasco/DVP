//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace DataAccess
{
    using System;
    using System.Collections.Generic;
    
    public partial class Inventarios
    {
        public int SalidasID { get; set; }
        public Nullable<int> MaterialSAP { get; set; }
        public string Descripcion { get; set; }
        public Nullable<decimal> Cantidad { get; set; }
        public Nullable<System.DateTime> FechaTransaccion { get; set; }
        public string TipoMovimientoSAP { get; set; }
        public Nullable<System.DateTime> FehaReporte { get; set; }
    }
}
