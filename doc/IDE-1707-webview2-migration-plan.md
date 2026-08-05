# IDE-1707 — Migrate HTML hosts from WPF `WebBrowser` to `WebView2`

## Why

The Language Server emits HTML UI (settings page, issue description, scan summary) that's authored and tested against Chromium-family engines (VS Code/Electron, IntelliJ/JCEF, Eclipse/Edge-Chromium). The VS plugin renders it in the WPF `WebBrowser` control, which wraps the legacy IE/MSHTML ActiveX. The two known symptoms:

- HTML renders at 96 DPI even on high-DPI displays (so it looks "tiny" relative to the rest of VS).
- `Settings/HtmlSettingsWindow.xaml.cs:63` already disables the DPI-aware flag because turning it on misrenders dropdowns — a workaround for an MSHTML quirk that does not exist in WebView2.

WebView2 (Chromium-based Evergreen Edge runtime) renders the LS HTML the same way the other Snyk IDE plugins do, has correct per-monitor DPI handling, and is the supported successor to the WebBrowser control.

## Scope

Three host surfaces, each backed by `<WebBrowser>` today:

1. `Settings/HtmlSettingsWindow.xaml` — modal `DialogWindow`, settings page from LS.
2. `UI/Toolwindow/HtmlDescriptionPanel.xaml` — issue details panel inside the tool window.
3. `UI/Toolwindow/SummaryHtmlPanel.xaml` — scan summary panel inside the tool window.

Plus `DebugHtmlSettingsWindow` (subclass of `HtmlSettingsWindow` for local-HTML iteration). The `HtmlProvider*` family and `HtmlResourceLoader` (CSS-variable templating) are engine-agnostic and don't need changes apart from removing the IE7 `X-UA-Compatible` meta if we want to.

Out of scope: the LS-side HTML, snyk-ls itself, fallback HTML semantics, theming, IE registry workarounds.

## Target design

A single new helper, `UI/Html/WebView2Host.cs`, encapsulates:

- `EnsureCoreWebView2Async(userDataFolder)` — async init with a fixed `UserDataFolder = %LOCALAPPDATA%\Snyk\WebView2`.
- Setting `CoreWebView2Settings`: disable dev tools / context menu / status bar / new-window-popup in release; allow them when `DebugHtmlSettingsWindow.AutoOpenOnStartup` or a debug flag is on.
- `NavigateAsync(string html)` — wraps `NavigateToString` with content-length guards (`NavigateToString` has a ~2 MB limit; if exceeded, write to a temp file under the UserDataFolder and `Source = new Uri(...)` instead).
- `ExecuteScriptAsync(string js)` — typed wrapper that dispatches via `CoreWebView2.ExecuteScriptAsync`, replacing `InvokeScript("eval", ...)`.
- A `WebMessageReceived` handler that JSON-parses the payload and dispatches to the panel-specific bridge.
- Registers, via `AddScriptToExecuteOnDocumentCreatedAsync`, a `window.external` polyfill (see below) so the existing LS HTML's `window.external.X(...)` calls continue to work with **zero LS-side changes**.

Each of the three panels becomes a thin shell that constructs a bridge object and hands it to the helper. `WebBrowserHostUIHandler.cs` and its entire `Native.*` COM interop section are deleted.

### Bridge surface (preserved verbatim from the JS side)

```
window.external.__saveIdeConfig__(jsonString)
window.external.__onFormDirtyChange__(isDirty)
window.external.__ideSaveAttemptFinished__(status)
window.external.__ideExecuteCommand__(command, argsJson, callbackId)
window.external.OpenLink(link)
window.external.OpenFileInEditor(filePath, startLine, endLine, startCharacter, endCharacter)
window.external.EnableDelta(isEnabled)
window.external.GenerateFixes(value)
window.external.ApplyFixDiff(fixID)
window.external.SubmitIgnoreRequest(...)
window.external.FocusToolWindow()
```

**Every one of these is fire-and-forget today** — JS does not consume the return value from any of them. `IsSaveComplete` on the bridge is read only from C#. The closest thing to a round-trip, `__ideExecuteCommand__`, registers a client-side callback under `window.__ideCallbacks__[id]` and is invoked later as a separate C#→JS script push, not as the return value of the original call.

Because the JS→C# direction is purely one-way, we use **`chrome.webview.postMessage`** rather than `AddHostObjectToScript` for the bridge. Rationale:

