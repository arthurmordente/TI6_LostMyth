using Logic.Scripts.GameDomain.MVC.Boss.Visuals;
using Logic.Scripts.GameDomain.MVC.Environment.Laki;
using UnityEditor;
using UnityEngine;

public static class CombatVisualPrefabGenerator
{
    private const string BaseFolder = "Assets/Logic/Scripts/GameDomain/MVC/Boss/Visuals";
    private const string GeneratedFolder = BaseFolder + "/GeneratedPrefabs";
    private const string CombatTelegraphsFolder = GeneratedFolder + "/CombatTelegraphs";
    private const string LakiArenaTilesFolder = GeneratedFolder + "/LakiArenaTiles";
    private const string MeshFolder = GeneratedFolder + "/Meshes";
    private const string MatFolder = GeneratedFolder + "/Materials";
    private const string LakiMatFolder = MatFolder + "/Laki";

    /// <summary>Must match <c>BossAttack</c> default <c>ProteanConesParams</c> (angle/sides; mesh is unit radius, scaled at runtime by attack radius).</summary>
    private const float ProteanConeMeshUnitRadius = 1f;
    private const float ProteanConeMeshAngleDeg = 60f;
    private const int ProteanConeMeshSides = 36;

    /// <summary>Must match <c>BossAttack</c> default <c>_wingSlash</c> cone shape for telegraph asset.</summary>
    private const float WingSlashConeMeshUnitRadius = 1f;
    private const float WingSlashConeMeshAngleDeg = 215f;
    private const int WingSlashConeMeshSides = 48;

