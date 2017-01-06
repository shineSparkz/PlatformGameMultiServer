using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;

namespace Server.GameSpecific.GameObjects
{
	class ShadowEnemy : GameObject
	{
		#region Constants
		const float DT = 1.0f / 60.0f;
		const float NUM_FRAMES_X = 4;
		const float NUM_FRAMES_Y = 10;
		const float ATTK_COUNT = 2.5f;
		#endregion

		enum ShadowAIState
		{
			Idle,
			Rising,
			Attacking,
			Sinking,
		}

		ShadowAIState m_AIState = ShadowAIState.Idle;
		float m_AttackWaitCount = 1.0f;
		float m_MillisPerFrame = 0.1f;
		float m_AnimTick = 0.08f;
		float m_FacingTime = 0.0f;
		int m_ColHeight = 4;
		bool m_CanSwitchDir = true;

		public ShadowEnemy(Vector2 p, GameObjectType obj_id, int unq_id, int isClient, bool updatable) :
			base(p, obj_id, unq_id, isClient, updatable)
		{
			m_Bounds = new Rectangle(0, 0, 32, 32);
		}

		public override void Update()
		{
			//if (m_EnemyParent->IsDead())
			//	return;

			m_AnimTick -= DT;
			m_AttackWaitCount -= DT;
			Vector2 player_centre = new Vector2(300, 300);// m_PlayerTransform->position + m_PlayerTransform->origin;
			bool break_idle = false;
			bool got_atatcked = false;
			float dist = Vector2.Distance(Position, player_centre);

			if (dist < 300)
			{
				break_idle = true;
			}

			m_FacingTime += DT;
			if (m_FacingTime > 0.7f)
			{
				m_CanSwitchDir = true;
				m_FacingTime = 0.0f;
			}

			if (m_AIState == ShadowAIState.Attacking || m_AIState == ShadowAIState.Idle)
			{
				if ((player_centre.X < Position.X) && m_CanSwitchDir == true)
				{
					m_Facing = Facing.Left;
					m_CanSwitchDir = false;
				}
				else if ((player_centre.X > Position.X) && m_CanSwitchDir == true)
				{
					m_Facing = Facing.Right;
					m_CanSwitchDir = false;
				}
			}

			Velocity = Vector2.Zero;

			// Logic
			switch (m_AIState)
			{
				case ShadowAIState.Rising:
					m_AttackWaitCount = 1.5f * ATTK_COUNT;
					break;
				case ShadowAIState.Idle:
				case ShadowAIState.Attacking:
					{
						// No object
						Velocity = new Vector2((float)m_Facing * 75.0f, 0.0f);

						// TODO *** Handle collision for walls
						break;
					}
				case ShadowAIState.Sinking:
					break;
			}

			Animate(break_idle, (got_atatcked && dist > 200));

			Position += Velocity;
		}

		private void Animate(bool break_idle, bool attacked)
		{
			// Yes, I know this is awful repetitive code, but YOLO
			int frame_size = 32;// frameSize
			int scale = 4;

			if (m_Facing == Facing.Left)
			{
				switch (m_AIState)
				{
					case ShadowAIState.Idle:
						{
							m_ColHeight = frame_size;

							frameY = 6;
							frameX = 0;

							if (break_idle && m_AttackWaitCount <= 0.0f)
							{
								m_AttackWaitCount = ATTK_COUNT;
								m_AIState = ShadowAIState.Rising;
							}
							break;
						}
					case ShadowAIState.Rising:
						{
							if (m_AnimTick <= 0.0f)
							{
								++frameX;

								m_ColHeight -= scale;

								if (frameX >= NUM_FRAMES_X)
								{
									frameX = 0;
									++frameY;

									if (frameY >= 7)
									{
										m_AIState = ShadowAIState.Attacking;
									}
								}

								m_AnimTick = m_MillisPerFrame;
							}

							break;
						}
					case ShadowAIState.Attacking:
						{
							frameY = 9;
							m_ColHeight = scale;

							if (m_AnimTick <= 0.0f)
							{
								++frameX;

								if (frameX >= NUM_FRAMES_X)
								{
									frameX = 0;
								}

								m_AnimTick = m_MillisPerFrame;
							}

							// Move Left and right
							if (m_AttackWaitCount <= 0.0f || !break_idle || attacked)   // far away
							{
								m_AttackWaitCount = ATTK_COUNT;
								frameY = 8;
								frameX = 1;
								m_AIState = ShadowAIState.Sinking;
							}
							break;
						}
					case ShadowAIState.Sinking:
						{
							if (m_AnimTick <= 0.0f)
							{
								--frameX;

								m_ColHeight += (scale + scale / 2);

								if (frameX < 0)
								{
									frameX = NUM_FRAMES_X - 1;

									--frameY;
									if (frameY < 5)
									{
										frameY = 5;
										frameX = 0;
										m_AIState = ShadowAIState.Idle;
									}
								}

								m_AnimTick = m_MillisPerFrame;
							}
							break;
						}
				}
			}
			else
			{
				switch (m_AIState)
				{
					case ShadowAIState.Idle:
						{
							m_ColHeight = frame_size;

							frameY = 0;
							frameX = 3;
							if (break_idle && m_AttackWaitCount <= 0.0f)
							{
								m_AttackWaitCount = ATTK_COUNT;
								m_AIState = ShadowAIState.Rising;
							}
							break;
						}
					case ShadowAIState.Rising:
						{
							if (m_AnimTick <= 0.0f)
							{
								--frameX;
								m_ColHeight -= scale;

								if (frameX < 0)
								{
									frameX = NUM_FRAMES_X - 1;
									++frameY;

									if (frameY > 2)
									{
										m_AIState = ShadowAIState.Attacking;
									}
								}

								m_AnimTick = m_MillisPerFrame;
							}

							break;
						}
					case ShadowAIState.Attacking:
						{
							frameY = 4;
							m_ColHeight = 4;

							if (m_AnimTick <= 0.0f)
							{
								--frameX;

								if (frameX < 0)
								{
									frameX = NUM_FRAMES_X - 1;
								}

								m_AnimTick = m_MillisPerFrame;
							}

							// Move Left and right
							if (m_AttackWaitCount <= 0.0f || !break_idle || attacked)
							{
								m_AttackWaitCount = ATTK_COUNT;
								frameY = 3;
								frameX = 2;
								m_AIState = ShadowAIState.Sinking;
							}
							break;
						}
					case ShadowAIState.Sinking:
						{
							if (m_AnimTick <= 0.0f)
							{
								++frameX;
								m_ColHeight += (scale + scale / 2);

								if (frameX >= NUM_FRAMES_X)
								{
									frameX = 0;

									--frameY;
									if (frameY < 0)
									{
										frameY = 0;
										frameX = 3;
										m_AIState = ShadowAIState.Idle;
									}
								}

								m_AnimTick = m_MillisPerFrame;
							}
							break;
						}
				}
			}
			//m_Collider->SetTopOffset(m_ColHeight);
		}
	}
}
