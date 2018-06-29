//#define ANIM_CMP_DEBUG_DATA
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;


namespace AnimCompression
{
    
    /// <summary>
    /// 曲线压缩数据集
    /// CompressedCurve主要存储
    ///     关键帧时间：无损压缩，将时间转换成帧序列位，有关键帧为【1】否则为【0】,最大压缩率为1/32。
    ///         帧不填满的时候可能没有压缩空间甚至更大，帧无法对齐时会有额外损失。
    ///     关键帧的值：有损压缩：关键帧的值，算法是在最大值和最小值之间线性采样成byte
    ///     关键帧切线：丢弃
    /// 
    /// </summary>
    [Serializable]
    public class CompressedCurveData
    {
        const int ValueMax = Byte.MaxValue;

        const float TanMin = -180;
        const float TanMax = 180;

        public enum ValueMode{
            Byte1,Byte2,ValueTan
        }
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
        public void encodeDebugData(List<Keyframe> keys){}
        public void decodeDebugData(Keyframe[] keys){}
        public void initProperty(ushort propertyId,string propertyPath, string propertyType, string propertyName)
        {
            this.property = propertyId;
        }
#endif
        /// <summary>
        /// property 12bit
        /// valueMode 4 bit
        /// mode 1:
        /// valueBase 5 bit
        /// valueAvg 13 bit
        /// mode 2:
        /// valueBase 5 bit
        /// 
        /// </summary>
        public int header;
        //属性ID
        public ushort property;

        public byte valueMode;
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
        public float[] keysTans;
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

        void deleteOverframe(float frameRate,List<Keyframe> keys)
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
        void encodBytes(byte[] bytes,ValueMode valueMode,int i, ref Keyframe key)
        {
            var value = key.value;
            var valueNormal = Mathf.InverseLerp(valueMin, valueMax, value);

            if (valueMode == ValueMode.Byte1)
            {
                bytes[i] = (byte)(valueNormal * byte.MaxValue);
            }
            else if (valueMode == ValueMode.Byte2)
            {
                ushort bytesValue = (ushort)(valueNormal * ushort.MaxValue);
                bytes[i * 2] = (byte)(bytesValue & 0XFF);
                bytes[i * 2 + 1] = (byte)((bytesValue >> 8) & 0XFF);
            }
            else if(valueMode == ValueMode.ValueTan)
            {
                ushort bytesValue = (ushort)(valueNormal * ushort.MaxValue);
                bytes[i * 2] = (byte)(valueNormal * byte.MaxValue);
                bytes[i * 2 + 1] = (byte)(Mathf.InverseLerp(TanMin, TanMax, key.inTangent) * byte.MaxValue);
            }
            else
            {
                throw new ArgumentException("dataMode:" + valueMode + " error!");
            }
        }
        void decodeBytes(byte[] bytes, ValueMode valueMode, int i,ref Keyframe key)
        {
            float valueNormal;
            if (valueMode == ValueMode.Byte1)
            {
                valueNormal = bytes[i] * 1.0f / byte.MaxValue;
            }
            else if (valueMode == ValueMode.Byte2)
            {
                int value = bytes[i * 2];
                value |= bytes[i * 2 + 1] << 8;
                valueNormal = value * 1.0f / ushort.MaxValue;
            }
            else if (valueMode == ValueMode.ValueTan)
            {
                valueNormal = bytes[i * 2] * 1.0f / byte.MaxValue;
                float tanNormal = bytes[i * 2 + 1] * 1.0f / byte.MaxValue; 
                key.inTangent = key.outTangent = Mathf.Lerp(TanMin, TanMax, tanNormal);
            }
            else
            {
                throw new ArgumentException();
            }
            key.value = Mathf.Lerp(valueMin, valueMax, valueNormal);
        }


        /// <summary>
        /// 编码成AnimationCurve
        /// </summary>
        /// <param name="clip"></param>
        /// <param name="curve"></param>
        public void encodeData(CompressedClipData clip, AnimationCurve curve, ValueMode valueMode,bool tan)
        {
            float maxTime = clip.length;
            float time = clip.length;
            float frameRate = clip.frameRate;
            List<Keyframe> keys = new List<Keyframe>(curve.keys);
            deleteOverframe(frameRate,keys);
            var length = keys.Count;
            //keysData = new uint[length];
            if(valueMode == ValueMode.Byte1)
            {
                keysValueByte = new byte[length];
            }
            else
            {
                keysValueByte = new byte[length * 2];
            }
            if (tan)
            {
                keysTans = new float[length];
            }
            //计算最大值和最小值
            valueMin = float.MaxValue;
            valueMax = float.MinValue;
            for (int i = 0; i < length; i++)
            {
                var key = keys[i];
                valueMin = Mathf.Min(valueMin, key.value);
                valueMax = Mathf.Max(valueMax, key.value);
            }
            int timeBytes = Mathf.CeilToInt((time * frameRate + 1) / 8);
            keysTime = new byte[timeBytes];

            float valueSize = valueMax - valueMin;
            //编码时间和关键帧值
            for (int i = 0; i < length; i++)
            {
                var key = keys[i];
                //时间
                int keyIndex = Mathf.RoundToInt(key.time * frameRate);
                int byteIndex = keyIndex / 8;
                int byteBit = keyIndex % 8;
                byte keysTimeByte = keysTime[byteIndex];
                keysTime[byteIndex] |= (byte)(1 << byteBit);

                encodBytes(keysValueByte, valueMode, i,ref key);
                if (tan)
                {
                    keysTans[i] = key.inTangent;
                }
            }
            this.valueMode = (byte)valueMode;
            encodeDebugData(keys);
        }
        //解码时间信息
        void decodeTime(CompressedClipData clip,Keyframe[] keys)
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
        //解码动画曲线
        public AnimationCurve decodeData(CompressedClipData clip)
        {
            float maxTime = clip.length;
            ValueMode valueMode = (ValueMode)this.valueMode;
            AnimationCurve curve = new AnimationCurve();
            var length = keysValueByte.Length;
            if(valueMode== ValueMode.Byte2 || valueMode == ValueMode.ValueTan)
            {
                length = length / 2;
            }
           // float ValueMax = valueMode == ValueMode.Byte1 ? byte.MaxValue : ushort.MaxValue;
            var keys = new Keyframe[length];
            bool tansData = keysTans != null && keysTans.Length > 0;
            for (int i = 0; i < length; i++)
            {
                Keyframe key = new Keyframe();
                decodeBytes(keysValueByte,valueMode,i,ref key);
                if (tansData)
                {
                    key.inTangent = keysTans[i];
                    key.outTangent = keysTans[i];
                }
                keys[i] = key;
            }
            decodeTime(clip, keys);
            decodeDebugData(keys);
            curve.keys = keys;
            if(valueMode != ValueMode.ValueTan && !tansData)
            {
                for (int i = 0; i < length; i++)
                {
                    curve.SmoothTangents(i, 0f);
                }
            }
            
            return curve;
        }
    }
}