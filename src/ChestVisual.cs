using UnityEngine;

namespace Quartermaster;

// Reversible decoration on the actual vanilla chest: every storage tier retains its model,
// build cost, capacity, network identity and normal item drops when dismantled.
public sealed class ChestVisual : MonoBehaviour
{
    private Container chest;
    private GameObject ornament;
    private LineRenderer[] gears;
    private Material material;
    private Material runeMaterial;
    private Mesh runeMesh;
    private Material haloMaterial;
    private Texture2D haloTexture;
    private Mesh runeHaloMesh;
    private Light[] ornamentLights;
    private GameObject receiptEffect;
    private LineRenderer receiptOutline;
    private Bounds chestBounds;
    private bool measured;
    private const float ReceiptDuration = 3f;
    private float pulseUntil;
    private float nextCheck;
    private bool deposit;
    internal static void Pulse(Container c)
    {
        var art = c ? c.GetComponent<ChestVisual>() : null;
        if (art) art.pulseUntil = Time.time + ReceiptDuration;
    }
    private void Awake() { chest = GetComponent<Container>(); }
    private void Update()
    {
        if (!chest || !Plugin.Enabled.Value)
        {
            if (ornament) ornament.SetActive(false);
            if (receiptEffect) receiptEffect.SetActive(false);
            return;
        }
        UpdateReceipt();
        if (Time.time >= nextCheck)
        {
            nextCheck = Time.time + .5f;
            deposit = ContainerRegistry.GetSettings(chest).Deposit;
            if (deposit && !ornament) Create();
            if (ornament) ornament.SetActive(deposit);
        }
        if (!ornament || !deposit) return;
        bool blocked = Automation.Status(chest).Contains("full") || Automation.Status(chest).Contains("No destination");
        Color color = blocked ? new Color(1f, .43f, .08f, .9f) : new Color(.22f, .85f, 1f, .9f);
        if (Time.time < pulseUntil) color *= 1.3f + .25f * Mathf.Sin(Time.time * 9);
        if (runeMaterial) runeMaterial.color = color;
        if (haloMaterial) haloMaterial.color = new Color(color.r, color.g, color.b, .28f);
        bool near = Player.m_localPlayer && (Player.m_localPlayer.transform.position - transform.position).sqrMagnitude < 625f;
        foreach (var light in ornamentLights)
        {
            light.enabled = near;
            light.color = color;
            light.intensity = Time.time < pulseUntil ? .4f : .28f;
        }
        for (int i = 0; i < gears.Length; i++)
        {
            gears[i].startColor = gears[i].endColor = color;
            gears[i].transform.localRotation = Quaternion.Euler(0, 0, Time.time * (i == 0 ? 12 : -18));
        }
    }
    private Bounds MeasureChest()
    {
        if (measured) return chestBounds;
        bool found = false; Bounds bounds = new Bounds();
        foreach (var r in GetComponentsInChildren<MeshRenderer>(true))
        {
            if (chest.m_open && r.transform.IsChildOf(chest.m_open.transform)) continue;
            if (!r.GetComponent<MeshFilter>() || !r.GetComponent<MeshFilter>().sharedMesh) continue;
            if (!r.enabled || r.forceRenderingOff || r.transform == chest.GetComponent<QuartermasterChestModel>()?.Lid) continue;
            var local = r.GetComponent<MeshFilter>().sharedMesh.bounds;
            for (int i = 0; i < 8; i++)
            {
                var p = local.center + Vector3.Scale(local.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                p = transform.InverseTransformPoint(r.transform.TransformPoint(p));
                if (!found) { bounds = new Bounds(p, Vector3.zero); found = true; } else bounds.Encapsulate(p);
            }
        }
        if (!found) bounds = new Bounds(new Vector3(0, .5f, 0), Vector3.one);
        chestBounds = bounds; measured = true;
        return bounds;
    }
    private void EnsureMaterial()
    {
        if (!material) material = new Material(Shader.Find("Sprites/Default"));
    }
    private void UpdateReceipt()
    {
        bool active = Time.time < pulseUntil;
        if (!active) { if (receiptEffect) receiptEffect.SetActive(false); return; }
        if (!receiptEffect)
        {
            var bounds = MeasureChest(); bounds.Expand(.055f);
            EnsureMaterial();
            receiptEffect = new GameObject("Quartermaster_ItemsReceived");
            receiptEffect.transform.SetParent(transform, false);
            receiptOutline = receiptEffect.AddComponent<LineRenderer>();
            receiptOutline.sharedMaterial = material; receiptOutline.useWorldSpace = false;
            receiptOutline.widthMultiplier = .014f; receiptOutline.numCornerVertices = 2;
            // Trace the box edges in one renderer, including short retraces between faces.
            var corners = new Vector3[8];
            for (int i = 0; i < corners.Length; i++)
                corners[i] = new Vector3((i == 1 || i == 2 || i == 5 || i == 6) ? bounds.max.x : bounds.min.x,
                    i >= 4 ? bounds.max.y : bounds.min.y,
                    (i == 2 || i == 3 || i == 6 || i == 7) ? bounds.max.z : bounds.min.z);
            int[] route = { 0, 1, 2, 3, 0, 4, 5, 1, 5, 6, 2, 6, 7, 3, 7, 4 };
            receiptOutline.positionCount = route.Length;
            for (int i = 0; i < route.Length; i++) receiptOutline.SetPosition(i, corners[route[i]]);
            receiptOutline.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            receiptOutline.receiveShadows = false;
        }
        receiptEffect.SetActive(true);
        float remaining = Mathf.Clamp01((pulseUntil - Time.time) / ReceiptDuration);
        var color = new Color(.22f, .85f, 1f, .9f * remaining);
        receiptOutline.startColor = receiptOutline.endColor = color;
    }
    private void Create()
    {
        var bounds = MeasureChest();
        DepositGull.Attach(chest, bounds);
        if (chest.GetComponent<QuartermasterChestModel>())
        {
            // The dedicated coffer has its own trim and owl plate.
            ornament = new GameObject("Quartermaster dedicated chest"); ornament.transform.SetParent(transform, false);
            gears = new LineRenderer[0]; ornamentLights = new Light[0];
            return;
        }
        ornament = new GameObject("Quartermaster_DepositGears"); ornament.transform.SetParent(transform, false);
        // Decorate both long faces so orientation and chest variants remain readable.
        gears = new LineRenderer[4];
        EnsureMaterial();
        // A feathered cross-section creates a soft halo even with bloom disabled.
        haloTexture = new Texture2D(1, 64, TextureFormat.RGBA32, false);
        haloTexture.name = "Quartermaster_SoftGlow";
        haloTexture.wrapMode = TextureWrapMode.Clamp; haloTexture.filterMode = FilterMode.Bilinear;
        var pixels = new Color[64];
        for (int y = 0; y < pixels.Length; y++)
        {
            float distance = Mathf.Abs(y / 63f * 2f - 1f);
            float alpha = Mathf.Pow(1f - distance * distance, 3f);
            pixels[y] = new Color(1f, 1f, 1f, alpha);
        }
        haloTexture.SetPixels(pixels); haloTexture.Apply(false, true);
        haloMaterial = new Material(material) { mainTexture = haloTexture };
        float radius = Mathf.Clamp(Mathf.Min(bounds.size.x * .15f, bounds.size.y * .2f), .045f, .22f);
        for (int i = 0; i < gears.Length; i++)
        {
            var go = new GameObject("RunicGear"); go.transform.SetParent(ornament.transform, false);
            go.transform.localPosition = new Vector3(bounds.center.x + (i % 2 == 0 ? -radius : radius), bounds.center.y,
                i < 2 ? bounds.min.z - .018f : bounds.max.z + .018f);
            var line = go.AddComponent<LineRenderer>(); line.sharedMaterial = material; line.useWorldSpace = false; line.loop = true;
            line.widthMultiplier = .008f; line.positionCount = 64; line.numCornerVertices = 1;
            float scale = i % 2 == 0 ? 1f : .72f;
            for (int j = 0; j < 64; j++)
            {
                float a = j * Mathf.PI * 2 / 64; float r = radius * scale * (j % 4 == 1 || j % 4 == 2 ? 1 : .78f);
                line.SetPosition(j, new Vector3(Mathf.Cos(a) * r, Mathf.Sin(a) * r, 0));
            }
            gears[i] = line;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            var glow = new GameObject("GearSoftGlow"); glow.transform.SetParent(go.transform, false);
            var halo = glow.AddComponent<LineRenderer>();
            halo.sharedMaterial = haloMaterial; halo.useWorldSpace = false; halo.loop = true;
            halo.widthMultiplier = .045f; halo.positionCount = line.positionCount; halo.numCornerVertices = 3;
            for (int j = 0; j < line.positionCount; j++) halo.SetPosition(j, line.GetPosition(j));
            halo.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            halo.receiveShadows = false;
        }
        runeMaterial = new Material(material);
        runeMesh = RuneInscription.CreateMesh();
        runeHaloMesh = RuneInscription.CreateMesh(true);
        ornamentLights = new Light[2];
        float height = Mathf.Min(bounds.size.y * .16f, bounds.size.x * .82f / RuneInscription.Width);
        for (int face = 0; face < 2; face++)
        {
            var go = new GameObject("DepositInscription_" + RuneInscription.Text);
            go.transform.SetParent(ornament.transform, false);
            go.transform.localPosition = new Vector3(bounds.center.x, bounds.min.y + bounds.size.y * .12f,
                face == 0 ? bounds.min.z - .022f : bounds.max.z + .022f);
            // Read left-to-right from outside either face, rather than mirroring the back.
            go.transform.localRotation = Quaternion.Euler(0, face == 0 ? 0 : 180, 0);
            go.transform.localScale = Vector3.one * height;
            go.AddComponent<MeshFilter>().sharedMesh = runeMesh;
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = runeMaterial;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            var glow = new GameObject("InscriptionSoftGlow"); glow.transform.SetParent(go.transform, false);
            glow.AddComponent<MeshFilter>().sharedMesh = runeHaloMesh;
            var glowRenderer = glow.AddComponent<MeshRenderer>(); glowRenderer.sharedMaterial = haloMaterial;
            glowRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            glowRenderer.receiveShadows = false;
            var spill = new GameObject("RunicIllumination"); spill.transform.SetParent(ornament.transform, false);
            spill.transform.localPosition = new Vector3(bounds.center.x, bounds.min.y + bounds.size.y * .4f,
                face == 0 ? bounds.min.z - .15f : bounds.max.z + .15f);
            var light = spill.AddComponent<Light>(); light.type = LightType.Point;
            light.range = Mathf.Clamp(bounds.size.magnitude * .8f, .8f, 2.3f);
            light.intensity = .28f; light.shadows = LightShadows.None;
            light.renderMode = LightRenderMode.Auto;
            ornamentLights[face] = light;
        }
    }
    private void OnDestroy()
    {
        if (material) Destroy(material);
        if (runeMaterial) Destroy(runeMaterial);
        if (runeMesh) Destroy(runeMesh);
        if (runeHaloMesh) Destroy(runeHaloMesh);
        if (haloMaterial) Destroy(haloMaterial);
        if (haloTexture) Destroy(haloTexture);
        if (ornament) Destroy(ornament);
        if (receiptEffect) Destroy(receiptEffect);
        if (chest) ContainerRegistry.Unregister(chest);
    }
}
