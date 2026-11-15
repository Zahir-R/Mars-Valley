namespace NasaValley.Domain;

public static class MovementSystem
{
	public static void Update(IGameContext ctx, in PlayerIntent intent, double dt)
	{
		var s = ctx.State;
		var b = ctx.Balance;
		var dx = intent.AxisX;
		var dy = intent.AxisY;
		s.Moving = intent.Moving;

		if (!s.Moving) return;

		var length = MathF.Sqrt(dx * dx + dy * dy);
		dx /= length;
		dy /= length;

		var speed = s.Vitals.Energy < b.TiredEnergy ? b.TiredSpeed : b.NormalSpeed;

		var nx = s.Px + dx * speed * (float)dt;
		if (!Blocked(ctx, nx, s.Py)) s.Px = nx;

		var ny = s.Py + dy * speed * (float)dt;
		if (!Blocked(ctx, s.Px, ny)) s.Py = ny;

		s.Px = Math.Clamp(s.Px, b.MoveMinX, b.MoveMaxX);
		s.Py = Math.Clamp(s.Py, b.MoveMinY, b.MoveMaxY);

		s.Face = MathF.Abs(dx) > MathF.Abs(dy)
			? (dx > 0 ? Direction.Right : Direction.Left)
			: (dy > 0 ? Direction.Down : Direction.Up);

		s.Walk += dt;
	}

	public static bool Blocked(BalanceConfig b, float x, float y) => ShipBlocks(b, x, y);

	/// <summary>
	/// The ship always blocks; every active vein and every station block too.
	/// Depleted veins stay walkable.
	/// </summary>
	public static bool Blocked(IGameContext ctx, float x, float y)
	{
		var s = ctx.State;
		var b = ctx.Balance;
		if (ShipBlocks(b, x, y)) return true;

		var bx = x - b.BodyHalfWidth;
		var by = y + 8 - 6;
		var bw = b.BodyWidth;
		var bh = b.BodyHeight;

		foreach (var n in s.Nodes)
		{
			if (n.Amount <= 0) continue;
			if (CircleRectOverlap(n.X, n.Y, b.NodeFootprintRadius, bx, by, bw, bh)) return true;
		}

		foreach (var station in s.Stations)
		{
			if (CircleRectOverlap(station.X, station.Y, b.StationFootprintRadius, bx, by, bw, bh)) return true;
		}

		return false;
	}

	private static bool ShipBlocks(BalanceConfig b, float x, float y)
	{
		var bx = x - b.BodyHalfWidth;
		var by = y + 8 - 6;
		var bw = b.BodyWidth;
		var bh = b.BodyHeight;
		return bx < b.ShipX + b.ShipWidth && bx + bw > b.ShipX
			&& by < b.ShipY + b.ShipHeight && by + bh > b.ShipY;
	}

	private static bool CircleRectOverlap(float cx, float cy, float radius, float rx, float ry, float rw, float rh)
	{
		var px = MathF.Max(rx, MathF.Min(cx, rx + rw));
		var py = MathF.Max(ry, MathF.Min(cy, ry + rh));
		var dx = cx - px;
		var dy = cy - py;
		return dx * dx + dy * dy < radius * radius;
	}
}

public static class SurvivalSystem
{
	public static void Update(IGameContext ctx, double dt)
	{
		var s = ctx.State;
		var b = ctx.Balance;
		var v = s.Vitals;
		var k = s.Storm.Left > 0 ? b.StormDecayMultiplier : 1;

		// Domes are pressurized safe zones: oxygen is not lost while inside one.
		var sheltered = SafeZones.Contains(s, s.Px, s.Py);
		if (!sheltered) v.O2 = Math.Max(0, v.O2 - b.O2Decay * k * dt);
		v.Food = Math.Max(0, v.Food - b.FoodDecay * dt);
		v.Energy = Math.Max(0, v.Energy - (b.EnergyDecay * k + (s.Moving ? b.EnergyMoveDecay : 0)) * dt);

		// Inside a dome the suit is protected: an empty tank no longer drains health.
		if (!sheltered && v.O2 <= 0)
			v.Health -= b.O2HealthDrain * dt;
		else if (v.Food <= 0)
			v.Health -= b.FoodHealthDrain * dt;
		else if (v.O2 > b.HealthRegenThreshold && v.Food > b.HealthRegenThreshold && v.Health < 100)
			v.Health = Math.Min(100, v.Health + b.HealthRegen * dt);

		if (v.O2 < b.LowO2Threshold && !s.Flags.O2)
		{
			s.Flags.O2 = true;
			ctx.Say("Oxígeno bajo. Recarga en la nave con hielo.", MessageKind.Warn);
		}
		if (v.O2 > b.LowO2Reset) s.Flags.O2 = false;

		if (v.Food < b.LowFoodThreshold && !s.Flags.Food)
		{
			s.Flags.Food = true;
			ctx.Say("Tienes hambre. Come una papa (F).", MessageKind.Warn);
		}
		if (v.Food > b.LowFoodReset) s.Flags.Food = false;
	}
}

