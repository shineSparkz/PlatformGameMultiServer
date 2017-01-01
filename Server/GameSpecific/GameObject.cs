using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

using Microsoft.Xna.Framework;

namespace Server.GameSpecific
{
	public enum GameObjectType
	{
		Empty,
		Wall,
		Player,
		Exit,
		Slime,
		Skull_Collect,
		Walking_Enemy,
		Health,
		Platform,
		Spike = 9,
		SpikeD = 10,
		SpikeL = 11,
		SpikeR = 12,
		EnemyBlueMinion = 13,
		EnemyMage,
		EnemyDisciple,
		EnemyTaurus,
		EnemyShadow,
		EnemyGreenHead,
		DissapearingPlatform,
		DestructablePlatform,
		DropHazard,
		BouncingBoulder,
		BossTrigger,
		CheckPoint,
		GoldSkull,

		NetworkType,

		//=================
		EnemyProjectile,
		PlayerProjectile,
		NUM_TYPES
	}

	public class GameObject
	{
        public enum Facing { Left = -1, Right = 1 };

		protected Rectangle m_Bounds;
        protected Vector2 m_Position;
        protected GameObjectType m_TypeId;
        protected Facing m_Facing = Facing.Right;
        protected int m_UniqueId;
        protected bool m_IsUpdatable;
        protected float frameX = 0.0f;
        protected float frameY = 0.0f;

        // Player only
		public Point[] points;

        public Vector2 Position
		{
			get
			{
				return m_Position;
			}
			set
			{
				m_Position = value;
				m_Bounds.X = (int)m_Position.X;
				m_Bounds.Y = (int)m_Position.Y;
			}	
		}

        public Vector2 Velocity;

        public GameObjectType TypeId()
        {
            return m_TypeId;
        }

        public int UnqId()
        {
            return m_UniqueId;
        }

        public int IsClient
        {
            get;set;
        }

        public float FrameX()
        {
            return frameX;
        }

        public float FrameY()
        {
            return frameY;
        }

        public bool IsUpdatable()
        {
            return m_IsUpdatable;
        }

        public Rectangle Bounds()
        {
            return m_Bounds;
        }
		
        //----------------------------------------
		public GameObject()
		{
			m_Bounds = new Rectangle(0, 0, 64, 64);
			m_Position = Vector2.Zero;
			m_TypeId = GameObjectType.Empty;
			m_UniqueId = -1;
            IsClient = 0;
            m_IsUpdatable = false;
		}

		public GameObject(GameObjectType obj_id, int unq_id, int isClient, bool updatable)
		{
            m_Bounds = new Rectangle(0, 0, 64, 64);
            Position = Vector2.Zero;
            m_TypeId = obj_id;
			m_UniqueId = unq_id;
            IsClient = isClient;
            m_IsUpdatable = updatable;

			CreateContactPoints();
		}

		public GameObject(Vector2 p, GameObjectType obj_id, int unq_id, int isClient, bool updatable)
		{
            m_Bounds = new Rectangle(0, 0, 64, 64);
            Position = p;
            m_TypeId = obj_id;
            m_UniqueId = unq_id;
            IsClient = isClient;
            m_IsUpdatable = updatable;

            CreateContactPoints();
		}

		private void CreateContactPoints()
		{
			if (this.m_TypeId == GameObjectType.Player)
			{
				points = new Point[8];

				points[0] = new Point(48, 38);
				points[1] = new Point(80, 38);

				points[2] = new Point(48, 128);
				points[3] = new Point(80, 128);

				points[4] = new Point(40, 58);
				points[5] = new Point(40, 108);

				points[6] = new Point(88, 58);
				points[7] = new Point(88, 108);
			}
		}

        public virtual void Update()
        {
        }
	}

    public class BlueMinionEnemy : GameObject
    {
        const float DT = 1.0f / 60.0f;
        const float ATTK_COUNT = 1.0f;
        const float k_MoveToPlayerDist = 560.0f;
        const float k_MinChargeDistance = 512;
        const float k_MillisPerFrame = 0.08f;
        const float m_NumFramesX = 3.0f;

        enum MinionAIState { Idle, Charge, Dash }
        private List<GameObject> m_Players = null;

        private Vector2 m_Direction = Vector2.Zero;
        private MinionAIState m_AIState = MinionAIState.Idle;
        private float m_AttackWaitCount = 0.0f;
        private float m_AnimTick = 0.0f;
        private int m_FoundTarget = 0;


        public BlueMinionEnemy(Vector2 p, GameObjectType obj_id, int unq_id, int isClient, bool updatable) :
            base(p, obj_id, unq_id, isClient, updatable)
        {
            
        }

        public override void Update()
        {
            //if (m_EnemyParent->IsDead())
            //    return;

            m_AnimTick += DT;

            // TODO : Get distance of closest player
            Vector2 player_centre = new Vector2(200, 300);// m_Players[0].Position;
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
                        }
                        break;
                    }
            }

            this.Position += this.Velocity;
        }
    }
}
