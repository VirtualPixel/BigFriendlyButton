using UnityEngine;

namespace BigFriendlyButton.Services
{
    // writes the tube screen the same way the game does when it changes it, minus the blip when silent
    internal static class TubeScreen
    {
        public static void Show(ExtractionPoint point, string text, Color color, bool silent = false)
        {
            if (point.tubeScreenTextString == text)
                return;
            if (!silent)
                point.soundActivate2.Play(point.transform.position);
            point.tubeScreenTextString = text;
            point.tubeScreenChangeTimer = 0.2f;
            point.tubeScreenText.isRightToLeftText = false;
            point.tubeScreenText.color = Color.white;
            point.tubeScreenLight.color = Color.white;
            point.tubeScreenTextColor = color;
        }
    }
}
