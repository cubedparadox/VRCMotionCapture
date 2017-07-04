using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Collections.Generic;

public class PoseRecorderEditor : MonoBehaviour
{
    public static string savePath;
    public static string fileName;

    static Transform[] recordObjs;
    static SignAnimationCreator[] objRecorders;

    static Vector3[] originPos;
    static Quaternion[] originRot;

    public static int animFileIndex;

    [MenuItem("Avatar Recording/Play Animation")]
    public static void PlayAnimation()
    {
        SetupPoseRecorder();

        if (PoseRecorder.Instance.targetAvatar == null)
        {
            Debug.LogError("There is no targetAvatar");
            return;
        }

        if (PoseRecorder.Instance.targetAvatar.isHuman)
        {
            Debug.LogError("TargetAvatar is humanoid, change rig type to generic");
            return;
        }

        Debug.Log("Playing animation");

        int index = 0;
        for (int i = 0; i < PoseRecorder.Instance.bones.Length; i++)
        {
            if (PoseRecorder.Instance.bones[i] == HumanBodyBones.Hips)
            {
                index = i;
                break;
            }
        }

        PoseRecorder.Instance.offset = new Vector3(PoseRecorder.Instance.loadPositions[0].positionX[index], 0, PoseRecorder.Instance.loadPositions[0].positionZ[index]);

        LoadFile(Selection.activeObject.ToString());

        PoseRecorder.Instance.playingAnim = true;
        PoseRecorder.Instance.animTimer = 0;
        PoseRecorder.Instance.keyframe = 0;
    }

    [MenuItem("Avatar Recording/Setup Selected Avatar")]
    static void SetupAvatar()
    {
        SetupPoseRecorder();

        PoseRecorder.Instance.targetAvatar = null;

        if (Selection.activeGameObject != null && Selection.activeGameObject.GetComponent<Animator>() != null)
            PoseRecorder.Instance.targetAvatar = Selection.activeGameObject.GetComponent<Animator>();

        if (PoseRecorder.Instance.targetAvatar == null)
        {
            Debug.LogError("There is no animator on selected object");
            return;
        }

        if (!PoseRecorder.Instance.targetAvatar.isHuman)
        {
            Debug.LogError("Avatar is not humanoid, change rig to humanoid and try again");
            return;
        }

        List<Transform> avatarBoneList = new List<Transform>();

        for (int i = 0; i < PoseRecorder.Instance.bones.Length; i++)
        {
            if (PoseRecorder.Instance.targetAvatar.GetBoneTransform(PoseRecorder.Instance.bones[i]) != null)
                avatarBoneList.Add(PoseRecorder.Instance.targetAvatar.GetBoneTransform(PoseRecorder.Instance.bones[i]));
        }

        PoseRecorder.Instance.avatarBones = avatarBoneList.ToArray();

        PoseRecorder.Instance.targetAvatar.gameObject.SetActive(false);

        Debug.Log("Avatar bones have been set up for " + Selection.activeGameObject.name + ". Total bones " + PoseRecorder.Instance.avatarBones.Length);
    }

    [MenuItem("Avatar Recording/Record Selected Animations")]
    public static void RecordSelectedAnimations()
    {
        SetupPoseRecorder();

        if (PoseRecorder.Instance.targetAvatar == null)
        {
            Debug.LogError("There is no target avatar, set up your avatar again");
            return;
        }

        if (PoseRecorder.Instance.targetAvatar.isHuman)
        {
            Debug.LogError("Avatar is humanoid, change rig to generic and try again");
            return;
        }

        Debug.Log("Starting to record Animation");

        animFileIndex = 0;

        originPos = new Vector3[PoseRecorder.Instance.avatarBones.Length];
        originRot = new Quaternion[PoseRecorder.Instance.avatarBones.Length];

        for (int i = 0; i < PoseRecorder.Instance.avatarBones.Length; i++)
        {
            if (PoseRecorder.Instance.avatarBones[i] == null)
                continue;

            originPos[i] = PoseRecorder.Instance.avatarBones[i].position;
            originRot[i] = PoseRecorder.Instance.avatarBones[i].rotation;
        }

        for (int i = 0; i < Selection.objects.Length; i++)
        {
            SetupRecoders();
            if (Selection.objects[animFileIndex].GetType() == typeof(TextAsset))
                LoadAndRecord(Selection.objects[animFileIndex].ToString());
            else
                Debug.LogWarning("Selected file is not a motion capture asset");
        }
    }

