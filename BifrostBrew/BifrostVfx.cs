using Jotunn.Managers;
using UnityEngine;
using UnityEngine.Rendering;

namespace BifrostBrew
{
    // Builds the Bifrost visual effects at runtime from vanilla particle materials and textures.
    //  - Channel: a rune-ringed portal opens under the drinker while blue motes spiral up around them.
    //  - Depart: a burst of light, sparks and a shockwave where the player leaves from.
    internal static class BifrostVfx
    {
        // Cloned for its networking setup (ZNetView + TimedDestruction): status-effect start
        // effects must have a ZNetView, because SEMan destroys them through it when the effect ends.
        // Being networked also makes the effects visible to other players.
        private const string NetworkedBase = "vfx_Potion_stamina_medium";

        private static readonly Color Cyan = new Color(0.45f, 0.85f, 1f);
        private static readonly Color Blue = new Color(0.3f, 0.5f, 1f);
        private static readonly Color Violet = new Color(0.7f, 0.45f, 1f);

        private static Material s_glow;   // soft round glow ("point")
        private static Material s_ring;   // ring ("shockwave")
        private static Material s_spark;  // four-point star ("starspark")
        private static Material s_rune;   // 2x2 sheet of runes ("PortalRunes")

        public static GameObject CreateChannelEffect()
        {
            if (!LoadMaterials())
                return null;

            GameObject root = CreateNetworkedRoot("vfx_BifrostChannel", 45f);

            // Spawned at the drinker's center; BifrostChannelVfx moves "portal" down to their feet.
            Transform portal = new GameObject("portal").transform;
            portal.SetParent(root.transform, false);

            var glow = AddParticles(portal, "portal_glow", s_glow, ParticleSystemRenderMode.HorizontalBillboard);
            Configure(glow, lifetime: (1.2f, 1.2f), size: (3.2f, 3.4f), rate: 4f);
            SetColor(glow, Blue, Cyan, 0.3f, 0.7f, maxAlpha: 0.6f);
            Spin(glow, 15f);

            var ring = AddParticles(portal, "portal_ring", s_ring, ParticleSystemRenderMode.HorizontalBillboard);
            Configure(ring, lifetime: (1.6f, 1.6f), size: (2.8f, 2.8f), rate: 2.5f);
            SetColor(ring, Cyan, Blue, 0.25f, 0.6f, maxAlpha: 0.9f);
            Grow(ring, 0.9f, 1.05f);
            Spin(ring, 40f);

            var pulse = AddParticles(portal, "portal_pulse", s_ring, ParticleSystemRenderMode.HorizontalBillboard);
            Configure(pulse, lifetime: (1.2f, 1.2f), size: (2.4f, 2.4f), rate: 1.2f);
            SetColor(pulse, Cyan, Violet, 0.05f, 0.35f, maxAlpha: 0.8f);
            Grow(pulse, 0.3f, 1.6f);

            var runes = AddParticles(portal, "portal_runes", s_rune, ParticleSystemRenderMode.Billboard);
            Configure(runes, lifetime: (2.6f, 2.6f), size: (0.22f, 0.32f), rate: 10f);
            Circle(runes, 1.25f, 0f);
            Orbit(runes, orbitY: 0.9f, upward: 0.25f, radial: 0f);
            SetColor(runes, Cyan, Blue, 0.2f, 0.7f, maxAlpha: 1f);
            var sheet = runes.textureSheetAnimation;
            sheet.enabled = true;
            sheet.mode = ParticleSystemAnimationMode.Grid;
            sheet.numTilesX = 2;
            sheet.numTilesY = 2;
            sheet.animation = ParticleSystemAnimationType.WholeSheet;
            sheet.frameOverTime = new ParticleSystem.MinMaxCurve(0f, 0.999f); // a random rune each
            sheet.cycleCount = 1;

            var swirl = AddParticles(portal, "swirl", s_glow, ParticleSystemRenderMode.Billboard);
            Configure(swirl, lifetime: (2.2f, 2.8f), size: (0.06f, 0.13f), rate: 70f);
            Circle(swirl, 0.85f, 0.15f);
            Orbit(swirl, orbitY: 3.2f, upward: 0.9f, radial: -0.15f);
            SetColor(swirl, Cyan, Blue, Violet, 0.1f, 0.65f);
            Trails(swirl, 0.7f, 0.35f);

            var outer = AddParticles(portal, "swirl_outer", s_glow, ParticleSystemRenderMode.Billboard);
            Configure(outer, lifetime: (2.8f, 3.2f), size: (0.1f, 0.2f), rate: 30f);
            Circle(outer, 1.4f, 0.2f);
            Orbit(outer, orbitY: -1.6f, upward: 0.5f, radial: 0f);
            SetColor(outer, Violet, Blue, Cyan, 0.2f, 0.6f);

            var sparkles = AddParticles(root.transform, "sparkles", s_spark, ParticleSystemRenderMode.Billboard);
            Configure(sparkles, lifetime: (0.6f, 1.1f), size: (0.15f, 0.35f), rate: 30f);
            Sphere(sparkles, 1.1f);
            SetColor(sparkles, Color.white, Cyan, 0.15f, 0.5f, maxAlpha: 1f);
            Spin(sparkles, 90f);

            AddLight(root.transform, "light", Cyan, 7f);
            root.AddComponent<BifrostChannelVfx>();

            PrefabManager.Instance.AddPrefab(root);
            return root;
        }