public static class WeatherSystem
{
	public static void Update(IGameContext ctx, double dt)
	{
		var s = ctx.State;
		var b = ctx.Balance;
		var st = s.Storm;

		if (st.Left > 0)
		{
			st.Left -= dt;
			if (st.Left <= 0)
			{
				st.Next = b.NextStormMin + ctx.Rng.NextDouble() * b.NextStormSpread;
				ctx.Say("La tormenta de polvo ha pasado.");
			}
		}
		else
		{
			st.Next -= dt;
			if (st.Next <= 0)
			{
				st.Left = b.StormDuration;
				ctx.Say("¡Tormenta de polvo! El oxígeno se agota más rápido.", MessageKind.Warn);
			}
		}
	}
}

public static class DomeSystem
{
	/// <summary>Solar charge during daylight, and energy spent holding the target temperature.</summary>
	public static void Update(IGameContext ctx, double dt)
	{
		var s = ctx.State;
		var b = ctx.Balance;
		var light = EnvironmentSystem.Light(s, b);

		foreach (var dome in s.Domes)
		{
			var biome = GeneratedBiomes.AtWorld(dome.CenterX, dome.CenterY);
			var external = EnvironmentSystem.ExternalTemp(biome, s, b);
			dome.ExternalTempC = external;

			// Only extended panels generate; there is no free passive charge.
			var generation = dome.DeployedPanelCount * b.SolarPanelOutput * light * biome.SolarFactor;
			dome.Energy = Math.Min(dome.EnergyCapacity(b), dome.Energy + generation * dt);

			// Batteries slowly fill their own reserve while the dome has a working panel.
			if (generation > 0)
			{
				foreach (var fixture in dome.Fixtures)
				{
					if (fixture.Kind == FixtureKind.Battery)
						fixture.Stored = Math.Min(b.BatteryStorage, fixture.Stored + b.BatteryChargeRate * dt);
				}
			}

			// Every dome pays a slow life-support drain, plus the cost of holding its temperature.
			var heating = Math.Abs(external - b.DomeTargetTempC) * b.DomeEnergyPerDegreePerSec * dt;
			var needed = heating + b.DomeIdleDrain * dt;
			if (dome.Energy >= needed)
			{
				dome.Energy -= needed;
				dome.InternalTempC = b.DomeTargetTempC;
			}
			else
			{
				dome.Energy = 0;
				dome.InternalTempC = external;
			}
		}
	}
}

public static class FarmingSystem
{
	/// <summary>
	/// Effective light and temperature for a plot. Domes (and parcels placed inside a
	/// dome footprint) use the dome's interior; a powered beacon gives light + heat to
	/// open-air plots, making them behave like a dome.
	/// </summary>
	public static (double Light, double Temp) Conditions(IGameContext ctx, Plot plot)
	{
		var s = ctx.State;
		var b = ctx.Balance;
		var natural = EnvironmentSystem.Light(s, b);
		var cx = plot.X + b.PlotSize / 2f;
		var cy = plot.Y + b.PlotSize / 2f;
		var biome = GeneratedBiomes.AtWorld(cx, cy);
		var external = EnvironmentSystem.ExternalTemp(biome, s, b);

		foreach (var dome in s.Domes)
		{
			if (SafeZones.Contains(dome, cx, cy)) return (natural, dome.InternalTempC);
		}

		foreach (var beacon in s.Beacons)
		{
			if (!beacon.Powered) continue;
			var dx = beacon.X - cx;
			var dy = beacon.Y - cy;
			if (dx * dx + dy * dy <= b.BeaconRadius * b.BeaconRadius)
			{
				// Dome-like conditions: enough light and a habitable temperature.
				return (Math.Max(natural, b.BeaconLight), Math.Max(external + b.BeaconHeatC, b.DomeTargetTempC));
			}
		}

		return (natural, external);
	}

	public static void Update(IGameContext ctx, double dt)
	{
		var s = ctx.State;
		var b = ctx.Balance;
		var speed = s.DevMode ? b.DevGrowSpeedMultiplier : 1;

		foreach (var plot in s.Plots)
		{
			if (plot.State != PlotState.Grow) continue;
			if (!ctx.Plants.TryGet(plot.PlantId, out var plant)) continue;

			var (light, temp) = Conditions(ctx, plot);
			if (light < plant.MinLight) continue;
			if (temp < plant.MinTempC || temp > plant.MaxTempC) continue;

			plot.Time -= dt * speed;
			if (plot.Time <= 0)
			{
				plot.State = PlotState.Ready;
				plot.Time = 0;
				ctx.Say($"{plant.Name} lista para cosechar.", MessageKind.Good);
			}
		}
	}

	/// <summary>Why a growing crop is stalled (empty string if it is growing fine).</summary>
	public static string GrowthBlocker(IGameContext ctx, Plot plot)
	{
		if (plot.State != PlotState.Grow) return string.Empty;
		if (!ctx.Plants.TryGet(plot.PlantId, out var plant)) return "Cultivo desconocido";

		var (light, temp) = Conditions(ctx, plot);
		if (light < plant.MinLight) return "Falta luz";
		if (temp < plant.MinTempC) return "Hace frío";
		if (temp > plant.MaxTempC) return "Hace calor";
		return string.Empty;
	}

