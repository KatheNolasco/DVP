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
    
    public partial class Usuario
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public Usuario()
        {
            this.TokenRegistro = new HashSet<TokenRegistro>();
            this.UsuarioRol = new HashSet<UsuarioRol>();
            this.Paros = new HashSet<Paros>();
        }
    
        public int UsuarioID { get; set; }
        public string Descripcion { get; set; }
        public string Email { get; set; }
        public string Nombre { get; set; }
        public string ContraseñaHash { get; set; }
        public string ADUsername { get; set; }
        public bool EsAD { get; set; }
        public Nullable<System.DateTime> FechaMigracionAD { get; set; }
        public string Token { get; set; }
        public Nullable<System.DateTime> UltimoLogin { get; set; }
        public string NumeroEmpleado { get; set; }
        public Nullable<int> PlantaID { get; set; }
        public Nullable<int> UnidadOperativaID { get; set; }
        public Nullable<int> PaisID { get; set; }
        public Nullable<int> GerenciaID { get; set; }
        public bool Active { get; set; }
    
        public virtual Gerencia Gerencia { get; set; }
        public virtual Pais Pais { get; set; }
        public virtual Planta Planta { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<TokenRegistro> TokenRegistro { get; set; }
        public virtual UnidadOperativa UnidadOperativa { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<UsuarioRol> UsuarioRol { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<Paros> Paros { get; set; }
    }
}
