using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace AnimCompression
{
    public class CompressionAnimatorStateFlag : StateMachineBehaviour
    {
        //当前状态的动画是否跳过压缩
        public bool skipCompressed = false;
        //当前状态的动画是否存储旋转的tan信息
        public bool rotationTan = false;
    }

}