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
    public class AnimatorCompressionUtils
    {
        static HashSet<string> UncompressFields = new HashSet<string>() {
            "m_IsActive"
        };
        const string CompressedAnimatorSuffix = ".anim_cpr";
        const string StoreDefaultStateName = "anim_cpr_gen_dft_store";
        [MenuItem("Tools/Build Bundle Test")]
        static void buildBundles()
        {
            BuildPipeline.BuildAssetBundles("bundles/", BuildAssetBundleOptions.ChunkBasedCompression, BuildTarget.StandaloneWindows);
        }
        
        
        static string ConvertAnimatorState(AnimatorState state, HashSet<EditorCurveBinding> stateMachineAllFields)
        {
            return new ClipCompress(state, stateMachineAllFields).Process();
        }
        static string ToCompressedAnimatorPath(string rawPath)
        {
            var fileDir = Path.GetDirectoryName(rawPath);
            var fileName = Path.GetFileName(rawPath);
            if (fileName.Contains(CompressedAnimatorSuffix))
            {
                //已经是压缩格式
                return null;
            }

            var assetName = Path.GetFileNameWithoutExtension(rawPath);
            var fileSuffix = Path.GetExtension(rawPath);
            return fileDir + "/" + assetName + CompressedAnimatorSuffix + fileSuffix;
        }
        static string ToRawAnimatorPath(string compPath)
        {
            var fileDir = Path.GetDirectoryName(compPath);
            var fileName = Path.GetFileName(compPath);
            if (!fileName.Contains(CompressedAnimatorSuffix))
            {
                return null;
            }
            fileName = fileName.Replace(CompressedAnimatorSuffix, "");
            return fileDir + "/" + fileName;
        }
        static void ConvertAnimator(AnimatorController rawAnimator)
        {
            //AssetDatabase.StartAssetEditing();
            string rawPath = AssetDatabase.GetAssetPath(rawAnimator);
            //var fileDir = Path.GetDirectoryName(rawPath);
            //var fileName = Path.GetFileNameWithoutExtension(rawPath);
            //var fileSuffix = Path.GetExtension(rawPath);

            string newAnimatorPath = ToCompressedAnimatorPath(rawPath);// fileDir +"/" + fileName+"_compressed"+ fileSuffix;
            if (newAnimatorPath == null)
            {
                rawPath = ToRawAnimatorPath(rawPath);
                rawAnimator = AssetDatabase.LoadAssetAtPath<AnimatorController>(rawPath);
                newAnimatorPath = ToCompressedAnimatorPath(rawPath);
            }
            AssetDatabase.DeleteAsset(newAnimatorPath);
            File.Copy(rawPath, newAnimatorPath,true);
            AssetDatabase.ImportAsset(newAnimatorPath);
            //CompressedShareData shareData = ScriptableObject.CreateInstance<CompressedShareData>();
            AnimatorController animator = AssetDatabase.LoadAssetAtPath<AnimatorController>(newAnimatorPath);
            //shareData.rawAnimatorGUID = AssetDatabase.AssetPathToGUID(rawPath);
            //string sharedDataPath = dataDir + "shared_data.asset";
            //AssetDatabase.CreateAsset(shareData, sharedDataPath);
            //shareData = AssetDatabase.LoadAssetAtPath<CompressedShareData>(sharedDataPath);
            var stateMachine = animator.layers[0].stateMachine;
            var defaultState = stateMachine.defaultState;
            var childStates = stateMachine.states;
            //当前状态机的所有字段，用于还原Write defaults效果
            //动画被替换后，所有字段都会被移除，这将破坏Write defaults效果
            HashSet<EditorCurveBinding> stateMachineAllBinding = new HashSet<EditorCurveBinding>();
            stateMachine.AddStateMachineBehaviour<CompressionAnimatorLayerProxy>();
            List<string> datasPathList = new List<string>();
            foreach (var cstate in childStates)
            {
                //跳过入口
                if (defaultState == cstate.state)
                {
                    //添加一个不包含数据的Proxy
                    cstate.state.AddStateMachineBehaviour<CompressionAnimatorStateProxy>();
                    continue;
                }
                var dataPath = ConvertAnimatorState(cstate.state,stateMachineAllBinding);
                if (!string.IsNullOrEmpty(dataPath))
                { 
                    datasPathList.Add(dataPath);
                } 
            }
            MakeStoreDefaultState(stateMachine, stateMachineAllBinding, rawPath);
            
            AssetDatabase.SaveAssets();
            //EditorApplication.delayCall += () => {
            //    ApplyShareData(sharedDataPath, datasPathList);
            //};
            //AssetDatabase.StopAssetEditing(); 
           // ApplyShareData(sharedDataPath,datasPathList);
        }
        /// <summary>
        /// 创建一个用于还原Write defaults的 state
        /// </summary>
        /// <param name="stateMachine"></param>
        /// <param name="stateMachineAllBinding"></param>
        private static void MakeStoreDefaultState(AnimatorStateMachine stateMachine, HashSet<EditorCurveBinding> stateMachineAllBinding,string animatorPath)
        {
            string animatorDir = Path.GetDirectoryName(animatorPath);
            string animatorName = Path.GetFileNameWithoutExtension(animatorPath);

            string defaultsClipPath = animatorDir + "/" + animatorName + ".defaults.anim";

            //查找是否已经存在
            AnimatorState storeDefaultsState = null;
            foreach(var state in stateMachine.states)
            {
                if(state.state.name == StoreDefaultStateName)
                {
                    storeDefaultsState = state.state;
                }
            }
            if (!storeDefaultsState)
            {
                storeDefaultsState = stateMachine.AddState(StoreDefaultStateName);
            }

            //创建一个用于还原Write defaults的clip
            AnimationClip defaultsClip = new AnimationClip();
            defaultsClip.name = "StoreDefaultStateName";
            foreach (var binding in stateMachineAllBinding)
            {
                AnimationCurve defaultsCurve = new AnimationCurve();
                defaultsCurve.AddKey(new Keyframe(0,0));
                if(binding.propertyName == "m_LocalRotation.w")
                {
                    defaultsCurve.AddKey(new Keyframe(1, 0));
                }
                AnimationUtility.SetEditorCurve(defaultsClip, binding, defaultsCurve);
            }
            AssetDatabase.CreateAsset(defaultsClip,defaultsClipPath);
            defaultsClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(defaultsClipPath);
            storeDefaultsState.motion = defaultsClip;
        }
        

        //private static void ApplyShareData(string sharedDataPath, List<string> datasPathList)
        //{
        //    CompressedShareData shareData = AssetDatabase.LoadAssetAtPath<CompressedShareData>(sharedDataPath);
        //    foreach(var dataPath in datasPathList)
        //    {
        //        CompressedClipData clipData = AssetDatabase.LoadAssetAtPath<CompressedClipData>(dataPath);
        //        clipData.shareData = shareData;
        //    }
        //    AssetDatabase.SaveAssets();
        //} 
        
        ///将动画还原 
        [MenuItem("Tools/Revert Compressed Animator")]
        static void RevertCompressedAnimator()
        {
            List<GameObject> gameObjects = new List<GameObject>();
            foreach (var obj in Selection.GetFiltered(typeof(GameObject), SelectionMode.Assets))
            {
                GameObject gameObject = (GameObject)obj;
                gameObjects.Add(gameObject);
                var animators = gameObject.GetComponentsInChildren<Animator>();
                foreach (var animator in animators)
                {
                    AnimatorController controller = (AnimatorController)animator.runtimeAnimatorController;
                    var path = AssetDatabase.GetAssetPath(controller);
                    var rawPath = ToRawAnimatorPath(path);
                    if (rawPath != null)
                    {
                        AnimatorController rawController = AssetDatabase.LoadAssetAtPath<AnimatorController>(rawPath);
                        if (rawController)
                        {
                            AnimatorController.SetAnimatorController(animator,rawController);
                        }
                    }
                }
            }
        }
        /// <summary>
        /// 将选择的Animator 或者prefab转换成压缩动画
        /// 转换后prefab内部的Animator组件将指向压缩后的动画
        /// </summary>
        [MenuItem("Tools/Compress Selected Animator")]
        static void CompressSelectedAnimator()
        {
            HashSet<AnimatorController> allController = new HashSet<AnimatorController>();
            EditorUtility.DisplayProgressBar("压缩动画", "准备", 0);
            try
            {
                foreach (var animator in Selection.GetFiltered(typeof(AnimatorController), SelectionMode.Assets))
                {
                    allController.Add((AnimatorController)animator);
                }
                List<GameObject> gameObjects = new List<GameObject>();
                foreach (var obj in Selection.GetFiltered(typeof(GameObject), SelectionMode.Assets))
                {
                    GameObject gameObject = (GameObject)obj;
                    gameObjects.Add(gameObject);
                    var animators = gameObject.GetComponentsInChildren<Animator>();
                    foreach (var animator in animators)
                    {
                        AnimatorController controller = (AnimatorController)animator.runtimeAnimatorController;
                        allController.Add(controller);
                    }
                }
                float i = 0;
                foreach (var animator in allController)
                {
                    EditorUtility.DisplayProgressBar("压缩动画", "处理"+ animator, i/ allController.Count);
                    ConvertAnimator(animator);
                    i++;
                }

                ApplyAnimator(gameObjects, true);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
            
        }

        static void ApplyAnimator(List<GameObject> gameObjects,bool toCompression)
        {
            foreach (var gameObject in gameObjects)
            {
                var animators = gameObject.GetComponentsInChildren<Animator>();
                foreach (var animator in animators)
                {
                    AnimatorController controller = (AnimatorController)animator.runtimeAnimatorController;
                    string path = AssetDatabase.GetAssetPath(controller);
                    string convertPath = toCompression ?ToCompressedAnimatorPath(path) : ToRawAnimatorPath(path);
                    if (convertPath != null)
                    {
                        var compController = AssetDatabase.LoadAssetAtPath<AnimatorController>(convertPath);
                        if (compController)
                        {
                            Debug.LogWarning("替换Animator "+ animator);
                            AnimatorController.SetAnimatorController(animator, compController);
                            if (!animator.GetComponent<CompressedAnimationDirver>())
                            {
                                animator.gameObject.AddComponent<CompressedAnimationDirver>();
                            }
                           
                        }
                    }
                }
            }
            AssetDatabase.SaveAssets();
        }



        static void CompressSelectedAnimatorPrefabs()
        {

        }
    }
}
