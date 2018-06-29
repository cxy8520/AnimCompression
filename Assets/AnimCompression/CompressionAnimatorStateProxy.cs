using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace AnimCompression
{
    /// <summary>
    /// 
    /// </summary>
    public class CompressionAnimatorStateProxy : StateMachineBehaviour
    {
        public CompressedClipData data;
        //AnimationClip clip;

        //AnimationClip getClip()
        //{
        //    if (!data)
        //    {
        //        return null;
        //    }
        //    if (!clip)
        //    {
        //        clip = data.getOrCreateClip();
        //    }
        //    return clip;
        //}
        CompressedAnimationDirver getDirver(Animator animator)
        {
            CompressedAnimationDirver dirver = animator.GetComponent<CompressedAnimationDirver>();
            if (!dirver)
            {
                dirver = animator.gameObject.AddComponent<CompressedAnimationDirver>();
            }
            return dirver;
        }

        //OnStateEnter is called when a transition starts and the state machine starts to evaluate this state
        override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            CompressedAnimationDirver dirver = getDirver(animator);
            dirver.DoStateEnter(animator,ref stateInfo, data);
        }

        // OnStateUpdate is called on each Update frame between OnStateEnter and OnStateExit callbacks
        //override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        //{

        //    if (clip)
        //    {
        //        clip.SampleAnimation(animator.gameObject, stateInfo.normalizedTime);
        //    }
        //}

        //OnStateExit is called when a transition ends and the state machine finishes evaluating this state
        override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            CompressedAnimationDirver dirver = getDirver(animator);
            dirver.DoStateExit(animator, ref stateInfo,data);
            //dirver.DoStateEnter(stateInfo, getClip());
            //var runner = animator.GetComponent<Animation>();
            //if (runner)
            //{
            //    runner.Stop(clip.name);
            //    //最后采样一次，确保最终状态正确
            //    clip.SampleAnimation(animator.gameObject, clip.length);

            //}
        }

      //  OnStateMove is called right after Animator.OnAnimatorMove(). Code that processes and affects root motion should be implemented here
        //override public void OnStateMove(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        //{
        //    //var runner = animator.GetComponent<Animation>();
        //    //if (runner)
        //    //{
        //    //    runner.Sample();
        //    //}
        // //   clip.SampleAnimation(animator.gameObject, stateInfo.normalizedTime * clip.length);
        //}

        //// OnStateIK is called right after Animator.OnAnimatorIK(). Code that sets up animation IK (inverse kinematics) should be implemented here.
        //override public void OnStateIK(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
        //    clip.SampleAnimation(animator.gameObject, stateInfo.normalizedTime * clip.length);

        //}
    }

}