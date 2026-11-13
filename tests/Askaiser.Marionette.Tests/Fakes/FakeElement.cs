namespace Askaiser.Marionette.Tests.Fakes;

internal sealed class FakeElement(string name) : IElement
{
    public string Name { get; } = name;

    public override string ToString() => this.Name;
}
