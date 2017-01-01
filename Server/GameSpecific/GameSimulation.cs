using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Diagnostics;

using Microsoft.Xna.Framework;

using Server.Server;


namespace Server.GameSpecific
{
	public class GameSimulation
	{
        const float FPS = 60.0f;
        const double DELTA_TICK = 0.1;
        const float MAX_FRAME_SKIP = 10;

        const int iterations = 3;
		const int UP = 0;
		const int DOWN = 1;
		const int LEFT = 2;
		const int RIGHT = 3;

        const int RELEASE = 0;
        const int PRESS = 1;

		private List<GameObject> m_GameObjects = new List<GameObject>();
        private List<GameObject> m_UpdateObjects = new List<GameObject>();
		private ServerManager m_ServerManager = null;
		private bool m_GameLoaded = false;
        private bool m_ShouldQuit = false;

		public GameSimulation(ServerManager sMan)
		{
			m_ServerManager = sMan;
		}

		public bool IsGameDataLoaded()
		{
			return m_GameLoaded;
		}

		public void ClearGameData()
		{
			m_GameObjects.Clear();
			m_GameLoaded = false;
		}

		public void LoadLevel(int levelNumber)
		{
            //**************************************************88
			// TODO : Load level in from client request
			m_GameLoaded = true;

			for (int i = 0; i < 12; ++i)
			{
				this.AddGameObject(new GameObject(new Vector2(i * 64, 450), GameObjectType.Wall, m_GameObjects.Count, 0, false));
			}

            this.AddGameObject(new GameObject(new Vector2(7 * 64, 350), GameObjectType.Wall, m_GameObjects.Count, 0, false));

            // Add enemy
            GameObject enemy = new BlueMinionEnemy(new Vector2(200, 200), GameObjectType.EnemyBlueMinion, m_GameObjects.Count, 0, true);
            this.AddGameObject(enemy);
		}

		public void AddGameObject(GameObject go)
		{
			m_GameObjects.Add(go);

            if (IsUpdateable(go.TypeId()))
            {
                m_UpdateObjects.Add(go);
            }
		}

        private bool IsUpdateable(GameObjectType t)
        {
            if (t != GameObjectType.Wall || t != GameObjectType.Spike || t != GameObjectType.Platform || t != GameObjectType.Empty)
            {
                return true;
            }

            return false;
        }

		public int NumObjects()
		{
			return m_GameObjects.Count;
		}

		public GameObject GetObject(int i)
		{
			return i >= 0 && i < m_GameObjects.Count ? m_GameObjects[i] : null;
		}

		public List<GameObject> GetObjects()
		{
			return m_GameObjects;
		}

        public List<GameObject> GetPlayers()
        {
            List<GameObject> ret = new List<GameObject>();

            foreach (GameObject go in m_GameObjects)
            {
                if (go.TypeId() == GameObjectType.Player)
                {
                    ret.Add(go);
                }
            }

            return ret;
        }

		public void InputUpdate(int handle, int key, int action)
		{
			GameObject player = m_GameObjects[handle];
            Vector2 velocity = player.Velocity;

            if (action == PRESS)
            {
                // Right 
                if (key == 3)
                {
                    velocity.X = 6.0f;
                }
                // Left
                else if (key == 0)
                {
                    velocity.X = -6.0f;
                }
                // Up
                else if (key == 22)
                {
                    // Would need to check if it was grounded
                    velocity.Y = -6.0f;
                }
                // Down
                //else if (key == 18)
            }
            else
            {
                // Right/Left
                if (key == 3 || key == 0)
                {
                    velocity.X = 0.0f;// = new Vector2(6.0f, 0.0f);
                }
            }

            // Just set velocity of player here and can process it in proper loop
            player.Velocity = velocity;
		}

        public void Run()
        {
            Stopwatch Timer = new Stopwatch();
            Timer.Start();

            double currentTime = Timer.Elapsed.TotalSeconds;
            double accumulator = 0.0;

            while (!m_ShouldQuit)
            {
                double newTime = Timer.Elapsed.TotalSeconds;
                double frameTime = newTime - currentTime;
                currentTime = newTime;

                accumulator += frameTime;

                while (accumulator >= DELTA_TICK)
                {
                    UpdateSimulation((float)DELTA_TICK);
                    accumulator -= DELTA_TICK;
                    //Console.WriteLine(string.Format("Time now elapsed seconds: {0} , ticks {1}", Timer.Elapsed.TotalSeconds, ++ticks));
                }
            }
        }

