using System;
using System.Collections.Generic;

namespace Microsoft.Playwright.Helpers;

#pragma warning disable SA1407 // dense math for motion model
#pragma warning disable SA1117 // compact parameter placement
#pragma warning disable SA1516 // elements separated by blank line
#pragma warning disable SA1119 // unnecessary parenthesis
#pragma warning disable SA1122 // use string.Empty
#pragma warning disable SA1201 // element order
#pragma warning disable SA1501 // statement on single line
#pragma warning disable RCS1007
internal delegate double Rng();

internal static class ClearcoteMotion
{
    internal static double Lerp(double a, double b, double t) => a + (b - a) * t;

    internal static double Clamp(double v, double lo, double hi) => Math.Max(lo, Math.Min(hi, v));

    private static double Log2(double x) => Math.Log(x) / Math.Log(2);

    internal static Rng Mulberry32(int seedInt)
    {
        int a = seedInt;
        return () =>
        {
            unchecked
            {
                a += 0x6d2b79f5;
                int t = a ^ (a >> 15);
                t = t * (1 | a);
                t = (t + ((t ^ (t >> 7)) * (61 | t))) ^ t;
                uint ut = (uint)t;
                return (ut ^ (ut >> 14)) / 4294967296.0;
            }
        };
    }

    internal static int HashSeed(object? seed)
    {
        if (seed == null)
            return (int)(new Random().NextDouble() * 0xffffffff);
        if (seed is int i && i != 0)
            return (int)((uint)Math.Abs(i) * 2654435761u);
        if (seed is string s)
            return HashString(s);
        return HashString(seed?.ToString() ?? "");
    }

    private static int HashString(string s)
    {
        unchecked
        {
            uint h = 0x811c9dc5;
            foreach (var c in s)
            {
                h ^= c;
                h *= 0x01000193;
            }
            return (int)h;
        }
    }

    internal static double GaussFrom(Rng rng)
    {
        double u = 0, v = 0;
        while (u == 0) u = rng();
        while (v == 0) v = rng();
        return Math.Sqrt(-2 * Math.Log(u)) * Math.Cos(2 * Math.PI * v);
    }

    internal sealed class Persona
    {
        public int Seed { get; init; }
        public double DeviceHz { get; init; }
        public double TremorHz { get; init; }
        public double TremorAmp { get; init; }
        public double DriftAmp { get; init; }
        public double DriftTau { get; init; }
        public double Jitter { get; init; }
        public double FittsA { get; init; }
        public double FittsB { get; init; }
        public double PrimaryFrac { get; init; }
        public double Overshoot { get; init; }
        public int MaxCorrections { get; init; }
        public double ApproachBias { get; init; }
        public double GrabMinMs { get; init; }
        public double GrabMaxMs { get; init; }
        public double ReleaseMinMs { get; init; }
        public double ReleaseMaxMs { get; init; }
        public double ClickHoldMinMs { get; init; }
        public double ClickHoldMaxMs { get; init; }
        public double KeyDwellMinMs { get; init; }
        public double KeyDwellMaxMs { get; init; }
    }

    internal static Persona MakePersona(object? seed)
    {
        var seedInt = HashSeed(seed);
        var r = Mulberry32(seedInt);
        return new Persona
        {
            Seed = seedInt,
            DeviceHz = Math.Round(Lerp(110, 155, r())),
            TremorHz = Lerp(8, 12, r()),
            TremorAmp = Lerp(0.12, 0.5, r()),
            DriftAmp = Lerp(0.1, 0.4, r()),
            DriftTau = Lerp(60, 160, r()),
            Jitter = Lerp(0.25, 0.7, r()),
            FittsA = Lerp(90, 150, r()),
            FittsB = Lerp(120, 190, r()),
            PrimaryFrac = Lerp(0.84, 0.94, r()),
            Overshoot = Lerp(0.03, 0.08, r()),
            MaxCorrections = r() < 0.15 ? 3 : r() < 0.6 ? 2 : 1,
            ApproachBias = (r() - 0.5) * 0.5,
            GrabMinMs = 130,
            GrabMaxMs = 360,
            ReleaseMinMs = 90,
            ReleaseMaxMs = 230,
            ClickHoldMinMs = 60,
            ClickHoldMaxMs = 150,
            KeyDwellMinMs = 45,
            KeyDwellMaxMs = 120,
        };
    }

