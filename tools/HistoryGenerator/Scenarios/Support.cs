using HistoryGenerator.Model;

namespace HistoryGenerator.Scenarios;

public static class Paths
{
    public static string Src(string area, string type) => $"src/Storefront/{area}/{type}.cs";
    public static string Resource(string file) => $"src/Storefront/Resources/{file}";
    public static string Unit(string area, string type) => $"tests/Storefront.UnitTests/{area}/{type}.cs";
    public static string Integration(string area, string type) => $"tests/Storefront.IntegrationTests/{area}/{type}.cs";
    public static string For(TestRef test) => test.Project == "Storefront.UnitTests"
        ? Unit(test.Namespace["Storefront.UnitTests.".Length..], test.Class)
        : Integration(test.Namespace["Storefront.IntegrationTests.".Length..], test.Class);
}

/// <summary>A scenario that owns one test (and the production code it exercises).</summary>
public abstract class SingleTestScenario : Scenario
{
    public abstract TestRef Test { get; }

    /// <summary>First commit in which the test exists.</summary>
    protected virtual int FirstCommit => 1;

    /// <summary>Commit in which the test is deleted, if any.</summary>
    protected virtual int? RemovedAt => null;

    protected bool Exists(int commit) => commit >= FirstCommit && (RemovedAt is null || commit < RemovedAt);

    public override IEnumerable<TestRef> Tests(int commit) => Exists(commit) ? [Test] : [];

    public override Dist Expected(TestRef test, Target target, int commit) =>
        test == Test && Exists(commit) ? Model(target, commit) : new Dist.Absent();

    protected abstract Dist Model(Target target, int commit);

    public override void Render(int commit, Snapshot snapshot)
    {
        foreach (var (path, content) in Files(commit)) snapshot.Add(path, content, Id);
    }

    protected abstract IEnumerable<(string Path, string Content)> Files(int commit);
}

/// <summary>A scenario that owns several tests with their own models.</summary>
public abstract class MultiTestScenario : Scenario
{
    public override void Render(int commit, Snapshot snapshot)
    {
        foreach (var (path, content) in Files(commit)) snapshot.Add(path, content, Id);
    }

    protected abstract IEnumerable<(string Path, string Content)> Files(int commit);
}
