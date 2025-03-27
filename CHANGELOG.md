# Changelog
## [3.0.4] - 2025-03-27
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