using UnityEditor;
using UnityEngine;

// ---------------------------------------------------------------------------------------------------------------------------
// Procedural Tree Editor - © 2015 Wasabimole http://wasabimole.com
// ---------------------------------------------------------------------------------------------------------------------------
// Part of ProceduralTree [this class is optional - to improve the ProceduralTree inspector, and add the create menu option]
// ---------------------------------------------------------------------------------------------------------------------------
// Please send your feedback and suggestions to mailto://contact@wasabimole.com
// ---------------------------------------------------------------------------------------------------------------------------

namespace Wasabimole.ProceduralTree
{
    [CustomEditor(typeof(ProceduralTree))]
    public class ProceduralTreeEditor : Editor
    {
        UpdateNotifications UpdateNotifications;
        GUIStyle iconGUIStyle;
        GUIContent iconGUIContent;

        static readonly int[] NumSides = new[] { 3, 4, 5, 6, 8, 12, 16, 24, 32 };

        // ---------------------------------------------------------------------------------------------------------------------------
        // Add an option to generate a new tree under GameObject / Create Procedural menu
        // ---------------------------------------------------------------------------------------------------------------------------

        [MenuItem("GameObject/Create Procedural/Procedural Tree")]
        static void CreateProceduralTree()
        {
            var procTree = new GameObject(string.Format("Tree_{0:X4}", Random.Range(0, 65536))).AddComponent<ProceduralTree>();
            procTree.Seed = Random.Range(0, 65536);
            procTree.Update();
            GetSampleMaterial(procTree);
        }

        // ---------------------------------------------------------------------------------------------------------------------------
        // Initialise editor data
        // ---------------------------------------------------------------------------------------------------------------------------

        void OnEnable()
        {
            UpdateNotifications = new UpdateNotifications(ProceduralTree.CurrentVersion, "Procedural Tree", 32907, Repaint, 0x64, true, true, false);

            if (iconGUIStyle == null)
            {
                iconGUIStyle = new GUIStyle() // Create a labelStyle for the notification icon
                {
                    alignment = TextAnchor.MiddleCenter, fontSize = 0, fixedHeight = 16, margin = new RectOffset(3, 3, 5, 0), padding = new RectOffset(0, 1, -2, 0), fontStyle = FontStyle.Bold,
                };
                iconGUIStyle.normal.background = IconContent("sv_icon_name6");
                iconGUIStyle.normal.textColor = Color.white;
            }
            iconGUIContent = new GUIContent("\x21", "Click to read new notification!"); // GUIContent for the notification icon
        }

        // ---------------------------------------------------------------------------------------------------------------------------
        // Draw ProceduralTree inspector GUI
        // ---------------------------------------------------------------------------------------------------------------------------

        public override void OnInspectorGUI()
        {
            UpdateNotifications.Update();

            var tree = (ProceduralTree)target;

            var pt = PrefabUtility.GetPrefabType(tree);
            if (pt != PrefabType.None && pt != PrefabType.DisconnectedPrefabInstance) // Prefabs are not dynamic
            {
                EditorGUILayout.HelpBox("Prefabs are static snapshots of a Procedural Tree. To edit tree parameters, select GameObject > Break Prefab Instance.", MessageType.Info);
                GUI.enabled = false;
                DrawDefaultInspector();
                return;
            }

            EditorGUILayout.HelpBox(tree.MeshInfo, MessageType.Info);

            DrawDefaultInspector();

            GUILayout.BeginHorizontal();
            {
                if (GUILayout.Button("Rand Seed")) // Randomize tree seed
                {
                    UpdateNotifications.AddUsage();
                    Undo.RecordObject(tree, "Random seed " + tree.name);
                    tree.Seed = Random.Range(0, 65536);
                    tree.Update();
                }
                if (GUILayout.Button("Rand Tree")) // Randomize all tree parameters
                {
                    UpdateNotifications.AddUsage();
                    Undo.RecordObject(tree, "Random tree " + tree.name);
                    Undo.RecordObject(tree.Renderer, "Random tree material " + tree.name);
                    tree.Seed = Random.Range(0, 65536);
                    tree.MaxNumVertices = 1024 << Random.Range(1, 6);
                    tree.NumberOfSides = NumSides[Random.Range(0, NumSides.Length)];
                    tree.BaseRadius = Random.Range(0.25f, 4f);
                    tree.RadiusStep = Random.Range(0.875f, 0.95f);
                    tree.MinimumRadius = Random.Range(0.01f, 0.2f);
                    tree.BranchRoundness = Random.value;
                    tree.SegmentLength = Random.Range(0.25f, 0.75f);
                    tree.Twisting = Random.Range(0f, 40f);
                    tree.BranchProbability = Random.Range(0f, 0.25f);
                    tree.Update();

                    // Randomize material only when tree has no material, or it's using one of the sample ones
                    if (tree.Renderer.sharedMaterial != null)
                    {
                        if (AssetDatabase.GetAssetPath(tree.Renderer.sharedMaterial).StartsWith(GetSampleMaterialsPath(tree)))
                            GetSampleMaterial(tree);
                    }
                    else GetSampleMaterial(tree);

                }
                if (GUILayout.Button("Help")) // Take the user to the help page
                {
                    UpdateNotifications.AddUsage();
                    Application.OpenURL("http://www.wasabimole.com/procedural-tree");
                }
                if (UpdateNotifications.HasNotification) // Draw '!' icon if there's a notification
                {
                    GUILayout.Label(iconGUIContent, iconGUIStyle, GUILayout.Width(25));
                    if (Event.current.type == EventType.mouseDown && GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition))
                        UpdateNotifications.AttendNotification();
                }
            }
            GUILayout.EndHorizontal();
        }

        // ---------------------------------------------------------------------------------------------------------------------------
        // Get Unity icon content texture
        // ---------------------------------------------------------------------------------------------------------------------------

        Texture2D IconContent(string name)
        {
            System.Reflection.MethodInfo mi = typeof(EditorGUIUtility).GetMethod("IconContent", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public, null, new System.Type[] { typeof(string) }, null);
            if (mi == null) mi = typeof(EditorGUIUtility).GetMethod("IconContent", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic, null, new System.Type[] { typeof(string) }, null);
            return (Texture2D)((GUIContent)mi.Invoke(null, new object[] { name })).image;
        }

        // ---------------------------------------------------------------------------------------------------------------------------
        // Get a sample material, if available (in SampleMaterials\ folder)
        // ---------------------------------------------------------------------------------------------------------------------------

        static string pathSampleMaterials;

        static string GetSampleMaterialsPath(ProceduralTree tree)
        {
            if (string.IsNullOrEmpty(pathSampleMaterials))
            {
                // Get materials folder
                pathSampleMaterials = AssetDatabase.GetAssetPath(MonoScript.FromMonoBehaviour(tree));
                pathSampleMaterials = System.IO.Path.GetDirectoryName(pathSampleMaterials) + "/SampleMaterials/";
            }
            return pathSampleMaterials;
        }

        static void GetSampleMaterial(ProceduralTree tree)
        {
            // Get sample materials
            string[] sampleMaterials = System.IO.Directory.GetFiles(GetSampleMaterialsPath(tree), "*.mat", System.IO.SearchOption.AllDirectories);
            // Return if none found
            if (sampleMaterials.Length < 1) return; 
            // Assign a random sample material
            tree.Renderer.sharedMaterial = AssetDatabase.LoadAssetAtPath(sampleMaterials[Random.Range(0, sampleMaterials.Length)], typeof(Material)) as Material;
        }
    }
}