- `postMessage` is non-blocking; sync host objects stall the JS thread for every call.
- No `[ComVisible]` / COM-marshalling constraints on the bridge classes — they become plain C# POCOs.
- No reliance on the non-standard `chrome.webview.hostObjects.sync` global, only the canonical `postMessage` channel.
- Avoids the WebView2 reentrancy footgun (synchronous host-object calls from inside a `WebMessageReceived` handler can deadlock).
- Tighter security surface: one C# entry point dispatching by command name, vs. exposing every public method on the bridge to JS.
- Trivially testable: hand the dispatcher a JSON string in a unit test, assert the right bridge method is called.

We keep the `window.external` contract by registering this polyfill via `AddScriptToExecuteOnDocumentCreatedAsync`, so it runs before any page script on every load:

```js
(function () {
  const post = (method, args) =>
    chrome.webview.postMessage({ method: method, args: args || [] });
  window.external = {
    __saveIdeConfig__:          (json)              => post('__saveIdeConfig__', [json]),
    __onFormDirtyChange__:      (isDirty)           => post('__onFormDirtyChange__', [isDirty]),
    __ideSaveAttemptFinished__: (status)            => post('__ideSaveAttemptFinished__', [status]),
    __ideExecuteCommand__:      (cmd, args, cbId)   => post('__ideExecuteCommand__', [cmd, args, cbId]),
    OpenLink:                   (href)              => post('OpenLink', [href]),
    OpenFileInEditor:           (f, sl, el, sc, ec) => post('OpenFileInEditor', [f, sl, el, sc, ec]),
    EnableDelta:                (enabled)           => post('EnableDelta', [enabled]),
    GenerateFixes:              (value)             => post('GenerateFixes', [value]),
    ApplyFixDiff:               (fixId)             => post('ApplyFixDiff', [fixId]),
    SubmitIgnoreRequest:        (id, t, r, exp)     => post('SubmitIgnoreRequest', [id, t, r, exp]),
    FocusToolWindow:            ()                  => post('FocusToolWindow', []),
  };
})();
```

C# side: a single `WebMessageReceived` handler parses `{ method, args }` and dispatches by `method` to the panel's bridge instance. Bridge classes drop their `[ComVisible(true)]` attribute and become normal POCOs.

`IsSaveComplete` stops being a property on the bridge. The `__ideSaveAttemptFinished__` handler completes a `TaskCompletionSource<bool>` that `OkButton_OnClick` awaits, replacing the existing 100 ms polling loop in `HtmlSettingsWindow.xaml.cs:238`.

### `InvokeScript` callsites

All become `await ExecuteScriptAsync(js)`:

- `HtmlSettingsWindow.OkButton_OnClick`: `SettingsBrowser.InvokeScript("getAndSaveIdeConfig")`
- `HtmlSettingsWindow.InvokeSetAuthToken`: builds and `Append`s a `<script>` element via `doc.CreateElement`. Replace with `ExecuteScriptAsync` of the same body.
- `HtmlSettingsWindow.InvokeCommandCallback`: same — replace document-mutation with `ExecuteScriptAsync`.
- `HtmlSettingsWindow.InjectIdeBridgeFunctions` (the post-load fallback injection): becomes `CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(...)`, which fires before page scripts on every navigation and removes the need for a "second injection just in case" path entirely.
- `HtmlDescriptionPanel.HtmlViewerOnLoadCompleted` and `SummaryHtmlPanel.HtmlViewerOnLoadCompleted`: same swap to `ExecuteScriptAsync(htmlProvider.GetInitScript())`.

### What gets deleted

- `UI/Toolwindow/WebBrowserHostUIHandler.cs` (entire file, ~300 lines of COM interop)
- `WebBrowserHostUIHandler.SetDpiAwareFlag(false)` call at `HtmlSettingsWindow.xaml.cs:63` and surrounding comment
- `SettingsBrowser.InvalidateVisual()` / `UpdateLayout()` workaround pairs in all three panels
- The `<!-- TEMP: WindowChrome removed to test DPI handling -->` block in `HtmlSettingsWindow.xaml`
- The "DPI scaling is different during startup vs from the settings page" caveat in `DebugHtmlSettingsWindow.xaml.cs`

## Async model: how WebView2 differs from the IE-based renderer

The largest *invisible* change in this migration is that almost everything WebView2 does is async, where the WebBrowser control was synchronous. This is not just an API-ergonomic difference — it changes when each operation is safe to call, what failure modes look like, and how the panels need to sequence their work.

### Control lifecycle

