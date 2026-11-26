using Microsoft.EntityFrameworkCore;
using Totalforsvaret.Areas.Offentlig.Models;
using Totalforsvaret.Models;

namespace Totalforsvaret.DataAccess;

internal sealed class PublicOverviewRepository(AppDbContext db) : IPublicOverviewRepository
{
    private IQueryable<Need> ActiveNeeds => db.Needs.AsNoTracking()
        .Where(need => StatusFilters.ActiveNeedStatuses.Contains(need.Status));

    public int CountActiveNeeds() => ActiveNeeds.Count();

    public List<Need> ListActiveNeedsOnMap() =>
        ActiveNeeds.Where(need => need.Latitude != null && need.Longitude != null).ToList();

    public List<Need> ListActiveNeeds() => ActiveNeeds.ToList();

    public List<CrisisMapAnnotation> ListAnnotations() =>
        db.CrisisMapAnnotations.AsNoTracking().ToList();

    public List<ResourceAssignment> ListActiveAssignments() => db.ResourceAssignments.AsNoTracking()
        .Include(assignment => assignment.Resource)
        .WhereResourceIsBusy()
        .ToList();

    public List<ResourceAssignment> ListClosedReviewsForActiveNeeds() => db.ResourceAssignments.AsNoTracking()
        .Where(assignment => (assignment.Status == AssignmentStatus.Rejected ||
                              assignment.Status == AssignmentStatus.Released) &&
                             StatusFilters.ActiveNeedStatuses.Contains(assignment.Need.Status))
        .ToList();

    public List<OverviewCrisis> ListCrises() => db.Crises.AsNoTracking()
        .Select(crisis => new OverviewCrisis(crisis.Id, crisis.Name, crisis.Type, crisis.Area,
            crisis.Severity, crisis.StartTime, crisis.Status, crisis.Description, crisis.Latitude, crisis.Longitude))
        .ToList();

    public List<OverviewNeed> ListNeeds() => db.Needs.AsNoTracking()
        .Where(need => StatusFilters.ActiveNeedStatuses.Contains(need.Status) || need.Status == NeedStatus.Resolved)
        .Select(need => new OverviewNeed(need.Id, need.Name, need.Type, need.Priority, need.Status,
            need.CrisisId, need.Crisis != null ? need.Crisis.Name : "", need.Latitude, need.Longitude))
        .ToList();
}