	/// <summary>Why <paramref name="plantId"/> would not grow right now on <paramref name="plot"/> (empty string if it would).</summary>
	public static string GrowthBlockerNow(IGameContext ctx, Plot plot, string plantId)
	{
		if (!ctx.Plants.TryGet(plantId, out var plant)) return "Cultivo desconocido";

		var (light, temp) = Conditions(ctx, plot);
		if (light < plant.MinLight) return "Falta luz";
		if (temp < plant.MinTempC) return "Hace frío";
		if (temp > plant.MaxTempC) return "Hace calor";
		return string.Empty;
	}
}

/// <summary>Beacons draw power from the nearest dome; no power means no light or heat.</summary>
public static class BeaconSystem
{
	public static void Update(IGameContext ctx, double dt)
	{
		var s = ctx.State;
		var b = ctx.Balance;

		foreach (var beacon in s.Beacons)
		{
			Dome? nearest = null;
			var best = b.BeaconEnergyRange;
			foreach (var dome in s.Domes)
			{
				var dx = dome.CenterX - beacon.X;
				var dy = dome.CenterY - beacon.Y;
				var d = MathF.Sqrt(dx * dx + dy * dy);
				if (d <= best) { best = d; nearest = dome; }
			}

			var cost = b.BeaconEnergyDrain * dt;
			if (nearest != null && nearest.Energy >= cost)
			{
				nearest.Energy -= cost;
				beacon.Powered = true;
			}
			else
			{
				beacon.Powered = false;
			}
		}
	}
}

/// <summary>Dust storms wear down exposed structures; at max damage they are destroyed.</summary>
public static class DamageSystem
{
	public static void Update(IGameContext ctx, double dt)
	{
		var s = ctx.State;
		var b = ctx.Balance;
		if (s.Storm.Left <= 0) return;

		foreach (var dome in s.Domes)
		{
			for (var i = dome.Fixtures.Count - 1; i >= 0; i--)
			{
				var fixture = dome.Fixtures[i];
				if (fixture.Kind != FixtureKind.SolarPanel || !fixture.Deployed) continue;
				fixture.Damage += b.PanelStormDamagePerSec * dt;
				if (fixture.Damage >= b.StructureMaxDamage)
				{
					dome.Fixtures.RemoveAt(i);
					ctx.Say("La tormenta destruyó un panel solar.", MessageKind.Warn);
				}
			}
		}

		for (var i = s.Plots.Count - 1; i >= 0; i--)
		{
			var plot = s.Plots[i];
			if (plot.State == PlotState.Empty) continue;
			if (plot.Dome != null) continue;
			if (SafeZones.Contains(s, plot.X + b.PlotSize / 2f, plot.Y + b.PlotSize / 2f)) continue;

			plot.Damage += b.FarmStormDamagePerSec * dt;
			if (plot.Damage >= b.StructureMaxDamage)
			{
				s.Plots.RemoveAt(i);
				ctx.Say("La tormenta destruyó un cultivo al descubierto.", MessageKind.Warn);
			}
		}

		for (var i = s.Beacons.Count - 1; i >= 0; i--)
		{
			var beacon = s.Beacons[i];
			beacon.Damage += b.BeaconStormDamagePerSec * dt;
			if (beacon.Damage >= b.StructureMaxDamage)
			{
				s.Beacons.RemoveAt(i);
				ctx.Say("La tormenta destruyó una baliza.", MessageKind.Warn);
			}
		}
	}
}

public static class NodeSystem
{
	public static void Update(IGameContext ctx, double dt)
	{
		var b = ctx.Balance;
		foreach (var node in ctx.State.Nodes)
		{
			if (node.Shake > 0) node.Shake -= dt;
			if (node.Amount > 0) continue;
			node.Regen -= dt;
			if (node.Regen > 0) continue;

			// A structure built over a vein keeps it from regrowing.
			if (WorldOccupancy.InsideAnyStructure(ctx.State, b, node.X, node.Y))
			{
				node.Regen = b.NodeBlockedRetry;
				continue;
			}

			node.Amount = b.NodeAmount;
		}
	}
}

public static class EffectsSystem
{
	public static void Update(IGameContext ctx, double dt)
	{
		var s = ctx.State;

		for (var i = s.Particles.Count - 1; i >= 0; i--)
		{
			var p = s.Particles[i];
			p.X += p.Vx * (float)dt;
			p.Y += p.Vy * (float)dt;
			p.Vy += 120 * (float)dt;
			p.Life -= dt;
			if (p.Life <= 0) s.Particles.RemoveAt(i);
		}

		for (var i = s.Floats.Count - 1; i >= 0; i--)
		{
			var f = s.Floats[i];
			f.Y -= 22 * (float)dt;
			f.Life -= dt;
			if (f.Life <= 0) s.Floats.RemoveAt(i);
		}
	}
}