**Today (`WebBrowser`).** `<WebBrowser>` in XAML is fully functional the moment the visual tree is constructed. `InitializeComponent()` returns and we can immediately set `ObjectForScripting`, call `NavigateToString`, attach event handlers. There is no separate "is the engine ready" step — the IE ActiveX is hosted inline and behaves synchronously from C#'s perspective. The browser is idle until you navigate, but binding to it is free. Construction never fails: IE is part of Windows, so there's no runtime-missing case.

**WebView2.** `<wv2:WebView2>` in XAML creates only a thin WPF host. The underlying `CoreWebView2` is **`null`** until we call `await webView.EnsureCoreWebView2Async()`. Almost every property and method throws `InvalidOperationException` if touched before init: `NavigateToString` (throws), `CoreWebView2` (null), `CoreWebView2Settings` (null), `AddHostObjectToScript` (NRE). `Source` is an exception — assigning it before init is allowed and queues the navigation.

Init does real work: locates the Evergreen runtime, launches `msedgewebview2.exe` as a child process, and establishes an IPC channel. Cold launches can take ~1–3 seconds; warm ones ~100–300 ms. Init **can fail**: runtime missing, AppContainer / permissions, UserDataFolder unwritable. We must handle these — the WebBrowser had no equivalent failure path.

**Implication for code shape:** each panel needs a stored init task, awaited at the top of any method that touches the WebView2. The first navigation cannot start until that task resolves.

```csharp
private readonly TaskCompletionSource<bool> _initTcs = new();

private async Task InitializeAsync()
{
    await webView.EnsureCoreWebView2Async();
    // configure settings, add host object, register pre-script...
    _initTcs.SetResult(true);
}

public async Task SetContentAsync(string html)
{
    await _initTcs.Task;
    webView.NavigateToString(html);
}
```

### Navigation

Once init has completed, `NavigateToString` is still fire-and-forget. Calling it before init throws. There is also a **2 MB cap** on `NavigateToString`; for content above that, we write to a temp file under `UserDataFolder` and assign `webView.Source = new Uri(tempPath)`. The LS settings HTML is well under that today, but `HtmlDescriptionPanel` content can grow with embedded base64 images — the helper should guard.

### Script invocation (the biggest behavioural change)

This is where the IE control's synchronous nature shows up most.

Today, `HtmlDescriptionPanel.xaml.cs:33`:
```csharp
HtmlViewer.InvokeScript("eval", new string[] { htmlProvider.GetInitScript() });
```
`InvokeScript` is **synchronous**. It blocks the UI thread until the JS finishes and returns the result inline as an `object`. Errors throw inline.

After:
```csharp
await webView.CoreWebView2.ExecuteScriptAsync(htmlProvider.GetInitScript());
```
`ExecuteScriptAsync` returns `Task<string>` — the script's return value, JSON-serialised. The call is marshalled over IPC to the Edge process; it cannot block the UI thread.

The ripple effects:

- `HtmlViewerOnLoadCompleted` becomes `async` and awaits, or we move the script call to `CoreWebView2.NavigationCompleted`.
- `HtmlSettingsWindow.OkButton_OnClick` currently does `SettingsBrowser.InvokeScript("getAndSaveIdeConfig")` then polls `IsSaveComplete` with a 100 ms `Task.Delay` loop. With `ExecuteScriptAsync` we can `await` the script directly, and replace the poll with a `TaskCompletionSource<bool>` flipped by the bridge's `__ideSaveAttemptFinished__` callback. The shape simplifies.
- `InvokeSetAuthToken` and `InvokeCommandCallback` today do `dynamic doc = SettingsBrowser.Document; doc.CreateElement("script"); head.AppendChild(...)` — synchronous COM round-trips into MSHTML. **WebView2 does not expose a `Document` property at all** — there is no DOM mutation API from C#. Both methods must be rewritten as `ExecuteScriptAsync(jsBody)`. The pattern is cleaner, but it's a different shape.

### Script injection timing

Today, `HtmlSettingsWindow` injects the bridge twice: first into the HTML string before navigation (`InjectBridgeScriptIntoHtml`), then again from `LoadCompleted` (`InjectIdeBridgeFunctions`). The second injection is a safety net because `LoadCompleted` races with page scripts.

WebView2 has a first-class API for this: `CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(js)`. Scripts registered there run **before any page script on every navigation**, deterministically. We can delete `InjectIdeBridgeFunctions` and the post-load fallback path entirely — one place, one mechanism, no string-munging on the HTML.

