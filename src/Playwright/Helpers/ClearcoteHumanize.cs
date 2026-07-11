using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Playwright.Core;

#pragma warning disable SA1407 // dense math for motion model
#pragma warning disable SA1117 // compact parameter placement
#pragma warning disable SA1649 // first type name != file name
#pragma warning disable RCS1007

namespace Microsoft.Playwright.Helpers;

internal static class ClearcoteHumanize
{
    private static readonly Dictionary<char, string> _nearby = new()
    {
        ['a'] = "sqwz",
        ['b'] = "vghn",
        ['c'] = "xdfv",
        ['d'] = "sfecx",
        ['e'] = "wrsdf",
        ['f'] = "dgrtcv",
        ['g'] = "fhtyb",
        ['h'] = "gjybn",
        ['i'] = "ujko",
        ['j'] = "hkunm",
        ['k'] = "jloi",
        ['l'] = "kop",
        ['m'] = "njk",
        ['n'] = "bhjm",
        ['o'] = "iklp",
        ['p'] = "ol",
        ['q'] = "wa",
        ['r'] = "edft",
        ['s'] = "awedxz",
        ['t'] = "rfgy",
        ['u'] = "yhji",
        ['v'] = "cfgb",
        ['w'] = "qase",
        ['x'] = "zsdc",
        ['y'] = "tghu",
        ['z'] = "asx",
    };

    internal static double Rand(double min, double max)
        => min + (NextDouble() * (max - min));

    internal static async Task GlideAsync(Page page, ClearcoteHumanizeState state, double x, double y)
    {
        if (state.Persona == null)
        {
            state.X = x;
            state.Y = y;
            return;
        }
        var steps = ClearcoteMotion.PlanMove(
            new ClearcoteMotion.MotionPoint(state.X, state.Y),
            new ClearcoteMotion.MotionPoint(x, y),
            state.Persona);
        foreach (var s in steps)
        {
            await DirectMoveAsync(page, (float)s.X, (float)s.Y).ConfigureAwait(false);
            await Task.Delay((int)Math.Round(s.SleepMs)).ConfigureAwait(false);
        }
        state.X = x;
        state.Y = y;
    }

    internal static async Task ClickAsync(Page page, ClearcoteHumanizeState state, float x, float y, MouseButton? button, int? clickCount, float? delay)
    {
        await GlideAsync(page, state, x, y).ConfigureAwait(false);
        await Task.Delay((int)Rand(40, 130)).ConfigureAwait(false);
        var hold = state.Persona != null ? ClearcoteMotion.ClickHold(state.Persona) : (double?)null;
        await DirectClickAsync(page, x, y, button, clickCount, delay ?? (float?)hold).ConfigureAwait(false);
    }

    internal static async Task WheelAsync(Page page, float deltaX, float deltaY)
    {
        var steps = Math.Max(5, Math.Min(24, (int)Math.Round((Math.Abs(deltaX) + Math.Abs(deltaY)) / 60)));
        static double Ease(double u) => 1 - Math.Pow(1 - u, 2.2);
        var previousX = 0;
        var previousY = 0;
        for (var i = 1; i <= steps; i++)
        {
            var f = Ease((double)i / steps);
            var nextX = (int)Math.Round(deltaX * f);
            var nextY = (int)Math.Round(deltaY * f);
            await DirectWheelAsync(page, nextX - previousX, nextY - previousY).ConfigureAwait(false);
            previousX = nextX;
            previousY = nextY;
            await Task.Delay((int)Rand(10, 38)).ConfigureAwait(false);
            if (NextDouble() < 0.07)
            {
                await Task.Delay((int)Rand(40, 120)).ConfigureAwait(false);
            }
        }
        if (previousX != (int)deltaX || previousY != (int)deltaY)
        {
            await DirectWheelAsync(page, deltaX - previousX, deltaY - previousY).ConfigureAwait(false);
        }
    }

    internal static async Task TypeTextAsync(Page page, ClearcoteHumanizeState state, string text)
    {
        for (var i = 0; i < text.Length; i++)
        {
            var ch = text[i];
            if (char.IsLetterOrDigit(ch) && NextDouble() < 0.02)
            {
                await DirectTypeAsync(page, NearbyKey(ch).ToString()).ConfigureAwait(false);
                await Task.Delay((int)Rand(120, 300)).ConfigureAwait(false);
                await DirectPressAsync(page, "Backspace", null).ConfigureAwait(false);
                await Task.Delay((int)Rand(80, 200)).ConfigureAwait(false);
            }

            var dwell = state.Persona != null ? ClearcoteMotion.KeyDwell(state.Persona) : (double?)null;
            await DirectPressAsync(page, ch.ToString(), (float?)dwell).ConfigureAwait(false);
            if (i < text.Length - 1)
            {
                await Task.Delay((int)(NextDouble() < 0.06 ? Rand(180, 450) : Rand(45, 150))).ConfigureAwait(false);
                if (char.IsWhiteSpace(ch))
                {
                    await Task.Delay((int)Rand(20, 100)).ConfigureAwait(false);
                }
            }
        }
    }

