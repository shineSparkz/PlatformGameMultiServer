using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Diagnostics;
using System.IO;

using Microsoft.Xna.Framework;

using Server.Server;
using Server.GameSpecific.GameObjects;


namespace Server.GameSpecific
{
	public class GameSimulation
	{
		#region Singleton
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
		#endregion

		#region Constants
		const double DELTA_TICK = 1 / 50.0;
        const int RELEASE = 0;
        const int PRESS = 1;
		const int BULLET_POOL_SIZE = 10;

		// Keys
		const int LEFT_K = 0;
		const int RIGHT_K = 3;
		const int JUMP_K = 22;
		const int SHOOT_K = 57;
		#endregion

		#region Static Helpers
		public static Random Rand = new Random(Environment.TickCount);

        public static int RandomRange(int min, int max)
        {
            return Rand.Next(min, max);
        }
		#endregion

		private List<GameObject> m_GameObjects = new List<GameObject>();
        private List<GameObject> m_UpdateObjects = new List<GameObject>();
        private Rectangle m_LevelBounds;
		private bool m_GameLoaded = false;
        private bool m_ShouldQuit = false;
		private bool m_ShouldClearData = false;
        private float m_MapWidth = 1920;
        private float m_MapHeight = 1080;
		private int m_PlayersInGameSession = 0;
		private int m_BulletPoolStart = 0;
        private double m_ElapsedTime = 0;


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