### JS → C# direction

Today: JS calls `window.external.foo(arg)` → synchronous COM dispatch into the `ObjectForScripting` instance, on the UI thread. The C# method runs sync; JS blocks until it returns.

WebView2 offers two mechanisms:

1. **`postMessage`** — JS calls `chrome.webview.postMessage(payload)`, C# receives `CoreWebView2.WebMessageReceived` on the UI thread. Asynchronous in both directions: JS does not block, C# handler runs when scheduled. There is no return value back to JS — every call is fire-and-forget.
2. **Host objects** — `AddHostObjectToScript("snyk", bridge)`. JS sees it as `chrome.webview.hostObjects.sync.snyk.foo(arg)` (synchronous, semantically identical to the IE bridge) or `chrome.webview.hostObjects.snyk.foo(arg)` (async Promise-based).

**We use `postMessage` exclusively.** Every existing `window.external.X(...)` call is fire-and-forget — JS does not read return values anywhere. Given that, host objects' downsides (synchronous JS-thread stall, `[ComVisible]` requirement, reliance on the non-standard `hostObjects.sync` global, reentrancy deadlock risk, wide method exposure) buy us nothing. See the "Bridge surface" subsection above for the polyfill that preserves `window.external` semantics.

### Reentrancy

Largely a non-issue under the `postMessage` design — `WebMessageReceived` is async in both directions, so there's no synchronous-call-during-handler deadlock to engineer around. The only thing to watch for is `ExecuteScriptAsync` called from inside a `WebMessageReceived` handler, which is safe by construction (it's async).

### DPI specifically

The reason we're doing this. Stated in async terms: the WebView2 control hooks WPF's `OnDpiChanged` automatically and forwards the new scale to the Chromium process over IPC. We write no DPI code. The current IE workarounds (`SetDpiAwareFlag`, the `Invalidate/UpdateLayout` pairs, the WindowChrome comment in XAML) all go away.

### Threading

Both controls require UI-thread access for all calls. WebView2 additionally:

- `EnsureCoreWebView2Async` must be invoked on the UI thread; it returns to whatever sync context the caller awaited on.
- `WebMessageReceived` and `NavigationCompleted` fire on the UI thread.
- `ExecuteScriptAsync` can be awaited from any thread; the dispatch is marshalled internally.

Practical rule: keep using `JoinableTaskFactory.SwitchToMainThreadAsync()` exactly as the codebase does today.

### Summary table

| Operation                  | WebBrowser (today)                                          | WebView2 (after)                                              |
| -------------------------- | ----------------------------------------------------------- | ------------------------------------------------------------- |
| Control ready              | Sync, with XAML construction                                | Async via `await EnsureCoreWebView2Async()`                   |
| Init failure mode          | Cannot happen                                               | Runtime missing / AppContainer / UserDataFolder errors        |
| Navigate to HTML           | `NavigateToString(html)` — sync fire-and-forget             | Same call, **only valid post-init**, ≤ 2 MB                   |
| Run JS                     | `InvokeScript("eval", ...)` — sync, returns `object`        | `await ExecuteScriptAsync(js)` — returns JSON string          |
| DOM mutation from C#       | `dynamic doc = browser.Document; doc.CreateElement(...)`    | **Not supported** — use `ExecuteScriptAsync` instead          |
| Inject before page scripts | String-munge HTML pre-nav + redundant post-load fallback    | `AddScriptToExecuteOnDocumentCreatedAsync` (single source)    |
| JS → C# call               | `window.external.foo(arg)` (sync) via `ObjectForScripting`  | `chrome.webview.postMessage({method, args})` → `WebMessageReceived`; `window.external` preserved via a polyfill |
| DPI tracking               | Manual `IDocHostUIHandler.DPI_AWARE` flag + workarounds     | Automatic, hooked from WPF `OnDpiChanged`                     |
| Load-complete event        | `browser.LoadCompleted` (`NavigationEventArgs`)             | `CoreWebView2.NavigationCompleted` (`CoreWebView2NavigationCompletedEventArgs`) |

## Packaging concerns

