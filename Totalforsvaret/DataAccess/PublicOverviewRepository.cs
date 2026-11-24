using Microsoft.EntityFrameworkCore;
using Totalforsvaret.Areas.Offentlig.Models;
using Totalforsvaret.Models;

namespace Totalforsvaret.DataAccess;

internal sealed class PublicOverviewRepository(AppDbContext db) : IPublicOverviewRepository
{
    private IQueryable<Need> ActiveNeeds => db.Needs.AsNoTracking()
        .Where(need => need.Status == "Åpen" || need.Status == "Under behandling");

    public int CountActiveNeeds() => ActiveNeeds.Count();
    public int CountResolvedNeeds() => db.Needs.Count(need => need.Status == "Løst");
    public int CountActiveCrises() => db.Crises.Count(crisis => crisis.Status == "Aktiv");

    public List<Need> ListActiveNeedsOnMap() =>
        ActiveNeeds.Where(need => need.Latitude != null && need.Longitude != null).ToList();

    public List<Need> ListLatestActiveNeeds() =>
        ActiveNeeds.OrderByDescending(need => need.Id).Take(5).ToList();

    public List<Need> ListActiveNeeds() => ActiveNeeds.ToList();

    public List<CrisisMapAnnotation> ListAnnotations() =>
        db.CrisisMapAnnotations.AsNoTracking().ToList();

    public List<ResourceAssignment> ListActiveAssignments() => db.ResourceAssignments.AsNoTracking()
        .Where(assignment => assignment.Status == AssignmentStatus.Requested ||
                             assignment.Status == AssignmentStatus.Assigned)
        .ToList();

    public List<ResourceAssignment> ListClosedReviewsForActiveNeeds() => db.ResourceAssignments.AsNoTracking()
        .Where(assignment => (assignment.Status == AssignmentStatus.Rejected ||
                              assignment.Status == AssignmentStatus.Released) &&
                             (assignment.Need.Status == "Åpen" || assignment.Need.Status == "Under behandling"))
        .ToList();

    public List<OverviewCrisis> ListCrises() => db.Crises.AsNoTracking()
        .Select(crisis => new OverviewCrisis(crisis.Id, crisis.Name, crisis.Type, crisis.Area,
            crisis.Severity, crisis.StartTime, crisis.Status, crisis.Description, crisis.Latitude, crisis.Longitude))
        .ToList();

    public List<OverviewNeed> ListNeeds() => db.Needs.AsNoTracking()
        .Where(need => need.Status == "Åpen" || need.Status == "Under behandling" || need.Status == "Løst")
        .Select(need => new OverviewNeed(need.Id, need.Name, need.Type, need.Priority, need.Status,
            need.CrisisId, need.Crisis != null ? need.Crisis.Name : "", need.Latitude, need.Longitude))
        .ToList();
}