    public static void LoadAndRecord(string animationFile)
    {
        SetupPoseRecorder();

        fileName = Selection.objects[animFileIndex].name;

        PoseRecorder.Instance.loadPositions = new List<SignKeyframe>();
        string animationString = animationFile;

        Debug.Log("Recording " + Selection.objects[animFileIndex].name);

        string[] splitAnimation = animationString.Split(new string[] { ";" }, StringSplitOptions.None);

        KeyframeInformation keyframeInfo = JsonUtility.FromJson<KeyframeInformation>(splitAnimation[0]);

        bool shouldRecord = true;

        if (keyframeInfo != null && keyframeInfo.version < 1)
        {
            shouldRecord = false;
            Debug.LogError("File not readable");
        }

        if (shouldRecord)
        {
            for (int i = 1; i < splitAnimation.Length; i++)
            {
                PoseRecorder.Instance.loadPositions.Add(JsonUtility.FromJson<SignKeyframe>(splitAnimation[i]));
            }

            for (int i = 0; i < PoseRecorder.Instance.loadPositions.Count - PoseRecorder.Instance.frameDiscardAmount; i++)
            {
                for (int j = 0; j < PoseRecorder.Instance.avatarBones.Length; j++)
                {
                    PoseRecorder.Instance.avatarBones[j].transform.position = new Vector3(PoseRecorder.Instance.loadPositions[i].positionX[j], PoseRecorder.Instance.loadPositions[i].positionY[j], PoseRecorder.Instance.loadPositions[i].positionZ[j]);
                    PoseRecorder.Instance.avatarBones[j].transform.rotation = new Quaternion(PoseRecorder.Instance.loadPositions[i].rotationX[j], PoseRecorder.Instance.loadPositions[i].rotationY[j], PoseRecorder.Instance.loadPositions[i].rotationZ[j], PoseRecorder.Instance.loadPositions[i].rotationW[j]);

                    Vector3 pos = PoseRecorder.Instance.avatarBones[j].transform.localPosition;
                    Quaternion rot = PoseRecorder.Instance.avatarBones[j].transform.localRotation;

                    objRecorders[j].AddFrame(i / PoseRecorder.Instance.framesPerSecond, pos, rot);
                }
            }

            for (int i = 0; i < PoseRecorder.Instance.avatarBones.Length; i++)
            {
                PoseRecorder.Instance.avatarBones[i].position = originPos[i];
                PoseRecorder.Instance.avatarBones[i].rotation = originRot[i];
            }

            ExportAnimationClip();
        }

        animFileIndex++;
    }

    static void ExportAnimationClip()
    {
        if (!Directory.Exists("Assets / Motion Capture Animations"))
            Directory.CreateDirectory("Assets/Motion Capture Animations");

        string exportFilePath = "Assets/Motion Capture Animations/" + fileName;

        exportFilePath += ".anim";

        AnimationClip clip = new AnimationClip();
        clip.name = fileName;

        for (int i = 0; i < objRecorders.Length; i++)
        {
            AnimationCurveContainer[] curves = objRecorders[i].curves;

            for (int x = 0; x < curves.Length; x++)
            {
                clip.SetCurve(objRecorders[i].pathName, typeof(Transform), curves[x].propertyName, curves[x].animCurve);
            }
        }

        clip.EnsureQuaternionContinuity();
        AssetDatabase.CreateAsset(clip, exportFilePath);

        Debug.Log(Selection.objects[animFileIndex].name + " animation created");
    }

