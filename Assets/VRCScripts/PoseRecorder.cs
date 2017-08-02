using UnityEngine;
using UnityEngine.UI;
using VRCSDK2;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;

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
    [Tooltip("Generates an audioclip from your microphone with the recording")]
    public bool recordAudio;

    [HideInInspector]
    public Animator targetAvatar;

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

    [Header("Don't touch this, only shown so you can verify transform setup")]
    public Transform[] avatarBones;

    private bool recordingTimerOn;
    private float recordingTimer;

    //Audio Nonsense
    //private AudioSource micInput;
    private AudioClip audioRecording = new AudioClip();
    private void Awake()
    {
        //micInput = GameObject.Find("USpeak").GetComponent<AudioSource>();
        SetupInstance();
    }

    public void SetupInstance()
    {
        if (Instance == null)
            Instance = this;
    }

    public void StartRecording()
    {
        if (recording)
            return;

        if (audioSource)
            audioSource.Play();

        if(recordAudio)
            audioRecording = Microphone.Start("", false, 3600, 44100);

        recordingFrames = new List<SignKeyframe>();
        keyframe = 0;
        animTimer = 0;
        recording = true;
    }

    public void DelayedRecording(float delay)
    {
        if (recordingTimerOn)
            return;

        Invoke("StartRecording", delay);
        recordingTimerOn = true;
        recordingTimer = -delay;
    }

    public void StopRecording()
    {
        if (recordAudio)
        {
            int tempPos = Microphone.GetPosition("");
            Microphone.End("");
            float[] tempData = new float[tempPos * 44100];
            audioRecording.GetData(tempData, 0);
            audioRecording = AudioClip.Create("recorded audio", tempPos, 1, 44100, false);
            audioRecording.SetData(tempData, 0);
        }

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
            version = 2
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
        if(recordAudio)
            SavWav.Save(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "/My Games/VRChat/MoCap Recordings/SignAnimation" + DateTime.Now.ToString("dd-MM-yyyy_hh-mm-ss") + ".wav", audioRecording);
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

            Vector3 hipPos = Vector3.zero;
            
            List<Quaternion> tempRotation = new List<Quaternion>();
            
            for (int i = 0; i < bones.Length; i++)
            {
                if (Networking.LocalPlayer.GetBoneTransform(bones[i]) == null)
                    continue;

                if (i == 0)
                    hipPos = Networking.LocalPlayer.GetBoneTransform(bones[i]).transform.position;

                tempRotation.Add(Networking.LocalPlayer.GetBoneTransform(bones[i]).transform.localRotation);
            }

            SignKeyframe tempFrame = new SignKeyframe(hipPos, tempRotation.ToArray());

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
                if (i == 0)
                {
                    avatarBones[i].position = new Vector3(loadPositions[keyframe].hipPositionX, loadPositions[keyframe].hipPositionY, loadPositions[keyframe].hipPositionZ);
                }

                avatarBones[i].localRotation = new Quaternion(loadPositions[keyframe].rotationX[i], loadPositions[keyframe].rotationY[i], loadPositions[keyframe].rotationZ[i], loadPositions[keyframe].rotationW[i]);
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
    public SignKeyframe(Vector3 hipPosition, Quaternion[] rotation)
    {
        hipPositionX = hipPosition.x;
        hipPositionX = hipPosition.y;
        hipPositionX = hipPosition.z;

        rotationW = rotation.Select( q => q.w ).ToArray();
        rotationX = rotation.Select( q => q.x ).ToArray();
        rotationY = rotation.Select( q => q.y ).ToArray();
        rotationZ = rotation.Select( q => q.z ).ToArray();
    }

    public float[] positionX;
    public float[] positionY;
    public float[] positionZ;

    public float hipPositionX;
    public float hipPositionY;
    public float hipPositionZ;

    public float[] rotationW;
    public float[] rotationX;
    public float[] rotationY;
    public float[] rotationZ;
}