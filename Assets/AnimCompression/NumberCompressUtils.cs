//#define ANIM_CMP_DEBUG_DATA
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;


namespace AnimCompression
{
    
    public static class NumberCompressUtils
    {
        public static short EncodeFloat(float value)
        {
            int cnt = 0;

            while (value != Mathf.Floor(value))
            {
                value *= 10.0f;
                cnt++;
                if (cnt > 16)
                {
                    throw new Exception();
                }
            }
            return (short)((cnt << 12) + (int)value);
        }

        public static float DecodeFloat(short value)
        {
            int cnt = value >> 12;
            float result = value & 0xfff;
            while (cnt > 0)
            {
                result /= 10.0f;
                cnt--;
            }
            return result;
        }

    }
}