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

        public bool Grounded
        {
            get; set;
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
            Grounded = true;
		}

		public GameObject(GameObjectType obj_id, int unq_id, int isClient, bool updatable)
		{
            m_Bounds = new Rectangle(0, 0, 64, 64);
            Position = Vector2.Zero;
            m_TypeId = obj_id;
			m_UniqueId = unq_id;
            IsClient = isClient;
            m_IsUpdatable = updatable;
            Grounded = true;
		}

		public GameObject(Vector2 p, GameObjectType obj_id, int unq_id, int isClient, bool updatable)
		{
            m_Bounds = new Rectangle(0, 0, 64, 64);
            Position = p;
            m_TypeId = obj_id;
            m_UniqueId = unq_id;
            IsClient = isClient;
            m_IsUpdatable = updatable;
            Grounded = true;
		}

        public virtual void Start()
        {
        }

        public virtual void Update()
        {
        }
	}
}
