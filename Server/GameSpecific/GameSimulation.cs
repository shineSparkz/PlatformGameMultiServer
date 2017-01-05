using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Diagnostics;

using Microsoft.Xna.Framework;

using Server.Server;
using Server.GameSpecific.GameObjects;


namespace Server.GameSpecific
{
	public class GameSimulation
	{
        private static GameSimulation _instance;
        public static GameSimulation instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new GameSimulation();
                }

                return _instance;
            }

            private set {; }
        }

        const float FPS = 60.0f;
        const double DELTA_TICK = 1 / 50.0f;
        const float MAX_FRAME_SKIP = 10;

        const int RELEASE = 0;
        const int PRESS = 1;

		private List<GameObject> m_GameObjects = new List<GameObject>();
        private List<GameObject> m_UpdateObjects = new List<GameObject>();
		private bool m_GameLoaded = false;
        private bool m_ShouldQuit = false;
		private bool m_ShouldClearData = false;
        private float m_MapWidth = 1920;
        private float m_MapHeight = 1080;
		private int m_PlayersInGameSession = 0; // TODO : this would need to be per cluster
		private int m_BulletPoolStart = 0;
		const int BULLET_POOL_SIZE = 10;
        private Rectangle m_LevelBounds;

        public static Random Rand = new Random(Environment.TickCount);

        public static int RandomRange(int min, int max)
        {
            return Rand.Next(min, max);
        }

        public GameSimulation()
		{
            m_LevelBounds = new Rectangle(0, 0, (int)m_MapWidth, (int)m_MapHeight);
		}

        public Rectangle LevelBounds()
        {
            return m_LevelBounds;
        }

		public int GetPlayersInGame()
		{
			return m_PlayersInGameSession;
		}

        public float MapWidth()
        {
            return m_MapWidth;
        }

        public float MapHeight()
        {
            return m_MapHeight;
        }

		public bool IsGameDataLoaded()
		{
			return m_GameLoaded;
		}

		public void ScheduleClearGameData()
		{
			m_ShouldClearData = true;
		}

		public void ClearGameData()
		{
			// Level has ended
			m_UpdateObjects.Clear();
			m_GameObjects.Clear();
			m_GameLoaded = false;
			m_PlayersInGameSession = 0;

			ServerManager.instance.ResetClientHandles();
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

			// Add Exit
			this.AddGameObject(new GameObject(new Vector2(14 * 64, 400), GameObjectType.Exit, m_GameObjects.Count, 0, false));

			// etc

			// Allocate bulletPool last
			m_BulletPoolStart = m_GameObjects.Count;
			for (int i = 0; i < BULLET_POOL_SIZE; ++i)
			{
				GameObject bullet = new PlayerProjectile(Vector2.Zero, GameObjectType.PlayerProjectile, m_GameObjects.Count, 0, true);
				bullet.Active = false;
				this.AddGameObject(bullet);
			}
		}

		public void AddGameObject(GameObject go)
		{
			m_GameObjects.Add(go);

            if (IsUpdateable(go.TypeId()))
            {
                m_UpdateObjects.Add(go);
            }

			if (go.TypeId() == GameObjectType.Player)
			{
				AddNewPlayerToSession();
			}
		}

		public void AddNewPlayerToSession()
		{
			++m_PlayersInGameSession;
		}

		private bool IsUpdateable(GameObjectType t)
        {

			if (t == GameObjectType.Player || t == GameObjectType.EnemyBlueMinion || t == GameObjectType.PlayerProjectile)
			{
				return true;
			}

			return false;

            //if (t != GameObjectType.Wall && t != GameObjectType.Spike && t != GameObjectType.Platform && t != GameObjectType.Empty)
            //{
            //    return true;
            //}

            //return false;
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

		public bool ArePeopleInGame()
		{
			bool peopleStillPlaying = false;

			// Loop clients, check if all of them are not in game and clear the level data if not
			foreach (KeyValuePair<int, GameClient> kvp in ServerManager.instance.GetClients())
			{
				if (kvp.Value.inGame)
				{
					peopleStillPlaying = true;
					break;
				}
			}

			return peopleStillPlaying;
		}

		public void InputUpdate(int handle, int key, int action)
		{
			if (m_ShouldClearData || (handle >= m_GameObjects.Count) || handle < 0)
				return;

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
				// Jump
				else if (key == 22)
				{
					// Would need to check if it was grounded
					if (player.Grounded)
					{
						velocity.Y = -16.0f;
						player.Grounded = false;
					}
				}
				// Shoot
				else if (key == 57)
				{
					for(int i = m_BulletPoolStart; i < (m_BulletPoolStart + BULLET_POOL_SIZE); ++i)
					{
						if(!m_GameObjects[i].Active)
						{
							GameObject bullet = m_GameObjects[i];
							bullet.Active = true;
							bullet.m_Facing = player.m_Facing;
							bullet.Position = player.Position + new Vector2(64, 64);
							bullet.Velocity.X = 20 * (float)bullet.m_Facing;
							break;
						}
					}
				}
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
            for (int i = 0; i < m_UpdateObjects.Count; ++i)
            {
				GameObject gameObj = m_UpdateObjects[i];

				if (gameObj != null)
				{
					// We will send a packet to each client from updateable things such as enemies
					gameObj.Update();

					// Send this player to all other clients, ensure we send one update when deactivated
					if (!gameObj.SentInactivePacket)
					{
						// Send out the new position update here
						PacketDefs.PlayerInputUpdatePacket updatePacket = new PacketDefs.PlayerInputUpdatePacket(
							gameObj.UnqId(), gameObj.Position.X, gameObj.Position.Y, gameObj.FrameX(), gameObj.FrameY(), gameObj.Active);

						ServerManager.instance.SendAllUdp(fastJSON.JSON.ToJSON(
							updatePacket, PacketDefs.JsonParams()));
					}

					if (!gameObj.Active && !gameObj.SentInactivePacket)
					{
						// So only send one packet out when deactivated
						// This will be set back to false when Active prop is set to true
						gameObj.SentInactivePacket = true;
					}
				}
            }

			if (m_ShouldClearData)
			{
				m_ShouldClearData = false;
				ClearGameData();
			}
        }

        public void Shutdown()
        {
            Console.WriteLine("Shutting down Game sim");
            m_ShouldQuit = true;
        }
	}
}
	

