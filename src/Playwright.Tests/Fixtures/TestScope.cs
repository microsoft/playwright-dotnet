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

using System.Reflection;
using System.Runtime.ExceptionServices;

namespace Microsoft.Playwright.Tests.Fixtures;

internal sealed class FixtureInstance
{
    public FixtureDefinition Definition { get; init; }

    public object Value { get; set; }

    public Func<Task> Teardown { get; set; }
}

/// <summary>The fixtures that were set up in one scope, a test or a worker.</summary>
internal sealed class FixtureStore
{
    private readonly Dictionary<FixtureDefinition, FixtureInstance> _instances = [];
    private readonly List<FixtureInstance> _order = [];

    public bool TryGet(FixtureDefinition definition, out FixtureInstance instance)
    {
        lock (_instances)
        {
            return _instances.TryGetValue(definition, out instance);
        }
    }

    public FixtureInstance Find(string name)
    {
        lock (_instances)
        {
            return _order.LastOrDefault(instance => instance.Definition.Name == name);
        }
    }

    public void Add(FixtureInstance instance)
    {
        lock (_instances)
        {
            _instances[instance.Definition] = instance;
            _order.Add(instance);
        }
    }

    /// <summary>Tears the fixtures down, the last one first, and collects the errors.</summary>
    public async Task TeardownAsync(List<Exception> errors)
    {
        List<FixtureInstance> instances;
        lock (_instances)
        {
            instances = [.. _order];
            _order.Clear();
            _instances.Clear();
        }
        for (var i = instances.Count - 1; i >= 0; i--)
        {
            if (instances[i].Teardown == null)
            {
                continue;
            }
            try
            {
                await instances[i].Teardown().ConfigureAwait(false);
            }
            catch (Exception e)
            {
                errors.Add(e);
            }
        }
    }
}

/// <summary><c>workerInfo</c> upstream.</summary>
public sealed class WorkerInfo
{
    internal WorkerInfo(int workerIndex, int parallelIndex)
    {
        WorkerIndex = workerIndex;
        ParallelIndex = parallelIndex;
    }

    /// <summary>Unique for every worker of the run.</summary>
    public int WorkerIndex { get; }

    /// <summary>Between 0 and the number of workers that run at the same time. It decides the ports of the test servers.</summary>
    public int ParallelIndex { get; }
}

/// <summary>Resolves the fixtures of one test, and of the worker that runs it.</summary>
internal sealed class TestScope
{
    private static readonly AsyncLocal<TestScope> _current = new();
    private readonly FixtureStore _store = new();
    private readonly HashSet<FixtureDefinition> _settingUp = [];
    private int _workerSetupDepth;
    private volatile bool _closed;

    public TestScope(Worker worker, TestClassInfo classInfo, object testObject, TestInfo testInfo)
    {
        Worker = worker;
        ClassInfo = classInfo;
        TestObject = testObject;
        TestInfo = testInfo;
    }

    /// <summary>The scope of the test that runs in this async flow, for <see cref="It"/>.</summary>
    public static TestScope Current
    {
        get => _current.Value;
        set => _current.Value = value;
    }

    public Worker Worker { get; }

    public TestClassInfo ClassInfo { get; }

    public object TestObject { get; }

    public TestInfo TestInfo { get; }

    public async Task<object> ResolveAsync(string name, MethodInfo requestedBy, bool workerOnly = false)
    {
        if (name == "testInfo" && !workerOnly)
        {
            return TestInfo;
        }
        if (name == "workerInfo")
        {
            return Worker.Info;
        }
        if (!ClassInfo.Fixtures.TryGetValue(name, out var definition))
        {
            throw new InvalidOperationException($"{Describe(requestedBy)} has unknown parameter \"{name}\": it is not the name of a fixture");
        }
        return await SetupAsync(definition, requestedBy, workerOnly).ConfigureAwait(false);
    }

    /// <summary>Calls a test, a hook or a fixture method: the first parameters take <paramref name="leading"/>, the others are fixtures.</summary>
    public async Task InvokeAsync(MethodInfo method, object[] leading = null)
    {
        var arguments = await ResolveArgumentsAsync(method, leading ?? [], null, null, false).ConfigureAwait(false);
        var result = Invoke(method, arguments);
        if (result is Task task)
        {
            await task.ConfigureAwait(false);
        }
    }

    public async Task SetupAutoFixturesAsync()
    {
        foreach (var definition in ClassInfo.AutoFixtures)
        {
            await SetupAsync(definition, definition.Method, false).ConfigureAwait(false);
        }
    }