    [MenuItem("Tools/Boss/Generate Combat Visual Prefabs (Telegraphs + Laki tiles)")]
    public static void Generate()
    {
        EnsureFolder("Assets/Logic/Scripts/GameDomain/MVC/Boss", "Visuals");
        EnsureFolder(BaseFolder, "GeneratedPrefabs");
        EnsureFolder(GeneratedFolder, "CombatTelegraphs");
        EnsureFolder(GeneratedFolder, "LakiArenaTiles");
        EnsureFolder(GeneratedFolder, "Meshes");
        EnsureFolder(GeneratedFolder, "Materials");
        EnsureFolder(MatFolder, "Laki");

        Material normal = CreateOrGetUnlitMaterial(MatFolder, "Mat_Normal", new Color(1f, 1f, 0f, 0.7f));
        Material pull = CreateOrGetUnlitMaterial(MatFolder, "Mat_Pull", new Color(0.2f, 0.8f, 1f, 0.7f));
        Material push = CreateOrGetUnlitMaterial(MatFolder, "Mat_Push", new Color(1f, 0.3f, 0.3f, 0.7f));

        Material lakiPositive = CreateOrGetLitMaterial(LakiMatFolder, "Mat_Laki_Positive", new Color(0.2f, 1f, 0.25f, 1f));
        Material lakiNeutral = CreateOrGetLitMaterial(LakiMatFolder, "Mat_Laki_Neutral", new Color(0.88f, 0.88f, 0.9f, 1f));
        Material lakiNegative = CreateOrGetLitMaterial(LakiMatFolder, "Mat_Laki_Negative", new Color(1f, 0.22f, 0.22f, 1f));

        Mesh proteanConeMesh = CreateOrGetMesh("Mesh_Cone_ProteanBase",
            BuildConeMesh(ProteanConeMeshUnitRadius, ProteanConeMeshAngleDeg, ProteanConeMeshSides));
        Mesh wingConeMesh = CreateOrGetMesh("Mesh_Cone_WingSlashBase",
            BuildConeMesh(WingSlashConeMeshUnitRadius, WingSlashConeMeshAngleDeg, WingSlashConeMeshSides));
        Mesh discMesh = CreateOrGetMesh("Mesh_Disc", BuildDiscMesh(1f, 48));
        Mesh stripMesh = CreateOrGetMesh("Mesh_FeatherStrip", BuildStripMesh(8f, 1f));

        GameObject coneTn = CreateSimpleMeshPrefab(CombatTelegraphsFolder, "Telegraph_Cones_Normal", proteanConeMesh, normal);
        GameObject coneTp = CreateSimpleMeshPrefab(CombatTelegraphsFolder, "Telegraph_Cones_Pull", proteanConeMesh, pull);
        GameObject coneTs = CreateSimpleMeshPrefab(CombatTelegraphsFolder, "Telegraph_Cones_Push", proteanConeMesh, push);

        GameObject wingTn = CreateSimpleMeshPrefab(CombatTelegraphsFolder, "Telegraph_WingSlash_Normal", wingConeMesh, normal);
        GameObject wingTp = CreateSimpleMeshPrefab(CombatTelegraphsFolder, "Telegraph_WingSlash_Pull", wingConeMesh, pull);
        GameObject wingTs = CreateSimpleMeshPrefab(CombatTelegraphsFolder, "Telegraph_WingSlash_Push", wingConeMesh, push);

        GameObject circleTn = CreateSimpleMeshPrefab(CombatTelegraphsFolder, "Telegraph_Circle_Normal", discMesh, normal);
        GameObject circleTp = CreateSimpleMeshPrefab(CombatTelegraphsFolder, "Telegraph_Circle_Pull", discMesh, pull);
        GameObject circleTs = CreateSimpleMeshPrefab(CombatTelegraphsFolder, "Telegraph_Circle_Push", discMesh, push);

        GameObject skyTn = CreateSimpleMeshPrefab(CombatTelegraphsFolder, "Telegraph_SkySwords_Normal", discMesh, normal);
        GameObject skyTp = CreateSimpleMeshPrefab(CombatTelegraphsFolder, "Telegraph_SkySwords_Pull", discMesh, pull);
        GameObject skyTs = CreateSimpleMeshPrefab(CombatTelegraphsFolder, "Telegraph_SkySwords_Push", discMesh, push);

        GameObject flTn = CreateSimpleMeshPrefab(CombatTelegraphsFolder, "Telegraph_FeatherLines_Normal", stripMesh, normal);
        GameObject flTp = CreateSimpleMeshPrefab(CombatTelegraphsFolder, "Telegraph_FeatherLines_Pull", stripMesh, pull);
        GameObject flTs = CreateSimpleMeshPrefab(CombatTelegraphsFolder, "Telegraph_FeatherLines_Push", stripMesh, push);

        GameObject orbTn = CreateSimpleMeshPrefab(CombatTelegraphsFolder, "Telegraph_Orb_Normal", discMesh, normal);
        GameObject orbTp = CreateSimpleMeshPrefab(CombatTelegraphsFolder, "Telegraph_Orb_Pull", discMesh, pull);
        GameObject orbTs = CreateSimpleMeshPrefab(CombatTelegraphsFolder, "Telegraph_Orb_Push", discMesh, push);

        GameObject bigOrbTn = CreateSimpleMeshPrefab(CombatTelegraphsFolder, "Telegraph_BigOrb_Normal", discMesh, normal);
        GameObject bigOrbTp = CreateSimpleMeshPrefab(CombatTelegraphsFolder, "Telegraph_BigOrb_Pull", discMesh, pull);
        GameObject bigOrbTs = CreateSimpleMeshPrefab(CombatTelegraphsFolder, "Telegraph_BigOrb_Push", discMesh, push);

        GameObject colTn = CreateSimpleMeshPrefab(CombatTelegraphsFolder, "Feather_Column_Normal", stripMesh, normal);
        GameObject colTp = CreateSimpleMeshPrefab(CombatTelegraphsFolder, "Feather_Column_Pull", stripMesh, pull);
        GameObject colTs = CreateSimpleMeshPrefab(CombatTelegraphsFolder, "Feather_Column_Push", stripMesh, push);

        GameObject[] lakiTiles = BuildLakiRouletteCanonicalTilePrefabs(lakiNeutral, lakiPositive, lakiNegative);

        TryAssignCatalog(
            coneTn, coneTp, coneTs,
            wingTn, wingTp, wingTs,
            circleTn, circleTp, circleTs,
            skyTn, skyTp, skyTs,
            flTn, flTp, flTs,
            orbTn, orbTp, orbTs,
            bigOrbTn, bigOrbTp, bigOrbTs,
            colTn, colTp, colTs,
            lakiTiles);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[CombatVisualPrefabGenerator] Generated combat telegraphs (CombatTelegraphs/), Laki tiles + materials (LakiArenaTiles/, Materials/Laki/), cleared area slots on CombatAttackVisualCatalog.");
    }

