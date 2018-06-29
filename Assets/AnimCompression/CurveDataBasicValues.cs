//#define ANIM_CMP_DEBUG_DATA
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;


namespace AnimCompression
{

    [Serializable]
    public class CurveDataBasicValues
    {
#if ANIM_CMP_DEBUG_DATA
        #region 调试数据
        //DebugDatas
        public string propertyPath;
        public string propertyType;
        public string propertyName;
        public float[] keysTansIn;
        public float[] keysTansOut;
        public float[] keysValues;
        public float[] keysTimes;
        //public string propertyName;

        public void encodeDebugData(List<Keyframe> keys)
        {
            var length = keys.Count;
            keysValues = new float[length];
            keysTimes = new float[length];
            keysTansIn = new float[length];
            keysTansOut = new float[length];
            for (int i = 0; i < length; i++)
            {
                var key = keys[i];
                keysTansIn[i] = key.inTangent;
                keysTansOut[i] = key.outTangent;
                keysValues[i] = key.value;
                keysTimes[i] = key.time;
            }
        }
        public void decodeDebugData(Keyframe[] keys)
        {
            var length = keys.Length;
            for (int i = 0; i < length; i++)
            {
                Keyframe key = keys[i];
                key.value = keysValues[i];
                key.time = keysTimes[i];
                key.inTangent = keysTansIn[i];
                key.outTangent = keysTansOut[i];
                keys[i] = key;
            }
        }
        public void initProperty(ushort propertyId, string propertyPath, string propertyType, string propertyName)
        {
            this.property = propertyId;
            this.propertyPath = propertyPath;
            this.propertyType = propertyType;
            this.propertyName = propertyName;
        }
        #endregion
#else
        public void encodeDebugData(List<Keyframe> keys) { }
        public void decodeDebugData(Keyframe[] keys) { }
        public void initProperty(ushort propertyId, string propertyPath, string propertyType, string propertyName)
        {
            this.property = propertyId;
        }
#endif
        //属性ID
        public ushort property;
       
        //256
        //byte 
        //
        /// <summary>
        /// 时间数据，无损压缩，每个byte对应时间轴上的一帧(非关键帧)，关键帧则为1，将时间转换成帧序列位，有关键帧为【1】否则为【0】,最大压缩率为1/32。
        /// </summary>
        public byte[] keysTime;

        //public byte mainData;

        /// <summary>
        /// 关键帧值数据，每个byte对应关键帧上一个值，在当前曲线最大值和最小值之间线性采样成byte
        /// </summary>
        public byte[] keysValueByte;
        
        ///// <summary>
        ///// 关键帧值数据，每个byte对应关键帧上一个值，在当前曲线最大值和最小值之间线性采样成byte
        ///// </summary>
        //public ushort[] keysValueShort;

        /// <summary>
        /// 当前曲线的最小值
        /// </summary>
        public float valueMin;
        /// <summary>
        /// 当前曲线的最大值
        /// </summary>
        public float valueMax;

        void deleteOverframe(float frameRate, List<Keyframe> keys)
        {
            //剔除重叠帧 *有些情况下，同一时刻会有重复帧，这将导致时间压缩算法出问题
            float frameTime = 1 / frameRate;
            for (int i = 1; i < keys.Count; i++)
            {
                float frameDiff = Mathf.Abs(keys[i].time - keys[i - 1].time);
                //小于帧时间间隔，则表示重叠帧
                if (frameDiff < frameTime / 2)
                {
                    keys.RemoveAt(i--);
                }
            }
        }
        protected virtual void EncodeFrameValue(int keyIndex, ref Keyframe keyframe)
        {
            var value = keyframe.value;
            var valueNormal = Mathf.InverseLerp(valueMin, valueMax, value);
            this.keysValueByte[keyIndex] = (byte)(valueNormal * byte.MaxValue);
        }
        protected virtual void DecodeFrame(int keyIndex, ref Keyframe keyframe)
        {
            float valueNormal = this.keysValueByte[keyIndex] * 1.0f / byte.MaxValue;
            keyframe.value = Mathf.Lerp(valueMin, valueMax, valueNormal);
        }


