using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Xna.Framework;

namespace Server.GameSpecific.GameObjects
{
    class Exit : GameObject
    {
        const float DT = 1 / 50.0f;
        const float MILLIS_PER_FRAME = 0.08f;
        const float NUM_FRAMES_X = 6;
        const float NUM_FRAMES_Y = 5;//
        private float m_AnimTick = MILLIS_PER_FRAME;

        public Exit(Vector2 p, GameObjectType obj_id, int unq_id, int isClient, bool updatable, Vector2 frameSize, ColliderOffset coloffset) :
            base(p, obj_id, unq_id, isClient, updatable, frameSize, coloffset)
        {
        }

        public override void Update()
        {
            m_AnimTick -= DT;

            if (m_AnimTick <= 0.0f)
            {
                ++frameX;

                if (frameX >= NUM_FRAMES_X)
                {
                    frameX = 0;

                    ++frameY;
                    if (frameY >= NUM_FRAMES_Y)
                    {
                        frameY = 0;
                    }
                }
                
                m_AnimTick = MILLIS_PER_FRAME;
            }
        }
    }
}
