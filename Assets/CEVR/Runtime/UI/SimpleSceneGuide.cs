using UnityEngine;

namespace ChulaEarthquakeVR
{
    public sealed class SimpleSceneGuide : MonoBehaviour
    {
        private Camera view;
        private GroundMotionPlayer motion;
        private GameFlowController tutorial;
        private HouseScenarioController house;
        private Transform cover, exit;
        private GUIStyle style;
        private void Start()
        {
            view = Camera.main;
            motion = FindFirstObjectByType<GroundMotionPlayer>();
            tutorial = FindFirstObjectByType<GameFlowController>();
            house = FindFirstObjectByType<HouseScenarioController>();
            CoverZone zone = FindFirstObjectByType<CoverZone>();
            cover = zone == null ? null : zone.transform;
            ExitAssemblyZone assembly = FindFirstObjectByType<ExitAssemblyZone>();
            GameObject houseExit = GameObject.Find("HouseAssemblyPoint");
            exit = assembly != null ? assembly.transform : houseExit == null ? null : houseExit.transform;
        }
        private void OnGUI()
        {
            if (view == null || view.stereoEnabled || (tutorial != null && !tutorial.IsTraining)) return;
            if (house != null && house.HasEnded) return;
            if (tutorial != null && (tutorial.CurrentPhase == GameplayPhase.Success || tutorial.CurrentPhase == GameplayPhase.Failure || tutorial.CurrentPhase == GameplayPhase.Debrief)) return;
            bool evacuate = house != null ? house.IsEvacuating : tutorial != null && tutorial.CurrentPhase == GameplayPhase.PostQuakeEvacuation;
            bool shaking = motion != null && motion.IsPlaying;
            Transform target = evacuate ? exit : cover;
            if (target == null) return;
            Vector3 local = view.transform.InverseTransformPoint(target.position);
            string direction = local.z < 0 ? "TURN AROUND" : local.x < -1 ? "LEFT" : local.x > 1 ? "RIGHT" : "AHEAD";
            string point = evacuate ? "EXIT / ASSEMBLY" : shaking ? "COVER: Z TO CRAWL" : "FIND THE STURDY TABLE";
            if (style == null) style = new GUIStyle(GUI.skin.box) { fontSize = 16, wordWrap = true, alignment = TextAnchor.MiddleCenter };
            float width = Mathf.Min(460, view.pixelWidth - 24);
            float x = view.pixelRect.x + view.pixelWidth / 2;
            GUI.Box(new Rect(x - width / 2, Screen.height - 135, width, 48),
                point + " | " + direction + " | " + Vector3.Distance(view.transform.position, target.position).ToString("0.0") + " m", style);
        }
    }
}
