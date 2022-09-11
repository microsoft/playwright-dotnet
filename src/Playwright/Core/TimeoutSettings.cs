/*
 * MIT License
 *
 * Copyright (c) Microsoft Corporation.
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */

namespace Microsoft.Playwright.Core;

internal class TimeoutSettings
{
    private const int DefaultTimeout = 30_000;

    private readonly TimeoutSettings _parent;
    private float? _defaultTimeout;
    private float? _defaultNavigationTimeout;

    public TimeoutSettings(TimeoutSettings parent = null)
    {
        _parent = parent;
    }

    public void SetDefaultTimeout(float timeout)
    {
        _defaultTimeout = timeout;
    }

    public void SetDefaultNavigationTimeout(float timeout)
    {
        _defaultNavigationTimeout = timeout;
    }

    public float NavigationTimeout(float? timeout)
    {
        if (timeout.HasValue)
        {
            return timeout.Value;
        }

        if (_defaultNavigationTimeout.HasValue)
        {
            return _defaultNavigationTimeout.Value;
        }

        if (_defaultTimeout.HasValue)
        {
            return _defaultTimeout.Value;
        }

        if (_parent != null)
        {
            return _parent.NavigationTimeout(timeout);
        }

        return DefaultTimeout;
    }

    public float Timeout(float? timeout)
    {
        if (timeout.HasValue)
        {
            return timeout.Value;
        }

        if (_defaultTimeout.HasValue)
        {
            return _defaultTimeout.Value;
        }

        if (_parent != null)
        {
            return _parent.Timeout(timeout);
        }

        return DefaultTimeout;
    }
}
