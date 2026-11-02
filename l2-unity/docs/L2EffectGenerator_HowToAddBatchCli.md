# How to add a batch effect generation entry (L2EffectGenerator)

Read this before adding another `Unity -executeMethod` batch for Lineage 2 effects.

This is **not** the armor importer. Armor meshes and textures are `docs/convert-armor/README.md`.

Working example to copy: `L2EffectGeneratorWindow.GenerateCubicsCli` in `Assets/Editor/L2EffectGenerator/UI/L2EffectGeneratorWindow.cs`. It generated 15 cubic classes on Unity **6000.0.68f1** and exited 0.

Human button vs batch must be two methods. The batch method calls `EditorApplication.Exit`. A menu item that calls the batch method closes the editor. Copy the deco split: `GenerateNpcDecoCli` exits, `GenerateNpcDecoBuffMenu` only shows a dialog.

---

## 1. Put the `.uc` where the generator looks

One Unreal class, one folder, file name = class name:

```
Assets/Resources/Data/Effects/<class>/<class>.uc
```

Example: `Assets/Resources/Data/Effects/s_u013_a/s_u013_a.uc` with `class s_u013_a extends NCubics;`.

Source scripts for this client:

```
C:\Users\hh-soft\Pictures\test_umodel\unreal script\lineageeffect_interlude_оригинала\lineageeffect\Classes\<class>.uc
```

The generator only scans the chosen folder and **one** subfolder level (`L2EffectGeneratorFolderBuilder.DiscoverUcAssetPaths`). A `.uc` buried deeper is ignored. A second copy under a skill folder (`vampiric/m_u003_c.uc`) is a different effect root. Do not point the batch at that skill folder when the class needs its own prefab.

After generation the runtime files are:

```
Assets/Resources/Data/Effects/<class>/<class>/<class>.prefab
Assets/Resources/Data/Effects/<class>/<class>_composite.prefab
```

`CubicService` loads the body from `Data/Effects/<class>/<class>/<class>` and a cast burst from `Data/Effects/<class>/<class>_composite`.

---

## 2. Add the editor entry

In `L2EffectGeneratorWindow`:

1. `AssetDatabase.Refresh()` first. Without it, a folder copied on disk is not `AssetDatabase.IsValidFolder` yet.
2. For each class call:

```csharp
L2EffectGeneratorFolderBuilder.CreateFromFolder(
    "Assets/Resources/Data/Effects/" + effectName,
    effectName,
    0,      // skill visual id. 0 = auto-detect from the launch table
    false); // do not write GlobalEffect / skill bind
```

3. `false` is required for cubics, enter events, and any emitter that is not a skill launch. `true` binds the composite into the global skill table.
4. If `Success` is false and `SkillIdCandidates` is not empty, call again with `SkillIdCandidates[0]` and bind still `false`. `m_u000_c` and `m_u003_c` are ambiguous and still generate this way.
5. Log every `result.LogLines` entry with the `[L2EffectGenerator]` prefix, and `Debug.LogError` on `result.ErrorMessage`.
6. Missing folder: log, set `ok = false`, continue with the other classes.
7. Only the `*Cli` method calls `EditorApplication.Exit(ok ? 0 : 1)`.

Menu item, if a person will click it:

```csharp
[MenuItem("Tools/L2 Effects/Generate Cubics")]
public static void GenerateCubicsMenu()
{
    bool ok = GenerateCubics(); // same loop, no Exit
    EditorUtility.DisplayDialog("L2 Effect Generator",
        ok ? "Cubics generated." : "Failed. See Console.", "OK");
}
```

NPC deco is the other pipeline (`CreateDecoFromFolder`): one emitter, no composite, no skill id. Use it only for deco. Skill and cubic visuals use `CreateFromFolder`.

---

## 3. Run Unity from this machine

Editor:

```
C:\Program Files\Unity\Hub\Editor\6000.0.68f1\Editor\Unity.exe
```

Project:

```
c:\unity\l2 client\l2-git-interlude\Unity-Client-for-L2J\l2-unity
```

Command that actually waits and returns Unity's exit code (this is the one that finished the cubic batch):

```bat
cmd /c start "unity-batch" /wait "C:\Program Files\Unity\Hub\Editor\6000.0.68f1\Editor\Unity.exe" -batchmode -nographics -quit -projectPath "c:\unity\l2 client\l2-git-interlude\Unity-Client-for-L2J\l2-unity" -executeMethod L2EffectGeneratorWindow.GenerateCubicsCli -logFile "c:\unity\l2 client\l2-git-interlude\Unity-Client-for-L2J\l2-unity\Logs\cubic-effect-generator.log"
```

Replace `GenerateCubicsCli` and the log file name for a new batch.

Do not launch a second Unity while one is open on this project.

These launches fail here:

- PowerShell `& Unity.exe` returns immediately. Unity is a Windows GUI binary; the shell does not wait.
- `Start-Process -Wait` hung with no `Unity.exe` child. Do not use it.
- If a batch dies, `Temp\UnityLockfile` can stay behind and the next start never writes a log. Delete that file only when `Get-Process Unity` is empty, then launch again.

`-executeMethod` runs only after script compile. `error CS` in the log means the method never ran. Fix the compile, clear the lock, run again.

`using System` plus `UnityEngine` makes `Object` ambiguous. Write `UnityEngine.Object.Destroy` / `Instantiate`, or the batch dies at compile with CS0104.

---

## 4. How to tell it worked

In the log, each class has:

```
[L2EffectGenerator] <class> Composite: composite created -> Assets/Resources/Data/Effects/<class>/<class>_composite.prefab
[L2EffectGenerator] <class> Skill bind: skipped
```

Process exit code is 0. `Exiting batchmode` with return code 1 means at least one class failed; read the `[L2EffectGenerator]` error above it. A missing mesh or texture line is still a failed class even if later classes succeed.

Confirm the prefab exists:

```
Assets/Resources/Data/Effects/<class>/<class>_composite.prefab
Assets/Resources/Data/Effects/<class>/<class>/<class>.prefab
```
