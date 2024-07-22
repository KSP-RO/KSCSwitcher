using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace regexKSP
{
    [KSPAddon(KSPAddon.Startup.MainMenu, false)]
    public class ScenarioSpawn : MonoBehaviour
    {
        void Start()
        {
            KSCLoader.instance ??= new KSCLoader();
            enabled = false;
        }
    }

    public class KSCLoader
    {
        public static KSCLoader instance = null;
        public KSCSiteManager Sites = new KSCSiteManager();

        private void OnGameStateCreated(Game game)
        {
            LastKSC.CreateSettings(game);
            if (HighLogic.LoadedScene == GameScenes.SPACECENTER)
            {
                ProtoScenarioModule m = HighLogic.CurrentGame.scenarios.FirstOrDefault(m => m.moduleName == "LastKSC");

                if (m == null) return;

                LastKSC l = (LastKSC)m.Load(ScenarioRunner.Instance);
                bool noSite;
                if (!string.IsNullOrEmpty(l.lastSite))
                {
                    // found a site, load it
                    ConfigNode site = Sites.GetSiteByName(l.lastSite);
                    if (site == null)
                    {
                        l.lastSite = Sites.defaultSite;
                        noSite = true;
                    }
                    else
                    {
                        KSCSwitcher.SetSiteAndResetCamera(site);
                        Debug.Log("[KSCSwitcher] set the launch site to the last site, " + l.lastSite);
                        return;
                    }
                }
                else
                {
                    l.lastSite = Sites.defaultSite;
                    noSite = true;
                }
                if (noSite)
                {
                    if (!string.IsNullOrEmpty(Sites.defaultSite))
                    {
                        ConfigNode site = Sites.GetSiteByName(Sites.defaultSite);
                        if (site == null)
                        {
                            Debug.LogError("[KSCSwitcher] found a default site name but could not retrieve the site config: " + Sites.defaultSite);
                            return;
                        }
                        else
                        {
                            KSCSwitcher.SetSiteAndResetCamera(site);
                            Debug.Log("[KSCSwitcher] set the initial launch site to the default" + Sites.defaultSite);
                        }
                    }
                }
            }
        }

        public KSCLoader()
        {
            GameEvents.onGameStateCreated.Add(OnGameStateCreated);
        }
    }
}
