using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Xna.Framework;

using Server.Server;


namespace Server.GameSpecific
{
	public class GameSimulation
	{
		const int iterations = 3;
		const int UP = 0;
		const int DOWN = 1;
		const int LEFT = 2;
		const int RIGHT = 3;

		private static List<GameObject> m_GameObjects = new List<GameObject>();

		public static void ClearGameData()
		{
			m_GameObjects.Clear();
		}

		public static void LoadLevel(int levelNumber)
		{
			// TODO : Load level in from client request

			for (int i = 1; i < 12; ++i)
			{
				m_GameObjects.Add(new GameObject(new Vector2(i * 64, 450), GameObjectType.Wall, m_GameObjects.Count));
			}

			m_GameObjects.Add(new GameObject(new Vector2(7 * 64, 350), GameObjectType.Wall, m_GameObjects.Count));
		}

		public static void AddGameObject(GameObject go)
		{
			m_GameObjects.Add(go);
		}

		public static int NumObjects()
		{
			return m_GameObjects.Count;
		}

		public static GameObject GetObject(int i)
		{
			return i >= 0 && i < m_GameObjects.Count ? m_GameObjects[i] : null;
		}

		public static List<GameObject> GetObjects()
		{
			return m_GameObjects;
		}

		public static void InputUpdate(int handle, int key)
		{

			// TODO : Check this handle for null and log if so
			GameObject player = m_GameObjects[handle];

			Vector2 velocity = Vector2.Zero;
			Vector2 m_Position = player.Position;

			// Right 
			if (key == 3)
			{
				velocity = new Vector2(6.0f, 0.0f);
			}
			// Left
			else if (key == 0)
			{
				velocity = new Vector2(-6.0f, 0.0f);
			}
			// Up
			else if (key == 22)
			{
				velocity = new Vector2(0.0f, -6.0f);
			}
			// Down
			else if (key == 18)
			{
				velocity = new Vector2(0.0f, 6.0f);
			}

			// ---- Resolve collision ----
			bool contactLeft = false, contactRight = false, contactYbottom = false, contactYtop = false;
			Vector2 predicted_speed = velocity * (1 / 60.0f);   // TODO : Get player to pass delta time
			float projectedMoveX, projectedMoveY, originalMoveX, originalMoveY;
			originalMoveX = predicted_speed.X;
			originalMoveY = predicted_speed.Y;

			foreach (GameObject go in m_GameObjects)
			{
				if (go.object_id == GameObjectType.Wall)
				{
					for (int dir = 0; dir < 4; dir++)
					{
						if (dir == UP && predicted_speed.Y > 0) continue;
						if (dir == DOWN && predicted_speed.Y < 0) continue;
						if (dir == LEFT && predicted_speed.X > 0) continue;
						if (dir == RIGHT && predicted_speed.X < 0) continue;

						projectedMoveX = (dir >= LEFT ? predicted_speed.X : 0);
						projectedMoveY = (dir < LEFT ? predicted_speed.Y : 0);

						while ((go.bounds.Contains(player.points[dir * 2].X + (int)m_Position.X + (int)projectedMoveX,
							player.points[dir * 2].Y + (int)m_Position.Y + (int)projectedMoveY)
							||
							go.bounds.Contains(player.points[dir * 2 + 1].X + (int)m_Position.X + (int)projectedMoveX,
								player.points[dir * 2 + 1].Y + (int)m_Position.Y + (int)projectedMoveY)))
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

					// Resolve collision form contact
					if (contactYbottom || contactYtop)
					{
						//m_Position.Y += predicted_speed.Y;
						velocity.Y = 0; // Would need to send this back to player
					}

					if (contactLeft || contactRight)
					{
						//m_Position.X += predicted_speed.X;
						velocity.X = 0;
					}
				}
			}

			//m_Position += velocity;
			//GameObjects[playerHandle].Position = new Vector2(((float)(int)m_Position.X), ((float)(int)m_Position.Y));
			m_GameObjects[handle].Position += velocity;

			// TODO : 
			// Send updated object
			ServerManager.SendAllUdp(string.Format("objupd:{0}:{1}:{2}:",
				handle,
				(int)m_GameObjects[handle].Position.X,
				(int)m_GameObjects[handle].Position.Y
			));
		}
	}
}
	

