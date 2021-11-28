using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Events_TenantUserApp.EF.TenantsDB
{
    public partial class EventsWithNoTicket
    {
        [Key]
        [Column(Order = 0)]
        public int EventId { get; set; }

        [Key]
        [Column(Order = 1)]
        [StringLength(50)]
        public string EventName { get; set; }

        [StringLength(50)]
        public string Subtitle { get; set; }

        [Key]
        [Column(Order = 2)]
        public DateTime Date { get; set; }
    }
}