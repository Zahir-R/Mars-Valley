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
		if (!Blocked(b, nx, s.Py)) s.Px = nx;

		var ny = s.Py + dy * speed * (float)dt;
		if (!Blocked(b, s.Px, ny)) s.Py = ny;

		s.Px = Math.Clamp(s.Px, b.MoveMinX, b.MoveMaxX);
		s.Py = Math.Clamp(s.Py, b.MoveMinY, b.MoveMaxY);

		s.Face = MathF.Abs(dx) > MathF.Abs(dy)
			? (dx > 0 ? Direction.Right : Direction.Left)
			: (dy > 0 ? Direction.Down : Direction.Up);

		s.Walk += dt;
	}

	public static bool Blocked(BalanceConfig b, float x, float y)
	{
		var bx = x - b.BodyHalfWidth;
		var by = y + 8 - 6;
		var bw = b.BodyWidth;
		var bh = b.BodyHeight;
		return bx < b.ShipX + b.ShipWidth && bx + bw > b.ShipX
			&& by < b.ShipY + b.ShipHeight && by + bh > b.ShipY;
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

		v.O2 = Math.Max(0, v.O2 - b.O2Decay * k * dt);
		v.Food = Math.Max(0, v.Food - b.FoodDecay * dt);
		v.Energy = Math.Max(0, v.Energy - (b.EnergyDecay * k + (s.Moving ? b.EnergyMoveDecay : 0)) * dt);

		if (v.O2 <= 0)
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

public static class FarmingSystem
{
	public static void Update(IGameContext ctx, double dt)
	{
		foreach (var plot in ctx.State.Plots)
		{
			if (plot.State != PlotState.Grow) continue;
			plot.Time -= dt;
			if (plot.Time <= 0)
			{
				plot.State = PlotState.Ready;
				plot.Time = 0;
				ctx.Say("Una papa está lista para cosechar.", MessageKind.Good);
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
			if (node.Regen <= 0) node.Amount = b.NodeAmount;
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