        public static GameObject CreateDepartEffect()
        {
            if (!LoadMaterials())
                return null;

            GameObject root = CreateNetworkedRoot("vfx_BifrostDepart", 4f);

            var burst = AddParticles(root.transform, "burst", s_glow, ParticleSystemRenderMode.Billboard, new Vector3(0f, 1f, 0f));
            Configure(burst, lifetime: (0.8f, 1.6f), size: (0.06f, 0.14f), rate: 0f, loop: false);
            Burst(burst, (0f, 160));
            Sphere(burst, 0.6f);
            var main = burst.main;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startSpeed = new ParticleSystem.MinMaxCurve(2f, 6f);
            main.gravityModifier = -0.3f;
            main.startColor = new ParticleSystem.MinMaxGradient(Cyan, Violet);
            SetColor(burst, Color.white, Color.white, 0.02f, 0.5f, maxAlpha: 1f);
            Trails(burst, 0.6f, 0.25f);

            var pillar = AddParticles(root.transform, "pillar", s_glow, ParticleSystemRenderMode.VerticalBillboard, new Vector3(0f, 4f, 0f));
            Configure(pillar, lifetime: (0.9f, 0.9f), size: (1f, 1f), rate: 0f, loop: false);
            Burst(pillar, (0f, 3));
            main = pillar.main;
            main.startSize3D = true;
            main.startSizeX = 1.4f;
            main.startSizeY = 9f;
            main.startSizeZ = 1f;
            var pillarSize = pillar.sizeOverLifetime;
            pillarSize.enabled = true;
            pillarSize.separateAxes = true;
            pillarSize.x = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.2f));
            pillarSize.y = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 1.2f));
            pillarSize.z = new ParticleSystem.MinMaxCurve(1f);
            SetColor(pillar, Color.white, Cyan, 0.05f, 0.4f, maxAlpha: 0.9f);

            var shockwave = AddParticles(root.transform, "shockwave", s_ring, ParticleSystemRenderMode.HorizontalBillboard, new Vector3(0f, 0.1f, 0f));
            Configure(shockwave, lifetime: (0.7f, 0.7f), size: (1.5f, 1.5f), rate: 0f, loop: false);
            Burst(shockwave, (0f, 1), (0.15f, 1));
            Grow(shockwave, 0.4f, 4f);
            SetColor(shockwave, Color.white, Cyan, 0.02f, 0.3f, maxAlpha: 1f);

            var sparkles = AddParticles(root.transform, "sparkles", s_spark, ParticleSystemRenderMode.Billboard, new Vector3(0f, 1f, 0f));
            Configure(sparkles, lifetime: (0.4f, 0.9f), size: (0.2f, 0.45f), rate: 0f, loop: false);
            Burst(sparkles, (0f, 40));
            Sphere(sparkles, 1.5f);
            SetColor(sparkles, Color.white, Cyan, 0.05f, 0.5f, maxAlpha: 1f);

            AddLight(root.transform, "light", Cyan, 10f).transform.localPosition = new Vector3(0f, 1f, 0f);
            root.AddComponent<BifrostDepartVfx>();

            PrefabManager.Instance.AddPrefab(root);
            return root;
        }

        private static GameObject CreateNetworkedRoot(string name, float lifetime)
        {
            GameObject root = PrefabManager.Instance.CreateClonedPrefab(name, NetworkedBase);

            // Drop the stamina potion's own particles and light; keep the root's network components.
            foreach (Transform child in Children(root.transform))
                Object.DestroyImmediate(child.gameObject);

            if (root.GetComponent<ZNetView>() == null)
                root.AddComponent<ZNetView>();

            // Safety net if the status effect never removes it (normally SEMan does).
            var timed = root.GetComponent<TimedDestruction>();
            if (timed == null)
                timed = root.AddComponent<TimedDestruction>();
            timed.m_timeout = lifetime;
            timed.m_triggerOnAwake = true;
            return root;
        }

        private static Transform[] Children(Transform parent)
        {
            var children = new Transform[parent.childCount];
            for (int i = 0; i < children.Length; i++)
                children[i] = parent.GetChild(i);
            return children;
        }

        // --- Materials -------------------------------------------------------------------------

        private static bool LoadMaterials()
        {
            if (s_glow != null)
                return true;

            // All use Legacy Shaders/Particles/Alpha Blended; particle colors come from vertex colors.
            Material spark = PrefabManager.Cache.GetPrefab<Material>("item_particle");
            Material glow = PrefabManager.Cache.GetPrefab<Material>("light_glow") ?? spark;
            Material ring = PrefabManager.Cache.GetPrefab<Material>("glow_pulse") ?? glow;
            if (glow == null)
            {
                Jotunn.Logger.LogWarning("Bifrost effects disabled: vanilla particle materials not found");
                return false;
            }

            s_glow = CopyMaterial(glow, "bifrost_glow");
            s_ring = CopyMaterial(ring, "bifrost_ring");
            s_spark = CopyMaterial(spark ?? glow, "bifrost_spark");
            s_rune = CopyMaterial(glow, "bifrost_rune");

            Texture2D runes = PrefabManager.Cache.GetPrefab<Texture2D>("PortalRunes");
            if (runes != null && !BifrostBrewPlugin.IsHeadless)
                s_rune.mainTexture = RunesWithAlpha(runes);
            return true;
        }

        private static Material CopyMaterial(Material source, string name)
        {
            var material = new Material(source) { name = name };
            // The legacy particle shader doubles _TintColor; (1, 1, 1, 0.5) lets bright particles
            // exceed 1 and bloom, with alpha unchanged.
            if (material.HasProperty("_TintColor"))
                material.SetColor("_TintColor", new Color(1f, 1f, 1f, 0.5f));
            return material;
        }

        // PortalRunes is a single-channel (BC4) texture; the alpha-blended shader needs the runes in
        // alpha. Make a white copy with alpha taken from the red channel.
        private static Texture2D RunesWithAlpha(Texture2D runes)
        {
            Texture2D tex = BifrostBrewPlugin.CopyRegion(runes, new Rect(0, 0, runes.width, runes.height), true);
            tex.wrapMode = TextureWrapMode.Clamp;
            var pixels = tex.GetPixels();
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = new Color(1f, 1f, 1f, pixels[i].r);
            tex.SetPixels(pixels);
            tex.Apply(true, true);
            return tex;
        }

        // --- Particle system helpers -----------------------------------------------------------

        private static ParticleSystem AddParticles(Transform parent, string name, Material material,
            ParticleSystemRenderMode renderMode, Vector3 localPosition = default)
        {
            var go = new GameObject(name);
            // Parent first: the prefab hierarchy is inactive, so the system doesn't start playing.
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;

            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.playOnAwake = true;
            main.loop = true;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            main.startSpeed = 0f;
            main.startColor = Color.white;
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.maxParticles = 500;

            var shape = ps.shape;
            shape.enabled = false;

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material;
            renderer.renderMode = renderMode;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return ps;
        }

        private static void Configure(ParticleSystem ps, (float min, float max) lifetime, (float min, float max) size,
            float rate, bool loop = true)
        {
            var main = ps.main;
            main.loop = loop;
            main.duration = loop ? 5f : 1f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(lifetime.min, lifetime.max);
            main.startSize = new ParticleSystem.MinMaxCurve(size.min, size.max);
            var emission = ps.emission;
            emission.rateOverTime = rate;
        }

        private static void Burst(ParticleSystem ps, params (float time, short count)[] bursts)
        {
            var array = new ParticleSystem.Burst[bursts.Length];
            for (int i = 0; i < bursts.Length; i++)
                array[i] = new ParticleSystem.Burst(bursts[i].time, bursts[i].count);
            ps.emission.SetBursts(array);
        }

        // Ring lying flat on the ground (the Circle shape is in the XY plane by default).
        private static void Circle(ParticleSystem ps, float radius, float thickness)
        {
            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = radius;
            shape.radiusThickness = thickness;
            shape.rotation = new Vector3(90f, 0f, 0f);
        }

        private static void Sphere(ParticleSystem ps, float radius)
        {
            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = radius;
        }

        // Swirl around the vertical axis (radians per second) while drifting up.
        private static void Orbit(ParticleSystem ps, float orbitY, float upward, float radial)
        {
            var velocity = ps.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.Local;
            // All three axes must use the same curve mode.
            velocity.x = 0f;
            velocity.y = upward;
            velocity.z = 0f;
            velocity.orbitalX = 0f;
            velocity.orbitalY = orbitY;
            velocity.orbitalZ = 0f;
            velocity.radial = radial;
        }

        private static void Spin(ParticleSystem ps, float degreesPerSecond)
        {
            var rotation = ps.rotationOverLifetime;
            rotation.enabled = true;
            rotation.z = degreesPerSecond * Mathf.Deg2Rad;
        }

        private static void Grow(ParticleSystem ps, float from, float to)
        {
            var size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, from, 1f, to));
        }

        private static void Trails(ParticleSystem ps, float ratio, float lifetime)
        {
            var trails = ps.trails;
            trails.enabled = true;
            trails.ratio = ratio;
            trails.lifetime = lifetime;
            trails.dieWithParticles = true;
            trails.sizeAffectsWidth = true;
            trails.inheritParticleColor = true;
            trails.widthOverTrail = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));
            ps.GetComponent<ParticleSystemRenderer>().trailMaterial = ps.GetComponent<ParticleSystemRenderer>().sharedMaterial;
        }

        // Color from a to b over each particle's life, fading in until fadeIn and out after fadeOut.
        private static void SetColor(ParticleSystem ps, Color a, Color b, float fadeIn, float fadeOut, float maxAlpha)
        {
            SetGradient(ps, new[] { new GradientColorKey(a, 0f), new GradientColorKey(b, 1f) }, fadeIn, fadeOut, maxAlpha);
        }

        private static void SetColor(ParticleSystem ps, Color a, Color b, Color c, float fadeIn, float fadeOut)
        {
            SetGradient(ps, new[] { new GradientColorKey(a, 0f), new GradientColorKey(b, 0.5f), new GradientColorKey(c, 1f) },
                fadeIn, fadeOut, 1f);
        }

        private static void SetGradient(ParticleSystem ps, GradientColorKey[] colors, float fadeIn, float fadeOut, float maxAlpha)
        {
            var gradient = new Gradient();
            gradient.SetKeys(colors, new[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(maxAlpha, fadeIn),
                new GradientAlphaKey(maxAlpha, fadeOut),
                new GradientAlphaKey(0f, 1f)
            });
            var color = ps.colorOverLifetime;
            color.enabled = true;
            color.color = new ParticleSystem.MinMaxGradient(gradient);
        }

        private static Light AddLight(Transform parent, string name, Color color, float range)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.range = range;
            light.intensity = 0f;
            light.shadows = LightShadows.None;
            return light;
        }
    }

    // Opens the portal under the drinker's feet and pulses the light while the channel runs.
    public class BifrostChannelVfx : MonoBehaviour
    {
        private const float OpenTime = 0.9f;

        private Transform m_portal;
        private Light m_light;
        private float m_time;

        private void Awake()
        {
            m_portal = transform.Find("portal");
            m_light = GetComponentInChildren<Light>();
        }

        private void Start()
        {
            PlaceOnGround();
            Apply(0f);
        }

        private void Update()
        {
            m_time += Time.deltaTime;
            Apply(m_time);
        }

        private void Apply(float t)
        {
            float open = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / OpenTime));
            if (m_portal != null)
                m_portal.localScale = Vector3.one * Mathf.Max(0.01f, open);
            if (m_light != null)
                m_light.intensity = open * (2.2f + 0.5f * Mathf.Sin(t * 5f));
        }

        // The effect spawns at the drinker's center (about waist height). For the drinker it is
        // attached to their transform, whose origin is at their feet. Other clients spawn it
        // unattached, so find the ground below instead.
        private void PlaceOnGround()
        {
            if (m_portal == null)
                return;

            float feet;
            if (transform.parent != null)
                feet = transform.parent.position.y;
            else if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, 3f,
                         LayerMask.GetMask("Default", "static_solid", "terrain", "piece", "vehicle")))
                feet = hit.point.y;
            else
                feet = transform.position.y - 0.9f;

            m_portal.position = new Vector3(transform.position.x, feet + 0.05f, transform.position.z);
        }
    }

    // Fades the departure flash.
    public class BifrostDepartVfx : MonoBehaviour
    {
        private const float FlashTime = 0.8f;

        private Light m_light;
        private float m_time;

        private void Awake()
        {
            m_light = GetComponentInChildren<Light>();
        }

        private void Update()
        {
            m_time += Time.deltaTime;
            if (m_light == null)
                return;

            float fade = 1f - Mathf.Clamp01(m_time / FlashTime);
            m_light.intensity = 8f * fade * fade;
        }
    }
}
