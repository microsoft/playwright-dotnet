# Threading model

The JS client runs on one event loop: a driver message is handled synchronously, and the promise continuations it resolves run as microtasks before the next message. Library code between two awaits can therefore check a state and subscribe to an event without anything happening in between. The .NET client keeps the JS method bodies, so it restores that property with one **dispatcher thread** per connection (`src/Playwright/Transport/Dispatcher.cs`).

## How it works

- **One thread, one item at a time.** The dispatcher thread runs driver messages (`Connection.Dispatch`) and all library code. The transport reads and parses on the thread pool and posts each message to the dispatcher. A connection that `BrowserType.ConnectAsync` creates shares the dispatcher of the connection that owns its pipe.
- **API calls hop on and off.** `Connection.WrapApiCallAsync` posts the body of a public call to the dispatcher, and returns the result on the thread pool. The caller's continuation never runs on the dispatcher thread, and a `SynchronizationContext` that the caller captured is honored as with any other `Task`.
- **Microtasks.** A `TaskCompletionSource` that library code awaits is completed with `Dispatcher.Complete` or `Dispatcher.Fail`: after the current item, before the next one. The awaiting code continues inline on the dispatcher thread, because every library `await` uses `ConfigureAwait(false)` and the thread has no `SynchronizationContext`. So a call resumes once the message that completed it has been handled in full: `Page.Load` has fired when `WaitForLoadStateAsync` returns.
- **Event accessors update a snapshot.** `+=` and `-=` run on the caller's thread and replace the handler list atomically (`ChannelOwner.UpdateEventHandler`), so the dispatcher thread raises either the old list or the new one. For the events that the server sends only while somebody listens, the dispatcher thread alone tells the server, from the handlers as they are when it runs.
- **Getters read snapshots.** Synchronous getters run on the caller's thread. Scalars (`Url`, `IsClosed`) are single reads. Lists (`Pages`, `Frames`, `ChildFrames`, `Workers`, `Contexts`) are copy-on-write through `Helpers/CopyOnWrite.cs`: a published list is never mutated.

Where user code runs:

| User code | Thread |
|---|---|
| Event handler, route handler, binding, locator handler: the synchronous part | dispatcher |
| The same handler after it awaited a Playwright call or anything else | thread pool |
| Predicate of a wait (`WaitForRequestAsync(r => ...)`, `WaitForURLAsync(url => ...)`) | dispatcher |
| Action of `RunAndWaitFor*Async` | thread pool |
| Callback of `Expect(callback).ToPassAsync()`, every attempt | thread pool |
| Code after `await` of any Playwright call | thread pool, or the caller's `SynchronizationContext` |

## Rules of thumb

1. **Wrap public async methods.** A public async method is either a single `SendMessageToServerAsync` call plus parsing of its result, or its whole body is inside `WrapApiCallAsync(async () => ...)`. Private helpers that a public method delegates to are wrapped as well; a nested wrap is inline and free. Put the wrap on the signature line and keep the body at the indentation of a method body, so that wrapping a method does not re-indent it:

   ```csharp
   public Task CloseAsync(PageCloseOptions? options = default) => WrapApiCallAsync(async () =>
   {
       ...
   });
   ```
2. **Touch state only on the dispatcher thread.** Fields such as `_routes`, `_loadStates`, `_pages` and `Bindings` are read and written there. Put `_connection.Dispatcher.AssertCurrent()` next to new state access; it fails in Debug builds, which is what the tests run.
3. **Complete through the dispatcher.** Use `Dispatcher.Complete` and `Dispatcher.Fail`, never `SetResult` or `SetException`, on a source that library code awaits. Do not create such a source with `RunContinuationsAsynchronously`.
4. **Switch back after a foreign await.** `await _connection.Dispatcher.SwitchTo()` before touching state after awaiting something that the dispatcher did not complete:
   - a user callback (put the switch in a `finally`, the callback may throw),
   - a timer or a timeout (`Task.Delay`, `WithTimeout`), file or pipe I/O,
   - a `Task` that another call started and cached,
   - any library call in a flow that a message started (`OnRouteAsync`, `BindingCall.CallAsync`). Such a flow is not inside an API call, so the calls it makes are outermost and return on the thread pool.
5. **Synchronous API never waits for the dispatcher.** It may be called from a handler that runs on it. Read a snapshot, or hand work to the dispatcher with `Dispatcher.RunOrPost`: it is ordered with the caller's other calls.
6. **Hand out copy-on-write lists.** A collection that a getter returns, or that synchronous API enumerates, is replaced with `CopyOnWrite.Add` and `CopyOnWrite.Remove`, never mutated in place.
7. **Never block the dispatcher thread.** No `.Result`, `.Wait()` or `GetAwaiter().GetResult()` on a Playwright call in library code that may run there.
8. **Keep `ConfigureAwait(false)` everywhere**, and pass `TaskScheduler.Default` to `ContinueWith` and `StartNew`.
9. **Clear the API zone around user code.** A call is nested, and stays on the dispatcher thread, when `Connection.ApiZone` is set. User code that runs inside a wrapped body must not inherit it; see `Waiter.CancelWaitOnExceptionAsync`.

## What users can rely on

- Events that the driver sent before a reply have been handled when the call returns.
- Blocking is safe everywhere except in the synchronous part of a handler: a synchronous wait on a Playwright call there stalls the connection until it times out.
- A handler that throws closes the connection, as before.
- `+=` and `-=` on an event take effect at once, as with any C# event. After `-=` has returned, the handler is not called for an event that is raised later; only an event that is being raised on the dispatcher thread at that very moment may still reach it.
- Arguments are read when the call runs on the dispatcher thread, shortly after it was made. Do not mutate an options object or a buffer that a call in flight still uses.

## Tests

`DispatcherTests.cs` pins the dispatcher itself. `ThreadingModelTests.cs` pins the rules above end to end; add a case there when a rule changes.
