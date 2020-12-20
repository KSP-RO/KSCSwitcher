using System;
using UniLinq;
using UnityEngine;

namespace regexKSP
{
    public class GrassSeasoner
    {
        public static Material[] KSCGrassMaterials { get; private set; } = null;
        public static GrassSeasoner Instance;
        private Color originalGrassColor;
        public static Color GroundColor
        {
            get
            {
                if (Instance?.originalGrassColor is Color col)
                {
                    return col;
                }
                else if (TryParseGroundColor(KSCSwitcher.KSCBody, KSCSwitcher.FindKSC().lat, KSCSwitcher.FindKSC().lon, out col))
                {
                    return col;
                }

                return new Color();
            }
        }

        public static void SetGrassColor(Color newColor)
        {
            KSCGrassMaterials ??= Resources.FindObjectsOfTypeAll<Material>().Where(m => m.shader.name.Contains("KSC")).ToArray();

            for (int i = KSCGrassMaterials.Length; i-- > 0;)
            {
                KSCGrassMaterials[i].SetColor("_GrassColor", newColor);
            }
        }

        public static bool TryGetKSCGrassColor(CelestialBody home, PQSCity ksc, ConfigNode pqsCity, out Color col)
        {
            col = new Color();
            if (pqsCity.HasValue("changeGrassColor"))
            {
                if (bool.TryParse(pqsCity.GetValue("changeGrassColor"), out bool btmp) && btmp)
                {
                    if (pqsCity.HasValue("grassColor"))
                    {
                        if (pqsCity.TryGetValue("grassColor", ref col))
                        {
                            Debug.Log($"KSCSwitcher found KSC grass color {col} from config");
                            return true;
                        }
                    }
                    else if (TryParseGroundColor(home, ksc.lat, ksc.lon, out col, 2f))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public static bool TryParseGroundColor(CelestialBody body, double lat, double lon, out Color col, float colorMult = 1f)
        {
            col = new Color();
            // GetPixelColor(int x, int y) returns the color of the pixel of coordinates (x,y),
            // where (0,0) identifies the bottom right corner and (width, height) matches the top left corner;
            // KSP maps are both horizontally and vertically flipped, and longitude has a 1/4 width offset;
            // maps are flipped vertically again when stored in MAPSO;
            // therefore:
            // latitude = +90 =>    y = height
            // latitude =   0 =>    y = height/2
            // latitude = -90 =>    y = 0
            // and:
            // longitude = -180 =>    x = 3/4 * width
            // longitude =  -90 =>    x = 1/2 * width
            // longitude =    0 =>    x = 1/4 * width
            // longitude =  +90 =>    x = 0
            // longitude = +180 =>    x = 3/4 * width

            if (FindColorMap(body) is MapSO texture)
            {
                int x = Convert.ToInt32((90 - lon) / 360 * texture.Width);
                int y = Convert.ToInt32((90 + lat) / 180 * texture.Height);

                x = x > 0 ? x : texture.Width + x;

                x = Mathf.Clamp(x, 0, texture.Width);
                y = Mathf.Clamp(y, 0, texture.Height);

                col = texture.GetPixelColor(x, y);
                Debug.Log($"KSCSwitcher parsed {col} from color map at {x}, {y}");
                col *= colorMult;
                return true;
            }

            return false;
        }

        public static MapSO FindColorMap(CelestialBody body)
        {
            Transform t;

            t = body?.pqsController?.transform?.Find("VertexColorMapBlend");
            var mod = t?.GetComponent<PQSMod_VertexColorMapBlend>();
            if (mod?.vertexColorMap is MapSO map)
                return map;

            // if VertexColorMapBlend is not there, try with VertexColorMap
            t = body.pqsController?.transform?.Find("VertexColorMap");
            var mod2 = t?.GetComponent<PQSMod_VertexColorMap>();
            if (mod2?.vertexColorMap is MapSO map2)
                return map2;

            var mods = Resources.FindObjectsOfTypeAll<PQSMod_VertexColorMapBlend>();
            return mods.FirstOrDefault(m => m.sphere.PQSModCBTransform.body == body)?.vertexColorMap;
        }
    }

    [KSPAddon(KSPAddon.Startup.EditorAny, false)]
    public class EditorGrassFixer : MonoBehaviour
    {
        public void Start()
        {
            GameObject scenery = GameObject.Find("VABscenery") ?? GameObject.Find("SPHscenery");
            Material material = scenery?.GetChild("ksc_terrain")?.GetComponent<Renderer>()?.sharedMaterial;

            if (material == null)
            {
                return;
            }

            material.color = GrassSeasoner.GroundColor * 1.5f;
        }
    }
}