- **NuGet:** add `Microsoft.Web.WebView2` (latest stable; pin to the version VS 2022 itself ships with to avoid loader-version conflicts — check `%ProgramFiles%\Microsoft Visual Studio\2022\*\Common7\IDE\Microsoft.Web.WebView2.*.dll` at dev time).
- **Loader DLL:** `WebView2Loader.dll` ships per-arch (x86, x64, arm64). The NuGet package places them under `runtimes/win-{arch}/native/`. For a VSIX, MSBuild needs `<IncludeAssemblyInVSIXContainer>` + a `Content` itemgroup that puts each arch's `WebView2Loader.dll` next to the managed assembly. VS itself is x86 today and arm64 in preview, both already in our `Platforms` matrix — confirm the loader is deployed for both.
- **Runtime:** the Evergreen WebView2 Runtime is required on the user's machine. It's present on Windows 11 by default, auto-installed by recent Windows 10 updates, and required by Visual Studio 2022 (which uses WebView2 for several built-in windows). We will not bundle the Fixed Version runtime. Add a runtime-present check with a friendly error in `EnsureCoreWebView2Async` for the (vanishingly rare) case it's missing.
- **TargetFramework stays at `net48`.** `Microsoft.Web.WebView2` supports it via `WPF`/`WinForms` targets in the package.

## Migration sequence (one PR per step where it makes sense)

### Step 1 — Add the dependency, wire up the loader

- Add `PackageReference` for `Microsoft.Web.WebView2` to `Snyk.VisualStudio.Extension.2022.csproj`.
- Confirm `WebView2Loader.dll` is copied into the VSIX for both x86 and arm64 (smoke-test by inspecting the produced `.vsix`).
- No behaviour change yet — nothing consumes WebView2.

### Step 2 — Introduce `WebView2Host` helper

- New file: `UI/Html/WebView2Host.cs`.
- Methods listed in "Target design" above.
- Includes the `window.external` → `chrome.webview.postMessage` polyfill (see "Bridge surface"), registered via `AddScriptToExecuteOnDocumentCreatedAsync`.
- A `WebMessageReceived` handler that JSON-parses `{ method, args }` and dispatches to a panel-supplied bridge.
- Unit-test the JS shim and the temp-file fallback for >2 MB HTML.

### Step 3 — Migrate `HtmlSettingsWindow`

- Replace `<WebBrowser>` in `HtmlSettingsWindow.xaml` with `<wv2:WebView2>`.
- Construct `WebView2Host` in code-behind, pass `HtmlSettingsScriptingBridge` as the host object.
- Replace `NavigateToString`, `InvokeScript`, document-mutation injection with `WebView2Host` methods.
- Delete the DPI flag plumbing and the `Invalidate/UpdateLayout` workarounds.
- Rework `OkButton_OnClick`: the existing `while (!IsSaveComplete) Task.Delay(100)` poll can become an `await tcs.Task` against a `TaskCompletionSource` flipped from `WebMessageReceived`.
- Verify `UpdateAuthToken` from `SnykLanguageClientCustomTarget.cs:276` still works (it calls into the singleton instance and pokes JS).

Acceptance: settings page opens at the correct DPI, dropdowns render correctly, OK and Cancel still save/discard, dirty-state still gates the OK button.

### Step 4 — Migrate `HtmlDescriptionPanel`

- Same swap. Bridge is `SnykScriptManager`.
- This control lives inside the tool window — we need to handle the case where the WebView2 init races against `SetContent` being called by the issue tree click handler. The cleanest fix: queue navigations until `EnsureCoreWebView2Async` resolves.

Acceptance: clicking an issue in the tree shows the description at correct DPI; OpenLink, OpenFileInEditor, EnableDelta, GenerateFixes, ApplyFixDiff, SubmitIgnoreRequest, FocusToolWindow all still work from the description HTML.

### Step 5 — Migrate `SummaryHtmlPanel`

- Same swap; same bridge (`SnykScriptManager`).
- `Init()` runs on startup before any scan — make sure the `EnsureCoreWebView2Async` await chain doesn't deadlock the UI thread (use `JoinableTaskFactory.RunAsync`, not `.Run`).

Acceptance: scan summary renders at correct DPI; theme variables propagate.

### Step 6 — Delete `WebBrowserHostUIHandler` and prune

