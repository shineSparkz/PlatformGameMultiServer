using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Xna.Framework;

namespace Server.GameSpecific.GameObjects
{
    public class BlueMinionEnemy : GameObject
    {
        const float DT = 1.0f / 50.0f;
        const float ATTK_COUNT = 1.0f;
        const float k_MoveToPlayerDist = 560.0f;
        const float k_MinChargeDistance = 512;
        const float k_MillisPerFrame = 0.08f;
        const float m_NumFramesX = 3.0f;

        enum MinionAIState { Idle, Charge, Dash }
        private List<GameObject> m_Players = new List<GameObject>();

        private Vector2 m_Direction = Vector2.Zero;
        private MinionAIState m_AIState = MinionAIState.Idle;
        private float m_AttackWaitCount = 0.0f;
        private float m_AnimTick = 0.0f;
        private int m_FoundTarget = 0;
        private int m_PickedTarget = 0;
        private int m_PlayerTargetIndex = 0;

        public BlueMinionEnemy(Vector2 p, GameObjectType obj_id, int unq_id, int isClient, bool updatable, Vector2 frameSize, ColliderOffset coloffset) :
            base(p, obj_id, unq_id, isClient, updatable, frameSize, coloffset)
        {
		}

		public override void Start()
        {
            base.Start();
        }

        public override void Update()
        {
            //if (m_EnemyParent->IsDead())
            //    return;
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

            // Get distance of closest player
            Vector2 player_centre =  m_Players[m_PlayerTargetIndex].Position;
            Vector2 my_centre = this.Position;

            if (player_centre.X < my_centre.X)
            {
                m_Facing = Facing.Left;
            }
            else
            {
                m_Facing = Facing.Right;
            }

            if (m_Facing == Facing.Left)
            {
                frameY = 0.0f;
            }
            else
            {
                frameY = 2.0f;
            }

            if (m_AnimTick >= k_MillisPerFrame)
            {
                ++frameX;

                if (frameX >= m_NumFramesX)
                {
                    frameX = 0;
                }

                m_AnimTick = 0.0f;
            }

            float dist = Vector2.Distance(my_centre, player_centre);

            // Dash Attack the player
            switch (m_AIState)
            {
                case MinionAIState.Idle:
                    {
                        if (m_FoundTarget == 0)
                        {
                            this.Velocity = new Vector2(0.0f, 0.0f);

                            if (dist < k_MoveToPlayerDist)
                            {
                                m_FoundTarget = 1;
                                m_Direction = Vector2.Normalize(player_centre - my_centre);
                            }
                        }
                        else
                        {
                            this.Velocity = m_Direction * 3.0f;
                            m_AttackWaitCount += DT;
                        }

                        if (m_AttackWaitCount > ATTK_COUNT)
                        {
                            m_AttackWaitCount = 0.0f;
                            m_AIState = MinionAIState.Charge;
                            m_FoundTarget = 0;
                        }
                        break;
                    }
                case MinionAIState.Charge:
                    {
                        if (dist < k_MinChargeDistance)
                        {
                            if ((player_centre.Y > this.Position.Y) && (player_centre.Y < this.Position.Y))
                            {
                                this.Velocity.X = 10.0f * (float)m_Facing;
                                m_AIState = MinionAIState.Dash;
                                break;
                            }
                        }

                        // Give up and try and get nearer
                        m_AttackWaitCount += DT;
                        if (m_AttackWaitCount > ATTK_COUNT)
                        {
                            m_AttackWaitCount = 0.0f;
                            m_AIState = MinionAIState.Idle;
                        }

                        break;
                    }
                case MinionAIState.Dash:
                    {
                        float m_friction = 0.2f;

                        // Apply friction
                        if (this.Velocity.X < 0)
                            this.Velocity.X += m_friction;
                        if (this.Velocity.X > 0)
                            this.Velocity.X -= m_friction;

                        // Set to Zero when speed is low enough
                        if (this.Velocity.X > 0 && this.Velocity.X < m_friction)
                        {
                            this.Velocity.X = 0;
                            m_AIState = MinionAIState.Idle;
                        }
                        if (this.Velocity.X < 0 && this.Velocity.X > -m_friction)
                        {
                            this.Velocity.X = 0;
                            m_AIState = MinionAIState.Idle;
                            m_PickedTarget = 0;
                        }
                        break;
                    }
            }

            this.Position += this.Velocity;
        }
    }
}
