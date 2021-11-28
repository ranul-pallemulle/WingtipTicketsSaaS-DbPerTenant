using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Events_TenantUserApp.EF.TenantsDB
{
    public partial class DatabaseFirewallRules
    {
        [Key]
        [Column("id", Order = 0)]
        public int Id { get; set; }

        [Key]
        [Column("name", Order = 1)]
        public string Name { get; set; }

        [Key]
        [Column("start_ip_address", Order = 2)]
        [StringLength(45)]
        public string StartIpAddress { get; set; }

        [Key]
        [Column("end_ip_address", Order = 3)]
        [StringLength(45)]
        public string EndIpAddress { get; set; }

        [Key]
        [Column("create_date", Order = 4)]
        public DateTime CreateDate { get; set; }

        [Key]
        [Column("modify_date", Order = 5)]
        public DateTime ModifyDate { get; set; }
    }
}