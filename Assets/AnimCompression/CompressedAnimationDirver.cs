using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace AnimCompression
{

    /// <summary>
    /// Animator对象挂载Animation组件冲突后，需要添加AnimationDirver来驱动。
    /// </summary>
    public class CompressedAnimationDirver : MonoBehaviour
    {
        Animation anim;
        Animator _animator;
        public AnimationClip playingClip;
        //Stub属性。@see AnimatorCompressUtils.ConvertAnimatorState
        public float stubAnimParam;
        public Animator animator
        {
            get
            {
                if (!_animator)
                {
                    _animator = GetComponent<Animator>();
                }
                return _animator;
            }
        }
        public Animation GetOrCreateAnimation()
        {
            if (anim)
            {
                return anim;
            }
            anim = GetComponent<Animation>();
            if (!anim)
            {
                anim = gameObject.AddComponent<Animation>();
            }
            return anim;
        }

        

        // Use this for initialization
        //void Awake()
        //{

        //    //anim.animatePhysics = true;
        //    //anim.cullingType = AnimationCullingType.BasedOnRenderers;
        //    ////丢到一个看不见的地方
        //    //anim.localBounds = new Bounds(new Vector3(9527 * 2, 9527 * 2 ,- 9527*100), new Vector3());
        //   // anim.enabled = false;
        //}

        // Update is called once per frame
        void LateUpdate()
        {
            if (!anim)
            {
                return;
            }
            //anim.Sample();
            //模拟Animator行为,停留在最后一个动画帧上
            if (playingClip && !anim.isPlaying)
            {
                playingClip.SampleAnimation(gameObject, playingClip.length);
            }
        }
        internal void DoStateLayerEnter(Animator animator, ref AnimatorStateInfo stateInfo)
        {
            //throw new NotImplementedException();
            
        }
        internal void DoStateLayerExit(Animator animator, ref AnimatorStateInfo stateInfo)
        {
            // throw new NotImplementedException();
           // playingClip = null;
        }

        private void OnDestroy()
        {
            playingClip = null;
            anim = null;
        }
        internal void DoStateEnter(Animator animator,ref AnimatorStateInfo stateInfo, CompressedClipData data)
        {
            if (!data)
            {
                playingClip = null;
                return;
            }
            var clip = data.getOrCreateClip();
            var anim = GetOrCreateAnimation();
            var clipState = anim[clip.name];
            if (clipState == null)
            {
                anim.AddClip(clip, clip.name);
            }
            if (!anim.isPlaying)
            {
                anim.Play(clip.name);
            }
            else
            {
                anim.CrossFade(clip.name);
            }
            animator.updateMode = AnimatorUpdateMode.AnimatePhysics;
            playingClip = clip;
            //anim.localBounds = new Bounds(new Vector3(9527 * 2, 9527 * 2, -9527 * 100), new Vector3());
        }

        internal void DoStateExit(Animator animator, ref AnimatorStateInfo stateInfo, CompressedClipData data)
        {
            if (!data)
            {
                return;
            }
            var clip = data.getOrCreateClip();
            anim.Stop(clip.name);
            if(clip == playingClip)
            {
                playingClip = null;
            }
        }
    }
}

