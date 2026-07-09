using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Playwright.Helpers;
using Microsoft.Playwright.Transport;
using Microsoft.Playwright.Transport.Converters;
using Microsoft.Playwright.Transport.Protocol;

namespace Microsoft.Playwright.Core;

internal class BindingCall : ChannelOwner
{
    private readonly BindingCallInitializer _initializer;

    public BindingCall(ChannelOwner parent, string guid, BindingCallInitializer initializer) : base(parent, guid)
    {
        _initializer = initializer;
    }

    public string Name => _initializer.Name;

    internal static async Task<object?> UnwrapTaskResultAsync(Task task)
    {
        await task.ConfigureAwait(false);
        if (task is Task<object?> to)
        {
            return await to.ConfigureAwait(false);
        }
        if (task is Task<string> ts)
        {
            return await ts.ConfigureAwait(false);
        }
        if (task is Task<bool> tb)
        {
            return await tb.ConfigureAwait(false);
        }
        if (task is Task<bool?> tnb)
        {
            return await tnb.ConfigureAwait(false);
        }
        if (task is Task<int> ti)
        {
            return await ti.ConfigureAwait(false);
        }
        if (task is Task<int?> tni)
        {
            return await tni.ConfigureAwait(false);
        }
        if (task is Task<long> tl)
        {
            return await tl.ConfigureAwait(false);
        }
        if (task is Task<long?> tnl)
        {
            return await tnl.ConfigureAwait(false);
        }
        if (task is Task<double> td)
        {
            return await td.ConfigureAwait(false);
        }
        if (task is Task<double?> tnd)
        {
            return await tnd.ConfigureAwait(false);
        }
        if (task is Task<float> tf)
        {
            return await tf.ConfigureAwait(false);
        }
        if (task is Task<float?> tnf)
        {
            return await tnf.ConfigureAwait(false);
        }
        if (task is Task<decimal> tdc)
        {
            return await tdc.ConfigureAwait(false);
        }
        if (task is Task<decimal?> tndc)
        {
            return await tndc.ConfigureAwait(false);
        }
        if (task is Task<short> tsh)
        {
            return await tsh.ConfigureAwait(false);
        }
        if (task is Task<short?> tnsh)
        {
            return await tnsh.ConfigureAwait(false);
        }
        if (task is Task<ushort> tush)
        {
            return await tush.ConfigureAwait(false);
        }
        if (task is Task<ushort?> tnush)
        {
            return await tnush.ConfigureAwait(false);
        }
        if (task is Task<uint> tui)
        {
            return await tui.ConfigureAwait(false);
        }
        if (task is Task<uint?> tnui)
        {
            return await tnui.ConfigureAwait(false);
        }
        if (task is Task<ulong> tul)
        {
            return await tul.ConfigureAwait(false);
        }
        if (task is Task<ulong?> tnul)
        {
            return await tnul.ConfigureAwait(false);
        }
        if (task is Task<byte> tby)
        {
            return await tby.ConfigureAwait(false);
        }
        if (task is Task<byte?> tnby)
        {
            return await tnby.ConfigureAwait(false);
        }
        if (task is Task<sbyte> tsby)
        {
            return await tsby.ConfigureAwait(false);
        }
        if (task is Task<sbyte?> tnsby)
        {
            return await tnsby.ConfigureAwait(false);
        }
        if (task is Task<char> tch)
        {
            return await tch.ConfigureAwait(false);
        }
        if (task is Task<char?> tnch)
        {
            return await tnch.ConfigureAwait(false);
        }
        if (task is Task<DateTime> tdt)
        {
            return await tdt.ConfigureAwait(false);
        }
        if (task is Task<DateTime?> tndt)
        {
            return await tndt.ConfigureAwait(false);
        }
        if (task is Task<DateTimeOffset> tdto)
        {
            return await tdto.ConfigureAwait(false);
        }
        if (task is Task<DateTimeOffset?> tndto)
        {
            return await tndto.ConfigureAwait(false);
        }
        if (task is Task<TimeSpan> tts)
        {
            return await tts.ConfigureAwait(false);
        }
        if (task is Task<TimeSpan?> tnts)
        {
            return await tnts.ConfigureAwait(false);
        }
        if (task is Task<Guid> tg)
        {
            return await tg.ConfigureAwait(false);
        }
        if (task is Task<Guid?> tng)
        {
            return await tng.ConfigureAwait(false);
        }
        return null;
    }

    internal async Task CallAsync(Func<BindingSource, object?[], Task<object?>> callback, Type[] paramTypes)
    {
        try
        {
            var args = new object[paramTypes.Length];
            for (int i = 0; i < paramTypes.Length; i++)
            {
                var argElement = _initializer.Args[i];
                args[i] = EvaluateArgumentValueConverter.Deserialize(argElement, paramTypes[i])!;
            }

            var source = new BindingSource(_initializer.Frame.Page.Context, _initializer.Frame.Page, _initializer.Frame);
            var result = await callback(source, args).ConfigureAwait(false);

            await SendMessageToServerAsync("resolve", new Dictionary<string, object?>
            {
                ["result"] = ScriptsHelper.SerializedArgument(result),
            }).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            await SendMessageToServerAsync(
                "reject",
                new Dictionary<string, object?>
                {
                    ["error"] = ex.ToObject(),
                }).ConfigureAwait(false);
        }
    }
}
