/*
 * MIT License
 *
 * Copyright (c) Microsoft Corporation.
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
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

using System;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

#nullable enable

namespace Microsoft.Playwright;

public partial interface IPage
{
    Task<JsonElement?> EvaluateAsync(string expression, object? arg = default);

    Task<JsonElement?> EvalOnSelectorAsync(string selector, string expression, object? arg = default);

    Task<JsonElement?> EvalOnSelectorAllAsync(string selector, string expression, object? arg = default);

    Task ExposeBindingAsync(string name, Action<BindingSource> callback);

    Task ExposeBindingAsync<T>(string name, Action<BindingSource, T> callback);

    Task ExposeBindingAsync<TResult>(string name, Func<BindingSource, TResult> callback);

    Task ExposeBindingAsync<TResult>(string name, Func<BindingSource, IJSHandle, TResult> callback);

    Task ExposeBindingAsync<T, TResult>(string name, Func<BindingSource, T, TResult> callback);

    Task ExposeBindingAsync<T1, T2, TResult>(string name, Func<BindingSource, T1, T2, TResult> callback);

    Task ExposeBindingAsync<T1, T2, T3, TResult>(string name, Func<BindingSource, T1, T2, T3, TResult> callback);

    Task ExposeBindingAsync<T1, T2, T3, T4, TResult>(string name, Func<BindingSource, T1, T2, T3, T4, TResult> callback);

    Task ExposeFunctionAsync<T>(string name, Action<T> callback);

    Task ExposeFunctionAsync<TResult>(string name, Func<TResult> callback);

    Task ExposeFunctionAsync<T, TResult>(string name, Func<T, TResult> callback);

    Task ExposeFunctionAsync<T1, T2, TResult>(string name, Func<T1, T2, TResult> callback);

    Task ExposeFunctionAsync<T1, T2, T3, TResult>(string name, Func<T1, T2, T3, TResult> callback);

    Task ExposeFunctionAsync<T1, T2, T3, T4, TResult>(string name, Func<T1, T2, T3, T4, TResult> callback);

    /// <inheritdoc cref="RouteAsync(string, Action{IRoute}, PageRouteOptions?)"/>
    Task RouteAsync(string url, Func<IRoute, Task> handler, PageRouteOptions? options = default);

    /// <inheritdoc cref="RouteAsync(Regex, Action{IRoute}, PageRouteOptions?)"/>
    Task RouteAsync(Regex url, Func<IRoute, Task> handler, PageRouteOptions? options = default);

    /// <inheritdoc cref="RouteAsync(Func{string, bool}, Action{IRoute}, PageRouteOptions?)"/>
    Task RouteAsync(Func<string, bool> url, Func<IRoute, Task> handler, PageRouteOptions? options = default);

    /// <inheritdoc cref="UnrouteAsync(string, Action{IRoute}?)"/>
    Task UnrouteAsync(string url, Func<IRoute, Task> handler);

    /// <inheritdoc cref="UnrouteAsync(Regex, Action{IRoute}?)"/>
    Task UnrouteAsync(Regex url, Func<IRoute, Task> handler);

    /// <inheritdoc cref="UnrouteAsync(Func{string, bool}, Action{IRoute}?)"/>
    Task UnrouteAsync(Func<string, bool> url, Func<IRoute, Task> handler);
}
