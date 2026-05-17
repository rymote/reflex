namespace Rymote.Reflex.Generators.Analysis;

internal sealed class ReactivePropertyModel
{
    internal string DeclaredTypeFullName { get; }
    internal string PropertyName { get; }
    internal ReactivePropertyKind Kind { get; }
    internal bool HasInitializer { get; }
    internal string? InitializerExpression { get; }
    internal bool IsPartialDeclaration { get; }

    internal ReactivePropertyModel(
        string declaredTypeFullName,
        string propertyName,
        ReactivePropertyKind kind,
        bool hasInitializer,
        string? initializerExpression,
        bool isPartialDeclaration)
    {
        DeclaredTypeFullName = declaredTypeFullName;
        PropertyName = propertyName;
        Kind = kind;
        HasInitializer = hasInitializer;
        InitializerExpression = initializerExpression;
        IsPartialDeclaration = isPartialDeclaration;
    }
}
