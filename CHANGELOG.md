# Changelog
## [4.0.0] - 2026-10-09
### Migration from 3.x
- Navigation requests now run in order; void APIs enqueue work. Do not await another queued manager operation inside lifecycle callbacks or animation overrides.
- Existing panels are reused and moved to the top instead of creating duplicate navigation entries. Already shown panels do not replay entrance events or animations.
- New instances stay inactive until onInit finishes; initialize onInit dependencies in serialized fields or field initializers instead of Awake.
- Built-in animations use unscaled time by default and propagate cancellation. Custom animations must honor cancellation before modifying targets.
- Manual input locks and scoped locks use separate counters; manual unlocks cannot release framework-owned scopes. UI mutations and scope disposal require Unity's main thread.

### Changes
- Make panel transitions safe when activation or cancellation callbacks start newer transitions; restore committed visuals and cancel unfinished sibling animations on failure.
- Release framework input scopes even when custom animations ignore cancellation; isolate manual locks and block keyboard/controller submission through the root CanvasGroup.
- Track requested panel types and owned GameObjects separately, including derived prefabs, component-only destruction, and destruction during initialization or activation.
- Restore sorting after unmanaged show failures and continue cleanup when cancellation callbacks throw.
- Enforce main-thread mutations, reject non-finite durations/settings, and keep the canvas inside custom camera clipping planes.
- Serialize navigation and preloading; reuse one instance per type without duplicate navigation entries.
- Release input locks on failed or cancelled loads, callbacks, and animations; restore recoverable panel state.
- Await all CloseUntil exit animations, preserve navigation on close failures, and fix replacement sorting and same-panel switches.
- Add optional cancellable and releasable loader interfaces; release unused preloads and late load results after cancellation.
- Use Resources.LoadAsync by default; animate with unscaled time and propagate cancellation without snapping to end values.
- Add explicit settings initialization, shutdown/disposal, optional DestroyOnClose, and popup animation recovery.
- Handle ClearAll cancellation reentrancy, external destruction, camera registration before initialization, and EventSystem ownership.
- Replace linear panel lookup and repeated editor prefab scanning; remove per-frame easing delegate creation.
- Add Unity PlayMode regression tests and document navigation, callbacks, resource ownership, and changed activation timing.

