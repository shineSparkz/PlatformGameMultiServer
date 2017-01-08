using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;


namespace Server.GameSpecific.GameObjects
{
    class DiscipleEnemy : GameObject
    {
        #region Constants
        const float DT = 1.0f / 50.0f;
        const float MILLIS_PER_FRAME = 0.1f;
        const float NUM_FRAMES_X = 4.0f;
        const float NUM_FRAMES_Y = 6.0f;
        #endregion

        float m_AnimTick = 0.0f;
        float m_ChangetargetTick = 0.0f;
        private int m_PickedTarget = 0;
        private int m_PlayerTargetIndex = 0;
        private List<GameObject> m_Players = new List<GameObject>();

        public DiscipleEnemy(Vector2 p, GameObjectType obj_id, int unq_id, int isClient, bool updatable, Vector2 frameSize, ColliderOffset coloffset) :
            base(p, obj_id, unq_id, isClient, updatable, frameSize, coloffset)
        {
        }

        public override void Update()
        {
            if (!Active)
                return;

            if (m_Players.Count == 0 || m_Players.Count < Server.ServerManager.instance.NumClients())
            {
                m_Players = GameSimulation.instance.GetPlayers();
            }

            if (m_Players.Count == 0)
                return;

            m_AnimTick += DT;

            if (m_PickedTarget == 0)
            {
                // Get Random target
                m_PlayerTargetIndex = GameSimulation.RandomRange(0, m_Players.Count);
                m_PickedTarget = 1;
            }
            else
            {
                m_ChangetargetTick += DT;

                if (m_ChangetargetTick > 8.0f)
                {
                    m_PickedTarget = 0;
                    m_ChangetargetTick = 0.0f;
                }
            }

            if (m_AnimTick >= MILLIS_PER_FRAME)
            {
                ++frameX;

                if (m_Facing == Facing.Right)
                {
                    frameY = 0.0f;
                    if (frameX >= NUM_FRAMES_X)
                    {
                        frameX = 0;
                    }
                }
                else
                {
                    frameY = 3.0f;
                    if (frameX >= NUM_FRAMES_X)
                    {
                        frameX = 0;
                    }
                }

                m_AnimTick = 0.0f;
            }

            Vector2 player_centre = m_Players[m_PlayerTargetIndex].Position;
            Vector2 my_centre = this.Position;

            if ((player_centre.X) < my_centre.X)
            {
                m_Facing = Facing.Left;
            }
            else
            {
                m_Facing = Facing.Right;
            }

            float dist = Vector2.Distance(player_centre, my_centre);
            const float speed = 0.8f;
            Vector2 direction = Vector2.Zero;

            // Seek Player
            if (dist < 400)
            {
                direction = Vector2.Normalize(player_centre - my_centre);
                Velocity = direction * speed;
            }
            else if (dist < 1800)
            {
                double elapsed = GameSimulation.instance.GetTotalTime();
                Velocity = new Vector2((float)Math.Cos(elapsed) * speed, (float)Math.Sin(elapsed) * speed);
            }

            // Update Pos
            Position += Velocity;
        }
    }
}
