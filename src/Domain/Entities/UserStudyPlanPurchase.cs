using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class UserStudyPlanPurchase
    {
        public Guid UserId { get; set; }
        public Guid StudyPlanId { get; set; }
        public DateTime PurchasedAt { get; set; }

        public virtual User User { get; set; } = null!;
        public virtual StudyPlan StudyPlan { get; set; } = null!;
    }
}
