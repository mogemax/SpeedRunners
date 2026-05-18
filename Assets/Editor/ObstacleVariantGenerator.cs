using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

public class ObstacleVariantGenerator
{
    private const string SpritesheetPath = "Assets/ImportedAssets/Sprites/all_obstacles.png";
    private const string BasePrefabPath = "Assets/Prefabs/Scenaries/ObstacleBase.prefab";
    private const string OutputFolder = "Assets/Prefabs/Scenaries/Obstacles";

    [MenuItem("Tools/Generate Obstacle Variants")]
    public static void GenerateVariants()
    {
        GameObject basePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BasePrefabPath);
        if (basePrefab == null)
        {
            Debug.LogError($"No se encontró el prefab base en: {BasePrefabPath}");
            return;
        }

        Object[] allAssets = AssetDatabase.LoadAllAssetsAtPath(SpritesheetPath);
        List<Sprite> spriteList = new List<Sprite>();
        foreach (Object asset in allAssets)
        {
            if (asset is Sprite s)
                spriteList.Add(s);
        }

        if (spriteList.Count == 0)
        {
            Debug.LogError($"No se encontraron sprites en: {SpritesheetPath}");
            return;
        }

        if (!Directory.Exists(OutputFolder))
            AssetDatabase.CreateFolder("Assets/Prefabs/Scenaries", "Obstacles");

        int created = 0;
        foreach (Sprite sprite in spriteList)
        {
            if (sprite == null) continue;

            string variantPath = $"{OutputFolder}/{sprite.name}.prefab";

            GameObject variantInstance = PrefabUtility.InstantiatePrefab(basePrefab) as GameObject;
            variantInstance.GetComponent<SpriteRenderer>().sprite = sprite;

            PrefabUtility.SaveAsPrefabAsset(variantInstance, variantPath);
            Object.DestroyImmediate(variantInstance);

            created++;
        }

        AssetDatabase.Refresh();
        Debug.Log($"Generados {created} Prefab Variants en {OutputFolder}");
    }
}
