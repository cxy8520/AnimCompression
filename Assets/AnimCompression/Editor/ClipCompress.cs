using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.IO;
using System;

namespace AnimCompression
{
    /// <summary>
    /// 动画压缩编辑时工具
    /// </summary>
    public class ClipCompress
    {
        static HashSet<string> UncompressFields = new HashSet<string>() {
            "m_IsActive"
        };

        CompressionAnimatorStateFlag stateFlag;
        AnimatorState animatorState;
        HashSet<EditorCurveBinding> stateMachineFields;

        CompressedShareData shareData;
        AnimationClip rawClip;
        AnimationClip stubClip;
        CompressedClipData clipData;

        public ClipCompress(AnimatorState state, HashSet<EditorCurveBinding> stateMachineFields)
        {
            this.animatorState = state;
            this.stateMachineFields = stateMachineFields;
        }
        private static CompressionAnimatorStateFlag findStateFlag(AnimatorState animatorState)
        {
            //查找标记，检查是否可以跳过
            foreach (var behaviour in animatorState.behaviours)
            {
                var flag = behaviour as CompressionAnimatorStateFlag;
                if (flag)
                {
                    return flag;
                }
            }
            return null;
        }
        public string Process()
        {
            this.rawClip = animatorState.motion as AnimationClip;
            if (!rawClip)
            {
                return null;
            }
            this.stateFlag = findStateFlag(animatorState);
            //查找标记，检查是否可以跳过
            if (stateFlag && stateFlag.skipCompressed)
            {
                return null;
            }
            var size = UnityEngine.Profiler.GetRuntimeMemorySize(rawClip);
            //小于16K 不压缩
            if (size < 1024 * 16)
            {
                return null;
            }

            string rawClipPath = AssetDatabase.GetAssetPath(rawClip);
            if (string.IsNullOrEmpty(rawClipPath))
            {
                return null;
            }
            
            string anim_gen_dir = Path.GetDirectoryName(rawClipPath) + "/_anim_gen_";
            string dataDir = anim_gen_dir + "/datas";
            string stubDir = anim_gen_dir + "/stubs";

            Directory.CreateDirectory(anim_gen_dir);
            Directory.CreateDirectory(dataDir);
            Directory.CreateDirectory(stubDir);

            this.shareData = getShareData(anim_gen_dir);
            this.stubClip = new AnimationClip();
            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(rawClip);
            stubClip.SetCurve("", typeof(CompressedAnimationDirver), "stubAnimParam", AnimationCurve.Linear(0, 0, rawClip.length, 1));
            stubClip.name = rawClip.name + "_stub";
            this.clipData = CompressClipData();
            AnimationUtility.SetAnimationClipSettings(stubClip, settings);
            
            //*注意：这里是为了兼容通过AnimationClipSettings设置Loop而Wrap没设置
            if (settings.loopTime)
            {
                clipData.wrapMode = (byte)WrapMode.Loop;
                stubClip.wrapMode = WrapMode.Loop;
            }
            else
            {
                stubClip.wrapMode = rawClip.wrapMode;
            }
            animatorState.motion = stubClip;
            AssetDatabase.CreateAsset(stubClip, stubDir + "/" + stubClip.name + ".anim");
            string dataPath = dataDir + "/" + clipData.name + ".asset";
            AssetDatabase.CreateAsset(clipData, dataPath);
            var stateProxy = animatorState.AddStateMachineBehaviour<CompressionAnimatorStateProxy>();
            stateProxy.data = clipData;
            return dataPath;
        }
        //压缩Clip数据
        public CompressedClipData CompressClipData()
        {
            CompressedClipData compressingData = ScriptableObject.CreateInstance<CompressedClipData>();
            compressingData.name = rawClip.name + "_data";
            compressingData.length = rawClip.length;
            compressingData.wrapMode = (byte)rawClip.wrapMode;
            compressingData.frameRate = (byte)rawClip.frameRate;
            //data.shareData = shareData; 
            EditorCurveBinding[] bindings = AnimationUtility.GetCurveBindings(rawClip);
           
            for (int i = 0; i < bindings.Length; i++)
            {
                CompressCurveData(compressingData, bindings[i]);
            }
            compressingData.shareData = shareData;
            EditorUtility.SetDirty(shareData);
            return compressingData;
        }
        //压缩曲线数据
        void CompressCurveData(CompressedClipData compressingData,EditorCurveBinding binding)
        {
            if (binding.isPPtrCurve)
            {
                //属性帧不压缩，还原到stubClip里
                var keyframes = AnimationUtility.GetObjectReferenceCurve(rawClip, binding);
                AnimationUtility.SetObjectReferenceCurve(stubClip, binding, keyframes);
            }
            else if (UncompressFields.Contains(binding.propertyName))
            {
                //白名单内的属性也不压缩
                var curve = AnimationUtility.GetEditorCurve(rawClip, binding);
                AnimationUtility.SetEditorCurve(stubClip, binding, curve);
            }
            else
            {
                //从共享数据里获取字段ID
                var propertyId = shareData.getFieldIndex(binding.path, binding.type.FullName, binding.propertyName);
                //
                var curve = AnimationUtility.GetEditorCurve(rawClip, binding);// curves[i];
                //检测是否需要存储切线数据
                bool needStoreTanData = false;
                if (stateFlag && stateFlag.rotationTan && binding.propertyName.StartsWith("m_LocalRotation."))
                {
                    needStoreTanData = true;
                }
                
                //选择不同的存储方式
                CurveDataBasicValues curveData;
                if (needStoreTanData)
                {
                    var storeTanData = new CurveDataStoreTan();
                    compressingData.curvesStoreTan.Add(storeTanData);
                    curveData = storeTanData;
                }
                else
                {
                    var calcTanData = new CurveDataCalcTan();
                    compressingData.curvesCalcTan.Add(calcTanData);
                    curveData = calcTanData;
                }
                curveData.initProperty(propertyId, binding.path, binding.type.FullName, binding.propertyName);
                curveData.encodeData(compressingData, curve);
                stateMachineFields.Add(binding);
            }
        }
        static CompressedShareData getShareData(string dataDir)
        {
            string sharedDataPath = dataDir + "/" + "shared_data.asset";
            CompressedShareData shareData = AssetDatabase.LoadAssetAtPath<CompressedShareData>(sharedDataPath);
            if (!shareData)
            {
                shareData = ScriptableObject.CreateInstance<CompressedShareData>();
                AssetDatabase.CreateAsset(shareData, sharedDataPath);
                shareData = AssetDatabase.LoadAssetAtPath<CompressedShareData>(sharedDataPath);
            }
            return shareData;
        }
    }
}
