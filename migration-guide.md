# DataExchange Connector UI Migration Guide

This guide documents SDK upgrades for the **Sample UI Connector**. The most recent
migration is listed first; earlier migrations are preserved below for reference.

- [🔄 Migration Guide: SDK 7.6.0-beta Upgrade](#-migration-guide-sdk-760-beta-upgrade) — **latest**
- [🔄 Migration Guide: SDK 7.5.0 Upgrade](#-migration-guide-sdk-750-upgrade)
- [🔄 Migration Guide: SDK 7.2.1-beta Upgrade](#-migration-guide-sdk-721-beta-upgrade)

---

## 🔄 Migration Guide: SDK 7.6.0-beta Upgrade

This section documents the migration from SDK 7.5.0 to **Autodesk Data Exchange SDK 7.6.0-beta**.

### 📋 Overview of Changes

- **SDK Version**: Upgraded to `Autodesk.DataExchange 7.6.0-beta`
- **UI SDK Version**: Upgraded to `Autodesk.DataExchange.UI 7.6.0-beta`
- **Breaking Changes**: Yes — this is **not** a pure version bump. `IClient.GetElementDataModelAsync`
  now returns `IElementDataModel`, `ElementDataModel.Elements` yields `IElement`, and a handful of
  string-Id-based APIs were renamed/obsoleted in favor of `SourceId`/`UniqueId`-based ones.
- **Build result**: 0 errors after fixes applied (`msbuild SampleConnector.sln -p:Configuration=Debug -p:Platform=x64`)

### 🚀 Key Dependency Updates

| Package | Previous Version | New Version | Impact |
|---------|------------------|-------------|---------|
| `Autodesk.DataExchange` | `7.5.0-beta` | `7.6.0-beta` | **Minor** - breaking changes |
| `Autodesk.DataExchange.UI` | `7.5.0-beta` | `7.6.0-beta` | **Minor** - breaking changes |

### ⚠️ Breaking Changes

#### 1. `Client.GetElementDataModelAsync` now returns `IElementDataModel` instead of `ElementDataModel`

The concrete `Autodesk.DataExchange.DataModels.ElementDataModel` class still exists and still
implements `IElementDataModel`, so an explicit cast is sufficient — no data model changes required.

**Before (7.5.0):**
```csharp
this.currentElementDataModel = response.Value;
```

**After (7.6.0-beta):**
```csharp
this.currentElementDataModel = (ElementDataModel)response.Value;
```

**Migration Action:** Add an explicit cast to `ElementDataModel` wherever
`Client.GetElementDataModelAsync(...).Value` is assigned to an `ElementDataModel`-typed field or variable.

#### 2. `ElementDataModel.Elements` yields `IElement`, not `Element`

Iterating `elementDataModel.Elements` now produces `IElement` instances (even on the concrete
`ElementDataModel` class). Methods that receive a *retrieved* element (as opposed to one just
created via `AddElement`) need to accept `IElement` instead of `Element`.

**Before (7.5.0):**
```csharp
public static async Task AddUniqueStringParameter(Element element)
```

**After (7.6.0-beta):**
```csharp
public static async Task AddUniqueStringParameter(IElement element)
```

**Migration Action:** Change parameter types that receive elements read back from
`ElementDataModel.Elements` from `Element` to `IElement` — e.g.
`CreateExchangeHelper.AddUniqueStringParameter`/`AddStringParameter`.

#### 3. `IClient.RetrieveLatestExchangeDataAsync` replaced by `RetrieveLatestExchangeAsync`

The `(IElementDataModel, string, string, CancellationToken)` overload is obsolete in favor of a
`CancellationToken`-only overload; the old one allowed `fromRevision`/`toRevision` to be passed out
of order and corrupt the local cache.

**Before (7.5.0):**
```csharp
var deltaResponse = await this.Client.RetrieveLatestExchangeDataAsync(this.currentElementDataModel).ConfigureAwait(false);
```

**After (7.6.0-beta):**
```csharp
var deltaResponse = await this.Client.RetrieveLatestExchangeAsync(this.currentElementDataModel, cancellationToken).ConfigureAwait(false);
```

**Migration Action:** Replace `RetrieveLatestExchangeDataAsync(model)` with
`RetrieveLatestExchangeAsync(model, cancellationToken)`.

#### 4. `IElement.Id` and `ElementDataModel.DeleteElement(string)` obsoleted — use `SourceId`/`UniqueId`

`IElement.Id` was ambiguous (it returned the connector-supplied source identifier, not the SDK
identity), and `DeleteElement(string)` deleted by that same ambiguous, non-unique Id.

**Before (7.5.0):**
```csharp
elementDataModel.DeleteElement(existingElements[0].Id);
```

**After (7.6.0-beta):**
```csharp
elementDataModel.DeleteElementByUniqueId(existingElements[0].UniqueId);
```

**Migration Action:** Replace `element.Id` with `element.SourceId` (connector-authored id) or
`element.UniqueId` (SDK-generated unique id), and replace `DeleteElement(sourceId)` with
`DeleteElementsBySourceId(sourceId)` (deletes all matches) or `DeleteElementByUniqueId(uniqueId)`
(deletes one unambiguous element — used here since a single, specific element was being deleted).

### 📝 Newly-Obsolete APIs (not removed, not yet migrated in this sample)

SDK 7.6.0-beta also marks `ElementProperties`, `ElementDataModel.AddElement(ElementProperties)`,
and `ElementDataModel.SetElementGeometry(Element, List<ElementGeometry>)` as `[Obsolete]` in favor
of `AddElement(sourceId, name, transformation, lengthUnit, displayLengthUnit)` combined with
`Classify()`/`DefineType()`/`SetType()`, and the `IElement`/`List<IElementGeometry>` overload of
`SetElementGeometry`. These APIs still work — `CreateExchangeHelper.cs` builds this sample's
geometry using the old shapes and is wrapped in `#pragma warning disable/restore CS0618` to keep
building under this project's `WarningsAsErrors=CS0618` setting. Migrating the geometry-creation
helpers to the new `Classify`/`DefineType`/`SetType` model is tracked as follow-up work, not part of
this version bump.

### 🔧 Migration Steps

#### Step 1: Update Package References

Update the version numbers in `src/SampleConnector.csproj` and
`test/SampleConnectorUnitTests/SampleConnectorUnitTests.csproj`:

```xml
<PackageReference Include="Autodesk.DataExchange" Version="7.6.0-beta" />
<PackageReference Include="Autodesk.DataExchange.UI" Version="7.6.0-beta" />
```

#### Step 2: Apply the Code Fixes

1. **`CustomReadWriteModel.cs`** — cast `Client.GetElementDataModelAsync(...).Value` to
   `ElementDataModel` at both call sites; swap `RetrieveLatestExchangeDataAsync` for
   `RetrieveLatestExchangeAsync(model, cancellationToken)`; swap
   `DeleteElement(existingElements[0].Id)` for `DeleteElementByUniqueId(existingElements[0].UniqueId)`.
2. **`CreateExchangeHelper.cs`** — change `AddUniqueStringParameter`/`AddStringParameter` to accept
   `IElement` instead of `Element`; add `#pragma warning disable/restore CS0618` around the class
   since it still uses the now-obsolete `ElementProperties`/`AddElement(ElementProperties)`/
   `SetElementGeometry(Element, List<ElementGeometry>)` APIs.

#### Step 3: Restore and Rebuild

**Command Line:**
```bash
BuildSolution.bat
```

### 🎯 Summary of Changes

| Aspect | SDK 7.5.0 | SDK 7.6.0-beta |
|--------|-----------|-----------------|
| Element retrieval | `Client.GetElementDataModelAsync` returns `ElementDataModel` | Returns `IElementDataModel`; cast to use as `ElementDataModel` |
| `ElementDataModel.Elements` | Yields `Element` | Yields `IElement` |
| Delta sync | `RetrieveLatestExchangeDataAsync(model)` | `RetrieveLatestExchangeAsync(model, cancellationToken)` |
| Element identity | `IElement.Id` (ambiguous) | `IElement.SourceId` / `IElement.UniqueId` |
| Element deletion | `DeleteElement(sourceId)` | `DeleteElementsBySourceId(sourceId)` / `DeleteElementByUniqueId(uniqueId)` |
| Element/geometry creation | `ElementProperties` + `AddElement(ElementProperties)` + `SetElementGeometry(Element, List<ElementGeometry>)` | Obsolete but functional; new model is `AddElement(...)` + `Classify()`/`DefineType()`/`SetType()` + `SetElementGeometry(IElement, List<IElementGeometry>)` (not yet adopted in this sample) |

### 🧪 Testing Your Migration

After upgrading, confirm:

- ✅ `msbuild SampleConnector.sln -p:Configuration=Debug -p:Platform=x64` builds with 0 errors
- ✅ The MSTest unit test suite passes (`vstest.console.exe` against `SampleConnectorUnitTests.dll`)
- ✅ Create Exchange publishes successfully (all geometry types)
- ✅ Update Exchange adds a new revision without errors
- ✅ Downloaded exchanges preview correctly in the integrated 3D viewer

---

**Migration Checklist:**
- [x] Updated all package references to 7.6.0-beta
- [x] Cast `GetElementDataModelAsync(...).Value` to `ElementDataModel` where required
- [x] Changed `Element` → `IElement` for elements retrieved from `ElementDataModel.Elements`
- [x] Replaced `RetrieveLatestExchangeDataAsync` with `RetrieveLatestExchangeAsync`
- [x] Replaced `Id`/`DeleteElement` usage with `UniqueId`/`DeleteElementByUniqueId`
- [x] Restored NuGet packages and rebuilt the solution (0 errors)
- [x] Ran the MSTest unit test suite (4/4 passed)
- [ ] Tested create / update / download workflows end to end
- [ ] Migrated `CreateExchangeHelper.cs` off the now-obsolete `ElementProperties`/`AddElement(ElementProperties)`/`SetElementGeometry(Element, ...)` APIs

### 📚 Additional Resources

- [APS DataExchange SDK Documentation](https://aps.autodesk.com/en/docs/dx-sdk/v1/developers_guide/overview/)
- [APS DataExchange Release Notes](https://aps.autodesk.com/en/docs/dx-sdk/v1/developers_guide/release_notes/)
- [Autodesk Platform Services Developer Portal](https://aps.autodesk.com/)
- [DataExchange API Reference](https://aps.autodesk.com/en/docs/dx-sdk/v1/reference/)
- [Sample Code Repository](https://github.com/autodesk-platform-services/aps-dataexchange-connector)

---

## 🔄 Migration Guide: SDK 7.5.0 Upgrade

This section documents the migration from SDK 7.2.1-beta to **Autodesk Data Exchange SDK 7.5.0**.

### 📋 Overview of Changes

- **SDK Version**: Upgraded to `Autodesk.DataExchange 7.5.0-beta`
- **UI SDK Version**: Upgraded to `Autodesk.DataExchange.UI 7.5.0-beta`
- **Breaking Changes**: Yes — see below
- **Build result**: 0 errors after fixes applied

### 🚀 Key Dependency Updates

| Package | Previous Version | New Version | Impact |
|---------|------------------|-------------|---------|
| `Autodesk.DataExchange` | `7.2.1-beta` | `7.5.0-beta` | **Minor** - breaking changes |
| `Autodesk.DataExchange.UI` | `7.2.1-beta` | `7.5.0-beta` | **Minor** - breaking changes |

### ⚠️ Breaking Changes

#### 1. `Client.GenerateViewableAsync` removed — viewable generation is now server-side

In SDK 7.5.0 viewable generation is handled **server-side**. The client-side
`Client.GenerateViewableAsync` API was removed with **no replacement** — after
`SyncExchangeDataAsync` completes, the service generates the viewable automatically.

**Before (7.2.1):**

```csharp
await this.Client.SyncExchangeDataAsync(dataExchangeIdentifier, elementDataModel);

// Explicitly request viewable generation from the client.
await this.Client.GenerateViewableAsync(exchangeItem.ExchangeID, dataExchangeIdentifier.CollectionId);
```

**After (7.5.0):**

```csharp
await this.Client.SyncExchangeDataAsync(dataExchangeIdentifier, elementDataModel);

// Viewable generation is handled server-side in SDK 7.5.0; the client-side
// Client.GenerateViewableAsync API was removed (no replacement).
```

**Migration Action:** Remove all calls to `Client.GenerateViewableAsync`.

#### 2. `RenderStyle` and `RGBA` default constructors deprecated

The parameterless constructors with property setters are marked `[Obsolete]`. Use the
parameterized constructors instead.

**Before (7.2.1):**

```csharp
private RenderStyle commonRenderStyle = new RenderStyle()
{
    Name = "Common Render Style",
    RGBA = new RGBA() { Red = 255, Green = 0, Blue = 0, Alpha = 255 },
    Transparency = 1
};
```

**After (7.5.0):**

```csharp
private RenderStyle commonRenderStyle = new RenderStyle("Common Render Style", new RGBA(255, 0, 0, 255), 1);
```

**Migration Action:** Replace every `new RenderStyle() { ... }` / `new RGBA() { ... }`
with the parameterized form `new RenderStyle(name, rgba, transparency)` /
`new RGBA(red, green, blue, alpha)`.

#### 3. Complete authentication before launching the Connector UI

The `Client` constructor calls `Initialize()` internally, and the Connector UI
requests a token as soon as it connects. Create the `Client` **off the UI thread**
and finish authentication (`GetAuthTokenAsync`) **before** wiring up and launching
the `IInteropBridge`, so the UI does not race the auth flow.

**After (7.5.0):**

```csharp
// Create the client off the UI thread so OAuth does not block message handling.
await Task.Run(() =>
{
    this.client = new Client(this.sdkOptions);
}).ConfigureAwait(true);

// Finish authentication before the Connector UI connects and requests a token.
await this.sdkOptions.AuthProvider.GetAuthTokenAsync().ConfigureAwait(true);

// Now build the bridge and launch the UI.
var bridgeOptions = InteropBridgeOptions.FromClient(this.client);
// ...
```

**Migration Action:** Move client creation onto a background thread and call
`GetAuthTokenAsync()` before creating/launching the `IInteropBridge`.

### 🔧 Migration Steps

#### Step 1: Update Package References

Update the version numbers in `src/SampleConnector.csproj`. The `PackageReference`
format is also simplified — the `IncludeAssets`/`ExcludeAssets` overrides are no
longer required (NuGet resolves transitive dependencies automatically):

**Before:**

```xml
<ItemGroup>
  <PackageReference Include="Autodesk.DataExchange" Version="7.2.1-beta">
    <IncludeAssets>all</IncludeAssets>
    <ExcludeAssets>runtime; build; native; contentfiles; analyzers</ExcludeAssets>
  </PackageReference>
  <PackageReference Include="Autodesk.DataExchange.UI" Version="7.2.1-beta" />
</ItemGroup>
```

**After:**

```xml
<ItemGroup>
  <PackageReference Include="Autodesk.DataExchange" Version="7.5.0-beta" />
  <PackageReference Include="Autodesk.DataExchange.UI" Version="7.5.0-beta" />
</ItemGroup>
```

Apply the same version updates to `test/SampleConnectorUnitTests/SampleConnectorUnitTests.csproj`.

#### Step 2: Apply the Code Fixes

1. **`CustomReadWriteModel.cs`** — remove the `Client.GenerateViewableAsync` call after `SyncExchangeDataAsync`.
2. **`CreateExchangeHelper.cs`** — replace `new RenderStyle() { ... }` / `new RGBA() { ... }` with the parameterized constructors.
3. **`SampleHostWindow.xaml.cs`** — create the `Client` off the UI thread and call `GetAuthTokenAsync()` before launching the Connector UI.

#### Step 3: Restore and Rebuild

**Visual Studio:**
- Open `src/SampleConnector.sln`
- Rebuild the solution (packages restore automatically)

**Command Line:**

```bash
BuildSolution.bat
```

### 🎯 Summary of Changes

| Aspect | SDK 7.2.1 | SDK 7.5.0 |
|--------|-----------|-----------|
| Viewable generation | Client-side via `GenerateViewableAsync` | Server-side; API removed |
| `RenderStyle` / `RGBA` | Default constructor + setters | Parameterized constructors required |
| Auth flow | Token fetched lazily | Call `GetAuthTokenAsync()` before launching the UI |
| Client construction | On UI thread | Off the UI thread (`Task.Run`) |
| `PackageReference` | `IncludeAssets`/`ExcludeAssets` overrides | Simplified — no asset overrides |

### 🧪 Testing Your Migration

After upgrading, launch the sample and confirm:

- ✅ OAuth2 sign-in completes before the Connector UI appears
- ✅ Create Exchange publishes successfully (all geometry types)
- ✅ Update Exchange adds a new revision without errors
- ✅ Downloaded exchanges preview correctly in the integrated 3D viewer

---

**Migration Checklist:**
- [x] Updated all package references to 7.5.0
- [x] Removed `Client.GenerateViewableAsync` calls
- [x] Replaced `RenderStyle`/`RGBA` with parameterized constructors
- [x] Completed auth before launching the Connector UI
- [ ] Tested create / update / download workflows end to end

### 📚 Additional Resources

- [APS DataExchange SDK Documentation](https://aps.autodesk.com/en/docs/dx-sdk/v1/developers_guide/overview/)
- [APS DataExchange Release Notes](https://aps.autodesk.com/en/docs/dx-sdk/v1/developers_guide/release_notes/)
- [Autodesk Platform Services Developer Portal](https://aps.autodesk.com/)
- [DataExchange API Reference](https://aps.autodesk.com/en/docs/dx-sdk/v1/reference/)
- [Sample Code Repository](https://github.com/autodesk-platform-services/aps-dataexchange-connector)

---

## 🔄 Migration Guide: SDK 7.2.1-beta Upgrade

This section documents the migration from SDK 7.2.0 to **Autodesk Data Exchange SDK 7.2.1-beta**.

### 📋 Overview of Changes

This patch upgrade removes the Description field from the Create Exchange form.

- **SDK Version**: Upgraded to `Autodesk.DataExchange 7.2.1-beta`
- **SDK Version**: Upgraded to `Autodesk.DataExchange.UI 7.2.1-beta`
- **Bug Fixes**: Description field removed from the Create Exchange form

### 🚀 Key Dependency Updates

| Package | Previous Version | New Version | Impact |
|---------|------------------|-------------|---------|
| `Autodesk.DataExchange` | `7.2.0` | `7.2.1-beta` | **Patch** - Bug fixes |
| `Autodesk.DataExchange.UI` | `7.2.0` | `7.2.1-beta` | **Patch** - Bug fixes |

### ⚠️ Breaking Changes

#### 1. Removal of Description field from the Create Exchange form

The `Description` field has been removed from the Create Exchange form UI.

**Before (SDK 7.2.0):**

```tsx
<FormTextField
  id="create-exchange-description"
  multiline
  minRows={1}
  maxRows={3}
  title={t("DESCRIPTION")}
  required={false}
  variant="outlined"
  placeholder={t("ADD_DESCRIPTION")}
  value={description}
  onChange={(e) => setDescription(e.target.value)}
/>
```

**After (SDK 7.2.1-beta):**

```tsx
// This component is no longer available and usage should be deleted.
//
// <FormTextField
//   id="create-exchange-description"
//   multiline
//   minRows={1}
//   maxRows={3}
//   title={t("DESCRIPTION")}
//   required={false}
//   variant="outlined"
//   placeholder={t("ADD_DESCRIPTION")}
//   value={description}
//   onChange={(e) => setDescription(e.target.value)}
// />
```

**Migration Action:** No changes required

### 🔧 Migration Steps

#### Step 1: Update Package References

Update the version numbers in your .csproj file:

**Before:**

```xml
<ItemGroup>
  <PackageReference Include="Autodesk.DataExchange" Version="7.2.0">
    <IncludeAssets>all</IncludeAssets>
    <ExcludeAssets>runtime; build; native; contentfiles; analyzers</ExcludeAssets>
  </PackageReference>
  <PackageReference Include="Autodesk.DataExchange.UI" Version="7.2.0" />
</ItemGroup>
```

**After:**

```xml
<ItemGroup>
  <PackageReference Include="Autodesk.DataExchange" Version="7.2.1-beta">
    <IncludeAssets>all</IncludeAssets>
    <ExcludeAssets>runtime; build; native; contentfiles; analyzers</ExcludeAssets>
  </PackageReference>
  <PackageReference Include="Autodesk.DataExchange.UI" Version="7.2.1-beta" />
</ItemGroup>
```

### 📚 Additional Resources

- [APS DataExchange SDK Documentation](https://aps.autodesk.com/en/docs/dx-sdk/v1/developers_guide/overview/)
- [APS DataExchange Release Notes](https://aps.autodesk.com/en/docs/dx-sdk/v1/developers_guide/release_notes/)
- [Autodesk Platform Services Developer Portal](https://aps.autodesk.com/)
- [DataExchange API Reference](https://aps.autodesk.com/en/docs/dx-sdk/v1/reference/)
- [Sample Code Repository](https://github.com/autodesk-platform-services/aps-dataexchange-connector)

For complex migration scenarios or specific technical questions, consult the official release notes and consider reaching out to Autodesk support channels.

---

*This migration guide provides guidance for the transition from version 7.2.0 to 7.2.1-beta. Always refer to the official documentation and release notes for the most accurate and up-to-date information.*
