namespace NasaValley.Domain;

/// <summary>Injected randomness keeps simulation deterministic and unit-testable.</summary>
public interface IRandom
{
	double NextDouble();

	int NextInt(int maxExclusive) => (int)(NextDouble() * maxExclusive);
}

public sealed class SystemRandom : IRandom
{
	private readonly Random _random;

	public SystemRandom(int? seed = null) => _random = seed.HasValue ? new Random(seed.Value) : new Random();

	public double NextDouble() => _random.NextDouble();
}

/// <summary>
/// xorshift-based generator ported from the original page's <c>rng()</c>, used to
/// reproduce the procedural terrain and to make tests fully deterministic.
/// </summary>
public sealed class XorShiftRandom : IRandom
{
	private uint _state;

	public XorShiftRandom(uint seed) => _state = seed;

	public double NextDouble()
	{
		_state |= 0;
		_state = unchecked(_state + 0x6D2B79F5u);
		var t = MathHelper.Imul(_state ^ (_state >> 15), 1 | _state);
		t = unchecked(t + MathHelper.Imul(t ^ (t >> 7), 61 | t)) ^ t;
		return ((t ^ (t >> 14)) >>> 0) / 4294967296.0;
	}
}

internal static class MathHelper
{
	public static uint Imul(uint a, uint b) => unchecked((uint)((int)a * (int)b));
}