        public double GetTotalTime()
        {
            return m_ElapsedTime;
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
            int count = 0;
            int y = 0;
            string line;

            int CellSize = 0;
            int MapWidth = 0;
            int MapHeight = 0;
            int MapRows = 0;
            int MapCols = 0;

            // Read the file and display it line by line.
            System.IO.StreamReader file = new System.IO.StreamReader(string.Format("obj_map_{0}.txt", levelNumber));
            while ((line = file.ReadLine()) != null)
            {
                if (count == 0)
                {
                    CellSize = Convert.ToInt32(line);
                }
                else if (count == 1)
                {
                    MapWidth = Convert.ToInt32(line);
                }
                else if (count == 2)
                {
                    MapHeight = Convert.ToInt32(line);

                    MapRows = MapHeight / CellSize;
                    MapCols = MapWidth / CellSize;

                    // TODO Set level bounds here
                }
                else
                {
                    if (string.IsNullOrEmpty(line))
                        break;

                    String[] spl = line.Split(',');

                    for (int x = 0; x < spl.Length; ++x)
                    {
                        int parsedId = Convert.ToInt32(spl[x]);
                        GameObjectType type = (GameObjectType)parsedId;

                        switch (type)
                        {
                            case GameObjectType.DestructablePlatform:
                                {
                                    GameObject box = new GameObject(new Vector2(x * 64, y * 64), GameObjectType.DestructablePlatform, m_GameObjects.Count, 0, false, new Vector2(64, 64), new ColliderOffset(0));
                                    this.AddGameObject(box);
                                }
                                break;
                            case GameObjectType.EnemyBlueMinion:
                                {
                                    GameObject enemy = new BlueMinionEnemy(new Vector2(x * 64, y * 64), GameObjectType.EnemyBlueMinion, m_GameObjects.Count, 0, true, new Vector2(45, 66), new ColliderOffset(9));
                                    this.AddGameObject(enemy);
                                }
                                break;
                            case GameObjectType.EnemyDisciple:
                                {
                                    GameObject disciple = new DiscipleEnemy(new Vector2(x * 64, y * 64), GameObjectType.EnemyDisciple, m_GameObjects.Count, 0, true, new Vector2(45 * 2, 51 * 2), new ColliderOffset(25, 25, 20, 6));
                                    this.AddGameObject(disciple);
                                }
                                break;
                            case GameObjectType.EnemyShadow:
                                {
                                    GameObject shadow = new ShadowEnemy(new Vector2(x * 64, y * 64), GameObjectType.EnemyShadow, m_GameObjects.Count, 0, true, new Vector2(80, 70), new ColliderOffset(12, 12, 4, 0));
                                    this.AddGameObject(shadow);
                                }
                                break;
                            case GameObjectType.Exit:
                                {
                                    this.AddGameObject(new Exit(new Vector2(x * 64, y * 64), GameObjectType.Exit, m_GameObjects.Count, 0, true, new Vector2(128, 128), new ColliderOffset(36, 36, 36, 10)));
                                }
                                break;
                            case GameObjectType.GoldSkull:
                                {
                                    GameObject skull = new GameObject(new Vector2(x * 64, y * 64), GameObjectType.GoldSkull, m_GameObjects.Count, 0, false, new Vector2(32, 32), new ColliderOffset(2));
                                    this.AddGameObject(skull);
                                }
                                break;
                            case GameObjectType.Spike:
                                {
                                    GameObject spike = new GameObject(new Vector2(x * 64, y * 64), GameObjectType.Spike, m_GameObjects.Count, 0, false, new Vector2(64, 64), new ColliderOffset(12, 12, 18, 0));
                                    this.AddGameObject(spike);
                                }
                                break;
                            case GameObjectType.Wall:
                                {
                                    this.AddGameObject(new GameObject(new Vector2(x * 64, y * 64), GameObjectType.Wall, m_GameObjects.Count, 0, false, new Vector2(64, 64), new ColliderOffset(0)));
                                }
                                break;
                            default:
                                break;
                        }
                    }

                    y++;
                }

                count++;
            }

            file.Close();

            //**************************************************
            // TODO : Load level in from client request
            m_GameLoaded = true;

            /*
            // Add Walls
			for (int i = 0; i < 12; ++i)
			{
				this.AddGameObject(new GameObject(new Vector2(i * 64, 450), GameObjectType.Wall, m_GameObjects.Count, 0, false, new Vector2(64,64), new ColliderOffset(0)));
			}

			this.AddGameObject(new GameObject(new Vector2(7 * 64, 350), GameObjectType.Wall, m_GameObjects.Count, 0, false, new Vector2(64, 64), new ColliderOffset(0)));

            // Add enemy
            GameObject enemy = new BlueMinionEnemy(new Vector2(200, 200), GameObjectType.EnemyBlueMinion, m_GameObjects.Count, 0, true, new Vector2(45, 66), new ColliderOffset(9));
            this.AddGameObject(enemy);

			// Add Exit
			this.AddGameObject(new Exit(new Vector2(14 * 64, 400), GameObjectType.Exit, m_GameObjects.Count, 0, true, new Vector2(128, 128), new ColliderOffset(36,36,36,10)));

            // Add Collectable skull (for exp)
            GameObject skull = new GameObject(new Vector2(10 * 64, 200), GameObjectType.GoldSkull, m_GameObjects.Count, 0, false, new Vector2(32, 32), new ColliderOffset(2));
            this.AddGameObject(skull);

            // Add Shadow
            GameObject shadow = new ShadowEnemy(new Vector2(300, 450 - 64), GameObjectType.EnemyShadow, m_GameObjects.Count, 0, true, new Vector2(80, 70), new ColliderOffset(12,12,4,0));
            this.AddGameObject(shadow);

            // Add Disciple
            GameObject disciple = new DiscipleEnemy(new Vector2(370, 450 - 64), GameObjectType.EnemyDisciple, m_GameObjects.Count, 0, true, new Vector2(45*2, 51*2), new ColliderOffset(25,25,20,6));
            this.AddGameObject(disciple);

            // Add Desctructable box
            GameObject box = new GameObject(new Vector2(590, 450-64), GameObjectType.DestructablePlatform, m_GameObjects.Count, 0, false, new Vector2(64, 64), new ColliderOffset(0));
            this.AddGameObject(box);

            // Add Spike hazard
            GameObject spike = new GameObject(new Vector2(520, 450-64), GameObjectType.Spike, m_GameObjects.Count, 0, false, new Vector2(64, 64), new ColliderOffset(12, 12, 18, 0));
            this.AddGameObject(spike);
            */

            // Allocate bulletPool last
            m_BulletPoolStart = m_GameObjects.Count;
			for (int i = 0; i < BULLET_POOL_SIZE; ++i)
			{
				GameObject bullet = new PlayerProjectile(Vector2.Zero, GameObjectType.PlayerProjectile, m_GameObjects.Count, 0, true, new Vector2(32, 32), new ColliderOffset(2));
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
			if (t == GameObjectType.Player || t == GameObjectType.EnemyBlueMinion || t == GameObjectType.PlayerProjectile
                || t == GameObjectType.EnemyShadow || t == GameObjectType.DestructablePlatform || t == GameObjectType.EnemyDisciple 
                || t == GameObjectType.EnemyProjectile || t == GameObjectType.EnemyTaurus || t == GameObjectType.Exit )
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

		public void InputUpdate(int handle, int key, int action, int clientId)
		{
			if (m_ShouldClearData || (handle >= m_GameObjects.Count) || handle < 0)
				return;

			GameObject player = m_GameObjects[handle];
            Vector2 velocity = player.Velocity;

            if (action == PRESS)
            {
				if (key == RIGHT_K)
				{
					player.m_Facing = GameObject.Facing.Right;
					velocity.X = 6.0f;
				}
				else if (key == LEFT_K)
				{
					player.m_Facing = GameObject.Facing.Left;
					velocity.X = -6.0f;
				}
				else if (key == JUMP_K)
				{
					// Would need to check if it was grounded
					if (player.Grounded)
					{
						velocity.Y = -16.0f;
						player.Grounded = false;
					}
				}
				else if (key == SHOOT_K)
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
                            bullet.InvokedBy = clientId;
							break;
						}
					}
				}
			}
            else
            {
                if (key == RIGHT_K || key == LEFT_K)
                {
                    velocity.X = 0.0f;
                }
            }

            // Just set velocity of player here and can process it in proper loop
            player.Velocity = velocity;
		}

        public void Run()
        {
            Stopwatch Timer = new Stopwatch();
            Timer.Start();

            m_ElapsedTime = Timer.Elapsed.TotalSeconds;
            double accumulator = 0.0;

            while (!m_ShouldQuit)
            {
                double newTime = Timer.Elapsed.TotalSeconds;
                double frameTime = newTime - m_ElapsedTime;
                m_ElapsedTime = newTime;

                accumulator += frameTime;

                while (accumulator >= DELTA_TICK)
                {
                    UpdateSimulation((float)DELTA_TICK);
                    accumulator -= DELTA_TICK;
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
			// Close down the Run Thread
			m_ShouldQuit = true;
        }
	}
}
	

