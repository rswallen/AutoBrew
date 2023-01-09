using AutoBrew.UIElements.Cookbook;
using AutoBrew.UIElements.Importer;
using AutoBrew.UIElements.Misc;
using BepInEx.Logging;
using HarmonyLib;
using PotionCraft.ManagersSystem;
using PotionCraft.ObjectBased.RecipeMap.RecipeMapObject;

namespace AutoBrew.UIElements
{
    internal static class UIManager
    {
        private static ManualLogSource Log => AutoBrewPlugin.Log;

        public static ImportPanel Importer;
        public static CookbookPanel Cookbook;

        [HarmonyPostfix, HarmonyPatch(typeof(RecipeMapObject), "Awake")]
        internal static void Awake_Postfix()
        {
            Setup();
        }

        private static void Setup()
        {
            Importer = ImportPanel.Create();
            Importer.transform.SetParent(Managers.Game.cam.transform, false);

            Cookbook = CookbookPanel.Create();
            Cookbook.transform.SetParent(Managers.Game.cam.transform);
            Cookbook.transform.localPosition = new(8f, -0.5f);

            var cookbookHandle = MoveUIHandle.Create("DescriptionWindow", 1000);
            cookbookHandle.ReplaceLink(Managers.Game.cam.transform, Cookbook.transform, new(3.7f, 4.7f));
            cookbookHandle.IsActive = true;
        }
    }
}
