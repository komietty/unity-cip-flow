using UnityEngine;

public class camera : MonoBehaviour {

    int framerate = 60;
    int frameCount;
    public bool recode = false;


    void Start()
    {
        board b = GameObject.FindGameObjectWithTag("Player").GetComponent<board>();
        float x = (float)b.latticeWidth / 2;
        float y = (float)b.latticeHeight / 2;
        float z = b.cameraPosZ;
        gameObject.transform.position = new Vector3(x, y, z);

        if (recode) StartRecording();
    }

    void StartRecording()
    {
        System.IO.Directory.CreateDirectory("Capture");
        Time.captureFramerate = framerate;
        frameCount = -1;
    }

    void Update()
    {
        if (frameCount > 0 && recode)
        {
            var name = "Capture/frame" + frameCount.ToString("0000") + ".png";
            Application.CaptureScreenshot(name);
        }

        frameCount++;
    }
}
