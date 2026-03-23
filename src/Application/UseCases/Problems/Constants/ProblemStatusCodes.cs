using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.UseCases.Problems.Constants;

public static class ProblemStatusCodes
{
    public const string Draft = "draft";
    public const string PendingReview = "pending_review";
    public const string Approved = "approved";
    public const string Published = "published";
    public const string Rejected = "rejected";
    public const string Archived = "archived";
}
