using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Persistence.Common
{
    public static class DateTimeHelper
    {
        public static DateTime Now()
            => DateTime.SpecifyKind(
                DateTime.UtcNow,
                DateTimeKind.Unspecified);
    }
}