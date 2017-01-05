using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;

namespace Server.GameSpecific.GameObjects
{
	public class PlayerProjectile : GameObject
	{
		const float TICK = 1 / 50.0f;
		private float m_LifeTick = 0.0f;

		public PlayerProjectile(Vector2 p, GameObjectType obj_id, int unq_id, int isClient, bool updatable) :
            base(p, obj_id, unq_id, isClient, updatable)
        {

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
			}
		}
	}
}
