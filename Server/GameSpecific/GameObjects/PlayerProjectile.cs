﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;

using Server.GameSpecific;
using Server.Server;

namespace Server.GameSpecific.GameObjects
{
	public class PlayerProjectile : GameObject
	{
		const float TICK = 1 / 50.0f;
		private float m_LifeTick = 0.0f;
		private List<GameObject> m_CollisionObjects = new List<GameObject>();

		public PlayerProjectile(Vector2 p, GameObjectType obj_id, int unq_id, int isClient, bool updatable) :
            base(p, obj_id, unq_id, isClient, updatable)
        {
			if (m_CollisionObjects.Count == 0)
			{
				foreach (GameObject go in GameSimulation.instance.GetObjects())
				{
					// TODO : Add other types that need collision checks
					if ( WantToCollideWith(go.TypeId()))
					{
						m_CollisionObjects.Add(go);
					}
				}
			}

			m_Bounds = new Rectangle(0, 0, 16, 16);
		}

        public bool WantToCollideWith(GameObjectType t)
        {
            if (t == GameObjectType.EnemyShadow || t == GameObjectType.EnemyTaurus || t == GameObjectType.EnemyBlueMinion || t == GameObjectType.DestructablePlatform
                || t == GameObjectType.EnemyDisciple)
            {
                return true;
            }

            return false;
        }

		public override void Update()
		{
			if (this.Active)
			{
				this.Position += Velocity;
				m_LifeTick += TICK;

				if (m_LifeTick > 2.0f)
				{
					m_LifeTick = 0.0f;
					this.Active = false;
					this.Velocity = Vector2.Zero;
				}

				// Check Collisions
				foreach (GameObject go in m_CollisionObjects)
				{
					if (go.Active)
					{
						if (this.Bounds().Intersects(go.Bounds()))
						{
							go.Active = false;
							this.Active = false;

                            // Add exp for killimg enemy
                            GameClient client = ServerManager.instance.GetClient(InvokedBy);
                            if (client != null)
                            {
                                client.localExpCache += 2;

                                // Send TCP pack with new exp update, note* only hit the database to increment exp when the level is finished
                                PacketDefs.UpdateExpPacket flp = new PacketDefs.UpdateExpPacket(client.localExpCache, PacketDefs.ID.OUT_TCP_ExpQueery);
                                ServerManager.instance.SendTcp(client.tcpSocket, fastJSON.JSON.ToJSON(flp, PacketDefs.JsonParams()));
                            }
                        }
					}
				}
			}
		}
	}
}