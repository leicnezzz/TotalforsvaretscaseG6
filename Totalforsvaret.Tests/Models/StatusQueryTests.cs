using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Totalforsvaret.DataAccess;

namespace Totalforsvaret.Tests.Models;

public class StatusQueryTests
{
    [Test]
    public void MariaDbTranslatesSharedStatusFilters()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseMySql("Server=127.0.0.1;Database=translation_test;User ID=translation_test",
                new MariaDbServerVersion(new Version(11, 4, 0)))
            .Options;
        using var db = new AppDbContext(options);

        var needsSql = db.Needs.Where(need =>
            StatusFilters.ActiveNeedStatuses.Contains(need.Status)).ToQueryString();
        var assignmentsSql = db.ResourceAssignments.Where(assignment =>
            StatusFilters.BusyAssignmentStatuses.Contains(assignment.Status) &&
            StatusFilters.ActiveNeedStatuses.Contains(assignment.Need.Status)).ToQueryString();

        Assert.That(needsSql, Does.Contain("Under behandling"));
        Assert.That(assignmentsSql, Does.Contain("Under behandling"));
    }
}
