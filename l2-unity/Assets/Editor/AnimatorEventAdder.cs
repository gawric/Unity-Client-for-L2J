using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using System; 
public class AnimatorEventAdder : EditorWindow
{
    private static List<AnimatorController> selectedControllers = new List<AnimatorController>();
    private static List<AnimatorOverrideController> selectedOverrideControllers = new List<AnimatorOverrideController>();
    private static List<string> targetStateNames = new List<string>();
    private static bool applyToAllStates = false;
    private Vector2 scrollPosition;
    private static string triggerFunctionName = "OnAnimationComplete"; 

    [MenuItem("Tools/Animation Event Manager")]
    public static void ShowWindow()
    {
        GetWindow<AnimatorEventAdder>("Animation Event Manager");
    }

    private void OnGUI()
    {
        GUILayout.Label("Animation Event Settings", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // Main scroll view
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        // Controller selection section
        GUILayout.Label("Controllers", EditorStyles.boldLabel);

        // Regular Animator Controllers
        EditorGUILayout.LabelField("Animator Controllers:");
        for (int i = 0; i < selectedControllers.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            selectedControllers[i] = (AnimatorController)EditorGUILayout.ObjectField(
                selectedControllers[i], typeof(AnimatorController), false);
            if (GUILayout.Button("X", GUILayout.Width(20)))
            {
                selectedControllers.RemoveAt(i);
                break;
            }
            EditorGUILayout.EndHorizontal();
        }

        // Button to add animator controller
        if (GUILayout.Button("Add Selected Animator Controller"))
        {
            AddSelectedAnimatorController();
        }

        EditorGUILayout.Space();

        // Override Controllers
        EditorGUILayout.LabelField("Override Controllers:");
        for (int i = 0; i < selectedOverrideControllers.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            selectedOverrideControllers[i] = (AnimatorOverrideController)EditorGUILayout.ObjectField(
                selectedOverrideControllers[i], typeof(AnimatorOverrideController), false);
            if (GUILayout.Button("X", GUILayout.Width(20)))
            {
                selectedOverrideControllers.RemoveAt(i);
                break;
            }
            EditorGUILayout.EndHorizontal();
        }

        // Button to add override controller
        if (GUILayout.Button("Add Selected Override Controller"))
        {
            AddSelectedOverrideController();
        }

        EditorGUILayout.Space();

        // Trigger settings section
        GUILayout.Label("Trigger Settings", EditorStyles.boldLabel);

        triggerFunctionName = EditorGUILayout.TextField("Trigger Function Name:", triggerFunctionName);

        EditorGUILayout.Space();

        // State names section
        GUILayout.Label("Target States", EditorStyles.boldLabel);

        applyToAllStates = EditorGUILayout.Toggle("Apply to All States", applyToAllStates);

        if (!applyToAllStates)
        {
            EditorGUILayout.Space();
            GUILayout.Label("Target State Names (one per line):");

            // Display current list
            for (int i = 0; i < targetStateNames.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                targetStateNames[i] = EditorGUILayout.TextField(targetStateNames[i]);
                if (GUILayout.Button("X", GUILayout.Width(20)))
                {
                    targetStateNames.RemoveAt(i);
                    break;
                }
                EditorGUILayout.EndHorizontal();
            }

            // Add new field
            EditorGUILayout.BeginHorizontal();
            string newState = EditorGUILayout.TextField("", GUILayout.ExpandWidth(true));
            if (!string.IsNullOrEmpty(newState) && !targetStateNames.Contains(newState))
            {
                targetStateNames.Add(newState);
            }
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.Space();

        // Action buttons
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Add Events to Selected Controllers", GUILayout.ExpandWidth(true)))
        {
            ProcessSelectedControllers();
        }
        if (GUILayout.Button("Remove Events from Selected Controllers", GUILayout.ExpandWidth(true)))
        {
            RemoveEventsFromSelectedControllers();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndScrollView();
    }

    private static void RemoveEventsFromSelectedControllers()
    {
        int removedCount = 0;

        try
        {
            // Process regular controllers
            foreach (var controller in selectedControllers)
            {
                if (controller != null)
                {
                    Debug.Log($"Removing events from controller: {controller.name}");
                    removedCount += RemoveEventsFromAnimatorController(controller);
                }
            }

            // Process override controllers
            foreach (var overrideController in selectedOverrideControllers)
            {
                if (overrideController != null)
                {
                    Debug.Log($"Removing events from override controller: {overrideController.name}");
                    removedCount += RemoveEventsFromOverrideController(overrideController);
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Finished removing animation events from {removedCount} clips");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error removing events from controllers: {e.Message}");
        }
    }

    private static int RemoveEventsFromOverrideController(AnimatorOverrideController overrideController)
    {
        int removedCount = 0;

        try
        {
            Debug.Log($"Removing events from override controller: {overrideController.name}");

            // Get the runtime animator controller
            var runtimeController = overrideController.runtimeAnimatorController as AnimatorController;
            if (runtimeController == null)
            {
                Debug.LogWarning($"No runtime controller found for {overrideController.name}");
                return 0;
            }

            // Get all animation clips from the runtime controller
            var allClips = new List<AnimationClip>();
            foreach (var layer in runtimeController.layers)
            {
                GetAllAnimationClips(layer.stateMachine, allClips);
            }

            // Get the override controller's overrides
            var overrideClips = new Dictionary<AnimationClip, AnimationClip>();

            try
            {
                List<KeyValuePair<AnimationClip, AnimationClip>> overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>();
                overrideController.GetOverrides(overrides);

                foreach (var pair in overrides)
                {
                    AnimationClip originalClip = pair.Key;
                    AnimationClip newClip = pair.Value;

                    if (originalClip != null && newClip != null)
                    {
                        overrideClips[originalClip] = newClip;
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error getting overrides: {e.Message}");
                return 0;
            }

            // Remove events from each state in the runtime controller
            foreach (var layer in runtimeController.layers)
            {
                removedCount += RemoveEventsFromStateMachine(layer.stateMachine, overrideClips);
            }

            Debug.Log($"Finished removing events from override controller {overrideController.name}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error removing events from override controller {overrideController.name}: {e.Message}");
        }

        return removedCount;
    }

    private static int RemoveEventsFromStateMachine(AnimatorStateMachine stateMachine)
    {
        int removedCount = 0;

        foreach (var state in stateMachine.states)
        {
            if (state.state.motion is AnimationClip clip)
            {
                if (ShouldProcessState(state.state.name))
                {
                    int removed = RemoveAnimationEvents(clip, state.state.name);
                    if (removed > 0)
                    {
                        Debug.Log($"Removed {removed} event(s) from {state.state.name} in {clip.name}");
                        removedCount += removed;
                    }
                }
            }
            else if (state.state.motion is BlendTree blendTree)
            {
                removedCount += RemoveEventsFromBlendTree(blendTree);
            }

            foreach (var subMachine in stateMachine.stateMachines)
            {
                removedCount += RemoveEventsFromStateMachine(subMachine.stateMachine);
            }
        }

        return removedCount;
    }

    private static int RemoveEventsFromStateMachine(AnimatorStateMachine stateMachine, Dictionary<AnimationClip, AnimationClip> overrideClips)
    {
        int removedCount = 0;

        foreach (var state in stateMachine.states)
        {
            if (state.state.motion is AnimationClip clip)
            {
                // Check if this clip has an override
                if (overrideClips.TryGetValue(clip, out AnimationClip overrideClip))
                {
                    int removed = RemoveAnimationEvents(overrideClip, state.state.name);
                    if (removed > 0)
                    {
                        Debug.Log($"Removed {removed} event(s) from {state.state.name} in {overrideClip.name}");
                        removedCount += removed;
                    }
                }
            }
            else if (state.state.motion is BlendTree blendTree)
            {
                removedCount += RemoveEventsFromBlendTree(blendTree, overrideClips);
            }

            foreach (var subMachine in stateMachine.stateMachines)
            {
                removedCount += RemoveEventsFromStateMachine(subMachine.stateMachine, overrideClips);
            }
        }

        return removedCount;
    }


    private static int RemoveEventsFromBlendTree(BlendTree blendTree)
    {
        int removedCount = 0;

        foreach (var child in blendTree.children)
        {
            if (child.motion is AnimationClip clip)
            {
                if (ShouldProcessState(blendTree.name))
                {
                    int removed = RemoveAnimationEvents(clip, blendTree.name);
                    if (removed > 0)
                    {
                        removedCount += removed;
                    }
                }
            }
            else if (child.motion is BlendTree subTree)
            {
                removedCount += RemoveEventsFromBlendTree(subTree);
            }
        }

        return removedCount;
    }

    private static int RemoveEventsFromBlendTree(BlendTree blendTree, Dictionary<AnimationClip, AnimationClip> overrideClips)
    {
        int removedCount = 0;

        foreach (var child in blendTree.children)
        {
            if (child.motion is AnimationClip clip)
            {
                // Check if this clip has an override
                if (overrideClips.TryGetValue(clip, out AnimationClip overrideClip))
                {
                    int removed = RemoveAnimationEvents(overrideClip, blendTree.name);
                    if (removed > 0)
                    {
                        removedCount += removed;
                    }
                }
            }
            else if (child.motion is BlendTree subTree)
            {
                removedCount += RemoveEventsFromBlendTree(subTree, overrideClips);
            }
        }

        return removedCount;
    }

    private static int RemoveAnimationEvents(AnimationClip clip, string eventName)
    {
        if (clip == null)
        {
            Debug.LogWarning("Attempted to remove events from null clip");
            return 0;
        }

        Debug.Log($"Removing events from clip: {clip.name}, state: {eventName}");

        // Get existing events
        AnimationEvent[] events = AnimationUtility.GetAnimationEvents(clip);
        List<AnimationEvent> filteredEvents = new List<AnimationEvent>();
        int removedCount = 0;

        foreach (var existingEvent in events)
        {
            if (existingEvent.functionName == triggerFunctionName)
            {
                // Skip this event (it will be removed)
                removedCount++;
                Debug.Log($"Removed event: {existingEvent.functionName}({existingEvent.stringParameter}) at time {existingEvent.time}");
            }
            else
            {
                // Keep this event
                filteredEvents.Add(existingEvent);
            }
        }

        if (removedCount > 0)
        {
            // Set the filtered events back
            AnimationUtility.SetAnimationEvents(clip, filteredEvents.ToArray());

            // Verify the events were removed
            AnimationEvent[] finalEvents = AnimationUtility.GetAnimationEvents(clip);
            Debug.Log($"Clip {clip.name} now has {finalEvents.Length} events (removed {removedCount})");

            // Mark the clip as dirty to save changes
            EditorUtility.SetDirty(clip);
            Debug.Log($"Marked clip {clip.name} as dirty");
        }
        else
        {
            Debug.Log($"No matching events found to remove from {clip.name}");
        }

        return removedCount;
    }




    private static int RemoveEventsFromAnimatorController(AnimatorController controller)
    {
        int removedCount = 0;

        foreach (var layer in controller.layers)
        {
            removedCount += RemoveEventsFromStateMachine(layer.stateMachine);
        }

        return removedCount;
    }


    private void AddSelectedAnimatorController()
    {
        var selectedObject = Selection.activeObject;
        if (selectedObject != null)
        {
            if (selectedObject is AnimatorController)
            {
                var controller = selectedObject as AnimatorController;
                if (!selectedControllers.Contains(controller))
                {
                    selectedControllers.Add(controller);
                    Debug.Log($"Added Animator Controller: {controller.name}");
                }
                else
                {
                    Debug.LogWarning($"Controller {controller.name} is already in the list");
                }
            }
            else
            {
                Debug.LogWarning("Selected object is not an Animator Controller");
            }
        }
        else
        {
            Debug.LogWarning("No object selected in the Project window");
        }
    }

    private void AddSelectedOverrideController()
    {
        var selectedObject = Selection.activeObject;
        if (selectedObject != null)
        {
            if (selectedObject is AnimatorOverrideController)
            {
                var controller = selectedObject as AnimatorOverrideController;
                if (!selectedOverrideControllers.Contains(controller))
                {
                    selectedOverrideControllers.Add(controller);
                    Debug.Log($"Added Override Controller: {controller.name}");
                }
                else
                {
                    Debug.LogWarning($"Controller {controller.name} is already in the list");
                }
            }
            else
            {
                Debug.LogWarning("Selected object is not an Animator Override Controller");
            }
        }
        else
        {
            Debug.LogWarning("No object selected in the Project window");
        }
    }


    private static void ProcessSelectedControllers()
    {
        int processedCount = 0;

        try
        {
            if (targetStateNames.Count > 0)
            {
                foreach (string selectedStateName in targetStateNames)
                {
                    foreach (var controller in selectedControllers)
                    {
                        if (controller != null)
                        {
                            Debug.Log($"Processing controller: {controller.name}");
                            processedCount += ProcessAnimatorController(controller);
                        }
                    }

                    // Process override controllers
                    foreach (var overrideController in selectedOverrideControllers)
                    {
                        if (overrideController != null)
                        {
                            Debug.Log($"Processing override controller: {overrideController.name}");
                            processedCount += ProcessOverrideController(overrideController, selectedStateName);
                        }
                    }
                }

            }



            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Finished adding animation events to {processedCount} states");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error processing controllers: {e.Message}");
        }
    }

    private static int ProcessAnimatorController(AnimatorController controller)
    {
        int processedCount = 0;

        foreach (var layer in controller.layers)
        {
            processedCount += ProcessStateMachine(layer.stateMachine);
        }

        return processedCount;
    }



    private static int ProcessOverrideController(AnimatorOverrideController overrideController , string selectedStateName)
    {
        int processedCount = 0;

        try
        {
            Debug.Log($"Processing override controller: {overrideController.name}");
            Debug.Log($"Controller asset path: {AssetDatabase.GetAssetPath(overrideController)}");

            // Get the runtime animator controller
            var runtimeController = overrideController.runtimeAnimatorController as AnimatorController;
            if (runtimeController == null)
            {
                Debug.LogWarning($"No runtime controller found for {overrideController.name}");
                return 0;
            }

            // Get all animation clips from the runtime controller
            var allClips = new List<AnimationClip>();
            foreach (var layer in runtimeController.layers)
            {
                GetAllAnimationClips(layer.stateMachine, allClips);
            }



            // Get the override controller's overrides
            var overrideClips = new Dictionary<AnimationClip, AnimationClip>();

          
            try
            {
                // Get all animation clips from the project
                //string[] clipGuids = AssetDatabase.FindAssets("t:AnimationClip", new[] { "Assets" });

                List<KeyValuePair<AnimationClip, AnimationClip>> overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>();
                overrideController.GetOverrides(overrides); 

                Debug.Log($"Найдено {overrides.Count} переопределений:");
                string selectStateNameOriginal = selectedStateName.Replace("j", "");
                foreach (var pair in overrides)
                {
                    AnimationClip originalClip = pair.Key;
                    AnimationClip newClip = pair.Value;

                    if (originalClip != null)
                    {
                        string lowerOriginal = selectStateNameOriginal.ToLower();
                        string lowerSub = originalClip.name.ToLower();

                        if (lowerSub.IndexOf(lowerOriginal) > -1)
                        {
                            overrideClips[originalClip] = pair.Value;
                        }

                        Debug.Log($"Оригинал: {originalClip.name} -> Переопределение: {(newClip != null ? newClip.name : "null (удалено)")}");
                    }
                }


            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error getting overrides: {e.Message}");
                Debug.LogError($"Stack trace: {e.StackTrace}");
                return 0;
            }

            // Process each state in the runtime controller
            foreach (var layer in runtimeController.layers)
            {
                Debug.Log($"Processing layer: {layer.name}");
                processedCount += ProcessStateMachine(layer.stateMachine, overrideClips , selectedStateName);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Finished processing override controller {overrideController.name}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error processing override controller {overrideController.name}: {e.Message}");
            Debug.LogError($"Stack trace: {e.StackTrace}");
        }

        return processedCount;
    }




    private static string GetStateNameFromClip(string clipName)
    {
        // Extract the state name from the clip name
        // Example: "FDarkElf_m000_b.ao_Atk01_Pole_FDarkElf" -> "Atk01_Pole"

        // Split by underscore and dot
        char[] separators = { '_', '.' };
        string[] parts = clipName.Split(separators);

        // Find the part after "ao"
        int aoIndex = System.Array.IndexOf(parts, "ao");
        if (aoIndex >= 0 && aoIndex < parts.Length - 1)
        {
            // Return the part after "ao"
            return parts[aoIndex + 1];
        }

        // If we can't find the pattern, return the original name
        return clipName;
    }

    private static bool ContainsStateName(string clipName, string stateName)
    {
        // Check if the clip name contains the state name
        // First try exact match
        if (clipName.Contains(stateName))
            return true;

        // Try removing the first character (for cases like "jatk01_pole" -> "atk01_pole")
        if (stateName.Length > 1)
        {
            string modifiedStateName = stateName.Substring(1);
            if (clipName.Contains(modifiedStateName))
                return true;
        }

        // Try case-insensitive match
        if (clipName.ToLower().Contains(stateName.ToLower()))
            return true;

        return false;
    }



    private static void GetAllAnimationClips(AnimatorStateMachine stateMachine, List<AnimationClip> clips)
    {
        foreach (var state in stateMachine.states)
        {
            if (state.state.motion is AnimationClip clip)
            {
                clips.Add(clip);
            }
            else if (state.state.motion is BlendTree blendTree)
            {
                GetBlendTreeClips(blendTree, clips);
            }

            foreach (var subMachine in stateMachine.stateMachines)
            {
                GetAllAnimationClips(subMachine.stateMachine, clips);
            }
        }
    }

    private static void GetBlendTreeClips(BlendTree blendTree, List<AnimationClip> clips)
    {
        foreach (var child in blendTree.children)
        {
            if (child.motion is AnimationClip clip)
            {
                clips.Add(clip);
            }
            else if (child.motion is BlendTree subTree)
            {
                GetBlendTreeClips(subTree, clips);
            }
        }
    }

    private static void GetOverrideClips(AnimatorStateMachine stateMachine, List<KeyValuePair<AnimationClip, AnimationClip>> overrideClips)
    {
        foreach (var state in stateMachine.states)
        {
            if (state.state.motion is AnimationClip clip)
            {
                // Check if this state has an override
                var overrideClip = GetOverrideClipForState(state);
                if (overrideClip != null)
                {
                    overrideClips.Add(new KeyValuePair<AnimationClip, AnimationClip>(clip, overrideClip));
                }
            }
            else if (state.state.motion is BlendTree blendTree)
            {
                GetBlendTreeOverrideClips(blendTree, overrideClips);
            }

            foreach (var subMachine in stateMachine.stateMachines)
            {
                GetOverrideClips(subMachine.stateMachine, overrideClips);
            }
        }
    }

    private static void GetBlendTreeOverrideClips(BlendTree blendTree, List<KeyValuePair<AnimationClip, AnimationClip>> overrideClips)
    {
        foreach (var child in blendTree.children)
        {
            if (child.motion is AnimationClip clip)
            {
                // Check if this child has an override
                var overrideClip = GetOverrideClipForMotion(clip);
                if (overrideClip != null)
                {
                    overrideClips.Add(new KeyValuePair<AnimationClip, AnimationClip>(clip, overrideClip));
                }
            }
            else if (child.motion is BlendTree subTree)
            {
                GetBlendTreeOverrideClips(subTree, overrideClips);
            }
        }
    }

    private static AnimationClip GetOverrideClipForState(ChildAnimatorState state)
    {
        // Try to get the override clip from the state
        if (state.state.motion is AnimationClip clip)
        {
            return GetOverrideClipForMotion(clip);
        }
        return null;
    }

    private static AnimationClip GetOverrideClipForMotion(Motion motion)
    {
        if (motion is AnimationClip clip)
        {
            // Try to find the override in the current override controller
            var overrideController = Selection.activeObject as AnimatorOverrideController;
            if (overrideController != null)
            {
                var overridesField = typeof(AnimatorOverrideController).GetField("mOverrides",
                    BindingFlags.NonPublic | BindingFlags.Instance);

                if (overridesField != null)
                {
                    var overrides = (Dictionary<AnimationClip, AnimationClip>)overridesField.GetValue(overrideController);
                    if (overrides != null && overrides.TryGetValue(clip, out AnimationClip overrideClip))
                    {
                        return overrideClip;
                    }
                }
            }
        }
        return null;
    }

    private static int ProcessStateMachine(AnimatorStateMachine stateMachine )
    {
        int processedCount = 0;

        foreach (var state in stateMachine.states)
        {
            if (state.state.motion is AnimationClip clip)
            {
                if (ShouldProcessState(state.state.name))
                {
                    Debug.Log($"Processing state: {state.state.name} with clip: {clip.name}");
                    AddEndAnimationEvent(clip, state.state.name);
                    processedCount++;
                }
            }
            else if (state.state.motion is BlendTree blendTree)
            {
                processedCount += ProcessBlendTree(blendTree);
            }

            foreach (var subMachine in stateMachine.stateMachines)
            {
                processedCount += ProcessStateMachine(subMachine.stateMachine);
            }
        }

        return processedCount;
    }

    private static int ProcessStateMachine(AnimatorStateMachine stateMachine, Dictionary<AnimationClip, AnimationClip> overrideClips , string selectedStateName)
    {
        int processedCount = 0;

        foreach (var state in stateMachine.states)
        {
            if (state.state.motion is AnimationClip clip)
            {
                // Check if this clip has an override
                if (overrideClips.TryGetValue(clip, out AnimationClip overrideClip))
                {
                    Debug.Log($"Processing state: {state.state.name} with original clip: {clip.name} and override clip: {overrideClip.name}");

                    // Get existing events from the override clip
                    AnimationEvent[] existingEvents = AnimationUtility.GetAnimationEvents(overrideClip);

                    // Check if the event already exists
                    bool eventExists = false;
                    foreach (var existingEvent in existingEvents)
                    {
                        if (existingEvent.functionName == "OnAnimationComplete")
                        {
                            eventExists = true;
                            Debug.Log($"Event for {state.state.name} already exists in {overrideClip.name}");
                            break;
                        }
                    }

                    // Only add the event if it doesn't already exist
                    if (!eventExists)
                    {
                        // Create the event for the override clip
                        AnimationEvent endEvent = new AnimationEvent();
                        endEvent.functionName = "OnAnimationComplete";
                        endEvent.time = overrideClip.length;
                        endEvent.stringParameter = selectedStateName;

                        // Create a new list with existing events
                        List<AnimationEvent> eventList = new List<AnimationEvent>(existingEvents);

                        // Add the new event
                        eventList.Add(endEvent);

                        // Set the events back using AnimationUtility
                        AnimationUtility.SetAnimationEvents(overrideClip, eventList.ToArray());

                        // Verify the events were set
                        AnimationEvent[] finalEvents = AnimationUtility.GetAnimationEvents(overrideClip);
                        Debug.Log($"Override clip {overrideClip.name} now has {finalEvents.Length} events");

                        // Mark the clip as dirty
                        EditorUtility.SetDirty(overrideClip);
                        processedCount++;

                        Debug.Log($"Added new event for {state.state.name} in {overrideClip.name}");
                    }
                    else
                    {
                        Debug.Log($"Skipping event creation for {state.state.name} - already exists in {overrideClip.name}");
                    }
                }
            }
            else if (state.state.motion is BlendTree blendTree)
            {
                processedCount += ProcessBlendTree(blendTree, overrideClips);
            }

            foreach (var subMachine in stateMachine.stateMachines)
            {
                processedCount += ProcessStateMachine(subMachine.stateMachine, overrideClips , selectedStateName);
            }
        }

        return processedCount;
    }


    private static int ProcessBlendTree(BlendTree blendTree)
    {
        int processedCount = 0;

        foreach (var child in blendTree.children)
        {
            if (child.motion is AnimationClip clip)
            {
                if (ShouldProcessState(blendTree.name))
                {
                    AddEndAnimationEvent(clip, blendTree.name);
                    processedCount++;
                }
            }
            else if (child.motion is BlendTree subTree)
            {
                processedCount += ProcessBlendTree(subTree);
            }
        }

        return processedCount;
    }

    private static int ProcessBlendTree(BlendTree blendTree, Dictionary<AnimationClip, AnimationClip> overrideClips)
    {
        int processedCount = 0;

        foreach (var child in blendTree.children)
        {
            if (child.motion is AnimationClip clip)
            {
                // Check if this clip has an override
                if (overrideClips.TryGetValue(clip, out AnimationClip overrideClip))
                {
                    Debug.Log($"Processing blend tree: {blendTree.name} with original clip: {clip.name} and override clip: {overrideClip.name}");

                    // Get existing events from the override clip
                    AnimationEvent[] existingEvents = AnimationUtility.GetAnimationEvents(overrideClip);

                    // Check if the event already exists
                    bool eventExists = false;
                    foreach (var existingEvent in existingEvents)
                    {
                        if (existingEvent.functionName == "OnAnimationComplete" &&
                            existingEvent.stringParameter == blendTree.name &&
                            Mathf.Abs(existingEvent.time - overrideClip.length) < 0.001f) // Small tolerance for floating point comparison
                        {
                            eventExists = true;
                            Debug.Log($"Event for {blendTree.name} already exists in {overrideClip.name}");
                            break;
                        }
                    }

                    // Only add the event if it doesn't already exist
                    if (!eventExists)
                    {
                        // Create the event for the override clip
                        AnimationEvent endEvent = new AnimationEvent();
                        endEvent.functionName = "OnAnimationComplete";
                        endEvent.time = overrideClip.length;
                        endEvent.stringParameter = blendTree.name;

                        // Create a new list with existing events
                        List<AnimationEvent> eventList = new List<AnimationEvent>(existingEvents);

                        // Add the new event
                        eventList.Add(endEvent);

                        // Set the events back using AnimationUtility
                        AnimationUtility.SetAnimationEvents(overrideClip, eventList.ToArray());

                        // Verify the events were set
                        AnimationEvent[] finalEvents = AnimationUtility.GetAnimationEvents(overrideClip);
                        Debug.Log($"Override clip {overrideClip.name} now has {finalEvents.Length} events");

                        // Mark the clip as dirty
                        EditorUtility.SetDirty(overrideClip);
                        processedCount++;

                        Debug.Log($"Added new event for {blendTree.name} in {overrideClip.name}");
                    }
                    else
                    {
                        Debug.Log($"Skipping event creation for {blendTree.name} - already exists in {overrideClip.name}");
                    }
                }
            }
            else if (child.motion is BlendTree subTree)
            {
                processedCount += ProcessBlendTree(subTree, overrideClips);
            }
        }

        return processedCount;
    }


    private static bool ShouldProcessState(string stateName)
    {
        if (applyToAllStates)
            return true;

        return targetStateNames.Contains(stateName);
    }

    private static void AddEndAnimationEvent(AnimationClip clip, string eventName)
    {
        if (clip == null)
        {
            Debug.LogWarning("Attempted to add event to null clip");
            return;
        }

        Debug.Log($"Adding event to clip: {clip.name}, state: {eventName}");

        // Get existing events
        AnimationEvent[] events = AnimationUtility.GetAnimationEvents(clip);
        Debug.Log($"Clip {clip.name} initially has {events.Length} events");

        // Check if the event already exists
        bool eventExists = false;
        foreach (var existingEvent in events)
        {
            if (existingEvent.functionName == "OnAnimationComplete" &&
                existingEvent.stringParameter == eventName &&
                Mathf.Abs(existingEvent.time - clip.length) < 0.001f) // Small tolerance for floating point comparison
            {
                eventExists = true;
                Debug.Log($"Event for {eventName} already exists in {clip.name}");
                break;
            }
        }

        // Only add the event if it doesn't already exist
        if (!eventExists)
        {
            // Create a new AnimationEvent
            AnimationEvent endEvent = new AnimationEvent();
            endEvent.functionName = "OnAnimationComplete";
            endEvent.time = clip.length;
            endEvent.stringParameter = eventName;

            // Create a new list with existing events
            List<AnimationEvent> eventList = new List<AnimationEvent>(events);

            // Add the new event
            eventList.Add(endEvent);
            Debug.Log($"Adding event to clip {clip.name}, total events will be: {eventList.Count}");

            // Set the events back using AnimationUtility
            AnimationUtility.SetAnimationEvents(clip, eventList.ToArray());

            // Verify the events were set
            AnimationEvent[] finalEvents = AnimationUtility.GetAnimationEvents(clip);
            Debug.Log($"Clip {clip.name} now has {finalEvents.Length} events");

            // Mark the clip as dirty to save changes
            EditorUtility.SetDirty(clip);
            Debug.Log($"Marked clip as dirty");
        }
        else
        {
            Debug.Log($"Skipping event creation for {eventName} - already exists");
        }
    }

}
