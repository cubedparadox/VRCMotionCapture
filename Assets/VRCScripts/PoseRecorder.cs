using UnityEngine;
using UnityEngine.UI;
using VRCSDK2;
using System;
using System.IO;
using System.Collections.Generic;

public class PoseRecorder : MonoBehaviour
{
    [Tooltip("How many frames per second the recording should contain")]
    public float framesPerSecond = 30;
    [Tooltip("The amount of frames that are not included when turning animFile into a Unity Animation")]
    public int frameDiscardAmount = 30;
    [Tooltip("The Audiosource that will play a sound when recording starts")]
    public AudioSource audioSource;
    [Tooltip("All the bones that it will attempt to record if possible (if bone doesn't exist it's automatically excluded when recording)")]
    public HumanBodyBones[] bones;
    [Tooltip("The text component which shows the recording time")]
    public Text timerText;

    [HideInInspector]
    public Animator targetAvatar;

    [HideInInspector]
    public Transform[] avatarBones;

    [HideInInspector]
    public static PoseRecorder Instance;

    [HideInInspector]
    public bool recording;
    [HideInInspector]
    public bool playingAnim = false;

    [HideInInspector]
    public int keyframe;

    private List<SignKeyframe> recordingFrames = new List<SignKeyframe>();
    [HideInInspector]
    public List<SignKeyframe> loadPositions = new List<SignKeyframe>();

    [HideInInspector]
    public float animTimer;

    [HideInInspector]
    public Vector3 offset;

    private bool recordingTimerOn;
    private float recordingTimer;

    private void Awake()
    {
        SetupInstance();
    }

    public void SetupInstance()
    {
        if (Instance == null)
            Instance = this;
    }

    public void StartRecording()
    {
        if (audioSource)
            audioSource.Play();

        recordingFrames = new List<SignKeyframe>();
        keyframe = 0;
        animTimer = 0;
        recording = true;
    }

    public void DelayedRecording(float delay)
    {
        Invoke("StartRecording", delay);
        recordingTimerOn = true;
        recordingTimer = -delay;
    }

    public void StopRecording()
    {
        recording = false;
        recordingTimerOn = false;
        SaveRecordPositions();
    }

    public void CancelRecording()
    {
        recording = false;
        recordingTimerOn = false;
    }

    private void SaveRecordPositions()
    {
        CreateDirectory();

        string json = "";

        KeyframeInformation keyframeInformation = new KeyframeInformation
        {
            version = 1
        };

        json += JsonUtility.ToJson(keyframeInformation);
        json += ";";

        for (int i = 0; i < recordingFrames.Count; i++)
        {
            json += JsonUtility.ToJson(recordingFrames[i]);
            if (i < recordingFrames.Count - 1)       //Don't add a ; if it's the last record point
                json += ";";
        }

        File.WriteAllText(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "/My Games/VRChat/MoCap Recordings/SignAnimation" + DateTime.Now.ToString("dd-MM-yyyy_hh-mm-ss") + ".txt", json);
    }

    private void CreateDirectory()
    {
        if (!Directory.Exists(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "/My Games"))
            Directory.CreateDirectory(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "/My Games");
        if (!Directory.Exists(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "/My Games/VRChat"))
            Directory.CreateDirectory(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "/My Games/VRChat");
        if (!Directory.Exists(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "/My Games/VRChat/MoCap Recordings"))
            Directory.CreateDirectory(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "/My Games/VRChat/MoCap Recordings");
    }

    private void Update()
    {
        if (recordingTimerOn)
        {
            if (timerText != null)
            {
                recordingTimer += Time.deltaTime;
                timerText.text = recordingTimer.ToString("F1");
            }
        }

        if (recording)
        {
            animTimer += Time.deltaTime;
            if (animTimer < (recordingFrames.Count / framesPerSecond))
                return;

            List<float> tempPositionX = new List<float>();
            List<float> tempPositionY = new List<float>();
            List<float> tempPositionZ = new List<float>();

            List<float> tempRotationX = new List<float>();
            List<float> tempRotationY = new List<float>();
            List<float> tempRotationZ = new List<float>();
            List<float> tempRotationW = new List<float>();

            for (int i = 0; i < bones.Length; i++)
            {
                if (Networking.LocalPlayer.GetBoneTransform(bones[i]) == null)
                    continue;

                tempPositionX.Add(Networking.LocalPlayer.GetBoneTransform(bones[i]).transform.position.x);
                tempPositionY.Add(Networking.LocalPlayer.GetBoneTransform(bones[i]).transform.position.y);
                tempPositionZ.Add(Networking.LocalPlayer.GetBoneTransform(bones[i]).transform.position.z);

                tempRotationX.Add(Networking.LocalPlayer.GetBoneTransform(bones[i]).transform.rotation.x);
                tempRotationY.Add(Networking.LocalPlayer.GetBoneTransform(bones[i]).transform.rotation.y);
                tempRotationZ.Add(Networking.LocalPlayer.GetBoneTransform(bones[i]).transform.rotation.z);
                tempRotationW.Add(Networking.LocalPlayer.GetBoneTransform(bones[i]).transform.rotation.w);
            }

            SignKeyframe tempFrame = new SignKeyframe()
            {
                positionX = tempPositionX.ToArray(),
                positionY = tempPositionY.ToArray(),
                positionZ = tempPositionZ.ToArray(),

                rotationX = tempRotationX.ToArray(),
                rotationY = tempRotationY.ToArray(),
                rotationZ = tempRotationZ.ToArray(),
                rotationW = tempRotationW.ToArray(),
            };

            if (tempFrame != null)
                Debug.Log("Transforms animated " + tempFrame.positionX.Length);
            else
                Debug.LogError("Tempframe is null");

            recordingFrames.Add(tempFrame);

            keyframe++;
        }
        else if (playingAnim)
        {
            animTimer += Time.deltaTime;

            if (animTimer < (keyframe / framesPerSecond))
                return;

            for (int i = 0; i < avatarBones.Length; i++)
            {
                if (bones[i] != HumanBodyBones.Head)
                    avatarBones[i].position = new Vector3(loadPositions[keyframe].positionX[i], loadPositions[keyframe].positionY[i], loadPositions[keyframe].positionZ[i]);

                avatarBones[i].rotation = new Quaternion(loadPositions[keyframe].rotationX[i], loadPositions[keyframe].rotationY[i], loadPositions[keyframe].rotationZ[i], loadPositions[keyframe].rotationW[i]);
            }
            keyframe++;

            if (keyframe >= loadPositions.Count - frameDiscardAmount)
            {
                playingAnim = false;
            }
        }
    }
}

[Serializable]
public class KeyframeInformation
{
    public int version;
}

[Serializable]
public class SignKeyframe
{
    public float[] positionX;
    public float[] positionY;
    public float[] positionZ;

    public float[] rotationW;
    public float[] rotationX;
    public float[] rotationY;
    public float[] rotationZ;
}