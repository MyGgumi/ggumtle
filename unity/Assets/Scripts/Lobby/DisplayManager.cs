using UnityEngine;

public class DisplayManager : MonoBehaviour 
{
    void Start()
    {
        // 가로 고정으로 변경
        Screen.orientation = ScreenOrientation.LandscapeLeft;
        Screen.autorotateToPortrait = false;
        Screen.autorotateToPortraitUpsideDown = false;
        Screen.autorotateToLandscapeLeft = false;  // 여전히 자동회전은 막기
        Screen.autorotateToLandscapeRight = false;
    }
}