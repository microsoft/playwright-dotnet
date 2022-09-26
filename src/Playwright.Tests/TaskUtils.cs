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

namespace Microsoft.Playwright.Tests;

internal static class TaskUtils
{
    public static Task WhenAll(params Task[] tasks) => Task.WhenAll(tasks);

    public static Task WhenAll(IEnumerable<Task> tasks) => Task.WhenAll(tasks);

    public static Task<T[]> WhenAll<T>(params Task<T>[] tasks) => Task.WhenAll(tasks);

    public static Task<T[]> WhenAll<T>(IEnumerable<Task<T>> tasks) => Task.WhenAll(tasks);

    public static async Task<(T1, T2)> WhenAll<T1, T2>(Task<T1> task1, Task<T2> task2)
    {
        await Task.WhenAll(task1, task2).ConfigureAwait(false);

        return (task1.Result, task2.Result);
    }

    public static async Task<T> WhenAll<T>(Task<T> task1, Task task2)
    {
        await Task.WhenAll(task1, task2).ConfigureAwait(false);

        return task1.Result;
    }

    public static async Task<(T1, T2, T3)> WhenAll<T1, T2, T3>(Task<T1> task1, Task<T2> task2, Task<T3> task3)
    {
        await Task.WhenAll(task1, task2, task3).ConfigureAwait(false);

        return (task1.Result, task2.Result, task3.Result);
    }

    public static async Task<(T1, T2, T3, T4)> WhenAll<T1, T2, T3, T4>(Task<T1> task1, Task<T2> task2, Task<T3> task3, Task<T4> task4)
    {
        await Task.WhenAll(task1, task2, task3, task4).ConfigureAwait(false);

        return (task1.Result, task2.Result, task3.Result, task4.Result);
    }

    public static async Task<(T1, T2)> WhenAll<T1, T2>(Task<T1> task1, Task<T2> task2, Task task3)
    {
        await Task.WhenAll(task1, task2, task3).ConfigureAwait(false);

        return (task1.Result, task2.Result);
    }

    public static async Task<bool> WithBooleanReturnType(this Task task)
    {
        await task.ConfigureAwait(false);
        return default;
    }
}
