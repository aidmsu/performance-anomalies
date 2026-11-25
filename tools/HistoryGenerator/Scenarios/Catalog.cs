using HistoryGenerator.Model;

namespace HistoryGenerator.Scenarios;

public static class Catalog
{
    public static readonly IReadOnlyList<Scenario> All =
    [
        // Controls
        new CtlStable(),
        new CtlNoisy(),
        new CtlHeavyTail(),
        new CtlMicro(),
        new CtlCpuBound(),
        new CtlQuiet(),
        // Steps
        new RegStepLarge(),
        new RegStepModerate(),
        new RegStepSmall(),
        new RegSensitivity3(),
        new RegSensitivity6(),
        new RegStepInNoise(),
        new RegStaircase(),
        new RegEarly(),
        new RegLate(),
        new RegAbsoluteLarge(),
        new RegRelativeLarge(),
        new RegCpuBound(),
        new RegConfig(),
        new RegFanout(),
        new RegSquashGiftCard(),
        new RegSquashReturns(),
        // Platform-specific
        new RegWindowsOnly(),
        new RegWindowsHard(),
        new RegMacOnly(),
        new RegArm64Only(),
        new RegPosixOnly(),
        new RegAllTargetsDifferent(),
        new RegOsTradeoff(),
        // Parameter-dependent
        new RegParamScaling(),
        new RegParamCase(),
        new RegParamHard(),
        // Fixed, reverted, transient
        new RegFixed(),
        new RegPartialFix(),
        new RegReintroduced(),
        new RegHotfix(),
        new RegShortLived(),
        // Distribution changes
        new RegBimodal(),
        new RegVariance(),
        new RegTailIntermittent(),
        // Optimizations
        new OptStep(),
        new OptReverted(),
        new OptBimodalFixed(),
        // Gradual changes
        new RegGradualLinear(),
        new RegGradualAccelerating(),
        new RegDriftSubNoise(),
        new OptGradual(),
        // Lifecycle and test scope
        new LifeGapWithChange(),
        new LifeNewSlowTest(),
        new LifeRenamed(),
        new LifeRemoved(),
        new TestScopeChange(),
        // Environment
        new EnvBadRunner(),
    ];
}