    internal static async Task<bool> ClickSelectorAsync(Frame frame, string selector, FrameClickOptions? options, int? steps)
    {
        if (!CanTarget(frame, options?.Trial, options?.Force, options?.Modifiers))
        {
            return false;
        }

        var point = await PointForAsync(frame, selector, options?.Position, options?.Timeout).ConfigureAwait(false);
        if (point == null)
        {
            return false;
        }

        await ClickAsync((Page)frame.Page, ((Page)frame.Page).ClearcoteHumanizeState!, point.Value.X, point.Value.Y, options?.Button, options?.ClickCount, options?.Delay).ConfigureAwait(false);
        return true;
    }

    internal static async Task<bool> DblClickSelectorAsync(Frame frame, string selector, FrameDblClickOptions? options, int? steps)
    {
        if (!CanTarget(frame, options?.Trial, options?.Force, options?.Modifiers))
        {
            return false;
        }

        var point = await PointForAsync(frame, selector, options?.Position, options?.Timeout).ConfigureAwait(false);
        if (point == null)
        {
            return false;
        }

        await ClickAsync((Page)frame.Page, ((Page)frame.Page).ClearcoteHumanizeState!, point.Value.X, point.Value.Y, options?.Button, 2, options?.Delay).ConfigureAwait(false);
        return true;
    }

    internal static async Task<bool> HoverSelectorAsync(Frame frame, string selector, FrameHoverOptions? options)
    {
        if (!CanTarget(frame, options?.Trial, options?.Force, options?.Modifiers))
        {
            return false;
        }

        var point = await PointForAsync(frame, selector, options?.Position, options?.Timeout).ConfigureAwait(false);
        if (point == null)
        {
            return false;
        }

        await GlideAsync((Page)frame.Page, ((Page)frame.Page).ClearcoteHumanizeState!, point.Value.X, point.Value.Y).ConfigureAwait(false);
        return true;
    }

    internal static async Task<bool> FillSelectorAsync(Frame frame, string selector, string value, FrameFillOptions? options)
    {
        if (!CanTarget(frame, null, options?.Force, null) || value.Length > 200)
        {
            return false;
        }

        var point = await PointForAsync(frame, selector, null, options?.Timeout).ConfigureAwait(false);
        if (point == null)
        {
            return false;
        }

        var page = (Page)frame.Page;
        await ClickAsync(page, page.ClearcoteHumanizeState!, point.Value.X, point.Value.Y, MouseButton.Left, 1, null).ConfigureAwait(false);
        await Task.Delay((int)Rand(40, 120)).ConfigureAwait(false);
        await DirectPressAsync(page, "Control+a", null).ConfigureAwait(false);
        await Task.Delay((int)Rand(30, 80)).ConfigureAwait(false);
        await DirectPressAsync(page, "Backspace", null).ConfigureAwait(false);
        await Task.Delay((int)Rand(40, 120)).ConfigureAwait(false);
        await TypeTextAsync(page, page.ClearcoteHumanizeState!, value).ConfigureAwait(false);
        return true;
    }

    internal static async Task<bool> TypeSelectorAsync(Frame frame, string selector, string text, FrameTypeOptions? options)
    {
        if (!CanTarget(frame, null, null, null))
        {
            return false;
        }

        var point = await PointForAsync(frame, selector, null, options?.Timeout).ConfigureAwait(false);
        if (point == null)
        {
            return false;
        }

        var page = (Page)frame.Page;
        await ClickAsync(page, page.ClearcoteHumanizeState!, point.Value.X, point.Value.Y, MouseButton.Left, 1, null).ConfigureAwait(false);
        await Task.Delay((int)Rand(40, 120)).ConfigureAwait(false);
        await TypeTextAsync(page, page.ClearcoteHumanizeState!, text).ConfigureAwait(false);
        return true;
    }

