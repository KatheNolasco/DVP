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
    
    public partial class CierreStatus
    {
        public int CierreStatusID { get; set; }
        public Nullable<System.DateTime> FechaCierre { get; set; }
        public Nullable<System.DateTime> FechaReporte { get; set; }
        public Nullable<int> ValidacionReportesID { get; set; }
        public Nullable<int> ParosID { get; set; }
        public Nullable<bool> Cerrado { get; set; }
    
        public virtual ValidacionReportes ValidacionReportes { get; set; }
        public virtual Paros Paros { get; set; }
    }
}
