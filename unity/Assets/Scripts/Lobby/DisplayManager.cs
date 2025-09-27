using UnityEngine;

public class DisplayManager : MonoBehaviour 
{
    void Start()
    {
        // 화면 회전 허용 설정
        Screen.autorotateToLandscapeLeft = true;
        Screen.autorotateToLandscapeRight = false;
        Screen.autorotateToPortrait = false;
        Screen.autorotateToPortraitUpsideDown = false;
        
        // 강제로 가로 방향 설정
        Screen.orientation = ScreenOrientation.LandscapeLeft;
    }
}