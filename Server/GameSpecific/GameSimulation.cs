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

        public static Random Rand = new Random(Environment.TickCount);

        public static int RandomRange(int min, int max)
        {
            return Rand.Next(min, max);
        }

        public GameSimulation()
		{
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
                // Jump
                else if (key == 22)
                {
                    // Would need to check if it was grounded
                    if (player.Grounded)
                    {
                        velocity.Y = -12.0f;
                        player.Grounded = false;
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
            foreach (GameObject gameObj in m_UpdateObjects)
            {
                // We will send a packet to each client from updateable things such as enemies
                gameObj.Update();

                // TODO : Maybe have a flag here to see if should bother sending a packet out

                // Send out the new position update here
                PacketDefs.PlayerInputUpdatePacket updatePacket = new PacketDefs.PlayerInputUpdatePacket(
                    gameObj.UnqId(), gameObj.Position.X, gameObj.Position.Y, gameObj.FrameX(), gameObj.FrameY());

                // Send this player to all other clients
                ServerManager.instance.SendAllUdp(fastJSON.JSON.ToJSON(
                    updatePacket, PacketDefs.JsonParams()));
            }
        }

        public void Shutdown()
        {
            Console.WriteLine("Shutting down Game sim");
            m_ShouldQuit = true;
        }
	}
}
	

