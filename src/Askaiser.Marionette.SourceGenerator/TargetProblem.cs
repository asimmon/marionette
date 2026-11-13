namespace Askaiser.Marionette.SourceGenerator;

internal enum TargetProblem
{
    None,
    MissingPartialModifier,
    InvalidDirectoryPath,
    NestedOrGenericClass,
    StaticClass,
    DirectoryNotFound,
    UnexpectedException,
}