    internal static async Task<bool> PressSelectorAsync(Frame frame, string selector, string key, FramePressOptions? options)
    {
        if (!CanTarget(frame, null, null, null))
        {
            return false;
        }

        var point = await PointForAsync(frame, selector, null, options?.Timeout).ConfigureAwait(false);
        if (point == null)
        {
            return false;
        }

        var page = (Page)frame.Page;
        await ClickAsync(page, page.ClearcoteHumanizeState!, point.Value.X, point.Value.Y, MouseButton.Left, 1, null).ConfigureAwait(false);
        await Task.Delay((int)Rand(40, 120)).ConfigureAwait(false);
        var dwell = page.ClearcoteHumanizeState?.Persona != null ? ClearcoteMotion.KeyDwell(page.ClearcoteHumanizeState.Persona) : (double?)null;
        await DirectPressAsync(page, key, options?.Delay ?? (float?)dwell).ConfigureAwait(false);
        return true;
    }

    internal static async Task<bool> DragAndDropAsync(Frame frame, string source, string target, FrameDragAndDropOptions? options)
    {
        if (!CanTarget(frame, options?.Trial, options?.Force, null))
        {
            return false;
        }

        var sourcePoint = await PointForAsync(frame, source, options?.SourcePosition == null ? null : new() { X = options.SourcePosition.X, Y = options.SourcePosition.Y }, options?.Timeout).ConfigureAwait(false);
        var targetPoint = await PointForAsync(frame, target, options?.TargetPosition == null ? null : new() { X = options.TargetPosition.X, Y = options.TargetPosition.Y }, options?.Timeout).ConfigureAwait(false);
        if (sourcePoint == null || targetPoint == null)
        {
            return false;
        }

        var page = (Page)frame.Page;
        var state = page.ClearcoteHumanizeState!;
        var (grabMs, releaseMs) = state.Persona != null
            ? ClearcoteMotion.DragDwell(state.Persona)
            : (Rand(130, 360), Rand(90, 230));
        await GlideAsync(page, state, sourcePoint.Value.X, sourcePoint.Value.Y).ConfigureAwait(false);
        await Task.Delay((int)Rand(100, 200)).ConfigureAwait(false);
        await DirectDownAsync(page).ConfigureAwait(false);
        await Task.Delay((int)grabMs).ConfigureAwait(false);
        await GlideAsync(page, state, targetPoint.Value.X, targetPoint.Value.Y).ConfigureAwait(false);
        await Task.Delay((int)releaseMs).ConfigureAwait(false);
        await DirectUpAsync(page).ConfigureAwait(false);
        return true;
    }

    internal static async Task AmbientMotionAsync(Page page, ClearcoteHumanizeState state, double ms = 1200)
    {
        if (state.Persona == null) return;
        try
        {
            var vp = await page.EvaluateAsync<JsonElement?>("({width: window.innerWidth, height: window.innerHeight})", null).ConfigureAwait(false);
            double vw = 1280, vh = 800;
            if (vp.HasValue)
            {
                vw = vp.Value.TryGetProperty("width", out var w) ? w.GetDouble() : 1280;
                vh = vp.Value.TryGetProperty("height", out var h) ? h.GetDouble() : 800;
            }
            var steps = ClearcoteMotion.PlanAmbient(
                new ClearcoteMotion.MotionPoint(state.X, state.Y),
                new ClearcoteMotion.MotionViewport(vw, vh),
                state.Persona, ms);
            foreach (var s in steps)
            {
                await DirectMoveAsync(page, (float)s.X, (float)s.Y).ConfigureAwait(false);
                await Task.Delay((int)Math.Round(s.SleepMs)).ConfigureAwait(false);
            }
            if (steps.Count > 0)
            {
                state.X = steps[steps.Count - 1].X;
                state.Y = steps[steps.Count - 1].Y;
            }
        }
        catch
        {
        }
    }

    private static bool CanTarget(Frame frame, bool? trial, bool? force, IEnumerable<KeyboardModifier>? modifiers)
        => trial != true
        && force != true
        && modifiers == null
        && frame.ParentFrame == null
        && frame.Page is Page page
        && page.ClearcoteHumanizeState?.Humanize == true;

