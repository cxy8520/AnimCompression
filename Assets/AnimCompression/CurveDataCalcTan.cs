//#define ANIM_CMP_DEBUG_DATA
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;


namespace AnimCompression
{
    /// <summary>
    /// 不存储切线的曲线数据(运行时计算)
    /// </summary>
    [Serializable]
    public class CurveDataCalcTan : CurveDataBasicValues
    {
        protected override void SmoothTangents(AnimationCurve curve, int keys)
        {
            for (int i = 0; i < keys; i++)
            {
                curve.SmoothTangents(i, 0f);
            }
        }
    }
}