    private static void SetupRecoders()
    {
        recordObjs = PoseRecorder.Instance.avatarBones;

        objRecorders = new SignAnimationCreator[recordObjs.Length];

        for (int i = 0; i < recordObjs.Length; i++)
        {
            if (recordObjs[i] == null)
                continue;

            string path = GetTransformPathName(PoseRecorder.Instance.targetAvatar.transform, recordObjs[i]);
            objRecorders[i] = new SignAnimationCreator(path, recordObjs[i], recordObjs[i].name);
        }
    }

    public static string GetTransformPathName(Transform rootTransform, Transform targetTransform)
    {
        string returnName = targetTransform.name;
        Transform tempObj = targetTransform;

        // it is the root transform
        if (tempObj == rootTransform)
            return "";

        while (tempObj.parent != rootTransform)
        {
            returnName = tempObj.parent.name + "/" + returnName;
            tempObj = tempObj.parent;
        }

        return returnName;
    }

    private static void LoadFile(string animationFile)
    {
        SetupPoseRecorder();

        fileName = Selection.objects[animFileIndex].name;

        PoseRecorder.Instance.loadPositions = new List<SignKeyframe>();
        string animationString = animationFile;

        string[] splitAnimation = animationString.Split(new string[] { ";" }, StringSplitOptions.None);

        KeyframeInformation keyframeInfo = JsonUtility.FromJson<KeyframeInformation>(splitAnimation[0]);

        bool shouldRecord = true;

        if (keyframeInfo != null && keyframeInfo.version < 1)
        {
            shouldRecord = false;
            Debug.LogError("File not readable");
        }

        if (shouldRecord)
        {
            for (int i = 1; i < splitAnimation.Length; i++)
            {
                PoseRecorder.Instance.loadPositions.Add(JsonUtility.FromJson<SignKeyframe>(splitAnimation[i]));
            }
        }
    }

    private static void SetupPoseRecorder()
    {
        if (FindObjectOfType<PoseRecorder>() != null)
            FindObjectOfType<PoseRecorder>().SetupInstance();

        if (PoseRecorder.Instance == null)
        {
            Debug.LogError("Could not find a PoseRecorder, make sure there's a Pose Recorder in your scene");
            return;
        }
    }
}

public class SignAnimationCreator
{
    public AnimationCurveContainer[] curves;
    public Transform observeGameObject;
    public string pathName = "";
    public string objName = "";

    public SignAnimationCreator(string hierarchyPath, Transform observeObj, string name)
    {
        pathName = hierarchyPath;
        observeGameObject = observeObj;
        objName = name;

        curves = new AnimationCurveContainer[7];

        curves[0] = new AnimationCurveContainer("localPosition.x");
        curves[1] = new AnimationCurveContainer("localPosition.y");
        curves[2] = new AnimationCurveContainer("localPosition.z");

        curves[3] = new AnimationCurveContainer("localRotation.x");
        curves[4] = new AnimationCurveContainer("localRotation.y");
        curves[5] = new AnimationCurveContainer("localRotation.z");
        curves[6] = new AnimationCurveContainer("localRotation.w");
    }

    public void AddFrame(float time, Vector3 pos, Quaternion rot)
    {
        curves[0].AddValue(time, pos.x);
        curves[1].AddValue(time, pos.y);
        curves[2].AddValue(time, pos.z);

        curves[3].AddValue(time, rot.x);
        curves[4].AddValue(time, rot.y);
        curves[5].AddValue(time, rot.z);
        curves[6].AddValue(time, rot.w);
    }
}

public class AnimationCurveContainer
{
    public string propertyName = "";
    public AnimationCurve animCurve;

    public AnimationCurveContainer(string _propertyName)
    {
        animCurve = new AnimationCurve();
        propertyName = _propertyName;
    }

    public void AddValue(float animTime, float animValue)
    {
        Keyframe key = new Keyframe(animTime, animValue, 0.0f, 0.0f);
        animCurve.AddKey(key);
    }
}