    private static async Task<(float X, float Y)?> PointForAsync(Frame frame, string selector, Position? position, float? timeout)
    {
        var locator = new Locator(frame, selector).First;
        await locator.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = timeout }).ConfigureAwait(false);
        await locator.ScrollIntoViewIfNeededAsync(new() { Timeout = timeout }).ConfigureAwait(false);
        if (!await locator.IsEnabledAsync(new() { Timeout = timeout }).ConfigureAwait(false))
        {
            return null;
        }

        var box = await locator.BoundingBoxAsync(new() { Timeout = timeout }).ConfigureAwait(false);
        if (box == null)
        {
            return null;
        }

        await Task.Delay(50).ConfigureAwait(false);
        var second = await locator.BoundingBoxAsync(new() { Timeout = timeout }).ConfigureAwait(false);
        if (second == null || Math.Abs(second.X - box.X) > 1 || Math.Abs(second.Y - box.Y) > 1)
        {
            return null;
        }

        if (position != null)
        {
            return ((float)(second.X + position.X), (float)(second.Y + position.Y));
        }

        if (((Page)frame.Page).ClearcoteHumanizeState?.Persona is ClearcoteMotion.Persona persona)
        {
            var cp = ClearcoteMotion.ClickPoint(
                new ClearcoteMotion.MotionBox(second.X, second.Y, second.Width, second.Height),
                new ClearcoteMotion.MotionPoint(((Page)frame.Page).ClearcoteHumanizeState!.X, ((Page)frame.Page).ClearcoteHumanizeState!.Y),
                persona);
            return ((float)cp.X, (float)cp.Y);
        }
        return ((float)(second.X + second.Width * Rand(0.3, 0.7)), (float)(second.Y + second.Height * Rand(0.3, 0.7)));
    }

    private static Task DirectMoveAsync(Page page, float x, float y)
        => page.SendMessageToServerAsync("mouseMove", new Dictionary<string, object?>
        {
            ["x"] = x,
            ["y"] = y,
        });

    private static Task DirectClickAsync(Page page, float x, float y, MouseButton? button, int? clickCount, float? delay)
        => page.SendMessageToServerAsync("mouseClick", new Dictionary<string, object?>
        {
            ["x"] = x,
            ["y"] = y,
            ["delay"] = delay,
            ["button"] = button,
            ["clickCount"] = clickCount,
        });

    private static Task DirectDownAsync(Page page)
        => page.SendMessageToServerAsync("mouseDown", new Dictionary<string, object?>
        {
            ["button"] = MouseButton.Left,
        });

    private static Task DirectUpAsync(Page page)
        => page.SendMessageToServerAsync("mouseUp", new Dictionary<string, object?>
        {
            ["button"] = MouseButton.Left,
        });

    private static Task DirectWheelAsync(Page page, float deltaX, float deltaY)
        => page.SendMessageToServerAsync("mouseWheel", new Dictionary<string, object?>
        {
            ["deltaX"] = deltaX,
            ["deltaY"] = deltaY,
        });

    private static Task DirectPressAsync(Page page, string key, float? delay)
        => page.SendMessageToServerAsync("keyboardPress", new Dictionary<string, object?>
        {
            ["key"] = key,
            ["delay"] = delay,
        });

    private static Task DirectTypeAsync(Page page, string text)
        => page.SendMessageToServerAsync("keyboardType", new Dictionary<string, object?>
        {
            ["text"] = text,
        });

    private static double Gaussian()
    {
        var u = 0.0;
        var v = 0.0;
        while (u == 0)
        {
            u = NextDouble();
        }
        while (v == 0)
        {
            v = NextDouble();
        }
        return Math.Sqrt(-2 * Math.Log(u)) * Math.Cos(2 * Math.PI * v);
    }

    private static char NearbyKey(char ch)
    {
        var lower = char.ToLowerInvariant(ch);
        if (!_nearby.TryGetValue(lower, out var nearby))
        {
            return ch;
        }

        var selected = nearby[System.Security.Cryptography.RandomNumberGenerator.GetInt32(nearby.Length)];
        return char.IsUpper(ch) ? char.ToUpperInvariant(selected) : selected;
    }

    private static double NextDouble()
        => System.Security.Cryptography.RandomNumberGenerator.GetInt32(int.MaxValue) / (double)int.MaxValue;
}

internal sealed class ClearcoteHumanizeState
{
    internal ClearcoteHumanizeState(bool humanize, bool showCursor, object? seed)
    {
        Humanize = humanize;
        ShowCursor = showCursor;
        Persona = humanize ? ClearcoteMotion.MakePersona(seed) : null;
        var rng = humanize ? ClearcoteMotion.Mulberry32(ClearcoteMotion.HashSeed(seed)) : ClearcoteMotion.Mulberry32(42);
        X = rng() * 240 + 140;
        Y = rng() * 150 + 90;
    }

    internal bool Humanize { get; }

    internal bool ShowCursor { get; }

    internal ClearcoteMotion.Persona? Persona { get; }

    internal double X { get; set; }

    internal double Y { get; set; }
}
