using Aspire.Hosting.ApplicationModel;

namespace Totalforsvaret.AppHost;

/// <summary>
/// Represents a database in the Aspire dashboard using its parent container as the runtime resource.
/// </summary>
public sealed class DatabaseDisplayResource(
    string name,
    ContainerResource parent)
    : Resource(name), IResourceWithParent<ContainerResource>
{
    public ContainerResource Parent { get; } = parent;
}