    public async Task RunBeforeAllAsync()
    {
        if (ClassInfo.BeforeAll.Count == 0 || !Worker.BeforeAllDone.Add(ClassInfo.Type))
        {
            return;
        }
        // The afterAll hooks run when the worker closes, with the fixtures of this class.
        foreach (var hook in ClassInfo.AfterAll)
        {
            var arguments = await ResolveArgumentsAsync(hook, [], null, null, true).ConfigureAwait(false);
            Worker.AfterAll.Add(async () =>
            {
                if (Invoke(hook, arguments) is Task task)
                {
                    await task.ConfigureAwait(false);
                }
            });
        }
        foreach (var hook in ClassInfo.BeforeAll)
        {
            var arguments = await ResolveArgumentsAsync(hook, [], null, null, true).ConfigureAwait(false);
            if (Invoke(hook, arguments) is Task task)
            {
                await task.ConfigureAwait(false);
            }
        }
    }

    public Task TeardownAsync(List<Exception> errors)
    {
        _closed = true;
        return _store.TeardownAsync(errors);
    }

    private async Task<object> SetupAsync(FixtureDefinition definition, MethodInfo requestedBy, bool workerOnly)
    {
        if (workerOnly && definition.Scope == FixtureScope.Test)
        {
            throw new InvalidOperationException($"{Describe(requestedBy)} runs in the worker scope, it cannot use the test fixture \"{definition.Name}\"");
        }
        var store = definition.Scope == FixtureScope.Worker ? Worker.Store : _store;
        if (store.TryGet(definition, out var existing))
        {
            return existing.Value;
        }
        if (_closed)
        {
            throw new InvalidOperationException($"Fixture \"{definition.Name}\" was requested after the test finished");
        }
        if (!_settingUp.Add(definition))
        {
            throw new InvalidOperationException($"Fixture \"{definition.Name}\" depends on itself");
        }
        var isWorker = definition.Scope == FixtureScope.Worker;
        if (isWorker && _workerSetupDepth++ == 0)
        {
            // The time to launch the browser or start the servers is not the time of the test.
            TestInfo.PauseTimeout();
        }
        try
        {
            var instance = new FixtureInstance { Definition = definition };
            var valueSource = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            var teardownSource = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            Func<object, Task> use = value =>
            {
                valueSource.TrySetResult(value);
                return teardownSource.Task;
            };
            var arguments = await ResolveArgumentsAsync(definition.Method, [], definition, use, isWorker).ConfigureAwait(false);
            var result = Invoke(definition.Method, arguments);
            if (HasUse(definition.Method))
            {
                var task = result as Task ?? throw new InvalidOperationException($"Fixture {definition} takes \"use\", so it returns a Task");
                await Task.WhenAny(valueSource.Task, task).ConfigureAwait(false);
                if (!valueSource.Task.IsCompleted)
                {
                    await task.ConfigureAwait(false);
                    throw new InvalidOperationException($"Fixture {definition} did not call \"use\"");
                }
                instance.Value = await valueSource.Task.ConfigureAwait(false);
                instance.Teardown = async () =>
                {
                    teardownSource.TrySetResult(true);
                    await task.ConfigureAwait(false);
                };
            }
            else if (result is Task task)
            {
                await task.ConfigureAwait(false);
                var resultProperty = task.GetType().GetProperty("Result");
                instance.Value = definition.Method.ReturnType == typeof(Task) ? null : resultProperty?.GetValue(task);
            }
            else
            {
                instance.Value = result;
            }
            store.Add(instance);
            return instance.Value;
        }
        finally
        {
            _settingUp.Remove(definition);
            if (isWorker && --_workerSetupDepth == 0)
            {
                TestInfo.ResumeTimeout();
            }
        }
    }

    private static bool HasUse(MethodInfo method)
    {
        var parameters = method.GetParameters();
        return parameters.Length > 0 && IsUse(parameters[^1]);
    }

    private static bool IsUse(ParameterInfo parameter) =>
        parameter.Name == "use" && (parameter.ParameterType == typeof(Func<Task>) ||
            (parameter.ParameterType.IsGenericType && parameter.ParameterType.GetGenericTypeDefinition() == typeof(Func<,>) && parameter.ParameterType.GetGenericArguments()[1] == typeof(Task)));

