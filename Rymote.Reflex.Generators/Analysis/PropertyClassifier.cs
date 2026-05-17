using Microsoft.CodeAnalysis;

namespace Rymote.Reflex.Generators.Analysis;

internal static class PropertyClassifier
{
    internal static ReactivePropertyKind Classify(IPropertySymbol propertySymbol)
    {
        foreach (AttributeData attribute in propertySymbol.GetAttributes())
        {
            string? attributeName = attribute.AttributeClass?.Name;
            if (attributeName == "ReactiveIgnoreAttribute") return ReactivePropertyKind.Ignored;
        }

        if (propertySymbol.IsPartialDefinition && propertySymbol.SetMethod is null)
            return ReactivePropertyKind.TrackedComputed;

        if (propertySymbol.SetMethod is null && propertySymbol.GetMethod is not null)
            return ReactivePropertyKind.TrackedComputed;

        if (propertySymbol.Type is INamedTypeSymbol namedType)
        {
            string genericDefinitionName = namedType.ConstructedFrom?.ToDisplayString() ?? "";
            if (genericDefinitionName == "System.Collections.Generic.List<T>"
                || genericDefinitionName == "System.Collections.Generic.Dictionary<TKey, TValue>"
                || genericDefinitionName == "System.Collections.Generic.HashSet<T>")
                return ReactivePropertyKind.TrackedCollection;

            foreach (INamedTypeSymbol implementedInterface in namedType.AllInterfaces)
                if (implementedInterface.ToDisplayString() == "Rymote.Reflex.Utilities.IReactive")
                    return ReactivePropertyKind.TrackedNestedReactive;
        }

        return ReactivePropertyKind.TrackedAuto;
    }
}
