using Xunit;

// UIA is COM-STA. Concurrent in-process E2E tests against the same automation
// instance will misbehave. Disable parallelization at the collection level so
// each test class runs sequentially within this assembly.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