        private void UpdateSimulation(float dt)
        {
            // Loop through game objects
            foreach (GameObject gameObj in m_UpdateObjects)
            {
                // Is updateable but not player
                if (gameObj.TypeId() != GameObjectType.Player)
                {
                    // We will send a packet to each client from updateable things such as enemies
                    gameObj.Update();

                    // Send out the new position update here
                    PacketDefs.PlayerInputUpdatePacket updatePacket = new PacketDefs.PlayerInputUpdatePacket(
                        gameObj.UnqId(), gameObj.Position.X, gameObj.Position.Y, gameObj.FrameX(), gameObj.FrameY());

                    // Send this player to all other clients
                    m_ServerManager.SendAllUdp(fastJSON.JSON.ToJSON(
                        updatePacket, PacketDefs.JsonParams()));
                }
                // Is player so more complex collision
                else
                {
                    // ---- Resolve collision ----
                    bool contactLeft = false, contactRight = false, contactYbottom = false, contactYtop = false;

                    Vector2 predicted_speed = gameObj.Velocity;// * dt;
                    float projectedMoveX, projectedMoveY, originalMoveX, originalMoveY;
                    originalMoveX = predicted_speed.X;
                    originalMoveY = predicted_speed.Y;

                    //Vector2 position = gameObj.Position;

                    foreach (GameObject colTest in m_GameObjects)
                    {
                        if (colTest.TypeId() == GameObjectType.Wall)
                        {
                            for (int dir = 0; dir < 4; dir++)
                            {
                                if (dir == UP && predicted_speed.Y > 0) continue;
                                if (dir == DOWN && predicted_speed.Y < 0) continue;
                                if (dir == LEFT && predicted_speed.X > 0) continue;
                                if (dir == RIGHT && predicted_speed.X < 0) continue;

                                projectedMoveX = (dir >= LEFT ? predicted_speed.X : 0);
                                projectedMoveY = (dir < LEFT ? predicted_speed.Y : 0);

                                while ((colTest.Bounds().Contains(gameObj.points[dir * 2].X + (int)gameObj.Position.X + (int)projectedMoveX,
                                    gameObj.points[dir * 2].Y + (int)gameObj.Position.Y + (int)projectedMoveY)
                                    ||
                                    colTest.Bounds().Contains(gameObj.points[dir * 2 + 1].X + (int)gameObj.Position.X + (int)projectedMoveX,
                                        gameObj.points[dir * 2 + 1].Y + (int)gameObj.Position.Y + (int)projectedMoveY)))
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
                                //position.Y += predicted_speed.Y;
                                gameObj.Velocity.Y = 0;// = new Vector2(gameObj.Velocity.X, 0);
                            }

                            if (contactLeft || contactRight)
                            {
                                //position.X += predicted_speed.X;
                                gameObj.Velocity.X = 0;// = new Vector2(0, gameObj.Velocity.Y);
                            }
                        }
                    }

                    //position += gameObj.Velocity * dt;
                    //int x = (int)position.X;
                    //int y = (int)position.Y;
                    //gameObj.Position = new Vector2(x,y);
                    gameObj.Position += gameObj.Velocity;

                    // Apply the force of gravity
                    gameObj.Velocity.Y += 0.981f;

                    // Send out the new position update here
                    PacketDefs.PlayerInputUpdatePacket updatePacket = new PacketDefs.PlayerInputUpdatePacket(
                        gameObj.UnqId(), gameObj.Position.X, gameObj.Position.Y, gameObj.FrameX(), gameObj.FrameY());   

                    // Send this player to all other clients
                    m_ServerManager.SendAllUdp(fastJSON.JSON.ToJSON(
                        updatePacket, PacketDefs.JsonParams()));
                }
            }
        }

        public void Shutdown()
        {
            Console.WriteLine("Shutting down Game sim");
            m_ShouldQuit = true;
        }
	}
}
	