    internal static double ClickHold(Persona p, Rng? rng = null)
    {
        rng ??= MathRandom;
        return p.ClickHoldMinMs + Math.Min(1, Math.Abs(GaussFrom(rng)) * 0.5) * (p.ClickHoldMaxMs - p.ClickHoldMinMs);
    }

    internal static double KeyDwell(Persona p, Rng? rng = null)
    {
        rng ??= MathRandom;
        return p.KeyDwellMinMs + Math.Min(1, Math.Abs(GaussFrom(rng)) * 0.45) * (p.KeyDwellMaxMs - p.KeyDwellMinMs);
    }

    internal static (double GrabMs, double ReleaseMs) DragDwell(Persona p, Rng? rng = null)
    {
        rng ??= MathRandom;
        return (Lerp(p.GrabMinMs, p.GrabMaxMs, rng()), Lerp(p.ReleaseMinMs, p.ReleaseMaxMs, rng()));
    }

    internal readonly record struct MotionPoint(double X, double Y);

    internal sealed record Step(double X, double Y, double SleepMs);

    internal sealed class PlanOpts
    {
        public double TargetW { get; init; } = 24;
        public double TargetH { get; init; } = 24;
        public bool Settle { get; init; }
        public Rng? Rng { get; init; }
    }

    internal static double MinJerk(double tau)
    {
        var t = Clamp(tau, 0, 1);
        return t * t * t * (10 + t * (-15 + 6 * t));
    }

    private static double Bez(double p0, double c1, double c2, double p1, double e)
    {
        var m = 1 - e;
        return m * m * m * p0 + 3 * m * m * e * c1 + 3 * m * e * e * c2 + e * e * e * p1;
    }

    private static double SampleSubmove(
        List<Step> outSteps, MotionPoint a, MotionPoint b, double durMs, Persona p, Rng rng,
        double t0, ref double driftX, ref double driftY, ref double jitX, ref double jitY, double tremorPhase, bool land)
    {
        var dx = b.X - a.X;
        var dy = b.Y - a.Y;
        var dist = Math.Sqrt(dx * dx + dy * dy);
        if (dist < 1e-6) dist = 1e-6;
        var stepMs = 1000.0 / p.DeviceHz;
        var n = Math.Min(180, Math.Max(4, (int)Math.Round(durMs / stepMs)));
        var nx = -dy / dist;
        var ny = dx / dist;
        var bowSign = p.ApproachBias >= 0 ? 1 : -1;
        var bow = bowSign * (0.04 + Math.Abs(GaussFrom(rng)) * 0.05) * Math.Min(dist, 260) * (0.5 + Math.Abs(p.ApproachBias));
        var c1x = a.X + dx * 0.33 + nx * bow;
        var c1y = a.Y + dy * 0.33 + ny * bow;
        var c2x = a.X + dx * 0.66 + nx * bow;
        var c2y = a.Y + dy * 0.66 + ny * bow;
        var t = t0;
        for (var i = 1; i <= n; i++)
        {
            var e = MinJerk((double)i / n);
            var px = Bez(a.X, c1x, c2x, b.X, e);
            var py = Bez(a.Y, c1y, c2y, b.Y, e);
            var last = i == n;
            if (!(last && land))
            {
                var k = stepMs / p.DriftTau;
                driftX += -k * driftX + Math.Sqrt(2 * k) * p.DriftAmp * GaussFrom(rng);
                driftY += -k * driftY + Math.Sqrt(2 * k) * p.DriftAmp * GaussFrom(rng);
                var tAmp = p.TremorAmp * (0.7 + 0.3 * Math.Sin((2 * Math.PI * t) / 850 + tremorPhase));
                var trX = tAmp * Math.Sin(tremorPhase + (2 * Math.PI * p.TremorHz * t) / 1000);
                var trY = tAmp * Math.Sin(tremorPhase + 1.0 + (2 * Math.PI * (p.TremorHz * 0.93) * t) / 1000);
                jitX = 0.7 * jitX + GaussFrom(rng) * p.Jitter * 0.22;
                jitY = 0.7 * jitY + GaussFrom(rng) * p.Jitter * 0.22;
                px += driftX + trX + jitX;
                py += driftY + trY + jitY;
            }
            else
            {
                px = b.X;
                py = b.Y;
            }
            var sleepMs = stepMs * Lerp(0.88, 1.12, rng());
            outSteps.Add(new Step(px, py, sleepMs));
            t += sleepMs;
        }
        return t;
    }