## [3.7.0] - 2026-05-18
- Make UIManager methods overridable: GetUI is now virtual, NewUIAsync and NewUI are now protected virtual to allow subclassing/extension without changing existing behavior.
## [3.6.9] - 2026-01-23
- Fix UIManager.ClearAll to avoid modifying collection during iteration
## [3.6.8] - 2026-01-14
- Remove URP UI camera stack management, use UIAdditionalCamera component instead
## [3.6.6] - 2026-01-14
- ClearAll DestroyImmediate => Destroy
## [3.6.4] - 2026-01-13
- Improve URP UI camera stack management
## [3.6.2] - 2025-12-22
- Introduces GetLockInputScope() to provide an IDisposable-based input lock mechanism. Refactors LockInputWhile to use this scope for safer and more robust input locking during asynchronous tasks.
## [3.6.1] - 2025-10-28
- Add Close and CloseAsync methods to UIManager
## [3.6.0] - 2025-08-25
- The WaitUntilUIHideEnd method now accepts an optional CancellationToken, allowing callers to cancel the wait operation if needed.
## [3.5.6] - 2025-08-01
- Fix sort order calculation in UIManager for better layer management
## [3.5.5] - 2025-06-23
- refactor sorting layer management and improve UI editor script handling
## [3.3.4] - 2025-05-29
- fix: UI panel visibility and initialization sequence
## [3.3.3] - 2025-05-29
- feat: add support for multiple back buttons in PopUIPanel
## [3.3.1] - 2025-05-27
- refactor: UIManager interface and methods to support async loading options
- change: `IUIManager.CurrentAbstractUIPanel` to `IUIManager.CurrentUIPanel`
- add: `IUIManager.UICanvas`
## [3.2.2] - 2025-05-21
- refactor: SafeArea calculations for improved accuracy
## [3.2.1] - 2025-05-09
- refactor: rename showFormStack and uiFormsList to showStack and panelList for clarity
## [3.2.0] - 2025-05-07
- feat: Add SafeDestroy method to UIManager for safe panel destruction
## [3.1.0] - 2025-04-29
- feat: `PanelAsyncLoading`
## [3.0.6] - 2025-03-27
- fix: `ShowThenClosePrevAsync` show panel has same order in layer with prev panel bug
- change: `AbstractUIPanel.SortOrder` to `AbstractUIPanel.OrderInLayer`
## [3.0.3] - 2025-02-19
- fix: urp bugs while only use uiCamera
## [3.0.2] - 2025-02-17
- add: URP support
## [2.6.2] - 2025-02-11
- add: `IUIManager.LockInputWhile(UniTask task)`
## [2.6.1] - 2024-12-25
- fix: `IUIManager.ClearAll` bug
## [2.6.0] - 2024-12-16
- fix: `IUIManager.GetUI` bug
## [2.5.6] - 2024-12-12
- remove: `SubPanel`, please use `IUIManager.ShowUIUnmanagableAsync`
## [2.5.5] - 2024-12-11
- fix: `GetCustomLayerCanvas` bugs
## [2.5.2] - 2024-12-04
- fix: `IUIManager.ClearAll` bug
## [2.5.0] - 2024-11-25
- mod: `Samples`
## [2.4.4] - 2024-11-22
- add `README.md`
## [2.4.3] - 2024-11-19
- fix: 'CloseCurrentAsync' bug
- add: `IUIManager.IsLockInput`
## [2.3.1] - 2024-11-04
- add `UISceneView`
## [2.2.2] - 2024-10-25
- add: IPanelLoader required for `UIManager.Init`, null will use `Resources.Load`
- remove: `LoadType`
## [2.1.2] - 2024-10-24
- mod: `LoadType`
## [2.1.1] - 2024-10-23
- remove: `AbstractUIPanel.OnAnimationInEnd`, `AbstractUIPanel.OnAnimationOutEnd`
## [2.1.0] - 2024-10-21
- add: auto create sorting layers
- remove: `UISettings.sortingLayer`. Canvas default `sortingLayer` will always be `UI`
## [2.0.3] - 2024-10-18
- add: `IUIManager.ShowThenClosePrevAsync`, `IUIManager.ShowThenClosePrev`
## [2.0.2] - 2024-10-18
- add: `IUIManager.ShowThenHidePrevAsync`, `IUIManager.ShowThenHidePrev`
## [2.0.1] - 2024-10-18
- mod: `IUIManager.GetCustomLayer` to `IUIManager.GetCustomLayerCanvas`, `AbstractUIPanel.SetSortOrder` to `AbstractUIPanel.SetOrderInLayer`
- add: `AbstractUIPanel.SetSortingLayer`
## [1.8.2] - 2024-10-17
-mod: improve `PanelCreator` Editor
## [1.8.0] - 2024-10-17
-add: `IUIManager.PreloadUIAsync`, `IUIManager.PreloadUI`
## [1.7.1] - 2024-10-17
- mod: `PanelCreator` types sort by name
## [1.7.0] - 2024-10-14
- mod: 'IUIManager' events to Non-Static
- mod: panel's UI property to 'IUIManager'
## [1.6.6] - 2024-10-14
- fix: `PanelCreator` panel can't show all items
## [1.6.5] - 2024-10-08
- fix: `PanelCreator` open Panel bug
## [1.6.3] - 2024-10-08
- mod: PanelCreator Editor Save Key
- add: `UISettings.depth` `UISettings.backgroundColor`
## [1.6.2] - 2024-08-30
- add: `IUIManager.WaitUntilUIHideEnd<T>`
## [1.6.1] - 2024-08-09
- fix: `UIManager.LockInput` bug
- remove `SimpleTask`
## [1.5.2] - 2024-08-08
- provide `IUIManager` interface
## [1.4.5] - 2024-07-29
- use UniTask
## [1.4.3] - 2024-07-19
- `CloseAsync` add `bool` `destroy` param
## [1.4.1] - 2024-07-15
- fix: `PopUIPanel` `cgBlocker` to protected
## [1.4.0] - 2024-07-01
- fix blocker bug
## [1.3.6] - 2024-06-17
- `SafeArea` auto update
## [1.3.3] - 2024-05-15
- add: `SafeArea` support
## [1.2.3] - 2024-05-14
- add: full `UISettings.matchWidthOrHeight`
## [1.2.2] - 2024-05-08
- add: `UISettings.matchWidthOrHeight`
## [1.2.1] - 2024-04-29
- mod: `UIManager` require self init before use
## [1.2.0] - 2024-03-20
- fix: `UIManager.CloseAsync` bug
## [1.1.9] - 2024-03-14
- add: `UIManager.CustomLayer` auto set `GameObject.layer`
## [1.1.7] - 2024-02-20
- rename: LICENSE.md
## [1.1.6] - 2024-02-19
- fix: bugs
## [1.1.5] - 2024-02-10
- support: `Addressables`
## [1.1.4] - 2024-02-06
- remove: auto create `UISettings` asset
## [1.0.9] - 2024-02-06
- support: YooAsset
## [1.0.8] - 2024-02-05
- add: `IPanelLoader` create log
## [1.0.6] - 2024-02-01
- mod: Editor config store case
## [1.0.5] - 2024-01-29
- add: Settings `nearClipPlane` `farClipPlane`
## [1.0.4] - 2024-01-22
- add: events `OnShow` `OnHide` `OnShowEnd` `OnHideEnd`
## [1.0.3] - 2024-01-20
- add: UICam Setting `ClearFlags`
## [1.0.2] - 2024-01-19
- mod: `CustomLayer`
- add: `SetCustomLayerSortingLayer()`
## [1.0.1] - 2024-01-19
- add: `CustomLayer`
- mod: `CurrentAbstractUIPanel` to Public
## [1.0.0] - 2024-01-15
- add: `HideCurrentAsync` `HideCurrent`
## [0.1.7] - 2024-01-12
- fix: Fix `PopPanel` bug
## [0.1.6] - 2024-01-09
- Remove `SimpleTask` Log
## [0.1.5] - 2024-01-09
- fix: Fix `SimpleTask` bug
## [0.1.4] - 2024-01-09
- add: `PlayerLoopHelper` `SimpleTask`
## [0.1.3] - 2024-01-09
- fix: `UIManager` `ClearAll` bug
## [0.1.2] - 2024-01-08
- Replace `Task.Yield()` by `Coroutine`
## [0.1.1] - 2024-01-08
- Add Samples~
## [0.1.0] - 2024-01-08
- Catch errors
- Change `UIManagerConfig` name into `UISettings`
- Change IPanelLoader `LoadPrefab` return value into `Task<GameObject>`
- Change Project folder Structure
- Fix `IPanelLoader` bug
- Add `AnimEase` enum
- Update `UIManagerConfig` Inspector
- Remove `UniTask`
## [0.0.1] - 2024-01-04
### This is the first release of *com.rickit.ui*.
- first commit