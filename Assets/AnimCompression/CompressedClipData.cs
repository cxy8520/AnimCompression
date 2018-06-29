using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
namespace AnimCompression
{
    /// <summary>
    /// 动画（AnimationClip）压缩数据集合
    /// 主要通过压缩曲线数据来进行动画压缩。
    /// *AnimCompression动画压缩是有损的，主要损失的是关键帧值精度和丢弃切线信息，其中关键帧时间是无损的
    /// </summary>
    public class CompressedClipData:ScriptableObject {
        /// <summary>
        /// 曲线数据
        /// </summary>
        public CompressedCurveData[] curves;

        public List<CurveDataCalcTan> curvesCalcTan = new List<CurveDataCalcTan>();
        public List<CurveDataStoreTan> curvesStoreTan = new List<CurveDataStoreTan>();

        
        public byte frameRate;
        public float length;
        public byte wrapMode; 
        //共享数据(属性路径)
        public CompressedShareData shareData;
        //运行时解码后的Clip
        AnimationClip clip;
        /// <summary>
        /// 解码或者获取一个已经解码的AnimationClip
        /// </summary>
        /// <returns></returns>
        public AnimationClip getOrCreateClip()
        {
            if (this.clip) 
            {
                return this.clip;
            }
            AnimationClip clip = new AnimationClip();
            clip.legacy = true;
            clip.name = this.name;
            clip.wrapMode = (WrapMode)this.wrapMode;
            clip.frameRate = frameRate;
            //解析每个曲线
            DecodeCuves(clip,this.curvesCalcTan);
            DecodeCuves(clip, this.curvesStoreTan);
            this.clip = clip;
            return clip;
        }
        
        public void DecodeCuves<T>(AnimationClip clip,List<T> curves) where T: CurveDataBasicValues
        {
            //解析曲线集合
            for (int i = 0,count = curves.Count; i < count; i++)
            {
                var curveData = curves[i];
                var curve = curveData.decodeData(this);
                if (shareData.fields.Count <= curveData.property)
                {
                    Debug.LogError("属性未找到：" + curveData.property + " clip:" + this.name + " :" + i);
                    continue;
                }
                CompressedShareData.DataField field = shareData.fields[curveData.property];
                clip.SetCurve(field.propertyPath, typeof(GameObject).Assembly.GetType(field.propertyType), field.propertyName, curve);
            }
        }

    }

}