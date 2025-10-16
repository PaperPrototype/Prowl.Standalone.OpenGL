// This file is part of the Prowl Game Engine
// Licensed under the MIT License. See the LICENSE file in the project root for details.

//
// Component Lifecycle Test
//
// This demo thoroughly tests component lifecycle methods (OnEnable, OnDisable, OnDispose)
// in various scenarios including:
// - Adding objects to inactive scenes
// - Scene activation/deactivation
// - GameObject enabled state toggling
// - Component enabled state toggling
// - Parent-child hierarchy enabled state changes
// - Removing objects from scenes
// - Disposing objects
//
// All lifecycle events are logged to the console for verification.
//

using Prowl.Runtime;
using Prowl.Runtime.Resources;

namespace LifecycleTest;

internal class Program
{
    static void Main(string[] args)
    {
        new LifecycleTestGame().Run("Test", 800, 600);
    }
}

public sealed class LifecycleTestGame : Game
{
    private Scene? scene1;
    private Scene? scene2;
    private GameObject? testObject1;
    private GameObject? testObject2;
    private GameObject? parentObject;
    private GameObject? childObject;

    public override void Initialize()
    {
        Debug.Log("==========================================");
        Debug.Log("COMPONENT LIFECYCLE TEST STARTING");
        Debug.Log("==========================================");

        RunAllTests();
    }