    internal static List<Step> PlanMove(MotionPoint from, MotionPoint to, Persona p, PlanOpts? opts = null)
    {
        opts ??= new PlanOpts();
        var rng = opts.Rng ?? MathRandom;
        var w = Math.Max(6, opts.TargetW);
        var d = Math.Sqrt(Math.Pow(to.X - from.X, 2) + Math.Pow(to.Y - from.Y, 2));
        var outSteps = new List<Step>();
        if (d < 1.5)
        {
            outSteps.Add(new Step(to.X, to.Y, 1000.0 / p.DeviceHz));
            return outSteps;
        }

        var id = Log2(d / w + 1);
        var mtLo = Math.Min(1700, Math.Max(70, 40 + d * 0.6));
        var mt = Clamp((p.FittsA + p.FittsB * id) * (0.85 + 0.3 * rng()), mtLo, 1700);

        var targets = new List<MotionPoint>();
        if (d >= 40)
        {
            var f = p.PrimaryFrac;
            var spread = Math.Min(16, d * p.Overshoot);
            targets.Add(new MotionPoint(
                from.X + (to.X - from.X) * f + GaussFrom(rng) * spread,
                from.Y + (to.Y - from.Y) * f + GaussFrom(rng) * spread));
            var nCorr = rng() < 0.08 * p.MaxCorrections ? 2 : 1;
            var cur = targets[0];
            for (var i = 0; i < nCorr; i++)
            {
                var close = Lerp(0.55, 0.8, rng());
                var nx = new MotionPoint(
                    cur.X + (to.X - cur.X) * close + GaussFrom(rng) * 1.2,
                    cur.Y + (to.Y - cur.Y) * close + GaussFrom(rng) * 1.2);
                targets.Add(nx);
                cur = nx;
            }
        }
        targets.Add(new MotionPoint(to.X, to.Y));

        var segDur = new List<double>();
        if (targets.Count == 1)
        {
            segDur.Add(mt);
        }
        else
        {
            segDur.Add(mt * 0.78);
            var rest = mt * 0.22;
            var nc = targets.Count - 1;
            for (var i = 0; i < nc; i++) segDur.Add((rest / nc) * Lerp(0.8, 1.2, rng()));
        }

        var driftX = 0.0;
        var driftY = 0.0;
        var jitX = 0.0;
        var jitY = 0.0;
        var tremorPhase = rng() * 2 * Math.PI;
        var aPt = from;
        var t = 0.0;
        for (var s = 0; s < targets.Count; s++)
        {
            var bPt = targets[s];
            var last = s == targets.Count - 1;
            t = SampleSubmove(outSteps, aPt, bPt, Math.Max(20, segDur[s]), p, rng, t, ref driftX, ref driftY, ref jitX, ref jitY, tremorPhase, last && !opts.Settle);
            if (!last && outSteps.Count > 0)
            {
                var lastStep = outSteps[outSteps.Count - 1];
                outSteps[outSteps.Count - 1] = lastStep with { SleepMs = lastStep.SleepMs + Lerp(40, 120, rng()) };
            }
            aPt = bPt;
        }

        if (opts.Settle)
        {
            var jig = 1 + (int)(Math.Floor(rng() * 2));
            for (var i = 0; i < jig; i++)
            {
                outSteps.Add(new Step(to.X + GaussFrom(rng) * 1.4, to.Y + GaussFrom(rng) * 1.4, Lerp(30, 90, rng())));
            }
            outSteps.Add(new Step(to.X, to.Y, 1000.0 / p.DeviceHz));
        }
        return outSteps;
    }

