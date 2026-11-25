using UnityEditor;
using UnityEngine;
using DyingHorizon;

[CustomEditor(typeof(CoastalProceduralGenerator))]
public class CoastalProceduralGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var generator = (CoastalProceduralGenerator)target;
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Controles de Geração Procedural", EditorStyles.boldLabel);

        if (GUILayout.Button("Gerar Mapa Procedural (Seed Atual)", GUILayout.Height(30)))
        {
            Undo.RegisterFullObjectHierarchyUndo(generator.gameObject, "Gerar Mapa Procedural");
            generator.GenerateMap();
            EditorUtility.SetDirty(generator);
        }

        if (GUILayout.Button("Gerar Mapa com Nova Seed Aleatória", GUILayout.Height(28)))
        {
            Undo.RegisterFullObjectHierarchyUndo(generator.gameObject, "Gerar Mapa Nova Seed");
            generator.GenerateWithNewRandomSeed();
            EditorUtility.SetDirty(generator);
        }
    }
}