    private void RunAllTests()
    {
        // Test 1: Adding objects to inactive scene
        Debug.Log("========== TEST 1: Adding GameObject to INACTIVE Scene ==========");
        scene1 = new Scene();
        testObject1 = new GameObject("TestObject1");
        var comp1 = testObject1.AddComponent<LifecycleComponent>();
        comp1.ComponentName = "Component1";

        Debug.Log("Adding TestObject1 to inactive scene...");
        scene1.Add(testObject1);
        Debug.Log("Expected: OnAddedToScene called, OnEnable NOT called (scene inactive)");

        Debug.Log("");
        Debug.Log("");
        Debug.Log("");

        // Test 2: Scene activation
        Debug.Log("========== TEST 2: Activating Scene ==========");
        Debug.Log("Activating scene1...");
        scene1.Activate();
        Debug.Log("Expected: OnEnable called for all enabled components");

        Debug.Log("");
        Debug.Log("");
        Debug.Log("");

        // Test 3: Adding object to active scene
        Debug.Log("========== TEST 3: Adding GameObject to ACTIVE Scene ==========");
        testObject2 = new GameObject("TestObject2");
        var comp2 = testObject2.AddComponent<LifecycleComponent>();
        comp2.ComponentName = "Component2";

        Debug.Log("Adding TestObject2 to active scene...");
        scene1.Add(testObject2);
        Debug.Log("Expected: OnAddedToScene called, then OnEnable called (scene active)");

        Debug.Log("");
        Debug.Log("");
        Debug.Log("");

        // Test 4: Component enabled state toggle
        Debug.Log("========== TEST 4: Toggling Component Enabled State ==========");
        Debug.Log("Disabling Component1...");
        comp1.Enabled = false;
        Debug.Log("Expected: OnDisable called");

        Debug.Log("");

        Debug.Log("Re-enabling Component1...");
        comp1.Enabled = true;
        Debug.Log("Expected: OnEnable called");

        Debug.Log("");
        Debug.Log("");
        Debug.Log("");

        // Test 5: GameObject enabled state toggle
        Debug.Log("========== TEST 5: Toggling GameObject Enabled State ==========");
        Debug.Log("Disabling TestObject1...");
        testObject1.Enabled = false;
        Debug.Log("Expected: OnDisable called for all enabled components");

        Debug.Log("");

        Debug.Log("Re-enabling TestObject1...");
        testObject1.Enabled = true;
        Debug.Log("Expected: OnEnable called for all enabled components");

        Debug.Log("");
        Debug.Log("");
        Debug.Log("");

        // Test 6: Scene deactivation
        Debug.Log("========== TEST 6: Deactivating Scene ==========");
        Debug.Log("Deactivating scene1...");
        scene1.Deactivate();
        Debug.Log("Expected: OnDisable called for all enabled components");

        Debug.Log("");
        Debug.Log("");
        Debug.Log("");

        // Test 7: Toggling states in inactive scene
        Debug.Log("========== TEST 7: Toggling States in INACTIVE Scene ==========");
        Debug.Log("Disabling Component1 (scene inactive)...");
        comp1.Enabled = false;
        Debug.Log("Expected: No OnDisable (scene inactive)");

        Debug.Log("");

        Debug.Log("Re-enabling Component1 (scene inactive)...");
        comp1.Enabled = true;
        Debug.Log("Expected: No OnEnable (scene inactive)");

        Debug.Log("");
        Debug.Log("");
        Debug.Log("");

        // Test 8: Re-activating scene
        Debug.Log("========== TEST 8: Re-activating Scene ==========");
        Debug.Log("Re-activating scene1...");
        scene1.Activate();
        Debug.Log("Expected: OnEnable called for all enabled components");

        Debug.Log("");
        Debug.Log("");
        Debug.Log("");

        // Test 9: Parent-child hierarchy
        Debug.Log("========== TEST 9: Parent-Child Hierarchy Enabled State ==========");
        parentObject = new GameObject("ParentObject");
        var parentComp = parentObject.AddComponent<LifecycleComponent>();
        parentComp.ComponentName = "ParentComponent";

        childObject = new GameObject("ChildObject");
        var childComp = childObject.AddComponent<LifecycleComponent>();
        childComp.ComponentName = "ChildComponent";

        Debug.Log("Adding ParentObject to scene...");
        scene1.Add(parentObject);

        Debug.Log("");

        Debug.Log("Parenting ChildObject to ParentObject...");
        childObject.SetParent(parentObject);
        Debug.Log("Expected: OnAddedToScene and OnEnable for child");

        Debug.Log("");

        Debug.Log("Disabling ParentObject...");
        parentObject.Enabled = false;
        Debug.Log("Expected: OnDisable for both parent and child components");

        Debug.Log("");

        Debug.Log("Re-enabling ParentObject...");
        parentObject.Enabled = true;
        Debug.Log("Expected: OnEnable for both parent and child components");

        Debug.Log("");
        Debug.Log("");
        Debug.Log("");

        // Test 10: Removing from scene
        Debug.Log("========== TEST 10: Removing GameObject from Scene ==========");
        Debug.Log("Removing TestObject1 from scene...");
        scene1.Remove(testObject1);
        Debug.Log("Expected: OnDisable (if enabled), then OnRemovedFromScene");

        Debug.Log("");

        // Test 11: Disposing GameObject
        Debug.Log("\n========== TEST 11: Disposing GameObject ==========");
        Debug.Log("Disposing TestObject2...");
        testObject2.Dispose();
        Debug.Log("Expected: OnDisable (if enabled), OnDispose, then removal from scene");

        Debug.Log("");

        // Test 12: Removing component
        Debug.Log("========== TEST 12: Removing Component ==========");
        Debug.Log("Removing ParentComponent...");
        parentObject.RemoveComponent(parentComp);
        Debug.Log("Expected: OnDisable (if enabled), then OnDispose");

        Debug.Log("");
        Debug.Log("");
        Debug.Log("");

        // Test 13: Multiple scene management
        Debug.Log("========== TEST 13: Multiple Scene Management ==========");
        scene2 = new Scene();
        var multiSceneObj = new GameObject("MultiSceneObject");
        var multiComp = multiSceneObj.AddComponent<LifecycleComponent>();
        multiComp.ComponentName = "MultiSceneComponent";

        Debug.Log("Adding to inactive scene2...");
        scene2.Add(multiSceneObj);
        Debug.Log("Expected: OnAddedToScene, no OnEnable (scene inactive)");

        Debug.Log("");

        Debug.Log("Activating scene2...");
        scene2.Activate();
        Debug.Log("Expected: OnEnable");

        Debug.Log("");

        Debug.Log("Moving object from scene2 to scene1 (both active)...");
        scene1.Add(multiSceneObj); // This should remove from scene2 and add to scene1
        Debug.Log("Expected: OnDisable (from scene2), OnRemovedFromScene, OnAddedToScene, OnEnable (to scene1)");

        Debug.Log("");
        Debug.Log("");
        Debug.Log("");

        // Test 14: Component on disabled GameObject added to active scene
        Debug.Log("========== TEST 14: Disabled GameObject Added to Active Scene ==========");
        var disabledObj = new GameObject("DisabledObject");
        disabledObj.Enabled = false;
        var disabledComp = disabledObj.AddComponent<LifecycleComponent>();
        disabledComp.ComponentName = "DisabledObjectComponent";

        Debug.Log("Adding disabled GameObject to active scene...");
        scene1.Add(disabledObj);
        Debug.Log("Expected: OnAddedToScene only, no OnEnable (GameObject disabled)");

        Debug.Log("");

        Debug.Log("Enabling the GameObject...");
        disabledObj.Enabled = true;
        Debug.Log("Expected: OnEnable");

        Debug.Log("");
        Debug.Log("");
        Debug.Log("");

        // Test 15: Deactivate scene and dispose
        Debug.Log("========== TEST 15: Scene Cleanup ==========");
        Debug.Log("Deactivating scene1...");
        scene1.Deactivate();
        Debug.Log("Expected: OnDisable for all enabled components");

        Debug.Log("");

        Debug.Log("Disposing scene1...");
        scene1.Dispose();
        Debug.Log("Expected: OnDispose for all components that had Start called");

        Debug.Log("");

        Debug.Log("Deactivating and disposing scene2...");
        scene2.Deactivate();
        scene2.Dispose();
        Debug.Log("Expected: OnDispose since OnDisable was already called");

        Debug.Log("");
        Debug.Log("");
        Debug.Log("");

        Debug.Log("==========================================");
        Debug.Log("ALL LIFECYCLE TESTS COMPLETED");
        Debug.Log("Review the logs above to verify expected behavior");
        Debug.Log("==========================================");

        Debug.Log("Press Escape to exit...");
    }

    public override void BeginUpdate()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Quit();
        }
    }
}
