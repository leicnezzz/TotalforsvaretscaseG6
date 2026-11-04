using Aspire.Hosting.ApplicationModel;

namespace Totalforsvaret.AppHost;

public sealed class DatabaseDisplayResource(
    string name,
    ContainerResource parent)
    : Resource(name), IResourceWithParent<ContainerResource>
{
    public ContainerResource Parent { get; } = parent;
}
