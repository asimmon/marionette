namespace Askaiser.Marionette.Tests;

internal sealed class FakeElement : IElement
{
    public FakeElement(string name)
    {
        this.Name = name;
    }

    public string Name { get; }

    public override string ToString()
    {
        return this.Name;
    }
}
