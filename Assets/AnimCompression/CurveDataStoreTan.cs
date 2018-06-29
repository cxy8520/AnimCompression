//#define ANIM_CMP_DEBUG_DATA
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;


namespace AnimCompression
{
    /// <summary>
    /// 存储切线的曲线数据
    /// </summary>
    [Serializable]
    public class CurveDataStoreTan : CurveDataBasicValues
    {
        public float[] keysTans;
        protected override void InitEncode(CompressedClipData clip, AnimationCurve curve, List<Keyframe> keys)
        {
            base.InitEncode(clip, curve, keys);
            keysTans = new float[keys.Count];
        }
        protected override void EncodeFrameValue(int keyIndex, ref Keyframe keyframe)
        {
            base.EncodeFrameValue(keyIndex, ref keyframe);
            keysTans[keyIndex] = keyframe.inTangent;
        }
        protected override void DecodeFrame(int keyIndex, ref Keyframe keyframe)
        {
            base.DecodeFrame(keyIndex, ref keyframe);
            keyframe.inTangent = keysTans[keyIndex];
            keyframe.outTangent = keysTans[keyIndex];
        }
        protected override void SmoothTangents(AnimationCurve curve, int keys)
        {
            //自己存储了Tan数据，不再需要SmoothTangents
            //base.SmoothTangents(curve, keys);
        }

    }
}