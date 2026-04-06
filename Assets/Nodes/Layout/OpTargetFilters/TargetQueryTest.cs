using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DLN
{
    public class TargetQueryTest : MonoBehaviour
    {
        [ContextMenu("Run TargetQuery Tests")]
        public void RunTargetQueryTests()
        {
            int passed = 0;
            int failed = 0;

            var failures = new List<string>();

            RunTest("RootOnly_ReturnsRootOnly", Test_RootOnly_ReturnsRootOnly, ref passed, ref failed, failures);
            RunTest("ImmediateChildren_NoLabel_ReturnsAllImmediateChildren", Test_ImmediateChildren_NoLabel_ReturnsAllImmediateChildren, ref passed, ref failed, failures);
            RunTest("ImmediateChildrenWithLabel_ReturnsOnlyMatchingImmediateChildren", Test_ImmediateChildrenWithLabel_ReturnsOnlyMatchingImmediateChildren, ref passed, ref failed, failures);
            RunTest("FirstMatchingDepthWithLabel_ReturnsOnlyFirstDepthMatches", Test_FirstMatchingDepthWithLabel_ReturnsOnlyFirstDepthMatches, ref passed, ref failed, failures);
            RunTest("AllMatchingDescendantsWithLabel_ReturnsAllMatchesRecursively", Test_AllMatchingDescendantsWithLabel_ReturnsAllMatchesRecursively, ref passed, ref failed, failures);
            RunTest("RecursiveModes_RequireNonEmptyLabel", Test_RecursiveModes_RequireNonEmptyLabel, ref passed, ref failed, failures);
            RunTest("RootNull_ReturnsEmpty", Test_RootNull_ReturnsEmpty, ref passed, ref failed, failures);

            if (failed == 0)
            {
                Debug.Log($"[TargetQueryTest] All tests passed ✅ ({passed} total)", this);
            }
            else
            {
                string message =
                    $"[TargetQueryTest] Tests finished with failures ❌\n" +
                    $"Passed: {passed}\n" +
                    $"Failed: {failed}\n\n" +
                    string.Join("\n\n", failures);

                Debug.LogError(message, this);
            }
        }

        private static void RunTest(
            string testName,
            Action test,
            ref int passed,
            ref int failed,
            List<string> failures)
        {
            try
            {
                test();
                passed++;
                Debug.Log($"[TargetQueryTest] PASS: {testName}");
            }
            catch (Exception ex)
            {
                failed++;
                string msg = $"[TargetQueryTest] FAIL: {testName}\n{ex}";
                failures.Add(msg);
                Debug.LogError(msg);
            }
        }

        private static void Test_RootOnly_ReturnsRootOnly()
        {
            GameObject root = null;

            try
            {
                root = CreateGO("Root");

                var query = new TargetQuery
                {
                    root = root,
                    include = TargetIncludeMode.RootOnly,
                    label = null
                };

                List<GameObject> results = TargetUtils.GetTargets(query);

                ExpectNames(results, "Root");
            }
            finally
            {
                SafeDestroy(root);
            }
        }

        private static void Test_ImmediateChildren_NoLabel_ReturnsAllImmediateChildren()
        {
            GameObject root = null;

            try
            {
                root = CreateGO("Root");

                CreateChild(root, "A");
                CreateChild(root, "B");
                CreateChild(root, "C");

                GameObject parent = CreateChild(root, "Parent");
                CreateChild(parent, "Nested");

                var query = new TargetQuery
                {
                    root = root,
                    include = TargetIncludeMode.ImmediateChildren,
                    label = null
                };

                List<GameObject> results = TargetUtils.GetTargets(query);

                ExpectNames(results, "A", "B", "C", "Parent");
            }
            finally
            {
                SafeDestroy(root);
            }
        }

        private static void Test_ImmediateChildrenWithLabel_ReturnsOnlyMatchingImmediateChildren()
        {
            GameObject root = null;

            try
            {
                root = CreateGO("Root");

                GameObject a = CreateChild(root, "A");
                AddLabels(a, "slot");

                GameObject b = CreateChild(root, "B");
                AddLabels(b, "other");

                GameObject c = CreateChild(root, "C");
                AddLabels(c, "slot");

                GameObject parent = CreateChild(root, "Parent");
                GameObject nested = CreateChild(parent, "Nested");
                AddLabels(nested, "slot");

                var query = new TargetQuery
                {
                    root = root,
                    include = TargetIncludeMode.ImmediateChildrenWithLabel,
                    label = "slot"
                };

                List<GameObject> results = TargetUtils.GetTargets(query);

                ExpectNames(results, "A", "C");
            }
            finally
            {
                SafeDestroy(root);
            }
        }

        private static void Test_FirstMatchingDepthWithLabel_ReturnsOnlyFirstDepthMatches()
        {
            GameObject root = null;

            try
            {
                root = CreateGO("Root");

                GameObject child1 = CreateChild(root, "Child1");
                GameObject child2 = CreateChild(root, "Child2");
                GameObject child3 = CreateChild(root, "Child3");

                AddLabels(child2, "slot");

                GameObject grand1 = CreateChild(child1, "Grand1");
                AddLabels(grand1, "slot");

                GameObject grand2 = CreateChild(child3, "Grand2");
                AddLabels(grand2, "slot");

                var query = new TargetQuery
                {
                    root = root,
                    include = TargetIncludeMode.FirstMatchingDepthWithLabel,
                    label = "slot"
                };

                List<GameObject> results = TargetUtils.GetTargets(query);

                // Since Child2 matches at depth 1, deeper matches should be ignored.
                ExpectNames(results, "Child2");
            }
            finally
            {
                SafeDestroy(root);
            }
        }

        private static void Test_AllMatchingDescendantsWithLabel_ReturnsAllMatchesRecursively()
        {
            GameObject root = null;

            try
            {
                root = CreateGO("Root");

                GameObject a = CreateChild(root, "A");
                AddLabels(a, "slot");

                GameObject b = CreateChild(root, "B");

                GameObject c = CreateChild(root, "C");
                AddLabels(c, "other");

                GameObject b1 = CreateChild(b, "B1");
                AddLabels(b1, "slot");

                GameObject b2 = CreateChild(b, "B2");

                GameObject b21 = CreateChild(b2, "B21");
                AddLabels(b21, "slot");

                var query = new TargetQuery
                {
                    root = root,
                    include = TargetIncludeMode.AllMatchingDescendantsWithLabel,
                    label = "slot"
                };

                List<GameObject> results = TargetUtils.GetTargets(query);

                ExpectNames(results, "A", "B1", "B21");
            }
            finally
            {
                SafeDestroy(root);
            }
        }

        private static void Test_RecursiveModes_RequireNonEmptyLabel()
        {
            GameObject root = null;

            try
            {
                root = CreateGO("Root");

                bool threwFirstMatchingDepth = false;
                bool threwAllMatchingDescendants = false;

                try
                {
                    TargetUtils.GetTargets(new TargetQuery
                    {
                        root = root,
                        include = TargetIncludeMode.FirstMatchingDepthWithLabel,
                        label = ""
                    });
                }
                catch (ArgumentException)
                {
                    threwFirstMatchingDepth = true;
                }

                try
                {
                    TargetUtils.GetTargets(new TargetQuery
                    {
                        root = root,
                        include = TargetIncludeMode.AllMatchingDescendantsWithLabel,
                        label = "   "
                    });
                }
                catch (ArgumentException)
                {
                    threwAllMatchingDescendants = true;
                }

                if (!threwFirstMatchingDepth)
                    throw new Exception("Expected FirstMatchingDepthWithLabel to throw ArgumentException for empty label.");

                if (!threwAllMatchingDescendants)
                    throw new Exception("Expected AllMatchingDescendantsWithLabel to throw ArgumentException for whitespace label.");
            }
            finally
            {
                SafeDestroy(root);
            }
        }

        private static void Test_RootNull_ReturnsEmpty()
        {
            var query = new TargetQuery
            {
                root = null,
                include = TargetIncludeMode.RootOnly,
                label = null
            };

            List<GameObject> results = TargetUtils.GetTargets(query);

            if (results == null)
                throw new Exception("Expected non-null result list.");

            if (results.Count != 0)
                throw new Exception($"Expected empty result list, but got {results.Count} results.");
        }

        private static GameObject CreateGO(string name)
        {
            var go = new GameObject(name);
            go.hideFlags = HideFlags.DontSave;

            // Ensure ToLocalBounds() has something to resolve in tests.
            go.AddComponent<BoxCollider>();

            return go;
        }

        private static GameObject CreateChild(GameObject parent, string name)
        {
            var child = CreateGO(name);
            child.transform.SetParent(parent.transform, false);
            return child;
        }

        private static void AddLabels(GameObject go, params string[] labels)
        {
            var meta = go.GetComponent<TargetMetadata>();
            if (meta == null)
                meta = go.AddComponent<TargetMetadata>();

            meta.labels = labels ?? Array.Empty<string>();
        }

        private static void ExpectNames(List<GameObject> actual, params string[] expectedNames)
        {
            if (actual == null)
                throw new Exception("Expected non-null results.");

            List<string> actualNames = actual
                .Select(x => x != null ? x.name : "<null>")
                .ToList();

            List<string> expected = expectedNames.ToList();

            if (actualNames.Count != expected.Count)
            {
                throw new Exception(
                    $"Expected {expected.Count} results but got {actualNames.Count}.\n" +
                    $"Expected: [{string.Join(", ", expected)}]\n" +
                    $"Actual:   [{string.Join(", ", actualNames)}]");
            }

            for (int i = 0; i < expected.Count; i++)
            {
                if (!string.Equals(actualNames[i], expected[i], StringComparison.Ordinal))
                {
                    throw new Exception(
                        $"Mismatch at index {i}.\n" +
                        $"Expected: {expected[i]}\n" +
                        $"Actual:   {actualNames[i]}\n" +
                        $"Full Expected: [{string.Join(", ", expected)}]\n" +
                        $"Full Actual:   [{string.Join(", ", actualNames)}]");
                }
            }
        }

        private static void SafeDestroy(GameObject go)
        {
            if (go == null)
                return;

            if (Application.isPlaying)
                UnityEngine.Object.Destroy(go);
            else
                UnityEngine.Object.DestroyImmediate(go);
        }
    }
}