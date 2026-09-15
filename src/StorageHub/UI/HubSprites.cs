using UnityEngine;

namespace StorageHub.UI
{
    /// <summary>
    /// Tiny UI sprites drawn in code so the hub does not need extra image files
    /// for stars, the preferences cog, and the Deposit / Resupply / Quick Stack
    /// action icons.
    /// </summary>
    internal static class HubSprites
    {
        internal static Sprite StarEmpty { get; private set; }
        internal static Sprite StarFilled { get; private set; }
        internal static Sprite Cog { get; private set; }
        internal static Sprite Deposit { get; private set; }
        internal static Sprite Resupply { get; private set; }
        internal static Sprite QuickStack { get; private set; }

        internal static void Load()
        {
            if (StarFilled != null)
            {
                return;
            }

            StarFilled = MakeSprite(DrawStar(filled: true), "hub_star_filled");
            StarEmpty = MakeSprite(DrawStar(filled: false), "hub_star_empty");
            Cog = MakeSprite(DrawCog(), "hub_cog");
            Deposit = MakeSprite(DrawArrowBox(up: false), "hub_deposit");
            Resupply = MakeSprite(DrawArrowBox(up: true), "hub_resupply");
            QuickStack = MakeSprite(DrawSwapArrows(), "hub_quickstack");
        }

        private static Sprite MakeSprite(Texture2D texture, string name)
        {
            texture.name = name;
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
            var sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                100f);
            sprite.name = name;
            return sprite;
        }

        private static Texture2D DrawStar(bool filled)
        {
            const int size = 48;
            var tex = new Texture2D(size, size, TextureFormat.ARGB32, false);
            var points = StarPoints(size * 0.48f, size * 0.20f, new Vector2(size * 0.5f, size * 0.5f));
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var p = new Vector2(x + 0.5f, y + 0.5f);
                    float a;
                    if (filled)
                    {
                        a = InsidePolygon(p, points) ? 1f : EdgeAlpha(p, points, 1.6f);
                    }
                    else
                    {
                        a = EdgeAlpha(p, points, 2.1f);
                    }

                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            }

            tex.Apply(false, true);
            return tex;
        }

        private static Texture2D DrawCog()
        {
            const int size = 48;
            var tex = new Texture2D(size, size, TextureFormat.ARGB32, false);
            var c = new Vector2(size * 0.5f, size * 0.5f);
            const int teeth = 8;
            var tooth = Mathf.PI / teeth;
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var p = new Vector2(x + 0.5f, y + 0.5f) - c;
                    var r = p.magnitude;
                    var ang = Mathf.Atan2(p.y, p.x);
                    if (ang < 0f)
                    {
                        ang += Mathf.PI * 2f;
                    }

                    var sector = ang % (tooth * 2f);
                    var inTooth = sector < tooth * 0.72f;
                    var outer = inTooth ? size * 0.46f : size * 0.34f;
                    var inner = size * 0.14f;
                    var hub = size * 0.28f;
                    float a = 0f;
                    if (r >= inner && r <= outer)
                    {
                        if (r <= hub || inTooth)
                        {
                            a = 1f;
                            if (r > outer - 1.2f)
                            {
                                a = Mathf.Clamp01(outer - r);
                            }
                            else if (r < inner + 1.2f)
                            {
                                a = Mathf.Clamp01(r - inner);
                            }
                        }
                    }

                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            }

            tex.Apply(false, true);
            return tex;
        }

        private static Texture2D DrawArrowBox(bool up)
        {
            const int size = 48;
            var tex = new Texture2D(size, size, TextureFormat.ARGB32, false);
            var c = new Vector2(size * 0.5f, size * 0.5f);
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var p = new Vector2(x + 0.5f, y + 0.5f);
                    var local = p - c;
                    if (!up)
                    {
                        local.y = -local.y;
                    }

                    float a = 0f;
                    if (local.y > 2f && local.y < 16f && Mathf.Abs(local.x) < 3.2f)
                    {
                        a = 1f;
                    }

                    var tip = local.y + 4f;
                    if (tip >= -10f && tip <= 4f && Mathf.Abs(local.x) <= (4f - tip) * 0.85f + 3.2f)
                    {
                        a = 1f;
                    }

                    if (local.y < -12f && local.y > -20f && Mathf.Abs(local.x) < 14f)
                    {
                        a = 1f;
                    }

                    if (local.y < -6f && local.y > -20f && Mathf.Abs(Mathf.Abs(local.x) - 14f) < 2.2f)
                    {
                        a = 1f;
                    }

                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            }

            tex.Apply(false, true);
            return tex;
        }

        private static Texture2D DrawSwapArrows()
        {
            const int size = 48;
            var tex = new Texture2D(size, size, TextureFormat.ARGB32, false);
            var c = new Vector2(size * 0.5f, size * 0.5f);
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var p = new Vector2(x + 0.5f, y + 0.5f) - c;
                    float a = 0f;
                    if (p.x < -2f)
                    {
                        var q = new Vector2(p.x + 8f, p.y);
                        if (q.y > -2f && q.y < 14f && Mathf.Abs(q.x) < 2.6f)
                        {
                            a = 1f;
                        }

                        if (q.y >= 8f && q.y <= 16f && Mathf.Abs(q.x) <= (16f - q.y) + 2.6f)
                        {
                            a = 1f;
                        }
                    }
                    else if (p.x > 2f)
                    {
                        var q = new Vector2(p.x - 8f, -p.y);
                        if (q.y > -2f && q.y < 14f && Mathf.Abs(q.x) < 2.6f)
                        {
                            a = 1f;
                        }

                        if (q.y >= 8f && q.y <= 16f && Mathf.Abs(q.x) <= (16f - q.y) + 2.6f)
                        {
                            a = 1f;
                        }
                    }

                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            }

            tex.Apply(false, true);
            return tex;
        }

        private static Vector2[] StarPoints(float outer, float inner, Vector2 center)
        {
            var points = new Vector2[10];
            for (var i = 0; i < 10; i++)
            {
                var r = (i & 1) == 0 ? outer : inner;
                var ang = (-90f + i * 36f) * Mathf.Deg2Rad;
                points[i] = center + new Vector2(Mathf.Cos(ang) * r, Mathf.Sin(ang) * r);
            }

            return points;
        }

        private static bool InsidePolygon(Vector2 p, Vector2[] poly)
        {
            var inside = false;
            for (int i = 0, j = poly.Length - 1; i < poly.Length; j = i++)
            {
                var a = poly[i];
                var b = poly[j];
                if (((a.y > p.y) != (b.y > p.y)) &&
                    (p.x < (b.x - a.x) * (p.y - a.y) / ((b.y - a.y) + 0.0001f) + a.x))
                {
                    inside = !inside;
                }
            }

            return inside;
        }

        private static float EdgeAlpha(Vector2 p, Vector2[] poly, float width)
        {
            var best = width + 1f;
            for (var i = 0; i < poly.Length; i++)
            {
                var d = DistanceToSegment(p, poly[i], poly[(i + 1) % poly.Length]);
                if (d < best)
                {
                    best = d;
                }
            }

            return Mathf.Clamp01(width - best);
        }

        private static float DistanceToSegment(Vector2 p, Vector2 a, Vector2 b)
        {
            var ab = b - a;
            var len = ab.sqrMagnitude;
            if (len < 0.0001f)
            {
                return Vector2.Distance(p, a);
            }

            var t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / len);
            return Vector2.Distance(p, a + ab * t);
        }
    }
}