    private static GameObject[] BuildLakiRouletteCanonicalTilePrefabs(Material matNeutral, Material matPositive, Material matNegative)
    {
        const float innerRadius = RouletteArenaService.INNER_RADIUS_DEFAULT;
        const float outerRadius = RouletteArenaService.OUTER_RADIUS_DEFAULT;
        const float radialSplit01 = 0.6f;
        const float arcStartDeg = 180f;
        const float arcDeg = 180f;
        const int sectorCount = 8;
        const float angularGapDeg = 2f;
        const float radialGap = 0.05f;
        const int angularSmooth = 8;
        Vector3 centerWorld = Vector3.zero;

        float sectorAngle = arcDeg / sectorCount;
        float split = innerRadius + radialSplit01 * (outerRadius - innerRadius);
        float halfGap = Mathf.Max(0f, angularGapDeg) * 0.5f;
        int s = 0;
        float a0 = arcStartDeg + s * sectorAngle + halfGap;
        float a1 = arcStartDeg + (s + 1) * sectorAngle - halfGap;

        float r0Inner = innerRadius;
        float r1Inner = split;
        float rMinInner = Mathf.Min(r0Inner, r1Inner) + Mathf.Max(0f, radialGap);
        float rMaxInner = Mathf.Max(r0Inner, r1Inner) - Mathf.Max(0f, radialGap);
        if (rMaxInner <= rMinInner) rMaxInner = rMinInner + 0.005f;

        float r0Outer = split;
        float r1Outer = outerRadius;
        float rMinOuter = Mathf.Min(r0Outer, r1Outer) + Mathf.Max(0f, radialGap);
        float rMaxOuter = Mathf.Max(r0Outer, r1Outer) - Mathf.Max(0f, radialGap);
        if (rMaxOuter <= rMinOuter) rMaxOuter = rMinOuter + 0.005f;

        float midAngleInner = (a0 + a1) * 0.5f * Mathf.Deg2Rad;
        float midRInner = (rMinInner + rMaxInner) * 0.5f;
        Vector3 tileCenterInner = centerWorld + new Vector3(
            Mathf.Cos(midAngleInner) * midRInner, 0f, Mathf.Sin(midAngleInner) * midRInner);
        Vector3 pivotInner = tileCenterInner - centerWorld;

        float midAngleOuter = (a0 + a1) * 0.5f * Mathf.Deg2Rad;
        float midROuter = (rMinOuter + rMaxOuter) * 0.5f;
        Vector3 tileCenterOuter = centerWorld + new Vector3(
            Mathf.Cos(midAngleOuter) * midROuter, 0f, Mathf.Sin(midAngleOuter) * midROuter);
        Vector3 pivotOuter = tileCenterOuter - centerWorld;

        Mesh innerMesh = CreateOrGetMesh("Mesh_LakiTile_Inner_Canonical_S0",
            LakiRouletteSectorMeshBuilder.BuildRingSectorMesh(rMinInner, rMaxInner, a0, a1, angularSmooth, pivotInner));
        Mesh outerMesh = CreateOrGetMesh("Mesh_LakiTile_Outer_Canonical_S0",
            LakiRouletteSectorMeshBuilder.BuildRingSectorMesh(rMinOuter, rMaxOuter, a0, a1, angularSmooth, pivotOuter));

        string[] innerNames = { "LakiRoulette_Inner_Neutral", "LakiRoulette_Inner_Positive", "LakiRoulette_Inner_Negative" };
        string[] outerNames = { "LakiRoulette_Outer_Neutral", "LakiRoulette_Outer_Positive", "LakiRoulette_Outer_Negative" };
        Material[] innerMats = { matNeutral, matPositive, matNegative };
        Material[] outerMats = { matNeutral, matPositive, matNegative };

        var tiles = new GameObject[6];
        for (int i = 0; i < 3; i++)
            tiles[i] = CreateSimpleMeshPrefab(LakiArenaTilesFolder, innerNames[i], innerMesh, innerMats[i]);
        for (int i = 0; i < 3; i++)
            tiles[3 + i] = CreateSimpleMeshPrefab(LakiArenaTilesFolder, outerNames[i], outerMesh, outerMats[i]);
        return tiles;
    }

