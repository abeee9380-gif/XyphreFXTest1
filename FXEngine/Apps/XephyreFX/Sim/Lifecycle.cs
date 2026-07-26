namespace XephyreFX.Sim;

public enum LifecyclePhase
{
    SpawningIn,
    Alive,
    Despawning,
    Dead
}

/// <summary>
/// Every object that appears/disappears in the scene (clouds, stars, raindrops, the sun/moon,
/// hearts) owns one of these. Nothing pops in or out -- it always rides an easing curve, which
/// is what "appearing/disappearing like a graph" means in practice.
/// </summary>
public sealed class Lifecycle
{
    public double FadeInDuration = 1.0;
    public double HoldDuration = -1; // -1 = stays alive until RequestDespawn() is called
    public double FadeOutDuration = 1.0;

    public LifecyclePhase Phase { get; private set; } = LifecyclePhase.SpawningIn;
    public double Opacity { get; private set; }
    public bool IsDead => Phase == LifecyclePhase.Dead;

    private double _phaseAge;
    private bool _despawnRequested;

    public void Tick(double dt)
    {
        _phaseAge += dt;

        switch (Phase)
        {
            case LifecyclePhase.SpawningIn:
                Opacity = Easing.InOutCubic(_phaseAge / Math.Max(FadeInDuration, 0.0001));
                if (_phaseAge >= FadeInDuration)
                {
                    Phase = LifecyclePhase.Alive;
                    _phaseAge = 0;
                    Opacity = 1;
                }
                break;

            case LifecyclePhase.Alive:
                Opacity = 1;
                if (_despawnRequested || (HoldDuration >= 0 && _phaseAge >= HoldDuration))
                {
                    Phase = LifecyclePhase.Despawning;
                    _phaseAge = 0;
                }
                break;

            case LifecyclePhase.Despawning:
                Opacity = 1 - Easing.InOutCubic(_phaseAge / Math.Max(FadeOutDuration, 0.0001));
                if (_phaseAge >= FadeOutDuration)
                {
                    Phase = LifecyclePhase.Dead;
                    Opacity = 0;
                }
                break;

            case LifecyclePhase.Dead:
                Opacity = 0;
                break;
        }
    }

    public void RequestDespawn() => _despawnRequested = true;
}
