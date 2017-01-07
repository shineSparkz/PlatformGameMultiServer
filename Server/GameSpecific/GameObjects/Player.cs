using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;

using Server.Server;

namespace Server.GameSpecific.GameObjects
{
    class Player : GameObject
    {
		#region Constants
		const int iterations = 3;
        const int UP = 0;
        const int DOWN = 1;
        const int LEFT = 2;
        const int RIGHT = 3;
		const float DT = 1.0f / 50.0f;
		const float MAX_FALL_SPEED = 100.0f;
		const float INVINCIBLE_TIME_RESET = 2.0f;
		#endregion

		private Point[] points;
        private GameView m_GameView = null;
		private UInt64 m_StepCounter = 0;
		private float m_MillisPerFrame = 0.07f;
		private float m_AnimTick = 0.0f;
		private float m_HurtCounter = 0.0f;
		private int m_ClientId;
		private int m_NumFramesX;
		private int m_Health = 100;
		private bool m_Dying = false;
		private bool m_Hurt = false;


        public Player(Vector2 p, GameObjectType obj_id, int unq_id, int isClient, bool updatable, int clientId, Vector2 frameSize, ColliderOffset coloffset) :
            base(p, obj_id, unq_id, isClient, updatable, frameSize, coloffset)
        {
            this.CreateContactPoints();

            m_GameView = new GameView(p, new Vector2(800, 600));
            this.m_ClientId = clientId;
        }

        public override void Update()
        {
			m_AnimTick -= DT;
			++m_StepCounter;

			UpdateAnimation();

			if (!m_Dying)
			{
				CheckCollisions();

				// Update position
				this.Position += this.Velocity;

				// Apply the force of gravity
				this.Velocity.Y += 0.981f;

				// Cap fall speed
				if (Velocity.Y > MAX_FALL_SPEED)
				{
					Velocity.Y = MAX_FALL_SPEED;
				}

				// Check for walking of a platform
				if (Grounded)
				{
					if (Velocity.Y > 100.0f)
					{
						Grounded = false;
					}
				}

				m_GameView.UpdateView(this.Position, 0.5f, m_ClientId);

				// Check for hurt
				if (m_Hurt)
				{
					m_HurtCounter += DT;

					//if ((m_StepCounter % 3 == 0))
						//this->m_Renderer->SetColour(sf::Color::White);
					//else
						//this->m_Renderer->SetColour(sf::Color::Red);

					if (m_HurtCounter >= INVINCIBLE_TIME_RESET)
					{
						m_HurtCounter = 0.0f;
						m_Hurt = false;

						//this->m_Renderer->SetColour(sf::Color::White);
					}
				}

				// Check for death
				if (m_Health <= 0 && !m_Dying)
				{
					//this->m_Renderer->SetColour(sf::Color::White);
					m_Dying = true;
					Position = Vector2.Zero;
					Velocity = Vector2.Zero;
					frameX = 0;
				}

				// Check for death
				if (!GameSimulation.instance.LevelBounds().Contains(m_Bounds))
				{
					// TODO : Send death to player msg, reset to a known spawn position
					this.Position = Vector2.Zero;
					Velocity = Vector2.Zero;
				}
			}
        }