    private static void TryAssignCatalog(
        GameObject coneTn, GameObject coneTp, GameObject coneTs,
        GameObject wingTn, GameObject wingTp, GameObject wingTs,
        GameObject circleTn, GameObject circleTp, GameObject circleTs,
        GameObject skyTn, GameObject skyTp, GameObject skyTs,
        GameObject flTn, GameObject flTp, GameObject flTs,
        GameObject orbTn, GameObject orbTp, GameObject orbTs,
        GameObject bigOrbTn, GameObject bigOrbTp, GameObject bigOrbTs,
        GameObject colTn, GameObject colTp, GameObject colTs,
        GameObject[] lakiTiles)
    {
        string[] guids = AssetDatabase.FindAssets("t:CombatAttackVisualCatalogSO");
        if (guids == null || guids.Length == 0) return;

        string path = AssetDatabase.GUIDToAssetPath(guids[0]);
        Object catalogObj = AssetDatabase.LoadAssetAtPath<Object>(path);
        if (catalogObj == null) return;

        SerializedObject so = new SerializedObject(catalogObj);
        SetDisplacementVisuals(so.FindProperty("_proteanCones"), coneTn, coneTp, coneTs, null, null, null);
        SetDisplacementVisuals(so.FindProperty("_wingSlash"), wingTn, wingTp, wingTs, null, null, null);
        SetDisplacementVisuals(so.FindProperty("_circle"), circleTn, circleTp, circleTs, null, null, null);
        SetDisplacementVisuals(so.FindProperty("_skySwords"), skyTn, skyTp, skyTs, null, null, null);
        SetDisplacementVisuals(so.FindProperty("_featherLines"), flTn, flTp, flTs, null, null, null);
        SetDisplacementVisuals(so.FindProperty("_orb"), orbTn, orbTp, orbTs, null, null, null);
        SetDisplacementVisuals(so.FindProperty("_bigOrb"), bigOrbTn, bigOrbTp, bigOrbTs, null, null, null);

        SerializedProperty feather = so.FindProperty("_featherColumns");
        if (feather != null)
        {
            feather.FindPropertyRelative("ColumnNormalPrefab").objectReferenceValue = colTn;
            feather.FindPropertyRelative("ColumnPullPrefab").objectReferenceValue = colTp;
            feather.FindPropertyRelative("ColumnPushPrefab").objectReferenceValue = colTs;
            feather.FindPropertyRelative("ColumnNormalAreaPrefab").objectReferenceValue = null;
            feather.FindPropertyRelative("ColumnPullAreaPrefab").objectReferenceValue = null;
            feather.FindPropertyRelative("ColumnPushAreaPrefab").objectReferenceValue = null;
        }

        SerializedProperty lakiInner = so.FindProperty("_lakiRouletteInnerTilePrefabs");
        SerializedProperty lakiOuter = so.FindProperty("_lakiRouletteOuterTilePrefabs");
        if (lakiInner != null && lakiInner.isArray && lakiTiles != null && lakiTiles.Length >= 6)
        {
            lakiInner.arraySize = 3;
            for (int i = 0; i < 3; i++)
                lakiInner.GetArrayElementAtIndex(i).objectReferenceValue = lakiTiles[i];
        }
        if (lakiOuter != null && lakiOuter.isArray && lakiTiles != null && lakiTiles.Length >= 6)
        {
            lakiOuter.arraySize = 3;
            for (int i = 0; i < 3; i++)
                lakiOuter.GetArrayElementAtIndex(i).objectReferenceValue = lakiTiles[3 + i];
        }

        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(catalogObj);
    }

    private static void SetDisplacementVisuals(SerializedProperty prop,
        GameObject normalT, GameObject pullT, GameObject pushT,
        GameObject normalA, GameObject pullA, GameObject pushA)
    {
        if (prop == null) return;
        prop.FindPropertyRelative("NormalTelegraphPrefab").objectReferenceValue = normalT;
        prop.FindPropertyRelative("PullTelegraphPrefab").objectReferenceValue = pullT;
        prop.FindPropertyRelative("PushTelegraphPrefab").objectReferenceValue = pushT;
        prop.FindPropertyRelative("NormalAreaPrefab").objectReferenceValue = normalA;
        prop.FindPropertyRelative("PullAreaPrefab").objectReferenceValue = pullA;
        prop.FindPropertyRelative("PushAreaPrefab").objectReferenceValue = pushA;
    }

