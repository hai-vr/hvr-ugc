namespace HVR.UGC
{
    public static class HVRUGCHardcodedTypeNames
    {
        // We need this to detect if a jiggle updater is in the scene.
        public const string HVRJiggleUpdate = "HVR.Integration.HVRJiggleUpdate";
        public const string HVRInitializer = "HVR.Integration.HVRInitializer";
        
        // We're exposing those to allow them during asset bundle sanitization.
        public const string UniversalAdditionalLightData = "UnityEngine.Rendering.Universal.UniversalAdditionalLightData";
        public const string TextMeshPro = "TMPro.TextMeshPro";
        public const string TextMeshProUGUI = "TMPro.TextMeshProUGUI";
        public const string JiggleRig = "GatorDragonGames.JigglePhysics.JiggleRig";
        public const string AutomaticFaceTracking = "HVR.Basis.Comms.AutomaticFaceTracking";
    }
}