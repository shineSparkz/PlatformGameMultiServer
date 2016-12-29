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
		public GameObjectType object_id;
		public int unique_id;
		public Rectangle bounds;
		private Vector2 position;

		// Player only
		public Point[] points;

		public int isClientPlayer = 0;

		public Vector2 Position
		{
			get
			{
				return position;
			}
			set
			{
				position = value;
				bounds.X = (int)position.X;
				bounds.Y = (int)position.Y;
			}	
		}

		public GameObject()
		{
			bounds = new Rectangle(0, 0, 64, 64);
			position = Vector2.Zero;
			object_id = GameObjectType.Empty;
			unique_id = -1;
		}

		public GameObject(GameObjectType obj_id, int unq_id)
		{
			bounds = new Rectangle(0, 0, 64, 64);
			position = Vector2.Zero;
			object_id = obj_id;
			unique_id = unq_id;

			CreateContactPoints();
		}

		public GameObject(Vector2 p, GameObjectType obj_id, int unq_id)
		{
			bounds = new Rectangle((int)p.X, (int)p.Y, 64, 64);
			position = p;
			object_id = obj_id;
			unique_id = unq_id;

			CreateContactPoints();
		}

		private void CreateContactPoints()
		{
			if (this.object_id == GameObjectType.Player)
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

				//{ 48, 38 },{ 80, 38 },			// Top
				//{ 48, 128 },{ 80, 128 },			// Bot
				//{ 40, 58 },{ 40, 108 },			// Left
				//{ 88, 58 },{ 88, 108 }			// Right
			}
		}
	}
}