    private static GameObject CreateSimpleMeshPrefab(string folder, string prefabName, Mesh mesh, Material material)
    {
        GameObject go = new GameObject(prefabName);
        MeshFilter filter = go.AddComponent<MeshFilter>();
        MeshRenderer renderer = go.AddComponent<MeshRenderer>();
        filter.sharedMesh = mesh;
        renderer.sharedMaterial = material;

        string path = $"{folder}/{prefabName}.prefab";
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
        return prefab;
    }

    private static Material CreateOrGetUnlitMaterial(string folder, string name, Color color)
    {
        string path = $"{folder}/{name}.mat";
        Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null) return existing;

        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Unlit/Color");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        Material mat = new Material(shader);
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
        if (mat.HasProperty("_Color")) mat.color = color;
        mat.enableInstancing = true;
        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }

    private static Material CreateOrGetLitMaterial(string folder, string name, Color color)
    {
        string path = $"{folder}/{name}.mat";
        Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null) return existing;

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Unlit/Color");
        Material mat = new Material(shader);
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
        if (mat.HasProperty("_Color")) mat.color = color;
        mat.enableInstancing = true;
        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }

    private static Mesh CreateOrGetMesh(string name, Mesh source)
    {
        string path = $"{MeshFolder}/{name}.asset";
        Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (existing != null) return existing;
        source.name = name;
        AssetDatabase.CreateAsset(source, path);
        return source;
    }

    private static Mesh BuildDiscMesh(float radius, int segments)
    {
        segments = Mathf.Max(12, segments);
        Mesh mesh = new Mesh();
        Vector3[] verts = new Vector3[segments + 1];
        int[] tris = new int[segments * 3];
        verts[0] = Vector3.zero;
        float step = Mathf.PI * 2f / segments;
        for (int i = 0; i < segments; i++)
        {
            float a = i * step;
            verts[i + 1] = new Vector3(Mathf.Cos(a) * radius, 0f, Mathf.Sin(a) * radius);
        }
        for (int i = 0; i < segments; i++)
        {
            int i0 = 0;
            int i1 = i + 1;
            int i2 = i == segments - 1 ? 1 : i + 2;
            int t = i * 3;
            tris[t] = i0;
            tris[t + 1] = i2;
            tris[t + 2] = i1;
        }
        mesh.vertices = verts;
        mesh.triangles = tris;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private static Mesh BuildConeMesh(float radius, float angleDeg, int sides)
    {
        sides = Mathf.Max(3, sides);
        float half = angleDeg * 0.5f;
        Vector3[] arc = new Vector3[sides + 1];
        for (int i = 0; i <= sides; i++)
        {
            float t = i / (float)sides;
            float a = Mathf.Lerp(-half, half, t) * Mathf.Deg2Rad;
            arc[i] = new Vector3(Mathf.Sin(a) * radius, 0f, Mathf.Cos(a) * radius);
        }

        Vector3[] verts = new Vector3[arc.Length + 1];
        int[] tris = new int[sides * 3];
        verts[0] = Vector3.zero;
        for (int i = 0; i < arc.Length; i++) verts[i + 1] = arc[i];
        int ti = 0;
        for (int i = 1; i < verts.Length - 1; i++)
        {
            tris[ti++] = 0;
            tris[ti++] = i;
            tris[ti++] = i + 1;
        }

        Mesh mesh = new Mesh();
        mesh.vertices = verts;
        mesh.triangles = tris;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private static Mesh BuildStripMesh(float length, float width)
    {
        float hL = length * 0.5f;
        float hW = width * 0.5f;
        Mesh mesh = new Mesh();
        mesh.vertices = new[]
        {
            new Vector3(-hL, 0f, -hW),
            new Vector3(-hL, 0f,  hW),
            new Vector3( hL, 0f,  hW),
            new Vector3( hL, 0f, -hW),
        };
        mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private static void EnsureFolder(string parent, string child)
    {
        string target = $"{parent}/{child}";
        if (!AssetDatabase.IsValidFolder(target))
            AssetDatabase.CreateFolder(parent, child);
    }
}