        /// <summary>
        /// 编码成AnimationCurve
        /// </summary>
        /// <param name="clip"></param>
        /// <param name="curve"></param>
        public void encodeData(CompressedClipData clip, AnimationCurve curve)
        {
            List<Keyframe> keys = new List<Keyframe>(curve.keys);
            deleteOverframe(clip.frameRate, keys);
            EncodeFrames(clip,curve,keys);
        }
        protected virtual void InitEncode(CompressedClipData clip, AnimationCurve curve, List<Keyframe> keys)
        {
            //计算最大值和最小值
            valueMin = float.MaxValue;
            valueMax = float.MinValue;
            var keysCount = keys.Count;
            for (int i = 0; i < keysCount; i++)
            {
                var key = keys[i];
                valueMin = Mathf.Min(valueMin, key.value);
                valueMax = Mathf.Max(valueMax, key.value);
            }
            this.keysValueByte = new byte[keysCount];
        }
        private void initFramesTime(float clipLength, float frameRate)
        {
            int timeBytes = Mathf.CeilToInt((clipLength * frameRate + 1) / 8);
            this.keysTime = new byte[timeBytes];
        }
        protected virtual void EncodeFrames(CompressedClipData clip, AnimationCurve curve,List<Keyframe> keyframes)
        {
            float maxTime = clip.length;
            float clipLength = clip.length;
            float frameRate = clip.frameRate;
            var keysCount = keyframes.Count;

            InitEncode(clip, curve, keyframes);
            initFramesTime(clipLength, frameRate);

            //编码时间和关键帧值
            for (int keyIndex = 0; keyIndex < keysCount; keyIndex++)
            {
                var keyframe = keyframes[keyIndex];
                EncodeFrameTime(keysTime, keyIndex, frameRate, keyframe.time);
                EncodeFrameValue(keyIndex, ref keyframe);
            }
            encodeDebugData(keyframes);
        }
        
        

        private void EncodeFrameTime(byte[] keysTime, int i, float frameRate, float keyTime)
        {
            //时间
            int keyIndex = Mathf.RoundToInt(keyTime * frameRate);
            int byteIndex = keyIndex / 8;
            int byteBit = keyIndex % 8;
            byte keysTimeByte = keysTime[byteIndex];
            keysTime[byteIndex] |= (byte)(1 << byteBit);
        }
        
        //解码时间信息
        void decodeTime(CompressedClipData clip, Keyframe[] keys)
        {
            float frameRate = clip.frameRate;
            for (int i = 0, idx = 0; i < keysTime.Length; i++)
            {
                byte timeBits = keysTime[i];
                for (int j = 0; j < 8; j++)
                {
                    if (((timeBits >> j) & 1) != 0)
                    {
                        if (keys.Length <= idx)
                        {
                            Debug.LogError("解码动画数据异常：帧时间索引：" + idx + " 当前帧：" + (i * 8 + j) + " 动画名称：" + clip.name + " 曲线：" + property);
                        }
                        else
                        {
                            keys[idx++].time = (i * 8 + j) / frameRate;
                        }
                    }
                }
            }
        }

        protected virtual void SmoothTangents(AnimationCurve curve,int keys)
        {
            
        }
        //解码动画曲线
        public virtual AnimationCurve decodeData(CompressedClipData clip)
        {
            float maxTime = clip.length;
            AnimationCurve curve = new AnimationCurve();
            var length = keysValueByte.Length;
            
            // float ValueMax = valueMode == ValueMode.Byte1 ? byte.MaxValue : ushort.MaxValue;
            var keys = new Keyframe[length];
            for (int i = 0; i < length; i++)
            {
                Keyframe key = new Keyframe();
                DecodeFrame(i, ref key);
                keys[i] = key;
            }
            decodeTime(clip, keys);
            decodeDebugData(keys);
            curve.keys = keys;
            SmoothTangents(curve,length);
            return curve;
        }
    }
}