        private void CheckCollisions()
        {
            // ---- Resolve collision ----
            bool contactLeft = false, contactRight = false, contactYbottom = false, contactYtop = false;

            Vector2 predicted_speed = this.Velocity;;
            float projectedMoveX, projectedMoveY, originalMoveX, originalMoveY;
            originalMoveX = predicted_speed.X;
            originalMoveY = predicted_speed.Y;

            //Vector2 position = gameObj.Position;

			// This needs to be from the qyadtree
            foreach (GameObject colTest in GameSimulation.instance.GetObjects())
            {
				if ( (colTest.TypeId() == GameObjectType.Wall || colTest.TypeId() == GameObjectType.DestructablePlatform) && colTest.Active)
				{
					for (int dir = 0; dir < 4; dir++)
					{
						if (dir == UP && predicted_speed.Y > 0) continue;
						if (dir == DOWN && predicted_speed.Y < 0) continue;
						if (dir == LEFT && predicted_speed.X > 0) continue;
						if (dir == RIGHT && predicted_speed.X < 0) continue;

						projectedMoveX = (dir >= LEFT ? predicted_speed.X : 0);
						projectedMoveY = (dir < LEFT ? predicted_speed.Y : 0);

						while ((colTest.Bounds().Contains(this.points[dir * 2].X + (int)this.Position.X + (int)projectedMoveX,
							this.points[dir * 2].Y + (int)this.Position.Y + (int)projectedMoveY)
							||
							colTest.Bounds().Contains(this.points[dir * 2 + 1].X + (int)this.Position.X + (int)projectedMoveX,
								this.points[dir * 2 + 1].Y + (int)this.Position.Y + (int)projectedMoveY)))
						{
							if (dir == UP)
								projectedMoveY++;
							if (dir == DOWN)
								projectedMoveY--;
							if (dir == LEFT)
								projectedMoveX++;
							if (dir == RIGHT)
								projectedMoveX--;
						}

						if (dir >= LEFT && dir <= RIGHT)
							predicted_speed.X = projectedMoveX;
						if (dir >= UP && dir <= DOWN)
							predicted_speed.Y = projectedMoveY;
					}

					// Resolve contact
					if (predicted_speed.Y > originalMoveY && originalMoveY < 0)
					{
						contactYtop = true;
					}

					if (predicted_speed.Y < originalMoveY && originalMoveY > 0)
					{
						contactYbottom = true;
					}

					if (predicted_speed.X - originalMoveX < -0.01f)
					{
						contactRight = true;
					}

					if (predicted_speed.X - originalMoveX > 0.01f)
					{
						contactLeft = true;
					}

					// Resolve collision from contact
					if (contactYbottom)
					{
						this.Velocity.Y = 0;
						this.Grounded = true;
					}
					else if (contactYtop)
					{
						this.Velocity.Y = 0;
					}

					if (contactLeft || contactRight)
					{
						this.Velocity.X = 0;
					}
				}
				else if (colTest.TypeId() == GameObjectType.Exit)
				{
					if (this.Bounds().Intersects(colTest.Bounds()))
					{
						// Stop updating this object
						Active = false;

						// Send Back to previous position
						this.Position = Vector2.Zero;
						this.Velocity = Vector2.Zero;

						GameClient client = ServerManager.instance.GetClient(m_ClientId);
						if (client != null)
						{
							client.inGame = false;

                            // NOTE** This is the only time we want to update the database (when they finish the level)
                            client.localExpCache += 30;

							// Give this client some experience on database
							ServerManager.instance.UpdateClientExpDB(client.userName, client.localExpCache);

							// Send him back to the lobby : TODO Get EXP from DB
							PacketDefs.UpdateExpPacket flp = new PacketDefs.UpdateExpPacket(client.localExpCache, PacketDefs.ID.OUT_TCP_FinishLevel);
							ServerManager.instance.SendTcp(client.tcpSocket, fastJSON.JSON.ToJSON(flp, PacketDefs.JsonParams()));

							if (!GameSimulation.instance.ArePeopleInGame())
							{
								// No one is in-game so clear data
								GameSimulation.instance.ScheduleClearGameData();
								return;
							}
						}
					}
				}
				else if (colTest.TypeId() == GameObjectType.GoldSkull)
				{
					if (this.Bounds().Intersects(colTest.Bounds()) && colTest.Active)
					{
						colTest.Active = false;

						// Send out a packet here informing them it's deactivated (rather than using standward way and doing it every frame)
						PacketDefs.PlayerInputUpdatePacket updatePacket = new PacketDefs.PlayerInputUpdatePacket(
							colTest.UnqId(), colTest.Position.X, colTest.Position.Y, colTest.FrameX(), colTest.FrameY(), colTest.Active);
						ServerManager.instance.SendAllUdp(fastJSON.JSON.ToJSON(
							updatePacket, PacketDefs.JsonParams()));

						// Add exp for collecting skull
						GameClient client = ServerManager.instance.GetClient(m_ClientId);
						if (client != null)
						{
                            client.localExpCache += 10;
							
							// Send TCP pack with new exp update, note* only hit the database to increment exp when the level is finished
							PacketDefs.UpdateExpPacket flp = new PacketDefs.UpdateExpPacket(client.localExpCache, PacketDefs.ID.OUT_TCP_ExpQueery);
							ServerManager.instance.SendTcp(client.tcpSocket, fastJSON.JSON.ToJSON(flp, PacketDefs.JsonParams()));
						}
					}
				}
            }
        }

		private void UpdateAnimation()
		{
			if (m_Dying)
			{
				m_MillisPerFrame = 0.09f;
				m_NumFramesX = 7;

				if (m_Facing == Facing.Left)
					frameY = 9;
				else
					frameY = 4;

				if (m_AnimTick <= 0.0f)
				{
					++frameX;

					if (frameX > m_NumFramesX)
					{
						// Dead
						frameX = 7;
						//EventSys::SendEvent(EventSys::EventID::PlayerDead, nullptr);
					}

					m_AnimTick = m_MillisPerFrame;
				}
			}
			else // Not Dying
			{
				int newXFrameOnCompleteRow = 0;

				if (Grounded)
				{
					newXFrameOnCompleteRow = 2;

					if (Math.Abs(Velocity.X) > 0.1f)
					{
						m_NumFramesX = 10;

						if (m_Facing == Facing.Left)
							frameY = 5;
						else
							frameY = 0;
					}
					else //Not Moving
					{
						m_NumFramesX = 0;

						if (m_Facing == Facing.Left)
							frameY = 5;
						else
							frameY = 0;
					}
				}
				else
				{
					m_NumFramesX = 7;
					newXFrameOnCompleteRow = 7;

					if (m_Facing == Facing.Left)
					{
						frameY = 6;
					}
					else
					{
						frameY = 1;
					}
				}

				// Tick
				if (m_AnimTick <= 0.0f)
				{
					++frameX;

					if (frameX >= m_NumFramesX)
						frameX = newXFrameOnCompleteRow;

					m_AnimTick = m_MillisPerFrame;
				}
			}
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
    }
}
