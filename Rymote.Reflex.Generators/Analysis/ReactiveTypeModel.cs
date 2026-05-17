using System.Collections.Generic;

namespace Rymote.Reflex.Generators.Analysis;

internal sealed class ReactiveTypeModel
{
    internal string NamespaceName { get; }
    internal string ClassName { get; }
    internal bool IsAbstract { get; }
    internal IReadOnlyList<ReactivePropertyModel> Properties { get; }

    internal ReactiveTypeModel(
        string namespaceName,
        string className,
        bool isAbstract,
        IReadOnlyList<ReactivePropertyModel> properties)
    {
        NamespaceName = namespaceName;
        ClassName = className;
        IsAbstract = isAbstract;
        Properties = properties;
    }
}
