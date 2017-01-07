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
        // I have marked the used ones, made a few cuts for deadline
		Empty,
		Wall,                       //<<
		Player,                     //<<
		Exit,                       //<<
		Slime,
		Skull_Collect,
		Walking_Enemy,
		Health,
		Platform,
		Spike = 9,                  //<<
		SpikeD = 10,
		SpikeL = 11,
		SpikeR = 12,
		EnemyBlueMinion = 13,       //<<
		EnemyMage,
		EnemyDisciple,              //<<
		EnemyTaurus,
		EnemyShadow,                //<<
		EnemyGreenHead,
		DissapearingPlatform,
		DestructablePlatform,       //<<
		DropHazard,
		BouncingBoulder,
		BossTrigger,
		CheckPoint,
		GoldSkull,                  //<<

		NetworkType,

		//=================
		EnemyProjectile,
		PlayerProjectile,           //<<
		NUM_TYPES
	}

    public class ColliderOffset
    {
        public int left, right, top, bottom;

        public ColliderOffset()
        {
            this.left = 0;
            this.right = 0;
            this.top = 0;
            this.bottom = 0;
        }

        public ColliderOffset(int l, int r, int t, int b)
        {
            this.left = l;
            this.right = r;
            this.top = t;
            this.bottom = b;
        }

        public ColliderOffset(int lrtb)
        {
            this.left = lrtb;
            this.right = lrtb;
            this.top = lrtb;
            this.bottom = lrtb;
        }
    };

	public class GameObject
	{
        public enum Facing { Left = -1, Right = 1 };

        public Facing m_Facing = Facing.Right;

		protected Rectangle m_Bounds;
        protected Vector2 m_Position;
        protected GameObjectType m_TypeId;
        protected int m_UniqueId;
        protected bool m_IsUpdatable;
		protected bool m_Active = true;
        protected float frameX = 0.0f;
        protected float frameY = 0.0f;
        protected ColliderOffset m_ColliderOffset = new ColliderOffset();

        public bool Grounded
        {
            get; set;
        }

		public bool SentInactivePacket = false;

		public bool Active
		{
			get
			{
				return m_Active;
			}
			set
			{
				m_Active = value;

				if (m_Active == true)
				{
					SentInactivePacket = false;
				}
			}
		}

        public Vector2 Position
		{
			get
			{
				return m_Position;
			}
			set
			{
				m_Position = value;

                m_Bounds.X = (int)m_Position.X + m_ColliderOffset.left;
                m_Bounds.Y = (int)m_Position.Y + m_ColliderOffset.top;
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

        // This is unfortunate, but too late in dev, it's for knowing who has created an object, bullet for example
        public int InvokedBy
        {
            get; set;
        }

        //----------------------------------------
		public GameObject(Vector2 p, GameObjectType obj_id, int unq_id, int isClient, bool updatable, Vector2 frameSize, ColliderOffset coloffset)
		{
            m_ColliderOffset = coloffset;
            m_Bounds = new Rectangle(
                (int)p.X + m_ColliderOffset.left,
                (int)p.Y + m_ColliderOffset.top,
                (int)frameSize.X - (m_ColliderOffset.right + m_ColliderOffset.left),
                (int)frameSize.Y - (m_ColliderOffset.top + m_ColliderOffset.bottom));

            Position = p;
            m_TypeId = obj_id;
            m_UniqueId = unq_id;
            IsClient = isClient;
            m_IsUpdatable = updatable;
            Grounded = true;
			Active = true;
		}

		public virtual void Start()
        {
        }

        public virtual void Update()
        {
        }
	}
}