    internal readonly record struct MotionBox(double X, double Y, double Width, double Height);

    internal static MotionPoint ClickPoint(MotionBox box, MotionPoint from, Persona p, Rng? rng = null)
    {
        rng ??= MathRandom;
        var cx = box.X + box.Width / 2;
        var cy = box.Y + box.Height / 2;
        var dx = cx - from.X;
        var dy = cy - from.Y;
        var d = Math.Sqrt(dx * dx + dy * dy);
        if (d < 1e-6) d = 1;
        var ux = -dx / d;
        var uy = -dy / d;
        var bx = ux * box.Width * 0.12;
        var by = uy * box.Height * 0.12;
        var x = cx + bx + GaussFrom(rng) * box.Width * 0.16;
        var y = cy + by + GaussFrom(rng) * box.Height * 0.16;
        return new MotionPoint(
            box.Width >= 4 ? Clamp(x, box.X + 2, box.X + box.Width - 2) : cx,
            box.Height >= 4 ? Clamp(y, box.Y + 2, box.Y + box.Height - 2) : cy);
    }

    internal readonly record struct MotionViewport(double Width, double Height);

    internal static List<Step> PlanAmbient(MotionPoint from, MotionViewport vp, Persona p, double ms, Rng? rng = null)
    {
        rng ??= MathRandom;
        var outSteps = new List<Step>();
        var cur = from;
        var elapsed = 0.0;
        var budget = Math.Max(200, ms);
        var bx0 = vp.Width * 0.12;
        var bx1 = vp.Width * 0.88;
        var by0 = vp.Height * 0.14;
        var by1 = vp.Height * 0.8;
        while (elapsed < budget && outSteps.Count < 4000)
        {
            var target = new MotionPoint(Lerp(bx0, bx1, rng()), Lerp(by0, by1, rng()));
            var seg = PlanMove(cur, target, p, new PlanOpts { TargetW = 80, Rng = rng });
            foreach (var s in seg)
            {
                outSteps.Add(s);
                elapsed += s.SleepMs;
            }
            var restMs = Lerp(120, 600, rng());
            var stepMs = 1000.0 / p.DeviceHz;
            var held = 0.0;
            var phase = rng() * 2 * Math.PI;
            var drX = 0.0;
            var drY = 0.0;
            while (held < restMs)
            {
                var k = stepMs / p.DriftTau;
                drX += -k * drX + Math.Sqrt(2 * k) * p.DriftAmp * GaussFrom(rng);
                drY += -k * drY + Math.Sqrt(2 * k) * p.DriftAmp * GaussFrom(rng);
                var ph = phase + (2 * Math.PI * p.TremorHz * held) / 1000;
                outSteps.Add(new Step(
                    target.X + drX + p.TremorAmp * Math.Sin(ph),
                    target.Y + drY + p.TremorAmp * Math.Sin(ph + Math.PI / 2),
                    stepMs * Lerp(0.9, 1.4, rng())));
                held += stepMs;
                elapsed += stepMs;
            }
            cur = target;
        }
        return outSteps;
    }

    private static readonly Random _sharedRandom = new();
    private static readonly Rng MathRandom = () =>
    {
        lock (_sharedRandom) { return _sharedRandom.NextDouble(); }
    };
}