    private async Task<object[]> ResolveArgumentsAsync(MethodInfo method, object[] leading, FixtureDefinition self, Func<object, Task> use, bool workerOnly)
    {
        var parameters = method.GetParameters();
        var arguments = new object[parameters.Length];
        for (var i = 0; i < parameters.Length; i++)
        {
            var parameter = parameters[i];
            if (i < leading.Length)
            {
                arguments[i] = ConvertCaseValue(leading[i], parameter, method);
                continue;
            }
            if (self != null && i == parameters.Length - 1 && IsUse(parameter))
            {
                arguments[i] = MakeUse(parameter.ParameterType, use);
                continue;
            }
            object value;
            if (self != null && parameter.Name == self.Name)
            {
                if (self.Super == null)
                {
                    throw new InvalidOperationException($"Fixture {self} takes itself as a parameter, and there is no fixture \"{self.Name}\" to override");
                }
                value = await SetupAsync(self.Super, method, workerOnly).ConfigureAwait(false);
            }
            else
            {
                value = await ResolveAsync(parameter.Name, method, workerOnly).ConfigureAwait(false);
            }
            if (value == null ? (parameter.ParameterType.IsValueType && Nullable.GetUnderlyingType(parameter.ParameterType) == null) : !parameter.ParameterType.IsInstanceOfType(value))
            {
                throw new InvalidOperationException($"{Describe(method)}: fixture \"{parameter.Name}\" is {(value == null ? "null" : TypeName(value.GetType()))}, the parameter is {TypeName(parameter.ParameterType)}");
            }
            arguments[i] = CloneOptions(value);
        }
        return arguments;
    }

    private static object ConvertCaseValue(object value, ParameterInfo parameter, MethodInfo method)
    {
        var type = Nullable.GetUnderlyingType(parameter.ParameterType) ?? parameter.ParameterType;
        if (value == null || type.IsInstanceOfType(value))
        {
            return value;
        }
        try
        {
            return type.IsEnum ? Enum.ToObject(type, value) : Convert.ChangeType(value, type, System.Globalization.CultureInfo.InvariantCulture);
        }
        catch (Exception)
        {
            throw new InvalidOperationException($"{Describe(method)}: the case value for \"{parameter.Name}\" is {TypeName(value.GetType())}, the parameter is {TypeName(parameter.ParameterType)}");
        }
    }

    private object Invoke(MethodInfo method, object[] arguments)
    {
        object target = null;
        if (!method.IsStatic)
        {
            if (TestObject == null || !method.DeclaringType.IsInstanceOfType(TestObject))
            {
                throw new InvalidOperationException($"{Describe(method)} is not static, so only the tests of {method.DeclaringType.Name} can use it. Make it static to use it in the nested classes");
            }
            target = TestObject;
        }
        try
        {
            return method.Invoke(target, arguments);
        }
        catch (TargetInvocationException e) when (e.InnerException != null)
        {
            ExceptionDispatchInfo.Capture(e.InnerException).Throw();
            throw;
        }
    }

    private static Delegate MakeUse(Type parameterType, Func<object, Task> use)
    {
        if (parameterType == typeof(Func<Task>))
        {
            return new Func<Task>(() => use(null));
        }
        var valueType = parameterType.GetGenericArguments()[0];
        return (Delegate)typeof(TestScope)
            .GetMethod(nameof(MakeTypedUse), BindingFlags.NonPublic | BindingFlags.Static)
            .MakeGenericMethod(valueType)
            .Invoke(null, [use]);
    }

    private static Func<T, Task> MakeTypedUse<T>(Func<object, Task> use) => value => use(value);

    /// <summary>
    /// A test gets its own copy of an options object, so that it can not change the options of the next test.
    /// </summary>
    private static object CloneOptions(object value)
    {
        var type = value?.GetType();
        if (type == null || type.Namespace != "Microsoft.Playwright" || !type.Name.EndsWith("Options", StringComparison.Ordinal))
        {
            return value;
        }
        var copyConstructor = type.GetConstructor([type]);
        return copyConstructor == null ? value : copyConstructor.Invoke([value]);
    }

    private static string Describe(MethodInfo method) => $"{method.DeclaringType?.Name}.{method.Name}";

    private static string TypeName(Type type)
    {
        if (!type.IsGenericType)
        {
            return type.Name;
        }
        return type.Name.Substring(0, type.Name.IndexOf('`')) + "<" + string.Join(", ", type.GetGenericArguments().Select(TypeName)) + ">";
    }
}
