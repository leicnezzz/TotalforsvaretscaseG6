using Aspire.Hosting.ApplicationModel;

namespace Totalforsvaret.AppHost;

/// <summary>
/// Represents a database in the Aspire dashboard using its parent container as the runtime resource.
/// </summary>
public sealed class DatabaseDisplayResource : Resource, IResourceWithParent<ContainerResource>
{
    public DatabaseDisplayResource(string name, ContainerResource parent) : base(name)
    {
        Parent = parent;
    }

    public ContainerResource Parent { get; }
}
