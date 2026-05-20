using System.Linq;
using UnityEditor;
using UnityEngine;

public static class HookProjectileCreator {
    private const string PrefabPath   = "Assets/Prefabs/Players/HookProjectile.prefab";
    private const string HookSpritePath  = "Assets/ImportedAssets/Sprites/hook.png";
    private const string BreakSpritePath = "Assets/ImportedAssets/Sprites/HookBreak.png";

    [MenuItem("Tools/Create HookProjectile Prefab")]
    public static void CreatePrefab() {
        // ── Sprites ────────────────────────────────────────────────────────
        Sprite hookSprite = AssetDatabase.LoadAssetAtPath<Sprite>(HookSpritePath);
        if (hookSprite == null) {
            Debug.LogError($"[HookProjectileCreator] No se encontró el sprite en {HookSpritePath}");
            return;
        }

        Object[] allBreak = AssetDatabase.LoadAllAssetsAtPath(BreakSpritePath);
        Sprite[] breakSprites = allBreak
            .OfType<Sprite>()
            .OrderBy(s => {
                // Ordenar numéricamente: HookBreak_0, HookBreak_1 ... HookBreak_27
                string num = s.name.Replace("HookBreak_", "");
                return int.TryParse(num, out int n) ? n : 0;
            })
            .ToArray();

        if (breakSprites.Length == 0) {
            Debug.LogWarning($"[HookProjectileCreator] No se encontraron sub-sprites en {BreakSpritePath}");
        }

        // ── Construir el GameObject ────────────────────────────────────────
        var go = new GameObject("HookProjectile");

        // SpriteRenderer con hook_0
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = hookSprite;
        sr.sortingOrder = 101;

        // HookProjectile con sus valores por defecto + sprites configurados
        var hp = go.AddComponent<HookProjectile>();
        hp.arcHeight     = 1.5f;
        hp.breakFrameRate = 20f;
        hp.breakSprites  = breakSprites;

        // ── Guardar como prefab ────────────────────────────────────────────
        bool success;
        PrefabUtility.SaveAsPrefabAsset(go, PrefabPath, out success);
        Object.DestroyImmediate(go);

        if (success) {
            Debug.Log($"[HookProjectileCreator] Prefab creado en {PrefabPath}");
            // Seleccionar el prefab en el Project panel
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        } else {
            Debug.LogError("[HookProjectileCreator] Error al guardar el prefab.");
        }
    }
}