- File deletion + remove dead imports.
- Sweep `bin/` references in `.csproj` for `System.Windows.Forms` if nothing else uses it (it's likely still needed — leave alone unless build proves otherwise).
- Re-check `AssemblyInfo.cs` — the `[ComVisible(true)]` attributes on bridges can stay (harmless) or be removed; WebView2's `hostObjects` does not require COM-visibility. Recommend keeping them for now to minimise diff surface and revisit later.

### Step 7 — Tests & QA pass

- Existing unit tests (`HtmlSettingsScriptingBridgeTest`, `ExecuteCommandBridgeTest`, `BaseHtmlProviderTest`) don't touch the WebBrowser — they should pass unchanged.
- Add a smoke test that verifies the `window.external` shim is wired correctly: spin up a `WebView2Host` headless-ish, load a tiny HTML that calls `window.external.__onFormDirtyChange__(true)`, assert the bridge's `onModified` ran.
- Manual QA matrix (see below).

## Risks and known gotchas

- **`postMessage` payload typing.** Every call goes through JSON now. Numeric strings stay strings (`"42"` is `"42"`, not `42`) — confirm the LS HTML isn't relying on JS-style coercion through `ObjectForScripting`. Most parameters in the bridge are already `string` (`__saveIdeConfig__(jsonString)`, `OpenFileInEditor(filePath, "5", "10", ...)`); `bool` arguments (`isDirty`, `isEnabled`) round-trip cleanly. The `__ideExecuteCommand__` payload's `argsJson` is already a pre-serialised string by design.
- **`WebMessageReceived` ordering.** Messages are delivered in order on the UI thread, but a `postMessage` issued during page load arrives *after* `NavigationCompleted`. If anything depends on synchronously processing a bridge message before navigation finishes, it will need rework — we don't have such a case today.
- **Initial focus / keyboard input.** WebView2 takes focus differently from `WebBrowser`. We may need `webView.Focus()` after `EnsureCoreWebView2Async` for the settings dialog to behave as before.
- **Cookie / cache persistence.** WebView2 persists cookies and storage under `UserDataFolder`. We don't need any of it for the LS HTML (which is stateless), but setting an explicit `UserDataFolder` under `%LOCALAPPDATA%\Snyk\WebView2` avoids it landing next to `devenv.exe`.
- **Modal `DialogWindow` + async init.** `HtmlSettingsWindow` is shown as a modal `DialogWindow`. The `Window_Loaded` handler is already `async void` and calls `LoadHtmlSettingsAsync`. Adding `await webView.EnsureCoreWebView2Async()` at the top of that chain is the obvious spot. Verify nothing races on `instance = this` being visible to `UpdateAuthToken` callers.
- **`WebView2` on STA/UI thread.** All WebView2 calls must happen on the UI thread. The existing code already uses `ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync()` in the right places — keep those.
- **`DebugHtmlSettingsWindow` startup path.** The `AutoOpenOnStartup` debug flow opens the window from `InitializeAsync`. WebView2 init at this very early VS startup time has, historically, taken several seconds on cold runtimes. The current comment ("DPI scaling is different during startup vs from the settings page") may already reflect this; WebView2 should not have that asymmetry, but spot-check.

## Manual QA checklist

- High-DPI: 125%, 150%, 200% scaling on both per-monitor and system DPI modes — settings page, description panel, summary panel.
- Multi-monitor: drag VS window between a high-DPI laptop screen and a 100% external monitor — content rescales without re-render artifacts.
- Theme: Dark, Light, Blue VS themes — CSS variables still resolve via `HtmlResourceLoader.ApplyTheme`.
- Settings dirty flow: OK enables only after a field changes; Cancel discards; OK persists and the LS receives `workspace/didChangeConfiguration`.
- Auth: trigger OAuth flow from settings, confirm `UpdateAuthToken` reaches the page.
- Description panel: click several issues, switch products (OSS, Code, IaC), invoke "Open in Editor" link, invoke "Generate AI fix", apply a fix diff, submit an ignore.
- Summary panel: cold-start scan summary loads; enabling/disabling delta findings via the tool window header works.
- Cold-start with no WebView2 Runtime installed (simulate by renaming the runtime folder) — confirm we show the friendly error, not a stack trace.

## Rollback

The migration is a self-contained branch. If WebView2 turns out to be unacceptable for any reason, revert the branch — the LS-side HTML and bridge contracts are unchanged, so there is no cross-repo coordination cost.

## Estimate

Rough sizing, assuming one engineer:

- Step 1 (deps): 0.5d
- Step 2 (`WebView2Host`): 1–1.5d
- Step 3 (settings): 1.5–2d (the trickiest, because of the singleton + auth-token push from LS)
- Step 4 (description panel): 1d
- Step 5 (summary panel): 0.5–1d
- Step 6 (prune): 0.25d
- Step 7 (QA + tests): 1–1.5d

Total: ~6–8 engineering days, plus a buffer for the high-DPI QA matrix.
