using Unity.Entities;

namespace ModernRA.Simulation
{
    /// <summary>
    /// FixedStepSimulationSystemGroup defaults to 60Hz. The authoritative ModernRA
    /// simulation contract is 30Hz, so every created world must explicitly configure
    /// the ECS fixed-step group before gameplay systems run.
    /// </summary>
    [UpdateInGroup(typeof(InitializationSystemGroup), OrderFirst = true)]
    public partial class FixedStepRateBootstrapSystem : SystemBase
    {
        protected override void OnCreate()
        {
            base.OnCreate();
            var fixedGroup = World.GetExistingSystemManaged<FixedStepSimulationSystemGroup>();
            if (fixedGroup != null)
                fixedGroup.Timestep = SimulationRate.SecondsPerTick;

            Enabled = false;
        }

        protected override void OnUpdate() { }
    }
}
